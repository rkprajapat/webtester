using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace WebTester
{
    class TestListGenerator
    {
        string log = Utilities.CreateLogFile("Application", false);

        //Prepare list of URLs to be tested
        public List<string> PrepareTestList()
        {
            Utilities.Log(log, "Preparing test list");
            //DBOps db = new DBOps();
            DBOps.Query = "SELECT [ID] FROM [inputUrl] WHERE [engaged]=0";

            URLProperties prop = new URLProperties();

            DataTable dt = DBOps.ReadTable();
            TimeSpan interval;

            List<string> TestsList = new List<string>();

            foreach (DataRow row in dt.Rows)
            {
                prop.GetURL(row["id"].ToString());
                DateTime lasttime = LastRunTime(prop.UrlID);

                if (lasttime != default(DateTime)) //lastruntime would be default if the URL wasn't tested earlier
                {
                    interval = DateTime.Now.Subtract(lasttime); //check difference between last run time and current time
                    if (interval.Minutes >= prop.Frequency)
                    {
                        TestsList.Add(@prop.UrlID); //create a list of url ids to be tested
                        DBOps.Query = "UPDATE [dbo].[inputUrl] SET [engaged] = 1 WHERE [id] = '" + prop.UrlID + "'";
                        DBOps.WriteToDB();
                    }
                }
                else
                {
                    TestsList.Add(@prop.UrlID); //create a list of url ids to be tested
                }
            }
            dt.Dispose();
            Utilities.Log(log, "List prepared");
            return TestsList; //return list of tests to be performed
        }

        //check last run time for the url
        protected DateTime LastRunTime(string id)
        {
            Utilities.Log(log, "Checking last run time for "+id);   
            //DBOps db = new DBOps();
            DBOps.Query = "SELECT COUNT(*) FROM [testResults] WHERE [urlid]='" + id + "'";

            DateTime timestamp = default(DateTime);

            int count = (int)DBOps.ReturnSingleValue();

            if (count != 0) // check if any data exists for given url
            {
                DBOps.Query = "SELECT MAX(timestamp) FROM [testResults] WHERE [urlid]='" + id + "'";
                timestamp = (DateTime)DBOps.ReturnSingleValue();
            }
            return timestamp;
        }
    }
}
