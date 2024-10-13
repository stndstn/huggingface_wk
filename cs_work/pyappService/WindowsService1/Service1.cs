using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace PyAppService
{
    public partial class Service1 : ServiceBase
    {
        Process processCmd;
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            //System.Diagnostics.Debugger.Launch();
            String modulePath = Process.GetCurrentProcess().MainModule.FileName;
            FileInfo fModule = new FileInfo(modulePath);
            String moduleDirPath = fModule.Directory.FullName;
            string startBatFileName = "start_winpy_app_service.bat";
            var config = System.Configuration.ConfigurationManager.OpenExeConfiguration(this.GetType().Assembly.Location);
            if (config.AppSettings.Settings["StartBatFileName"] != null)
            {
                startBatFileName = config.AppSettings.Settings["StartBatFileName"].Value;
            }

            String batFilePath = moduleDirPath + "\\" + startBatFileName;
            //String winPyDir = fModule.Directory.Parent.FullName + "\\WPy64-3830";
            ProcessStartInfo psi = new ProcessStartInfo(batFilePath);
            psi.WorkingDirectory = moduleDirPath;
            psi.UseShellExecute = false;
            processCmd = Process.Start(psi);
        }

        protected override void OnStop()
        {
            if (!processCmd.HasExited)
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                   "SELECT ProcessId, ParentProcessId FROM Win32_Process WHERE ParentProcessId=" + processCmd.Id);
                ManagementObjectCollection collection = searcher.Get();
                foreach (var item in collection)
                {
                    // Find the parent and child in the dictionary.
                    int child_id = Convert.ToInt32(item["ProcessId"]);
                    int parent_id = Convert.ToInt32(item["ParentProcessId"]);
                    Process processChild = Process.GetProcessById(child_id);
                    processChild.Kill();
                }
            }
            if (!processCmd.HasExited)  //cmd process may exit after started child process exit
            {
                processCmd.Kill();
            }
        }
    }
}
