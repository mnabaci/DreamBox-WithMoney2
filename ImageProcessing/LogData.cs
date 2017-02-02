using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ImageProcessing
{
    public enum LogType
    {
        [Description("Normal")]
        Normal,
        [Description("Uyarı")]
        Warning,
        [Description("Hata")]
        Error,
    }
    public class LogData
    {
        LogType _logType;
        public LogType LogType
        {
            get
            {
                return _logType;
            }
            set
            {
                _logType = value;
            }
        }
        DateTime _logTime;
        public DateTime LogTime
        {
            get
            {
                return _logTime;
            }
            private set
            {
                _logTime = value;
            }
        }
        string _logMessage;
        public string LogMessage
        {
            get
            {
                return _logMessage;
            }
            set
            {
                LogTime = DateTime.Now;
                _logMessage = value;
            }
        }
        public LogData(string logMessage, LogType logType = LogType.Normal)
        {
            LogMessage = logMessage;
            LogType = logType;
        }
    }
    public class CoinData
    {
        public double UsedValue { get; set; }
        public double RemainedValue { get; set; }
        public double TotalValue { get; set; }
        public double Value { get; set; }
        public CoinData(double value, double usedValue, double remainedValue, double totalValue)
        {
            Value = value;
            UsedValue = usedValue;
            RemainedValue = remainedValue;
            TotalValue = totalValue;
        }
        public CoinData(double value, double usedValue, double totalValue)
        {
            UsedValue = usedValue;
            TotalValue = totalValue;
            RemainedValue = TotalValue - UsedValue;
        }
    }
    public class CoinLogData
    {
        DateTime _logTime;
        public DateTime LogTime
        {
            get
            {
                return _logTime;
            }
            private set
            {
                _logTime = value;
            }
        }
        CoinData _coinData;
        public CoinData CoinData
        {
            get
            {
                return _coinData;
            }
            private set
            {
                _coinData = value;
            }
        }
        public CoinLogData(CoinData coinData)
        {
            LogTime = DateTime.Now;
            CoinData = coinData;
        }
    }
}
