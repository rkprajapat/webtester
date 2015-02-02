using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Net.Mail;
using System.Configuration;

namespace WebTester
{
    public sealed class Runner
    {
        public bool isRunning = false;
        string log = Utilities.CreateLogFile("Application", false);
        string errorlog = Utilities.CreateLogFile("Error", true);
        string URLLog;
        //DBOps db = new DBOps();

        /// <summary>
        /// Start runner class
        /// </summary>
        public void start()
        {
            try
            {
                isRunning = true;

                URLProperties prop = new URLProperties();
                TestListGenerator generator = new TestListGenerator();

                //generate a list of URL to test            
                List<string> list = generator.PrepareTestList();

                ///*
                ///Start fiddler
                int port = GetOpenPort();
                if (!Fiddler.FiddlerApplication.IsStarted())
                {
                    Fiddler.CONFIG.IgnoreServerCertErrors = true;
                    Fiddler.CONFIG.QuietMode = true;
                    Fiddler.CONFIG.bMITM_HTTPS = true;
                    Fiddler.CONFIG.bCaptureCONNECT = true;
                    Fiddler.CONFIG.DecryptWhichProcesses = Fiddler.ProcessFilterCategories.All;
                    //Fiddler.WinINETCache.ClearCookies();
                    //Fiddler.WinINETCache.ClearFiles();
                    //Fiddler.WinINETCache.ClearCacheItems(true, true);

                    Utilities.Log(log, "Fiddler starting on port " + port);
                    Fiddler.FiddlerApplication.Startup(port, Fiddler.FiddlerCoreStartupFlags.Default);
                }

                while (!Fiddler.FiddlerApplication.IsStarted())
                {
                    Utilities.Log(log, "Waiting for fiddler to start");
                    Thread.Sleep(300);
                }
                Utilities.Log(log, "Fiddler started on port " + port);

                ///*
                ///go through each url
                ///                
                Utilities.Log(log, "Total " + list.Count + " URLs to test.");
                foreach (string @id in list)
                {
                    prop.GetURL(id);
                    URLLog = Utilities.CreateLogFile(id, true);

                    Uri uri = new Uri(prop.URL);
                    if (isAccessible(uri))
                    {
                        Utilities.Log(log, "Starting test on " + prop.URL);
                        string LogFile = id;
                        Tester tester = new Tester();
                        tester.Log = Utilities.CreateLogFile(LogFile, true);
                        tester.URL = prop.URL;
                        tester.ID = prop.UrlID;
                        tester.CodeFile = prop.CodeFile;                        
                        if (prop.IEBrowser == 1)
                        {
                            tester.IETest();
                        }
                        else
                        {
                            tester.ChromeTest();
                        }
                    }
                    else
                    {
                        Sendmail("URL not accessible.", prop.URL + " Not accessible", prop.UrlID);
                    }
                }

                if (Fiddler.FiddlerApplication.IsStarted())
                {
                    Utilities.Log(log, "Fiddler stopping");
                    Fiddler.FiddlerApplication.Shutdown();
                    while (Fiddler.FiddlerApplication.isClosing)
                    {
                        Utilities.Log(log, "Waiting for fiddler to stop");
                        Thread.Sleep(300);
                    }
                    Utilities.Log(log, "Fiddler stopped");
                }

                isRunning = false;
            }
            catch (Exception ex)
            {
                Utilities.Log(errorlog, ex.Message);
            }
        }


