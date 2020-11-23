using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using System.Text;

namespace KafkaService
{
    public partial class Service1 : ServiceBase
    {
        #region declare
        private Process kafkaProcess = null;
        #endregion

        #region initialize 
        public Service1()
        {
            InitializeComponent();
        }
        #endregion

        #region Onstart service
        protected override void OnStart(string[] args)
        {
            string volume = AppDomain.CurrentDomain.BaseDirectory.Substring(0, AppDomain.CurrentDomain.BaseDirectory.IndexOf(":"));
            System.IO.Directory.SetCurrentDirectory(volume + ":\\kafka\\bin\\windows\\");

            WriteLog("kafka Service Start");
            startKafka();
        }
        #endregion

        #region stop service
        protected override void OnStop()
        {
            KillProcessAndChildren(kafkaProcess.Id);
            WriteLog("kafka  Service Stop\n");
        }
        #endregion

        #region 
        private void startKafka(Object sender = null, EventArgs e = null)
        {
            //kafkaProcess = StartProcess(Constants.KafkaProcess);
            WriteLog("startKafka start");
            string volume = AppDomain.CurrentDomain.BaseDirectory.Substring(0, AppDomain.CurrentDomain.BaseDirectory.IndexOf(":"));
            //string kafkaCommand = volume + ":\\kafka\\bin\\windows\\kafka-server-start.bat " + volume + ":\\kafka\\config\\server.properties";
            kafkaProcess = new Process();
            kafkaProcess.StartInfo.FileName = volume + ":\\kafka\\bin\\windows\\kafka-server-start.bat ";
            kafkaProcess.StartInfo.Arguments = volume + ":\\kafka\\config\\server.properties";
            kafkaProcess.StartInfo.CreateNoWindow = true;
            kafkaProcess.StartInfo.UseShellExecute = false;
            // Guardian to restart
            kafkaProcess.EnableRaisingEvents = true;
            kafkaProcess.Exited += new EventHandler(startKafka);
            // The process output
            kafkaProcess.StartInfo.RedirectStandardOutput = true;
            kafkaProcess.StartInfo.RedirectStandardError = true;
            kafkaProcess.OutputDataReceived += new DataReceivedEventHandler(MyProcOutputHandler);
            kafkaProcess.ErrorDataReceived += new DataReceivedEventHandler(MyProcOutputHandler);
            kafkaProcess.Start();
            kafkaProcess.BeginOutputReadLine();
            kafkaProcess.BeginErrorReadLine();
        }
        #endregion

        #region process call
        private static Process StartProcess(string command)
        {
            var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                Verb = "runas"
            };

            return Process.Start(processInfo);
        }
        #endregion

        #region kill process
        private static void KillProcessAndChildren(int pid)
        {
            using (var searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid))
            {
                var managementObjects = searcher.Get();

                foreach (var obj in managementObjects)
                {
                    var managementObject = (ManagementObject)obj;
                    KillProcessAndChildren(Convert.ToInt32(managementObject["ProcessID"]));
                }

                try
                {
                    var proc = Process.GetProcessById(pid);
                    proc.Kill();
                }
                catch (ArgumentException)
                {
                    // Process already exited.
                    WriteLog("ArgumentException", true);
                }
            }
        }
        #endregion

        #region  process out put
        private void MyProcOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                WriteLog(outLine.Data, false);
            }
        }
        #endregion

        #region write log
        private static void WriteLog(string logStr, bool wTime = true)
        {
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\KafkaAutoService.log", true))
            {
                string timeStr = wTime == true ? DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss ") : "";
                sw.WriteLine(timeStr + logStr);
            }
        }
        #endregion
    }
}
