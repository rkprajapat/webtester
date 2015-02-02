using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Net;

namespace WebTester
{
    public partial class Service1 : ServiceBase
    {
        System.Timers.Timer _timer = new System.Timers.Timer(1 * 60 * 1000);
        string log;
        Runner runner = new Runner();

        public Service1()
        {
            InitializeComponent();
            _timer.Elapsed += _timer_Elapsed;
        }

        void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //call the singleton class.           
            if (!runner.isRunning)
            {
                log = Utilities.CreateLogFile("Application", true);
                runner.start();                
            }
        }

        /// <summary>
        /// when windows service starts
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            log = Utilities.CreateLogFile("Application", true);
            string errorlog = Utilities.CreateLogFile("Error", true);
            Utilities.Log(log,"WebTester Started");

            _timer.Start();

            //DBOps db = new DBOps();
            DBOps.Query = "update inputUrl set engaged = 0";
            DBOps.WriteToDB();
        }

        /// <summary>
        /// when windows service stops
        /// </summary>
        protected override void OnStop()
        {
            _timer.Stop();
            _timer.Dispose();
            if (Fiddler.FiddlerApplication.IsStarted())
            {
                Fiddler.FiddlerApplication.Shutdown();
                Utilities.Log(log, "Fiddler stopped");
            }            
                        
            Utilities.Log(log,"WebTester Stopped");
        }
    }
}
