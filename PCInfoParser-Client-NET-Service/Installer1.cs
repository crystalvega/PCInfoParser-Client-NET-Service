using System.ComponentModel;
using System.ServiceProcess;

namespace PCInfoParser_Client_NET_Service
{
    [RunInstaller(true)]
    public partial class Installer1 : System.Configuration.Install.Installer
    {
        ServiceInstaller serviceInstaller;
        ServiceProcessInstaller processInstaller;
        public Installer1()
        {
            InitializeComponent();
        }

    }


}
