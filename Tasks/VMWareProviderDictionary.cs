using System;
using System.Collections.Generic;


namespace Tasks
{
    public class VMWareProviderDictionary
    {
        Dictionary<string, VmWareProvider> ProviderList = new Dictionary<string, VmWareProvider>();

        public VMWareProviderDictionary(Dictionary<string, VmWareProvider> providerList)
        {
            ProviderList = providerList ?? throw new ArgumentNullException(nameof(providerList));
        }

        public void AddProvider(VmWareProvider provider)
        {
            
        }

        public void DeleteProvider(string IPAddress)
        {

        }

        public string[] GetProviderList()
        {
            string[] provList = new string[] { };
            Dictionary<string,VmWareProvider>.KeyCollection collection= ProviderList.Keys;
            collection.CopyTo(provList, 0);
            return provList;
        }
    }

   
}