using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Tasks
{
    public class VM
    {
        static string _IPaddress = null;
        static string _Name = null;
        static string _fullPathOfVM = null;

        public VM(string Name)
        {
           // _IPaddress = IPaddress ?? throw new ArgumentNullException(nameof(IPaddress));
            _Name = Name ?? throw new ArgumentNullException(nameof(Name));
            if (!IsVmExist()) throw new Exception("VM : " + Name + " does not Exist");
            _IPaddress = GetIPAddress();
          

        }

        public enum Status
        {
            POWERED_ON,
            POWERED_OFF,
            RUNNING
        }

        public string Name => _Name;
        public string IP_Address => _IPaddress;

      

        public bool IsPoweredOn()
        { return true; }

        public Status GetStatus()
        {
            throw new NotImplementedException();
        }

        private string GetIPAddress()
        {
            if (!IsPoweredOn())
                throw new Exception("The VM is not powered On.");
            else
            {
                return new ProcessTasks().Start_ProcessSynchronous("GetIP", _fullPathOfVM);
            }
        }

        public void Start()
        {
            new ProcessTasks().Start_ProcessAsynchronous("StartVm", _fullPathOfVM);
        }
        public void Stop()
        {
            new ProcessTasks().Start_ProcessAsynchronous("StopVm", _fullPathOfVM);
        }
        public void Reboot()
        {
            new ProcessTasks().Start_ProcessAsynchronous("RebootVm", _fullPathOfVM);
        }

        public void Init()
        {
            new ProcessTasks().Start_ProcessAsynchronous("InitVm", _fullPathOfVM);
        }

    
      

        private static bool IsVmExist()
        {
            bool result = false;
            List<string> availablevmlist = new List<string>();
            if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VMPath")))
            {
                Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VMPath"));
                return false;
            }
            else
            {
                string[] subdirectoryEntries = Directory.GetDirectories(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VMPath"));
                foreach (string subdirectory in subdirectoryEntries)
                {
                    string[] fileEntries = Directory.GetFiles(subdirectory);
                    foreach (string fileName in fileEntries)
                    {
                        if (fileName.Contains("vmx"))
                        {
                            if (!fileName.Contains("vmxf"))
                            {
                                if (_Name == Path.GetFileNameWithoutExtension(fileName))
                                {
                                    result = true;
                                    _fullPathOfVM = fileName;
                                }
                                break;
                            }
                        }
                    }
                }
            }
            return result;
        }
    }

    internal class ProcessTasks
    {
        Process AsyncProcess = null;
        Process SyncProc = null;
        string _vmFullPath = string.Empty;

       
        private Process GetProcessObject()
        {
            Process proc = new Process();
            proc.StartInfo.WorkingDirectory = ConfigurationManager.AppSettings["VMCommandBrokerWorkingDirectory"];
            proc.StartInfo.FileName = ConfigurationManager.AppSettings["VMCommandBroker"];
            proc.StartInfo.CreateNoWindow = true;
            return proc;
        }

        public void Start_ProcessAsynchronous(string vmActionType, string VMFullPath, string SnapshotName=null, string RefVMImagePath=null)
        {
            AsyncProcess = GetProcessObject();
            Thread.Sleep(1000);
            switch (vmActionType)
            {
                case "StartVm":
                    AsyncProcess.StartInfo.Arguments = String.Format("{0} {1}", vmActionType, VMFullPath);
                    break;
                case "StopVm":
                    AsyncProcess.StartInfo.Arguments = String.Format("{0} {1}", vmActionType, VMFullPath);
                    break;
                case "RebootVm":
                    AsyncProcess.StartInfo.Arguments = String.Format("{0} {1}", vmActionType, VMFullPath);
                    break;
                case "InitVm":
                    AsyncProcess.StartInfo.Arguments = String.Format("{0} {1} {2}", vmActionType, VMFullPath, SnapshotName);
                    break;
                case "CreateVm":
                    if (!RegistryUtils.IsVMWaretoolsInstalled())
                        throw new Exception("VMWare tools not Installed in the Provider");
                    else if (RegistryUtils.GetPathofExe("VMware VIX") == string.Empty)
                        throw new Exception(" VMWare tools path not found");
                    else
                    {
                        AsyncProcess.StartInfo.Arguments = String.Format("{0} {1} {2} {3}", vmActionType, RegistryUtils.GetPathofExe("VMware VIX"), RefVMImagePath, VMFullPath);
                    }
                   
                    break;
                default:
                    Console.WriteLine("Default case");
                    break;
            }
            if (AsyncProcess.StartInfo.Arguments == null)
                throw new ArgumentNullException("Command is not in valid format");
            AsyncProcess.Start();

        }

        public string Start_ProcessSynchronous(string actionType, string VMFullPath)
        {
            SyncProc = GetProcessObject();
            switch (actionType)
            {
                case "GetIP":
                    SyncProc.StartInfo.Arguments = String.Format("{0} {1}", actionType, VMFullPath);
                    break;
                case "SnapShotList":
                    SyncProc.StartInfo.Arguments = String.Format("{0} {1}", actionType, VMFullPath);
                    break;
                default:
                    Console.WriteLine("Default case");
                    break;

            }
            SyncProc.Start();
            SyncProc.WaitForExit();
            string output = SyncProc.StandardOutput.ReadToEnd();
            output = output.Replace("\r\n", "").Trim();
            return output;
        }
    }


}