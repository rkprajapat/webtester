using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Mail;
using System.Net;
using System.IO;
using System.Management;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Diagnostics;
using System.Drawing;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;

using Fiddler;

namespace WebTester
{
    class Tester
    {
        System.Diagnostics.StackTrace stacktrace = new System.Diagnostics.StackTrace();
        //DBOps db = new DBOps();

        public string Log {get; set;}
        public string URL {get; set;}
        public string ID {get; set;}        
        public string CodeFile {get; set;}
        private int PID { get; set; }

        private double loadtimecached = 0;
        public double LoadTimeCached { get { return loadtimecached; } set { loadtimecached = value; } }
        private double loadtimenoncached = 0;
        public double LoadTimeNonCached { get { return loadtimenoncached; } set { loadtimenoncached = value; } }        

        private bool clear = true;// assign default value to clear cache on first run
        public bool ClearCache { 
            get{ return clear;}
            set { clear = value; } 
        }   

        IWebDriver driver;
        string errorlog = Utilities.CreateLogFile("Error", false);

        /// <summary>
        /// Load URLs in internet explorer and logs load time
        /// </summary>
        public void IETest()
        {
            try
            {
                string harfile = Utilities.CreateLogFile("HAR_" + ID, true);

                //if URL had failed earlier, change the flag to not failed
                DBOps.Query = "update inputUrl set failed = 0 where id='" + ID + "'";
                DBOps.WriteToDB();

                //*********************************
                //Setup FiddlerCore
                //********************************* 
                List<Fiddler.Session> oAllSessions = new List<Fiddler.Session>();
                Fiddler.FiddlerApplication.BeforeRequest += delegate(Fiddler.Session oS)
                {
                    Monitor.Enter(oAllSessions);
                    oAllSessions.Add(oS);
                    Monitor.Exit(oAllSessions);
                };

                List<Fiddler.Session> AfterCompleteSessions = new List<Fiddler.Session>();
                Fiddler.FiddlerApplication.AfterSessionComplete += delegate(Fiddler.Session oS)
                {
                    Monitor.Enter(AfterCompleteSessions);
                    AfterCompleteSessions.Add(oS);
                    Monitor.Exit(AfterCompleteSessions);
                };

                if (ClearCache)
                {
                    Utilities.Log(Log, "Clearing browser cache & cookies");
                    Fiddler.WinINETCache.ClearCookies();
                    Fiddler.WinINETCache.ClearFiles();
                    Fiddler.WinINETCache.ClearCacheItems(true, true);
                }

                //*********************************
                //setup webdriver
                //*********************************
                InternetExplorerOptions options = new InternetExplorerOptions();
                options.InitialBrowserUrl = "about:blank";
                options.UnexpectedAlertBehavior = InternetExplorerUnexpectedAlertBehavior.Dismiss;

                options.ToCapabilities();

                InternetExplorerDriverService service = InternetExplorerDriverService.CreateDefaultService();
                service.HideCommandPromptWindow = true;

                //create webdriver instance            
                driver = new InternetExplorerDriver(service, options);
                driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromMinutes(10));
                driver.Manage().Timeouts().SetPageLoadTimeout(TimeSpan.FromMinutes(10));
                driver.Manage().Timeouts().SetScriptTimeout(TimeSpan.FromMinutes(10));
                driver.Manage().Cookies.DeleteAllCookies();

                try
                {
                    //navigate to the URL
                    Utilities.Log(Log, "Navigating the URL");
                    driver.Navigate().GoToUrl(URL);
                    Utilities.Log(Log, "Navigated the URL");

                    int ServiceID = service.ProcessId;
                    
                    Utilities.Log(Log, oAllSessions.Count.ToString());
                    ScreenshotFile();

                    if (oAllSessions.Count != 0) //incase the URL didn't load or had a popup, there could be problems and it could be hung.
                    {
                        Utilities.Log(Log, "Checking for URL load completion");

                        //wait till URL has not loaded completely
                        while (!WaitForFinish(oAllSessions, AfterCompleteSessions))
                        {
                            Thread.Sleep(500);
                            Utilities.Log(Log, "Still checking");
                        }
                        Utilities.Log(Log, "URL loaded completely");                       
                        Utilities.Log(harfile, "Start Time -- End Time -- Duration -- Code -- URL", false);

                        ///
                        ///here we are trying to find exact process id for the browser which is generating session
                        ///
                        Monitor.Enter(oAllSessions);
                        foreach (Session s in oAllSessions)
                        {
                            int SessionID = s.LocalProcessID;
                            int SessionParentID = GetParentPID(SessionID);
                            int GrandParentID = GetParentPID(SessionParentID);

                            //record only valid sessions, as fiddler may record other browser activities as well.
                            if (GrandParentID == ServiceID)
                            {
                                PID = SessionID;
                                break;
                            }
                        }
                        Monitor.Exit(oAllSessions);

                        ///
                        /// here we are writing har log
                        Monitor.Enter(oAllSessions);
                        int startcount = -1;
                        int endcount = 0;
                        int i = 0;
                        foreach (Session s in oAllSessions)
                        {
                            //record only valid sessions, as fiddler may record other browser activities as well.
                            if (s.LocalProcessID == PID)
                            {
                                while (s.state != SessionStates.Aborted && s.state != SessionStates.Done)
                                {
                                    Thread.Sleep(100);
                                }

                                if (startcount == -1)
                                {
                                    startcount = i;
                                }
                                endcount = i;
                                double timespent = s.Timers.ClientDoneResponse.Subtract(s.Timers.ClientBeginRequest).TotalSeconds;
                                Utilities.Log(harfile, s.Timers.ClientBeginRequest + " -- " + s.Timers.ClientDoneResponse + " -- " + timespent + "s -- " + s.responseCode + " -- " + s.url, false);
                            }
                            i++;
                        }
                        Monitor.Exit(oAllSessions);

                        ///
                        ///write total load time
                        ///
                        if (ClearCache)
                        { 
                            LoadTimeNonCached = oAllSessions[endcount].Timers.ClientDoneResponse.Subtract(oAllSessions[startcount].Timers.ClientBeginRequest).TotalSeconds;
                            Utilities.Log(harfile, "Total Load Time without cache " + LoadTimeNonCached, false);
                        }
                        else
                        { 
                            LoadTimeCached = oAllSessions[endcount].Timers.ClientDoneResponse.Subtract(oAllSessions[startcount].Timers.ClientBeginRequest).TotalSeconds;
                            Utilities.Log(harfile, "Total Load Time with cache " + LoadTimeCached, false);
                        }                        
                    }
                    else
                    {
                        Utilities.Log(Log, "Something bad happened. URL didn't load.");
                    }
                }
                catch (Exception e)
                {
                    Utilities.Log(errorlog, e.ToString() + Environment.NewLine + stacktrace.ToString());
                }
                finally
                {
                    try
                    {
                        oAllSessions.Clear();
                        AfterCompleteSessions.Clear();
                        driver.Quit();
                        service.Dispose();
                        KillProcess();
                        Utilities.Log(Log, "Closed browser");

                        if (ClearCache)
                        {
                            ClearCache = false;
                            IETest();
                        }

                        Utilities.Log(Log, "Saving results to database");
                        WriteResults(ID, Math.Round(LoadTimeNonCached, 2), Math.Round(LoadTimeCached, 2), harfile, ScreenshotFile());

                        DBOps.Query = "Update [inputURL] set engaged = 0 where id='" + ID + "'";
                        DBOps.WriteToDB();
                    }
                    catch (Exception ex)
                    {
                        Utilities.Log(errorlog, ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.Log(errorlog, ex.ToString());
            }
        }


        //its used only for chrome
        public void ChromeTest()
        {
            try
            {
                string harfile = Utilities.CreateLogFile("HAR_" + ID, true);

                //if URL had failed earlier, change the flag to not failed
                DBOps.Query = "update inputUrl set failed = 0 where id='" + ID + "'";
                DBOps.WriteToDB();

                //*********************************
                //Setup FiddlerCore
                //********************************* 
                List<Fiddler.Session> oAllSessions = new List<Fiddler.Session>();
                Fiddler.FiddlerApplication.BeforeRequest += delegate(Fiddler.Session oS)
                {
                    Monitor.Enter(oAllSessions);
                    oAllSessions.Add(oS);
                    Monitor.Exit(oAllSessions);
                };

                List<Fiddler.Session> AfterCompleteSessions = new List<Fiddler.Session>();
                Fiddler.FiddlerApplication.AfterSessionComplete += delegate(Fiddler.Session oS)
                {
                    Monitor.Enter(AfterCompleteSessions);
                    AfterCompleteSessions.Add(oS);
                    Monitor.Exit(AfterCompleteSessions);
                };

                if (ClearCache)
                {
                    Utilities.Log(Log, "Clearing browser cache & cookies");
                    Fiddler.WinINETCache.ClearCookies();
                    Fiddler.WinINETCache.ClearFiles();
                    Fiddler.WinINETCache.ClearCacheItems(true, true);
                }


                //*********************************
                //setup webdriver
                //*********************************
                ChromeOptions options = new ChromeOptions();
                DesiredCapabilities caps = new DesiredCapabilities();
                caps.IsJavaScriptEnabled = true;
                caps.SetCapability("applicationCacheEnabled", false);
                caps.SetCapability(ChromeOptions.Capability, options);

                ChromeDriverService service = ChromeDriverService.CreateDefaultService();
                service.HideCommandPromptWindow = true;
                service.EnableVerboseLogging = true;

                //create webdriver instance            
                driver = new ChromeDriver(service, options);
                driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromMinutes(10));
                driver.Manage().Timeouts().SetPageLoadTimeout(TimeSpan.FromMinutes(10));
                driver.Manage().Timeouts().SetScriptTimeout(TimeSpan.FromMinutes(10));
                driver.Manage().Cookies.DeleteAllCookies();

                try
                {
                    //navigate to the URL
                    Utilities.Log(Log, "Navigating the URL");
                    driver.Navigate().GoToUrl(URL);
                    Utilities.Log(Log, "Navigated the URL");
                    int ServiceID = service.ProcessId;                    

                    if (oAllSessions.Count != 0) //incase the URL didn't load or had a popup, there could be problems and it could be hung.
                    {
                        Utilities.Log(Log, "Checking for URL load completion");

                        //wait till URL has not loaded completely
                        while (!WaitForFinish(oAllSessions, AfterCompleteSessions))
                        {
                            Thread.Sleep(500);
                            Utilities.Log(Log, "Still checking");
                        }
                        Utilities.Log(Log, "URL loaded completely");                        
                        Utilities.Log(harfile, "Start Time -- End Time -- Duration -- Code -- URL", false);

                        ///
                        ///here we are trying to find exact process id for the browser which is generating session
                        ///
                        Monitor.Enter(oAllSessions);
                        foreach (Session s in oAllSessions)
                        {
                            int SessionID = s.LocalProcessID;
                            int SessionParentID = GetParentPID(SessionID);
                            if (SessionParentID == ServiceID)
                            {
                                PID = SessionID;
                                break;
                            }
                        }
                        Monitor.Exit(oAllSessions);

                        ///
                        /// here we are writing har log
                        int startcount = -1;
                        int endcount = 0;
                        int i = 0;
                        Monitor.Enter(oAllSessions);
                        foreach (Session s in oAllSessions)
                        {
                            //record only valid sessions, as fiddler may record other browser activities as well.
                            if (s.LocalProcessID == PID)
                            {
                                while (s.state != SessionStates.Aborted && s.state != SessionStates.Done)
                                {
                                    Thread.Sleep(100);
                                }

                                if (startcount == -1)
                                {
                                    startcount = i;
                                }
                                endcount = i;
                                double timespent = s.Timers.ClientDoneResponse.Subtract(s.Timers.ClientBeginRequest).TotalSeconds;
                                Utilities.Log(harfile, s.Timers.ClientBeginRequest + " -- " + s.Timers.ClientDoneResponse + " -- " + timespent + "s -- " + s.responseCode + " -- " + s.url, false);
                            }
                            i++;
                        }
                        Monitor.Exit(oAllSessions);

                        ///
                        ///write total load time
                        ///
                        if (ClearCache)
                        {
                            LoadTimeNonCached = oAllSessions[endcount].Timers.ClientDoneResponse.Subtract(oAllSessions[startcount].Timers.ClientBeginRequest).TotalSeconds;
                            Utilities.Log(harfile, "Total Load Time without cache " + LoadTimeNonCached, false);
                        }
                        else
                        {
                            LoadTimeCached = oAllSessions[endcount].Timers.ClientDoneResponse.Subtract(oAllSessions[startcount].Timers.ClientBeginRequest).TotalSeconds;
                            Utilities.Log(harfile, "Total Load Time with cache " + LoadTimeCached, false);
                        } 

                    }
                    else
                    {
                        Utilities.Log(Log, "Something bad happened. URL didn't load.");
                    }
                }
                catch (Exception e)
                {
                    Utilities.Log(errorlog, e.ToString() + Environment.NewLine + stacktrace.ToString());
                }
                finally
                {
                    try
                    {
                        oAllSessions.Clear();
                        AfterCompleteSessions.Clear();
                        driver.Quit();
                        service.Dispose();
                        KillProcess();
                        Utilities.Log(Log, "Closed browser");

                        if (ClearCache)
                        {
                            ClearCache = false;
                            ChromeTest();
                        }

                        Utilities.Log(Log, "Saving results to database");
                        WriteResults(ID, Math.Round(LoadTimeNonCached, 2), Math.Round(LoadTimeCached, 2), harfile, ScreenshotFile());

                        DBOps.Query = "Update [inputURL] set engaged = 0 where id='" + ID + "'";
                        DBOps.WriteToDB();
                    }
                    catch (Exception ex)
                    {
                        Utilities.Log(errorlog, ex.ToString());
                    }

                }
            }
            catch (Exception ex)
            {
                Utilities.Log(errorlog, ex.ToString());
            }
        }

        /// <summary>
        /// Waits till URL has loaded completely
        /// </summary>
        /// <param name="before"></param>
        /// <param name="after"></param>
        /// <returns></returns>
        protected bool WaitForFinish(List<Fiddler.Session> before, List<Fiddler.Session> after)
        {
            int count;
            if (before.Count <= after.Count)
            {
                count = before.Count;
                Thread.Sleep(3000);

                if (count != before.Count)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Kills a process
        /// </summary>
        protected void KillProcess()
        {
            //if process doesn't exists; return
            if (!Process.GetProcesses().Any(x => x.Id == PID)) { return; }

            try
            {
                Process p = Process.GetProcessById(PID);
                p.Kill();
            }
            catch (Exception ex)
            {
                Utilities.Log(errorlog, "Could not kill process " + ex.Message);
            }
        }


        /// <summary>
        /// generates screenshot
        /// </summary>
        /// <returns>string file path</returns>
        protected string ScreenshotFile()
        {
            try
            {
                //take a screenshot of URL
                string path = AppDomain.CurrentDomain.BaseDirectory;
                if (!Directory.Exists(path + "\\UrlScreenshot"))
                {
                    Directory.CreateDirectory(path + "\\UrlScreenshot");
                }
                string filename = path + "\\UrlScreenshot\\" + ID + ".png";

                Screenshot ss = ((ITakesScreenshot)driver).GetScreenshot();
                ss.SaveAsFile(filename, System.Drawing.Imaging.ImageFormat.Png);

                return @filename;
            }
            catch (Exception ex)
            {
                Utilities.Log(Log, "unable to take screenshot. " + ex.Message);
                return null;
            }
        }



        /// <summary>
        /// Write Results to DB
        /// </summary>
        /// <param name="UrlID"></param>
        /// <param name="interval"></param>
        /// <param name="harfilepath"></param>
        /// <param name="screenshotfilepath"></param>
        protected void WriteResults(string UrlID, double intervalnoncached, double intervalcached, string harfilepath, string screenshotfilepath)
        {
            try
            {
                DBOps.Query = "INSERT INTO [testResults] ([urlid],[loadTimeUncached],[loadTimeCached],[harfile],[timestamp],[screenshot]) VALUES('" + ID + "'," + intervalnoncached + "," + intervalcached + ",'" + harfilepath + "','" + DateTime.Now + "','" + screenshotfilepath + "')";
                DBOps.WriteToDB();

                DBOps.Query = "UPDATE [inputUrl] SET [engaged] = 0 WHERE [id] = '" + ID + "'";
                DBOps.WriteToDB();
            }
            catch (Exception ex)
            {
                Utilities.Log(errorlog, "unable to write results in db. " + ex.Message);
            }
        }

        /// <summary>
        /// returns Parent process id for any process id
        /// </summary>
        /// <param name="PID"></param>
        /// <returns></returns>
        private int GetParentPID(int PID)
        {
            try
            {
                int ParentID = 0;
                string query = "select * from win32_process where ProcessId=" + PID;
                SelectQuery squery = new SelectQuery(query);
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(squery);
                ManagementObjectCollection processList = searcher.Get();
                foreach (ManagementObject process in processList)
                {
                    ParentID = Convert.ToInt32(process["ParentProcessId"].ToString());
                }
                return ParentID;
            }
            catch (Exception ex)
            {
                Utilities.Log(errorlog, ex.Message);
                return 0;
            }
        }
    }
}
