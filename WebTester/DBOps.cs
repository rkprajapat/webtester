using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;

namespace WebTester
{
    static class DBOps
    {
        static string log = Utilities.CreateLogFile("Application", false);
        static string errorlog = Utilities.CreateLogFile("Error", false);

        //static SqlConnection conn = new SqlConnection("data source=vfmstest002;" + "Integrated Security=SSPI;" + "Database=WebTesterDB;");
        static SqlConnection conn = new SqlConnection();
        static SqlCommand cmd = new SqlCommand();        

        //Set database query
        public static string Query {get; set;}        

        //return SQL Reader with values
        public static DataTable ReadTable()
        {      
            OpenConnection();

            DataTable dt = new DataTable();

            try
            {
                //Utilities.Log(log, "reading table from db with '" + Query+"'");
                dt.Load(cmd.ExecuteReader());
            }
            catch (Exception e)
            {
                Utilities.Log(errorlog, e.Message + Environment.NewLine + Query);                
            }
            finally
            {
                CloseConnection();
            }
            return dt;
        }

        //write value to DB
        public static void WriteToDB()
        {
            OpenConnection();

            try
            {
                //Utilities.Log(log, "writing to db with '" + Query + "'");
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Utilities.Log(errorlog, e.Message + Environment.NewLine + Query);                                
            }
            finally
            {
                CloseConnection();
            }
        }

        //return single value from DB
        public static object ReturnSingleValue()
        {
            OpenConnection();

            object result;
            try
            {
                //Utilities.Log(log, "reading from db with '" + Query + "'");
                result = cmd.ExecuteScalar();
            }
            catch (Exception e)
            {
                Utilities.Log(errorlog, e.Message + Environment.NewLine + Query);                                                
                result = null;
            }
            finally
            {
                CloseConnection();
            }
            return result;
        }

        //Open connection to database
        private static void OpenConnection()
        {
            conn.ConnectionString = ConfigurationManager.ConnectionStrings["WebTester.Properties.Settings.WebTesterDBConnectionString"].ConnectionString;            

            cmd.CommandText = Query;
            cmd.Connection = conn;
            try
            {
                conn.Open();
            }
            catch (Exception e)
            {
                Utilities.Log(errorlog, e.Message);                                                                
            }
        }

        //Close database connection
        private static void CloseConnection()
        {
            conn.Close();
        }
    }
}
