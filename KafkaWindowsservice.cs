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
        private Process kafkaProcess = null;

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            string volume = AppDomain.CurrentDomain.BaseDirectory.Substring(0, AppDomain.CurrentDomain.BaseDirectory.IndexOf(":"));
            System.IO.Directory.SetCurrentDirectory(volume + ":\\kafka\\bin\\windows\\");

            wLog("kafka Service Start");
            startKafka();
        }

        protected override void OnStop()
        {
            KillProcessAndChildren(kafkaProcess.Id);
            wLog("kafka  Service Stop\n");
        }

       
        private void startKafka(Object sender = null, EventArgs e = null)
        {
            //kafkaProcess = StartProcess(Constants.KafkaProcess);
            wLog("startKafka start");
            string volume= AppDomain.CurrentDomain.BaseDirectory.Substring(0, AppDomain.CurrentDomain.BaseDirectory.IndexOf(":"));
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
                    wLog("ArgumentException",true);
                }
            }
        }
        private void MyProcOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                wLog(outLine.Data, false);
            }
        }
        private static void wLog(string logStr, bool wTime = true)
        {
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\KafkaAutoService.log", true))
            {
                string timeStr = wTime == true ? DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss ") : "";
                sw.WriteLine(timeStr + logStr);
            }
        }

    }
}
