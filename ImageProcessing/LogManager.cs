using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;

namespace ImageProcessing
{

    public static class LogManager
    {
        static SQLiteConnection connection = new SQLiteConnection("Data Source=Logs.db;Version=3;");
        public static void Log(LogData logData)
        {
            bool Complated = false;
            do
            {
                if (connection.State == System.Data.ConnectionState.Closed) ;
                connection.Open();

                SQLiteCommand sqCommand = new SQLiteCommand();
                sqCommand.Connection = connection;
                SQLiteTransaction myTrans;

                // Start a local transaction
                myTrans = connection.BeginTransaction();
                // Assign transaction object for a pending local transaction
                sqCommand.Transaction = myTrans;

                try
                {
                    sqCommand.CommandText = "INSERT INTO system_logs(date,time,type,message) Values('" +
                        logData.LogTime.ToString("yyyy-MM-dd") + "','" +
                        logData.LogTime.ToString("HH:mm:ss") + "'," +
                        (int)logData.LogType + ",'" +
                        logData.LogMessage + "')";
                    sqCommand.ExecuteNonQuery();
                    myTrans.Commit();
                    Complated = true;
                    string _logMessage = string.Format("[{0}] [{1}] {2}" + Environment.NewLine,
                    EnumHelper.GetEnumDescription(logData.LogType),
                    logData.LogTime.ToString("dd-MM-yyyy HH-mm-ss"),
                    logData.LogMessage);
                    System.Diagnostics.Debug.Write(_logMessage);
                }
                catch (Exception e)
                {
                    myTrans.Rollback();
                }
                finally
                {
                    connection.Close();
                }
            } while (!Complated);
        }
        public static void LogCoinData(CoinLogData coinLogData)
        {
            bool Complated = false;
            do
            {
                if (connection.State == System.Data.ConnectionState.Closed) ;
                connection.Open();

                SQLiteCommand sqCommand = new SQLiteCommand();
                sqCommand.Connection = connection;
                SQLiteTransaction myTrans;

                // Start a local transaction
                myTrans = connection.BeginTransaction();
                // Assign transaction object for a pending local transaction
                sqCommand.Transaction = myTrans;

                try
                {
                    sqCommand.CommandText = "INSERT INTO coin_logs(date,time,value,used_value,remained_value,total_value) Values('" +
                        coinLogData.LogTime.ToString("yyyy-MM-dd") + "','" +
                        coinLogData.LogTime.ToString("HH:mm:ss") + "','" +
                        coinLogData.CoinData.Value.ToString() + "','" +
                        coinLogData.CoinData.UsedValue.ToString() + "','" +
                        coinLogData.CoinData.RemainedValue.ToString() + "','" +
                        coinLogData.CoinData.TotalValue.ToString() + "')";
                    sqCommand.ExecuteNonQuery();
                    myTrans.Commit();
                    Complated = true;
                    string _logMessage = string.Format("[{0}] [{1}] Value: {2} Used: {3}, Remained: {4}, Total: {5}" + Environment.NewLine,
                    EnumHelper.GetEnumDescription(LogType.Normal),
                    coinLogData.LogTime.ToString("dd-MM-yyyy HH-mm-ss"),
                    coinLogData.CoinData.Value,
                    coinLogData.CoinData.UsedValue,
                    coinLogData.CoinData.RemainedValue,
                    coinLogData.CoinData.TotalValue);
                    System.Diagnostics.Debug.Write(_logMessage);
                }
                catch (Exception)
                {
                    myTrans.Rollback();
                }
                finally
                {
                    connection.Close();
                }
            } while (!Complated);
        }
        public static CoinData GetLastCoinData()
        {
            CoinData data = new CoinData(0, 0, 0);
            string sql = "Select * from coin_logs where id = (SELECT seq FROM sqlite_sequence where name='coin_logs')";
            if (connection.State == System.Data.ConnectionState.Closed)
                connection.Open();
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                try
                {
                    data.Value = Double.Parse(reader["value"].ToString(), System.Globalization.CultureInfo.InvariantCulture);
                    data.RemainedValue = Double.Parse(reader["remained_value"].ToString(), System.Globalization.CultureInfo.InvariantCulture);
                    data.UsedValue = Double.Parse(reader["used_value"].ToString(), System.Globalization.CultureInfo.InvariantCulture);
                    data.TotalValue = Double.Parse(reader["total_value"].ToString(), System.Globalization.CultureInfo.InvariantCulture);
                }
                catch (FormatException)
                {
                    
                }
            }
            connection.Close();
            return data;
        }
    }
}
