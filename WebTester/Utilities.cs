using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WebTester
{
    class Utilities
    {
        public static string CreateLogFile(string NameOfFile, bool overwrite)
        {
            string pathstring = AppDomain.CurrentDomain.BaseDirectory;
            if (!Directory.Exists(pathstring + "\\Logs"))
            {
                Directory.CreateDirectory(pathstring + "\\Log");
            }
            pathstring = pathstring + "\\Log\\" + NameOfFile + ".log";

            if (overwrite)
            {
                if (System.IO.File.Exists(pathstring))
                {
                    System.IO.File.Delete(pathstring);
                }
            }

            if (!System.IO.File.Exists(pathstring))
            {
                System.IO.StreamWriter file = System.IO.File.CreateText(pathstring);
                file.Close();
            }
            return pathstring;
        }

        public static void Log(string pathstring, string message, bool TimeStamp = true)
        {
            System.IO.StreamWriter file = File.AppendText(pathstring);
            if (TimeStamp)
            {
                file.WriteLine(DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss tt") + " -- " + message);
            }
            else 
            {
                file.WriteLine(message);
            }
            file.Close();
        }
    }
}
