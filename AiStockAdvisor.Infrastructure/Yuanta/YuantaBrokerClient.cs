using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using AiStockAdvisor.Domain;
using AiStockAdvisor.Application.Interfaces;
using AiStockAdvisor.Infrastructure.Yuanta.DataStructs;
using YuantaOneAPI;

namespace AiStockAdvisor.Infrastructure.Yuanta
{
    /// <summary>
    /// 實作 <see cref="IBrokerClient"/> 介面，使用元大 API 提供服務。
    /// 負責管理與元大後端的連線、登入及資料訂閱。
    /// </summary>
    public class YuantaBrokerClient : IBrokerClient, IDisposable
    {
        private readonly IYuantaApiAdapter _trader;
        private readonly ILogger? _logger;
        private readonly bool _popUpMsg;
        private readonly bool _requiresQuoteConnection;
        private readonly ManualResetEventSlim _connectedEvent = new ManualResetEventSlim(false);
        private readonly ManualResetEventSlim _quoteConnectedEvent = new ManualResetEventSlim(false);
        private readonly ManualResetEventSlim _loginEvent = new ManualResetEventSlim(false);
        private volatile bool _isConnected;
        private volatile bool _isQuoteConnected;
        private volatile bool _isLoggedIn;
        private volatile string _lastLoginMsgCode = string.Empty;
        private volatile string _lastLoginMsgContent = string.Empty;
        private volatile uint _lastLoginCount;
        private readonly HashSet<string> _seenSubIndexes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly object _dedupeLock = new object();
        private readonly Dictionary<string, TickSignature> _lastTickBySymbol = new Dictionary<string, TickSignature>(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc />
        public event Action<Tick>? OnTickReceived;

        /// <summary>
        /// 初始化 <see cref="YuantaBrokerClient"/> 類別的新執行個體。
        /// 使用預設的生產環境元大 API Wrapper 實作。
        /// </summary>
        public YuantaBrokerClient(ILogger logger) : this(new YuantaApiWrapper(), logger, popUpMsg: true)
        {
        }

        /// <summary>
        /// 使用特定的 API 適配器初始化 <see cref="YuantaBrokerClient"/> 類別的新執行個體。
        /// 此建構子主要用於測試時注入 Mock 適配器。
        /// </summary>
        /// <param name="adapter">用於 API 互動的適配器。</param>
        public YuantaBrokerClient(IYuantaApiAdapter adapter, ILogger? logger = null, bool popUpMsg = false)
        {
            _trader = adapter;
            _logger = logger;
            _popUpMsg = popUpMsg;
            _requiresQuoteConnection = adapter is YuantaApiWrapper;
            
            // Optional: Set up logging or events here
            _trader.OnResponse += _trader_OnResponse;
            _trader.SetLogType(enumLogType.COMMON);
            _trader.SetPopUpMsg(_popUpMsg);
        }

        private const enumEnvironmentMode DefaultEnvironmentMode = enumEnvironmentMode.PROD;

        private static bool IsConnectedMessage(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;
            return message.IndexOf("Is Connected", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsQuoteConnectedMessage(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;
            return message.IndexOf("台股報價/國外期貨報價Is Connected", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsNotConnectedMessage(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;
            return message.IndexOf("尚未連線", StringComparison.OrdinalIgnoreCase) >= 0
                   || message.IndexOf("Is Disconnected", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsQuoteDisconnectedMessage(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;
            return message.IndexOf("台股報價/國外期貨報價Is Disconnected", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsLoginResponse(int intMark, string strIndex, object objValue)
            => intMark == 1 && string.Equals(strIndex, "Login", StringComparison.OrdinalIgnoreCase) && objValue is byte[];

        private static string DecodeFixedString(byte[] data, int offset, int length)
        {
            try
            {
                // Yuanta sample uses Big5 in helper. MsgCode is numeric but Big5 is safe.
                var enc = System.Text.Encoding.GetEncoding(950);
                var slice = new byte[length];
                Buffer.BlockCopy(data, offset, slice, 0, length);
                var s = enc.GetString(slice);
                var nullIndex = s.IndexOf('\0');
                return (nullIndex >= 0 ? s.Substring(0, nullIndex) : s).Trim();
            }
            catch
            {
                // Fallback: ASCII
                var slice = new byte[length];
                Buffer.BlockCopy(data, offset, slice, 0, length);
                var s = System.Text.Encoding.ASCII.GetString(slice);
                var nullIndex = s.IndexOf('\0');
                return (nullIndex >= 0 ? s.Substring(0, nullIndex) : s).Trim();
            }
        }

        private static bool TryParseLoginResponse(byte[] data, out string msgCode, out string msgContent, out uint count)
        {
            // Based on YuantaAPI_TestAP/DataStructs/Login.cs:
            // MsgCode: TByte5, MsgContent: TByte50, Count: uint
            msgCode = "";
            msgContent = "";
            count = 0;

            const int msgCodeLen = 5;
            const int msgContentLen = 50;
            const int countLen = 4;
            var minLen = msgCodeLen + msgContentLen + countLen;
            if (data == null || data.Length < minLen) return false;

            msgCode = DecodeFixedString(data, 0, msgCodeLen);
            msgContent = DecodeFixedString(data, msgCodeLen, msgContentLen);
            count = BitConverter.ToUInt32(data, msgCodeLen + msgContentLen);
            return true;
        }

        private void _trader_OnResponse(int intMark, uint dwIndex, string strIndex, object objHandle, object objValue)
        {
            // Mirror the sample project behavior: surface system/RQ responses for troubleshooting.
            if (intMark == 0)
            {
                var msg = Convert.ToString(objValue);
                _logger?.LogInformation($"[Yuanta API] System: {msg}");

                if (IsConnectedMessage(msg))
                {
                    _isConnected = true;
                    _connectedEvent.Set();
                }
                else if (IsNotConnectedMessage(msg))
                {
                    _isConnected = false;
                    _connectedEvent.Reset();
                }

                if (IsQuoteConnectedMessage(msg))
                {
                    _isQuoteConnected = true;
                    _quoteConnectedEvent.Set();
                }
                else if (IsQuoteDisconnectedMessage(msg))
                {
                    _isQuoteConnected = false;
                    _quoteConnectedEvent.Reset();
                }
            }
            else if (IsLoginResponse(intMark, strIndex, objValue))
            {
                var bytes = (byte[])objValue;
                if (TryParseLoginResponse(bytes, out var msgCode, out var msgContent, out var count))
                {
                    _lastLoginMsgCode = msgCode;
                    _lastLoginMsgContent = msgContent;
                    _lastLoginCount = count;
                    _isLoggedIn = msgCode == "0001" || msgCode == "00001";
                    _loginEvent.Set();
                    _logger?.LogInformation($"[Yuanta API] Login response: Code={msgCode}, Msg={msgContent}, Count={count}");
                }
                else
                {
                    _logger?.LogInformation($"[Yuanta API] Login response received (length={bytes.Length}).");
                }
            }
            else if (intMark == 1 && string.IsNullOrWhiteSpace(strIndex))
            {
                _logger?.LogError($"[Yuanta API] RQ/RP error: {Convert.ToString(objValue)}");
            }

            // Subscription responses (Quote stream). If strIndex is empty, it's an error per official sample.
            if (intMark == 2)
            {
                if (string.IsNullOrWhiteSpace(strIndex))
                {
                    _logger?.LogError($"[Yuanta API] Subscribe error: {Convert.ToString(objValue)}");
                }
                else if (objValue is byte[] bytes)
                {
                    var key = strIndex.Trim();
                    bool shouldLog = false;
                    lock (_seenSubIndexes)
                    {
                        shouldLog = _seenSubIndexes.Add(key);
                    }

                    if (shouldLog)
                    {
                        _logger?.LogInformation($"[Yuanta API] Subscribe callback: Index={key}, Length={bytes.Length}");
                    }
                }
                else
                {
                    var key = strIndex?.Trim() ?? string.Empty;
                    bool shouldLog = false;
                    lock (_seenSubIndexes)
                    {
                        shouldLog = _seenSubIndexes.Add(key);
                    }

                    if (shouldLog)
                    {
                        _logger?.LogInformation($"[Yuanta API] Subscribe callback: Index={key}, Type={objValue?.GetType().FullName ?? "<null>"}");
                    }
                }
            }

            if (intMark == 2 &&
                string.Equals(strIndex?.Trim(), "210.10.40.10", StringComparison.OrdinalIgnoreCase) &&
                objValue is byte[] data)
            {
                ParseStockTick(data);
            }
        }

        private void ParseStockTick(byte[] data)
        {
            try
            {
                // NOTE:
                // The official sample uses YuantaDataHelper to read fields sequentially.
                // Values in the live stream are in big-endian (network order) for numeric fields;
                // marshaling into a struct on little-endian Windows produces values like 0x01000000.
                if (!TryParseStockTick210104010(data, out var symbol, out var time, out var dealPriceRaw, out var dealVolRaw, out var serialNo, out var key))
                {
                    _logger?.LogError($"[YuantaBrokerClient] Tick parse failed (len={data?.Length ?? 0}).");
                    return;
                }

                // De-duplicate exact repeated ticks.
                // In practice, quote servers may resend the last tick (e.g. during idle/after-hours or on reconnect).
                // Use serialNo when available; otherwise fall back to (time, priceRaw, volRaw).
                var signature = new TickSignature(serialNo, key, time, dealPriceRaw, dealVolRaw);
                lock (_dedupeLock)
                {
                    if (_lastTickBySymbol.TryGetValue(symbol, out var last) && last.Equals(signature))
                    {
                        return;
                    }

                    _lastTickBySymbol[symbol] = signature;
                }

                // Convert data to Domain Entity
                // In practice, Yuanta may return price as either price*100 (2 decimals) or price*10000.
                // Normalize to a human price (2 decimals) using a conservative heuristic.
                decimal price = NormalizePrice(dealPriceRaw);
                decimal volume = dealVolRaw;

                var tick = new Tick(symbol, time, price, volume);
                
                OnTickReceived?.Invoke(tick);

                // Log in a clear JSON shape for downstream parsing/diagnostics.
                // Example: {"stockNo":"2327","price":222.00,"vol":1,"serialNo":123,"key":"..."}
                var tickJson =
                    $"{{\"stockNo\":\"{symbol}\",\"price\":{price.ToString("0.00", CultureInfo.InvariantCulture)},\"vol\":{dealVolRaw},\"serialNo\":{serialNo},\"key\":\"{key}\",\"tickTime\":\"{time:HH:mm:ss.fff}\"}}";
                _logger?.LogInformation($"[YuantaBrokerClient] Tick: {tickJson}");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[YuantaBrokerClient] Error parsing tick", ex);
            }
        }

        private readonly struct TickSignature : IEquatable<TickSignature>
        {
            public TickSignature(uint serialNo, string key, DateTime time, int priceRaw, int volRaw)
            {
                SerialNo = serialNo;
                Key = key;
                Time = time;
                PriceRaw = priceRaw;
                VolRaw = volRaw;
            }

            public uint SerialNo { get; }
            public string Key { get; }
            public DateTime Time { get; }
            public int PriceRaw { get; }
            public int VolRaw { get; }

            public bool Equals(TickSignature other)
            {
                if (SerialNo != 0 && other.SerialNo != 0)
                {
                    return SerialNo == other.SerialNo;
                }

                return Time == other.Time && PriceRaw == other.PriceRaw && VolRaw == other.VolRaw && string.Equals(Key, other.Key, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is TickSignature other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = (int)SerialNo;
                    hash = (hash * 397) ^ (Key?.GetHashCode() ?? 0);
                    hash = (hash * 397) ^ Time.GetHashCode();
                    hash = (hash * 397) ^ PriceRaw;
                    hash = (hash * 397) ^ VolRaw;
                    return hash;
                }
            }
        }

        private static decimal NormalizePrice(int dealPriceRaw)
        {
            // Typical TWSE price*100 is in tens of thousands (e.g. 2327 @ 222.50 => 22250).
            // If we see values 100x larger, interpret as price*10000.
            var abs = Math.Abs((long)dealPriceRaw);
            if (abs >= 1_000_000)
            {
                return dealPriceRaw / 10000m;
            }

            return dealPriceRaw / 100m;
        }

        private static bool TryParseStockTick210104010(
            byte[] data,
            out string symbol,
            out DateTime time,
            out int dealPrice,
            out int dealVol,
            out uint serialNo,
            out string key)
        {
            symbol = string.Empty;
            time = DateTime.MinValue;
            dealPrice = 0;
            dealVol = 0;
            serialNo = 0;
            key = string.Empty;

            if (data == null || data.Length < 62) return false;

            int offset = 0;

            // abyKey (22)
            var keyBytes = new byte[22];
            Buffer.BlockCopy(data, offset, keyBytes, 0, 22);
            key = DecodeNullTerminatedAscii(keyBytes);
            offset += 22;

            // byMarketNo (1)
            offset += 1;

            // abyStkCode (12)
            var codeBytes = new byte[12];
            Buffer.BlockCopy(data, offset, codeBytes, 0, 12);
            offset += 12;

            // uintSerialNo (4)
            serialNo = ReadUInt32BE(data, offset);
            offset += 4;

            // TYuantaTime: hour(1) min(1) sec(1) msec(2)
            byte hour = data[offset++];
            byte minute = data[offset++];
            byte second = data[offset++];
            ushort msec = ReadUInt16BE(data, offset);
            offset += 2;

            // intBuyPrice (4) - not used
            offset += 4;
            // intSellPrice (4) - not used
            offset += 4;
            // intDealPrice (4)
            dealPrice = ReadInt32BE(data, offset);
            offset += 4;
            // intDealVol (4) - official sample reads as Int32
            dealVol = ReadInt32BE(data, offset);
            offset += 4;

            // byInOutFlag (1) + byType (1) - not used
            // offset += 2;

            symbol = DecodeNullTerminatedAscii(codeBytes);
            time = DateTime.Today.AddHours(hour).AddMinutes(minute).AddSeconds(second).AddMilliseconds(msec);
            return !string.IsNullOrWhiteSpace(symbol);
        }

        private static string DecodeNullTerminatedAscii(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return string.Empty;
            int len = bytes.Length;
            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] == 0)
                {
                    len = i;
                    break;
                }
            }
            return System.Text.Encoding.ASCII.GetString(bytes, 0, len).Trim();
        }

        private static ushort ReadUInt16BE(byte[] data, int offset)
        {
            return (ushort)((data[offset] << 8) | data[offset + 1]);
        }

        private static uint ReadUInt32BE(byte[] data, int offset)
        {
            unchecked
            {
                return ((uint)data[offset] << 24)
                       | ((uint)data[offset + 1] << 16)
                       | ((uint)data[offset + 2] << 8)
                       | data[offset + 3];
            }
        }

        private static int ReadInt32BE(byte[] data, int offset)
        {
            unchecked
            {
                return (data[offset] << 24)
                       | (data[offset + 1] << 16)
                       | (data[offset + 2] << 8)
                       | data[offset + 3];
            }
        }

        /// <inheritdoc />
        public void Login(string username, string password)
        {
            var envMode = DefaultEnvironmentMode;
            _logger?.LogInformation($"[YuantaBrokerClient] Connecting to Yuanta API ({envMode})... PopUpMsg={_popUpMsg}");

            // Set environment first
            _trader.Open(envMode);

            // In the official sample, users click Open first and Login after the host is connected.
            // Here we wait briefly for the connection event to avoid sending Login too early.
            if (_logger != null)
            {
                if (!_connectedEvent.Wait(TimeSpan.FromSeconds(15)))
                {
                    _logger.LogError("[YuantaBrokerClient] Timed out waiting for Yuanta host connection.");
                }
            }
            
            _logger?.LogInformation($"[YuantaBrokerClient] logging in as User: {username}...");

            // Reset login state for this attempt
            _isLoggedIn = false;
            _loginEvent.Reset();
            bool loginSuccess = _trader.Login(username, password);
            
            if (loginSuccess)
            {
                _logger?.LogInformation("[YuantaBrokerClient] Login successfully initiated.");

                // Wait for the Login RQ response so downstream operations (Subscribe) don't race.
                if (_logger != null)
                {
                    if (!_loginEvent.Wait(TimeSpan.FromSeconds(15)))
                    {
                        _logger.LogError("[YuantaBrokerClient] Timed out waiting for Login response.");
                    }
                    else if (!_isLoggedIn)
                    {
                        _logger.LogError($"[YuantaBrokerClient] Login rejected: Code={_lastLoginMsgCode}, Msg={_lastLoginMsgContent}");
                    }
                }
            }
            else
            {
                _logger?.LogError("[YuantaBrokerClient] Login failed to initiate. Please check system status.");
                // Depending on requirement, might throw exception or just log
            }
        }

        /// <inheritdoc />
        public void Subscribe(string symbol)
        {
            if (_logger != null && !_isLoggedIn)
            {
                _logger.LogError("[YuantaBrokerClient] Subscribe blocked: not logged in yet.");
                return;
            }

            // The quote stream connects after trade host login (see official sample).
            // Subscribing before quote host is connected can fail with "執行異常".
            if (_requiresQuoteConnection && !_isQuoteConnected)
            {
                _logger?.LogInformation("[YuantaBrokerClient] Waiting for quote server connection before subscribing...");
                if (!_quoteConnectedEvent.Wait(TimeSpan.FromSeconds(15)))
                {
                    _logger?.LogError("[YuantaBrokerClient] Subscribe blocked: quote server not connected yet.");
                    return;
                }
            }

            _logger?.LogInformation($"[YuantaBrokerClient] Subscribing to {symbol}...");
            
            var list = new List<StockTick>();
            var tick = new StockTick
            {
                MarketNo = (byte)enumMarketType.TWSE, // Default to TWSE for now
                StockCode = symbol
            };
            list.Add(tick);

            _trader.SubscribeStockTick(list);
        }

        public void Dispose()
        {
            try
            {
                _trader.Close();
            }
            catch (Exception ex)
            {
                _logger?.LogError("[YuantaBrokerClient] Error while closing Yuanta API", ex);
            }

            try
            {
                _trader.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogError("[YuantaBrokerClient] Error while disposing Yuanta API", ex);
            }
        }
    }
}
