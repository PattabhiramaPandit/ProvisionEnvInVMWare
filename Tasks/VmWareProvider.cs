using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading;

namespace Tasks
{

    public class Credentials
    {
        public string username;
        public string password;

        public Credentials(string username, string password)
        {
            this.username = username ?? throw new ArgumentNullException(nameof(username));
            this.password = password ?? throw new ArgumentNullException(nameof(password));
        }
    }
    public class VMImage
    {
        public string Name = string.Empty;
        public string VMCreationPath = string.Empty;

        public enum ImageType
        {
            VM_WIN_10,
            VM_WIN_07
        }

        private readonly string _imagePath = string.Empty;

        public VMImage(string nameFortheVMtobeCreated, ImageType imgtype)
        {
            if (nameFortheVMtobeCreated == null)
            {
                throw new ArgumentNullException(nameof(nameFortheVMtobeCreated));

            }
            Name = nameFortheVMtobeCreated;

            _imagePath = Path.Combine(ConfigurationManager.AppSettings["ImagePath"],
                         Enum.GetName(imgtype.GetType(), imgtype) + Enum.GetName(imgtype.GetType(), imgtype) + ".VMX");
            if (!File.Exists(_imagePath))
            {
                throw new FileNotFoundException("Reference Image is not Found" + " " + _imagePath);
            }

            if (!Directory.Exists(ConfigurationManager.AppSettings["VMCreationPath"]))
            {
                Directory.CreateDirectory(ConfigurationManager.AppSettings["VMCreationPath"]);
            }

            VMCreationPath = ConfigurationManager.AppSettings["VMCreationPath"] + "\\" + Name + "\\" + Name + ".vmx";

        }

        public string ImagePath => _imagePath;




    }
    public class VmWareProvider
    {
        private readonly Credentials hostcredentials;
        private readonly string IpAddressofProvider;
        private readonly string ProviderName;
        private static SystemInfo _systemInfo = null;
        private static Capacity _capacity = null;

        public enum Status
        {
            DISCONNECTED,
            POWERED_ON
        }

        internal class SystemInfo
        {
            private readonly Dictionary<string, string> _diskspace;
            private readonly int _RAM;
            private readonly int _Core;
            private readonly string _OS;

            public SystemInfo(Credentials credentials)
            {

                _diskspace = TotalHd_AvailableHd();
                _RAM = Convert.ToInt32(WMITasks.GetManagementObjectValue("RAM", WMITasks.EnvironmentType.PROVIDER, credentials) ?? throw new NullReferenceException(nameof(_RAM)));
                _Core = Convert.ToInt32(WMITasks.GetManagementObjectValue("CORE", WMITasks.EnvironmentType.PROVIDER, credentials) ?? throw new ArgumentNullException(nameof(_Core)));
                _OS = WMITasks.GetManagementObjectValue("OS", WMITasks.EnvironmentType.PROVIDER, credentials) ?? throw new ArgumentNullException(nameof(_OS));
            }

            public Dictionary<string, string> diskspace => _diskspace;
            public int RAM => _RAM;
            public int Core => _Core;
            public string OS => _OS;


            /// <summary>
            /// TotalHd_AvailableHd
            /// </summary>
            /// <returns></returns>
            private Dictionary<string, string> TotalHd_AvailableHd()
            {
                Dictionary<string, string> data = new Dictionary<string, string>();
                DriveInfo[] allDrives = DriveInfo.GetDrives();
                foreach (DriveInfo dInfo in allDrives)
                {
                    if (dInfo.IsReady == true)
                    {
                        string driveName = dInfo.Name; // C:\, E:\, etc:\
                        DriveType driveType = dInfo.DriveType;
                        switch (driveType)
                        {
                            case System.IO.DriveType.Fixed:
                                if (!driveName.ToLower().Contains("C"))
                                {
                                    Console.WriteLine("Available space to current user:{0, 15} bytes", dInfo.AvailableFreeSpace);
                                    double total = Convert.ToDouble(dInfo.AvailableFreeSpace / (1024 * 1024));
                                    int t = Convert.ToInt32(Math.Ceiling(total / 1024).ToString());
                                    string memory = t.ToString();// ram detail
                                    Console.WriteLine("total available space" + " " + memory);
                                    data.Add("AvailableDiskSpace", Convert.ToInt32(memory).ToString());

                                    total = Convert.ToDouble(dInfo.TotalSize / (1024 * 1024));
                                    t = Convert.ToInt32(Math.Ceiling(total / 1024).ToString());
                                    memory = t.ToString();
                                    Console.WriteLine("Total size of drive" + " " + memory);
                                    data.Add("TotalDiskSpace", Convert.ToInt32(memory).ToString());

                                }
                                break;
                        }

                    }
                }
                return data;
            }

        }

        internal class Capacity
        {
            public OccupiedCapacity occupiedCapacity { get; set; }

            internal class OccupiedCapacity
            {
                private static int s_ram;
                private static int s_totalHD;
                private static int _core;

                public static int Ram { get => s_ram; set => s_ram = value; }
                public static int TotalHD { get => s_totalHD; set => s_totalHD = value; }

