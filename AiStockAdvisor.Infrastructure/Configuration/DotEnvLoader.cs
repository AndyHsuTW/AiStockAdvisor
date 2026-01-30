using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AiStockAdvisor.Infrastructure.Configuration
{
    /// <summary>
    /// 從 .env 檔案載入環境變數。
    /// </summary>
    public static class DotEnvLoader
    {
        /// <summary>
        /// 從指定路徑載入 .env 檔案並設定為環境變數。
        /// </summary>
        /// <param name="filePath">.env 檔案路徑。若為 null，則自動搜尋程式目錄。</param>
        /// <param name="overwrite">是否覆蓋已存在的環境變數。預設為 false。</param>
        /// <returns>載入的變數數量。</returns>
        public static int Load(string? filePath = null, bool overwrite = false)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = FindEnvFile();
            }

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return 0;
            }

            return LoadFromFile(filePath, overwrite);
        }

        /// <summary>
        /// 在程式目錄中搜尋 .env 檔案。
        /// </summary>
        private static string? FindEnvFile()
        {
            var searchPaths = new List<string>();

            // 1. 目前工作目錄
            searchPaths.Add(Environment.CurrentDirectory);

            // 2. 執行檔所在目錄
            var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            if (!string.IsNullOrEmpty(exePath))
            {
                var exeDir = Path.GetDirectoryName(exePath);
                if (!string.IsNullOrEmpty(exeDir))
                {
                    searchPaths.Add(exeDir);
                }
            }

            // 3. 入口程式集目錄
            var entryPath = System.Reflection.Assembly.GetEntryAssembly()?.Location;
            if (!string.IsNullOrEmpty(entryPath))
            {
                var entryDir = Path.GetDirectoryName(entryPath);
                if (!string.IsNullOrEmpty(entryDir))
                {
                    searchPaths.Add(entryDir);
                }
            }

            // 搜尋 .env 檔案
            foreach (var basePath in searchPaths)
            {
                var envPath = Path.Combine(basePath, ".env");
                if (File.Exists(envPath))
                {
                    return envPath;
                }
            }

            return null;
        }

        /// <summary>
        /// 從檔案載入環境變數。
        /// </summary>
        private static int LoadFromFile(string filePath, bool overwrite)
        {
            var count = 0;
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // 跳過空行和註解
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                {
                    continue;
                }

                // 解析 KEY=VALUE
                var equalsIndex = trimmedLine.IndexOf('=');
                if (equalsIndex <= 0)
                {
                    continue;
                }

                var key = trimmedLine.Substring(0, equalsIndex).Trim();
                var value = trimmedLine.Substring(equalsIndex + 1).Trim();

                // 移除引號
                value = RemoveQuotes(value);

                // 設定環境變數
                if (overwrite || string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                {
                    Environment.SetEnvironmentVariable(key, value);
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 移除值前後的引號。
        /// </summary>
        private static string RemoveQuotes(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 2)
            {
                return value;
            }

            // 處理雙引號
            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                return value.Substring(1, value.Length - 2);
            }

            // 處理單引號
            if (value.StartsWith("'") && value.EndsWith("'"))
            {
                return value.Substring(1, value.Length - 2);
            }

            return value;
        }
    }
}
