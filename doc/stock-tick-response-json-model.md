# StockTickResponse JSON Data Model

Purpose
- Define a stable JSON shape when converting the Yuanta StockTickResponse (210.10.40.10) raw bytes to JSON.
- Preserve the raw fields and provide optional derived fields for normalized price and time string.

Source Mapping
- abyKey (TByte22)          -> key (string, ASCII, trim at null)
- byMarketNo (byte)         -> marketNo (integer)
- abyStkCode (TByte12)      -> stockCode (string, ASCII, trim at null)
- dwSerialNo (uint32)       -> serialNo (integer)
- struTime (TYuantaTime)    -> tickTime (object) + tickTimeText (string)
- intBuyPrice (int32)       -> buyPriceRaw (integer)
- intSellPrice (int32)      -> sellPriceRaw (integer)
- intDealPrice (int32)      -> dealPriceRaw (integer)
- dwDealVol (uint32)        -> dealVolRaw (integer)
- byInOutFlag (byte)        -> inOutFlag (integer)
- byType (byte)             -> tickType (integer)

Official Field Descriptions (IO Spec: D20A280A_210.10.40.10_IO_Spec.docx)
- abyKey: 鍵值; byMarketNo+abyStkCode(不足補0x00)
- byMarketNo: 市場代碼
- abyStkCode: 股票代碼
- dwSerialNo: 序號; 以股票代碼個別編序號，從1開始(0xFFFFFFFF代表商品清盤)
- struTime: 時間結構; byHour=時, byMin=分, bySec=秒, wMSec=毫秒
- intBuyPrice: 買價; 當dwSerialNo=0xFFFFFFFF時,此欄位代表漲停價; 市價買:999999999; 前端需除以10000
- intSellPrice: 賣價; 當dwSerialNo=0xFFFFFFFF時,此欄位代表跌停價; 市價賣:-999999999; 前端需除以10000
- intDealPrice: 成交價; 當dwSerialNo=0xFFFFFFFF時,此欄位代表開盤參考價; 前端需除以10000
- dwDealVol: 成交量
- byInOutFlag: 內外盤註記; 0:內盤 1:外盤 2:定價內盤 3:定價外盤 10:一般揭示 11:暫緩撮合且瞬間趨跌 12:暫緩撮合且瞬間趨漲 13:試算後延後收盤 14:暫停交易 15:恢復交易; 註1: 當是10,11,12,13時, 只有市場碼與股票代碼有值, 其餘的欄位皆是0; 註2: 當是14,15時, 只有市場碼、股票代碼與時間有值, 其餘的欄位皆是0
- byType: 明細類別; 0:Normal

JSON Object (raw fields + optional derived fields)
```
{
  "key": "string",
  "marketNo": 1,
  "stockCode": "2330",
  "serialNo": 12345,
  "tickTime": {
    "hour": 13,
    "minute": 45,
    "second": 12,
    "msec": 345
  },
  "tickTimeText": "13:45:12.345",
  "tradeDate": "2025-01-08",
  "buyPriceRaw": 22250,
  "sellPriceRaw": 22260,
  "dealPriceRaw": 22250,
  "dealVolRaw": 1,
  "inOutFlag": 0,
  "tickType": 0,

  "dealPrice": 222.50,
  "dealVol": 1
}
```

Notes
- tickTimeText uses local date implicitly; the raw payload does not include a trade date.
- In this project, the actual trade date is derived by GetTaipeiTradeDate() (Taipei time zone) and injected when building Tick / log JSON. If a date is needed in JSON, use that injected tradeDate (YYYY-MM-DD) or produce an ISO-8601 string from it.
- dealPrice is optional and is derived from dealPriceRaw using the same normalization rule as the application (raw / 100 or raw / 10000).
- dealVol is optional and mirrors dealVolRaw when you want a normalized numeric type for downstream systems.
