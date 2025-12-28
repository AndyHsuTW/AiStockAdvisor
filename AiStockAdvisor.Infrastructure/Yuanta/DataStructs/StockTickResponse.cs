using System;
using System.Runtime.InteropServices;

namespace AiStockAdvisor.Infrastructure.Yuanta.DataStructs
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TByte22
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 22)]
        public byte[] Value;

        public override string ToString()
        {
            return System.Text.Encoding.Default.GetString(Value).TrimEnd('\0');
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TByte12
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] Value;

        public override string ToString()
        {
            return System.Text.Encoding.Default.GetString(Value).TrimEnd('\0');
        }
    }

    /// <summary>
    /// Represents Yuanta Time structure.
    /// Matches YuantaShareStructList.TYuantaTime: Hour, Minute, Second, Millisecond.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TYuantaTime
    {
        public byte bytHour;
        public byte bytMin;
        public byte bytSec;
        public ushort ushtMSec;
    }

    /// <summary>
    /// Corresponds to Yuanta API's ChildStruct_Out for Stock Tick (210.10.40.10)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StockTickResponse
    {
        public TByte22 abyKey;
        public byte byMarketNo;
        public TByte12 abyStkCode;
        public uint uintSerialNo;
        public TYuantaTime struTime;
        public int intBuyPrice;
        public int intSellPrice;
        public int intDealPrice;
        public uint dwDealVol;
        public byte byInOutFlag;
        public byte byType;
    }
}
