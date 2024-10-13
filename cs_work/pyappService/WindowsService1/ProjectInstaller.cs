using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace WindowsService1
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {        
        public ProjectInstaller()
        {
            InitializeComponent();

            var config = System.Configuration.ConfigurationManager.OpenExeConfiguration(this.GetType().Assembly.Location);
            if (config.AppSettings.Settings["ServiceName"] != null)
            {
                this.serviceInstaller1.ServiceName = config.AppSettings.Settings["ServiceName"].Value;
            }
            if (config.AppSettings.Settings["DisplayName"] != null)
            {
                this.serviceInstaller1.DisplayName = config.AppSettings.Settings["DisplayName"].Value;
            }
            /*
            var appSettings = System.Configuration.ConfigurationManager.AppSettings;
            if (appSettings["ServiceName"] != null)
            {
                this.serviceInstaller1.ServiceName = appSettings["ServiceName"];
            }
            if (appSettings["DisplayName"] != null)
            {
                this.serviceInstaller1.DisplayName = appSettings["DisplayName"];
            }
            */
        }
    }
}