                public static int Cores
                {
                    get => _core;
                    set => _core = value;
                }
            }

            internal class AvailableCapacity
            {
                public int AvailableRamInProvider => _systemInfo.RAM - Convert.ToInt32(ConfigurationManager.AppSettings["ReserveRAM"]);
                public int AvailablediskSpaceInProvider => Convert.ToInt32(_systemInfo.diskspace) - Convert.ToInt32(ConfigurationManager.AppSettings["ReserveHD"]);
                public int AvailableCoreInProvider => _systemInfo.Core - Convert.ToInt32(ConfigurationManager.AppSettings["ReserveCore"]);

            }
            internal class TemplateForVMClone
            {
                public static int Ram = Convert.ToInt32(ConfigurationManager.AppSettings["BaseVMRAM"]);
                public static int DiskSpace = Convert.ToInt32(ConfigurationManager.AppSettings["BaseVMDiskSpace"]);
                public static int Core = Convert.ToInt32(ConfigurationManager.AppSettings["BaseVmCore"]);



            }

            private AvailableCapacity avlblCapacity = new AvailableCapacity();



            public int VMsPossible
            {
                get
                {
                    int NoOfVMs_RAM = (avlblCapacity.AvailableRamInProvider - OccupiedCapacity.Ram) / TemplateForVMClone.Ram;
                    int NoOfVMs_Diskspace = (avlblCapacity.AvailablediskSpaceInProvider - OccupiedCapacity.TotalHD) / TemplateForVMClone.DiskSpace;
                    int NoOfVMs_Core = (avlblCapacity.AvailableCoreInProvider - OccupiedCapacity.Cores) / TemplateForVMClone.Core;
                    return Math.Min(NoOfVMs_RAM, Math.Min(NoOfVMs_Diskspace, NoOfVMs_Core));
                }
            }




        }

        public void CloneVM(string nameoftheVMtobeCreated, VMImage.ImageType imgType)
        {
            VMImage referenceVM = new VMImage(nameoftheVMtobeCreated, imgType);

            if (_capacity.VMsPossible > 0)
            {
                new ProcessTasks().Start_ProcessAsynchronous("CloneVM", referenceVM.VMCreationPath, referenceVM.ImagePath);
                Capacity.OccupiedCapacity.Ram = Capacity.OccupiedCapacity.Ram + Capacity.TemplateForVMClone.Ram;
                Capacity.OccupiedCapacity.TotalHD = Capacity.OccupiedCapacity.TotalHD + Capacity.TemplateForVMClone.DiskSpace;
                Capacity.OccupiedCapacity.Cores = Capacity.OccupiedCapacity.Cores + Capacity.TemplateForVMClone.Core;
            }
        }

        public void DeleteVM(string nameOfTheVmTobeDeleted)
        {
            vmList = GetAvailableVmList();
            foreach (VM vm in vmList)
            {
                if (vm.Name == nameOfTheVmTobeDeleted)
                {
                    if (vm.IsPoweredOn() == true)
                    {
                        vm.Init();
                        Thread.Sleep(10000);
                    }

                    Directory.Delete(ConfigurationManager.AppSettings["ImageCreationPath"] + "\\" + nameOfTheVmTobeDeleted + "\\", true);
                    Thread.Sleep(10000);
                    Capacity.OccupiedCapacity.Ram = Capacity.OccupiedCapacity.Ram + Capacity.TemplateForVMClone.Ram;
                    Capacity.OccupiedCapacity.TotalHD = Capacity.OccupiedCapacity.TotalHD + Capacity.TemplateForVMClone.DiskSpace;
                    Capacity.OccupiedCapacity.Cores = Capacity.OccupiedCapacity.Cores + Capacity.TemplateForVMClone.Core;

                }
            }
        }

        private List<VM> GetAvailableVmList()
        {
            List<VM> availablevmlist = new List<VM>();
            string[] subdirectoryEntries = Directory.GetDirectories(ConfigurationManager.AppSettings["ImageCreationPath"]);
            foreach (string subdirectory in subdirectoryEntries)
            {
                string[] fileEntries = Directory.GetFiles(subdirectory);
                foreach (string fileName in fileEntries)
                {
                    if (fileName.Contains("vmx"))
                    {
                        if (!fileName.Contains("vmxf"))
                        {
                            availablevmlist.Add(new VM(Path.GetFileNameWithoutExtension(fileName)));
                        }
                    }
                }
            }
            return availablevmlist;
        }

        public Status GetStatus()
        {
            return Status.POWERED_ON;
        }

        public List<VM> vmList = new List<VM>();

        public VmWareProvider(Credentials credentials, string iPAddress, string hostname)
        {
            hostcredentials = credentials ?? throw new ArgumentNullException(nameof(credentials));

            PingReply reply = new Ping().Send(iPAddress);
            if (reply.Status != IPStatus.Success)
            {
                throw new PingException("New Provider Not Connecting");
            }

            IpAddressofProvider = iPAddress ?? throw new ArgumentNullException(nameof(iPAddress));
            ProviderName = hostname ?? throw new ArgumentNullException(nameof(hostname));
            _systemInfo = new SystemInfo(hostcredentials);


        }


    }
}
