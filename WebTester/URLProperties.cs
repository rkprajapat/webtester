using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace WebTester
{
    class URLProperties
    {
        string id;
        public string UrlID
        {
            set { this.id = value; }
            get { return this.id; }
        }

        string urlname;
        public string URL
        {
            set { this.urlname = value; }
            get { return this.urlname; }
        }

        int frnc;
        public int Frequency
        {
            set { this.frnc = value; }
            get { return this.frnc; }
        }

        string ttle;
        public string Title
        {
            set { this.ttle = value; }
            get { return this.ttle; }
        }

        string codefile;
        public string CodeFile
        {
            set { this.codefile = value; }
            get { return this.codefile; }
        }

        public int IEBrowser
        {
            get;
            set;
        }

        public int ClearCache { get; set; }


        //URL parameters generator
        public void GetURL(string id)
        {
            //DBOps db = new DBOps();
            DBOps.Query = "SELECT * FROM [inputUrl] WHERE id='" + id + "'";

            DataTable dt = DBOps.ReadTable();

            foreach (DataRow row in dt.Rows)
            {
                UrlID = row["id"].ToString();
                URL = row["url"].ToString();
                Frequency = (int)row["frequency"];
                Title = row["title"].ToString();
                CodeFile = @row["codefile"].ToString();
                IEBrowser = (int)row["IEBrowser"];
                ClearCache = (int)row["ClearCache"];
            }
            dt.Dispose();
        }
    }
}
