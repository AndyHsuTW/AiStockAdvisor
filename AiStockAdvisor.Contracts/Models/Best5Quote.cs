using System;

namespace AiStockAdvisor.Contracts.Models
{
    /// <summary>
    /// 五檔報價（Best 5）
    /// </summary>
    public class Best5Quote
    {
        /// <summary>報價時間</summary>
        public DateTime Time { get; }

        /// <summary>買價</summary>
        public decimal[] BidPrices { get; }

        /// <summary>買量</summary>
        public int[] BidVolumes { get; }

        /// <summary>賣價</summary>
        public decimal[] AskPrices { get; }

        /// <summary>賣量</summary>
        public int[] AskVolumes { get; }

        public Best5Quote(DateTime time, decimal[] bidPrices, int[] bidVolumes, decimal[] askPrices, int[] askVolumes)
        {
            Time = time;
            BidPrices = ValidateArray(bidPrices, nameof(bidPrices));
            BidVolumes = ValidateArray(bidVolumes, nameof(bidVolumes));
            AskPrices = ValidateArray(askPrices, nameof(askPrices));
            AskVolumes = ValidateArray(askVolumes, nameof(askVolumes));
        }

        private static T[] ValidateArray<T>(T[] values, string name)
        {
            if (values == null)
                throw new ArgumentNullException(name);
            if (values.Length != 5)
                throw new ArgumentException("Expected 5 levels.", name);
            return values;
        }
    }
}
