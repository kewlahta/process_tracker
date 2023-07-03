using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Data.SQLite;

namespace ProcessTracker
{
    class Program
    {
        public static string dbDataSource = @"<path here>\appTracker.db";

        static void Main(string[] args)
        {
            List<string> procList = GetDBProcessNames();
            int sleepTimeInMillis = 1000;
            
            Dictionary<string, AppTimeTrack> processDictionary = new Dictionary<string, AppTimeTrack>();

            while(true)
            {
                //do something
                Process[] currentProcessArr = Process.GetProcesses();

                for(int counter = 0; counter < currentProcessArr.Length; counter++)
                {
                    if (procList.Contains(currentProcessArr[counter].ProcessName))
                    {
                        if(processDictionary.ContainsKey(currentProcessArr[counter].ProcessName) == false)
                        {
                            AppTimeTrack att = new AppTimeTrack();
                            att.startDt = DateTime.Now;
                            att.endDT = DateTime.Now;   

                            processDictionary.Add(currentProcessArr[counter].ProcessName, att);
                        }

                        processDictionary[currentProcessArr[counter].ProcessName].endDT = DateTime.Now;
                    }
                }

                List<string> procsToRemoveList = new List<string>();

                // show the currently tracked sessions
                foreach(string key in processDictionary.Keys)
                {
                    Console.WriteLine("\t" + key + " session time: " + processDictionary[key].totalSessionSeconds.ToString() + " second(s)");


                    // check the end time of the process' current session
                    // if the persisted end time more than 5 seconds, persist to the db, and remove the session from the persisted dictionary
                    if((DateTime.Now - processDictionary[key].endDT).TotalSeconds >= 5)
                    {
                        WriteSessionToDB(key, processDictionary[key]);
                        procsToRemoveList.Add(key);
                    }
                }

                foreach (string proc in procsToRemoveList)
                    processDictionary.Remove(proc);

                procsToRemoveList.Clear();


                System.Threading.Thread.Sleep(sleepTimeInMillis);
            }
            
            Console.ReadLine();
        }

        #region "helper methods"

        public static void WriteSessionToDB(string processName, AppTimeTrack att)
        {
            // get process id for the p name
            SQLiteConnectionStringBuilder dbCSB = new SQLiteConnectionStringBuilder();
            dbCSB.DataSource = dbDataSource;

            SQLiteConnection dbConn = new SQLiteConnection(dbCSB.ToString());
            dbConn.Open();

            SQLiteCommand dbCmd = new SQLiteCommand(dbConn);
            dbCmd.CommandType = System.Data.CommandType.Text;
            dbCmd.CommandText = "select process_id from process where process_name = @pname";

            SQLiteParameter param = new SQLiteParameter("@pname", processName);

            dbCmd.Parameters.Add(param);

            int processID = int.Parse(dbCmd.ExecuteScalar().ToString());

            // write the session values
            dbCmd.Parameters.Clear();

            dbCmd.CommandText = @"
                insert into process_session (
                    process_id, 
                    start_time, 
                    end_time, 
                    session_total_seconds
                ) values (
                    @process_id,
                    @start_time,
                    @end_time,
                    @seconds
                )";

            SQLiteParameter p1 = new SQLiteParameter("@process_id", processID);
            SQLiteParameter p2 = new SQLiteParameter("@start_time", att.startDt.ToString());
            SQLiteParameter p3 = new SQLiteParameter("@end_time", att.endDT.ToString());
            SQLiteParameter p4 = new SQLiteParameter("@seconds", att.totalSessionSeconds);

            dbCmd.Parameters.Add(p1);
            dbCmd.Parameters.Add(p2);
            dbCmd.Parameters.Add(p3);
            dbCmd.Parameters.Add(p4);

            int rowsAffected = dbCmd.ExecuteNonQuery();

            dbCmd.Dispose();
            dbConn.Dispose();

            Console.WriteLine(string.Format("{0} session(s) created.", rowsAffected.ToString()));
        }

        public static List<string> GetDBProcessNames()
        {
            List<string> retVal = new List<string>();

            // Connect to the db, query the list of processes and add to our list. 
            SQLiteConnectionStringBuilder dbCSB = new SQLiteConnectionStringBuilder();
            dbCSB.DataSource = dbDataSource;

            SQLiteConnection dbConn = new SQLiteConnection(dbCSB.ToString());
            dbConn.Open();

            SQLiteCommand dbCmd = new SQLiteCommand(dbConn);
            dbCmd.CommandType = System.Data.CommandType.Text;
            dbCmd.CommandText = "select process_name from process";

            SQLiteDataReader dbRdr = dbCmd.ExecuteReader();

            while(dbRdr.Read())
                retVal.Add(dbRdr.GetString(0));


            dbRdr.Close();
            dbCmd.Dispose();
            dbConn.Dispose();
                        
            return retVal;
        }

        #endregion
    }

    #region "helper objects"
    public class AppTimeTrack
    {
        public DateTime startDt;
        public DateTime endDT;

        public int totalSessionSeconds
        {
            get
            {
                return (int)(endDT - startDt).TotalSeconds;
            }
        }

    }


    #endregion
}
