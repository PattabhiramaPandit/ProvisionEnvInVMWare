using Microsoft.Win32;
using System;
using System.Management;
using System.Net;

namespace Tasks
{
    internal class WMITasks
    {
        private static string _VmIP = string.Empty;
        private static readonly IPAddress _address = IPAddress.None;
        public enum EnvironmentType
        {
            PROVIDER, VM
        }

        private static ManagementScope CreateNewManagementScope(string VmIP, Credentials _credentials)
        {
            _VmIP = VmIP ?? throw new ArgumentNullException(nameof(VmIP));
            if (_address != IPAddress.Parse(_VmIP))
            {
                throw new FormatException("IP Address not in correct format");
            }

            string serverString = @"\\" + _VmIP + @"\root\cimv2";
            ManagementScope scope = new ManagementScope(serverString);
            ConnectionOptions options = new ConnectionOptions
            {
                Username = _credentials.username ?? throw new ArgumentNullException(nameof(_credentials.username)),
                Password = _credentials.password ?? throw new ArgumentNullException(nameof(_credentials.password)),
                Impersonation = ImpersonationLevel.Impersonate,
                Authentication = AuthenticationLevel.PacketPrivacy
            };
            scope.Options = options;
            return scope;
        }


        public static string GetManagementObjectValue(string key, EnvironmentType type, Credentials credentials, string IPaddress = null)
        {
            string Data = string.Empty;
            ManagementScope scope = null;
            ManagementObjectSearcher searcher = null;
            SelectQuery query = new SelectQuery("select * from Win32_OperatingSystem");
            if (type == EnvironmentType.VM)
            {
                scope = CreateNewManagementScope(IPaddress, credentials);
                searcher = new ManagementObjectSearcher(scope, query);
            }
            else
            {
                searcher = new ManagementObjectSearcher(query);
            }

            using (searcher)
            {
                ManagementObjectCollection services = searcher.Get();
                foreach (ManagementObject mo in services)
                {
                    PropertyDataCollection searcherProperties = mo.Properties;
                    switch (key)
                    {
                        case "OS":
                            if (mo["Caption"] != null)
                            {
                                Data = mo["Caption"].ToString();
                            }
                            break;
                        case "RAM":
                            if (mo["TotalPhysicalMemory"] != null)
                            {
                                ulong total = Convert.ToUInt64(mo["TotalPhysicalMemory"]);
                                double toal = Convert.ToDouble(total / (1024 * 1024));
                                int ram = Convert.ToInt32(Math.Ceiling(toal / 1024).ToString());
                                Data = ram.ToString();

                            }
                            break;
                        case "CORE":
                            int coreCount = 0;
                            coreCount += int.Parse(mo["NumberOfCores"].ToString());
                            Data = coreCount.ToString();
                            break;
                    }
                }
            }
            return Data;
        }

       

    }

    internal class RegistryUtils
    {
        public static bool IsVMWaretoolsInstalled()
        {
            foreach (var item in Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall").GetSubKeyNames())
            {

                object programName = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\" + item).GetValue("DisplayName");
                if (programName != null)
                    if (string.Equals(programName, "VMware VIX"))
                    {
                        return true;
                    }
            }
            return false;
        }

        public static string GetPathofExe(string filename)
        {
            foreach (var item in Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall").GetSubKeyNames())
            {

                object programName = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\" + item).GetValue("DisplayName");
                if (programName != null)
                    if (string.Equals(programName, "VMware VIX"))
                    {
                        return Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\" + item).GetValue("InstallLocation").ToString();
                    }
            }
            return string.Empty;
        }
    }
}