        /// <summary>
        /// find available ports
        /// </summary>
        /// <returns>integer</returns>
        private int GetOpenPort()
        {
            try
            {
                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                sock.Bind(new IPEndPoint(IPAddress.Any, 0));
                int port = ((IPEndPoint)sock.LocalEndPoint).Port;
                sock.Dispose();
                return port;
            }
            catch (Exception ex)
            {
                Utilities.Log(errorlog, ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// checks whether URL is accessible
        /// </summary>
        /// <param name="uri"></param>
        /// <returns>boolean value</returns>
        private bool isAccessible(Uri uri)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uri);
                request.Credentials = System.Net.CredentialCache.DefaultCredentials;
                request.Method = "HEAD"; //get headers only
                request.Timeout = 5000;
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:24.0) Gecko/20100101 Firefox/24.0";
                request.Credentials = CredentialCache.DefaultCredentials;

                HttpStatusCode response = HttpStatusCode.OK;
                try
                {
                    Utilities.Log(URLLog, "Fetching response");
                    HttpWebResponse r = (HttpWebResponse)request.GetResponse();
                    response = r.StatusCode;
                    r.Close();
                }
                catch (WebException ex)
                {
                    Utilities.Log(URLLog, ex.ToString());
                    HttpWebResponse r = (HttpWebResponse)ex.Response;
                    response = r.StatusCode;
                    r.Close();
                }

                //check if site is available
                if (!onlineStatusCodes.Contains(response))
                {
                    Utilities.Log(URLLog, "Response result  : " + response.ToString());
                    return true;
                }
                else
                {
                    Utilities.Log(URLLog, response.ToString());
                    return false;
                }
            }
            catch (Exception ex)
            {
                //Utilities.Log(errorlog, ex.ToString() + Environment.NewLine + stacktrace.ToString());
            }
            return true;
        }

        /// <summary>
        /// list of codes to be evaluated
        /// </summary>
        private static IEnumerable<HttpStatusCode> onlineStatusCodes = new[]
        {
            //all codes that could result in url not being available
            HttpStatusCode.BadRequest,            
            HttpStatusCode.Conflict,
            HttpStatusCode.ExpectationFailed,
            HttpStatusCode.Forbidden,
            HttpStatusCode.Gone,
            HttpStatusCode.LengthRequired,
            HttpStatusCode.MethodNotAllowed,
            HttpStatusCode.NotAcceptable,
            HttpStatusCode.NotFound,
            HttpStatusCode.PaymentRequired,
            HttpStatusCode.PreconditionFailed,
            HttpStatusCode.ProxyAuthenticationRequired,
            HttpStatusCode.RequestedRangeNotSatisfiable,
            HttpStatusCode.RequestEntityTooLarge,
            HttpStatusCode.RequestTimeout,
            HttpStatusCode.RequestUriTooLong,
            HttpStatusCode.ServiceUnavailable,            
            HttpStatusCode.UpgradeRequired
            // add more codes as needed
        };

        /// <summary>
        /// sends email
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="message"></param>
        /// <param name="ID"></param>
        private void Sendmail(string subject, string message, string ID)
        {
            System.Collections.Specialized.NameValueCollection appSettings = ConfigurationManager.AppSettings;
            string smtp_server = "";
            int smtp_port = 0;
            string from_email = "";
            if (appSettings.Count != 0)
            {
                smtp_server = appSettings["smtp_server"];
                smtp_port = Convert.ToInt16(appSettings["smtp_port"]);
                from_email = appSettings["from_email"];
            }

            try
            {
                Utilities.Log(URLLog, "Sending Email");
                //Send failure email only if it has failed for the first time
                //if the failed flag is set, dont proceed further                
                DBOps.Query = "select failed from inputUrl where id='" + ID + "'";
                if ((int)DBOps.ReturnSingleValue() != 1)
                {
                    //fetch email address from DB
                    DBOps.Query = "select email from inputUrl where id='" + ID + "'";
                    string email = DBOps.ReturnSingleValue().ToString();
                    if (!string.IsNullOrEmpty(email))
                    {
                        //send failure email
                        string to = email;
                        string from = from_email;
                        MailMessage mailmessage = new MailMessage(from, to);
                        mailmessage.Subject = subject;
                        mailmessage.Body = message + Environment.NewLine + "This is a system generated email. Please do not respond.";
                        SmtpClient client = new SmtpClient();
                        client.Host = smtp_server;
                        client.Port = smtp_port;
                        // Credentials are necessary if the server requires the client  
                        // to authenticate before it will send e-mail on the client's behalf.
                        client.UseDefaultCredentials = true;

                        try
                        {
                            client.Send(mailmessage);
                        }
                        catch (Exception ex)
                        {
                            Utilities.Log(errorlog, "could not send email:" + ex.Message);
                        }
                        //URL has failed, set failed flag in DB
                        DBOps.Query = "update inputUrl set failed = 1 where id='" + ID + "'";
                        DBOps.WriteToDB();
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.Log(errorlog, ex.Message);
            }
        }
    }
}
