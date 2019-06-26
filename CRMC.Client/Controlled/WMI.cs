using CRMC.Common.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CRMC.Client.Controlled
{
    public static class WMIHelper
    {
        public static List<string> namespaces = new List<string>();
        private static bool startGettingNamespaces = false;
        public static void SendNamespaces(Guid aId)
        {
            if (startGettingNamespaces)
            {
                Telnet.Instance.Send( new CommandContent(Common.ApiCommand.WMI_Namespace,aId, Global.CurrentClient.Id, namespaces));
                return;
            }
            startGettingNamespaces = true;
            // List<string> items = new List<string>();
            GetNamespaces("root");
            // return items.ToArray();

            Task GetNamespaces(string root)
            {
                return Task.Run(async () =>
                {
                    // Enumerates all WMI instances of 
                    // __namespace WMI class.
                    ManagementClass nsClass =
                        new ManagementClass(
                        new ManagementScope(root),
                        new ManagementPath("__namespace"),
                        null);
                    ManagementObjectCollection n = null;
                    try
                    {
                        foreach (ManagementObject ns in nsClass.GetInstances())
                        {
                            // Add namespaces to the list.
                            string namespaceName = root + "\\" + ns["Name"].ToString();
                            // items.Add(namespaceName);
                            namespaces.Add(namespaceName);
                            Telnet.Instance.Send( new CommandContent(Common.ApiCommand.WMI_Namespace,aId, Global.CurrentClient.Id, new string[] { namespaceName }));

                            await GetNamespaces(namespaceName);

                        }
                    }
                    catch (Exception ex)
                    {

                    }
                });
            }

        }
        public static void SendWMIClasses(string @namespace,Guid aId)
        {
            Task.Run(() =>
            {
                var items = GetWMIClasses(@namespace).classes;
                var classes = from x in items select new WMIClassInfo() { Namespace = @namespace, Class = x };

                Telnet.Instance.Send( new CommandContent(Common.ApiCommand.WMI_Classes,aId, Global.CurrentClient.Id,classes.ToArray()));
            });
        }

        public static (string[] classes, string[] methodClasses, string[] eventClasses) GetWMIClasses(string @namespace)
        {
            List<string> classList = new List<string>();
            List<string> classMethodList = new List<string>();
            List<string> classEventList = new List<string>();
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(new ManagementScope(
                   @namespace), new WqlObjectQuery("select * from meta_class"), null);

                foreach (ManagementClass wmiClass in searcher.Get())
                {
                    if (wmiClass.Derivation.Contains("__Event"))
                    {
                        classEventList.Add(wmiClass["__CLASS"].ToString());
                    }
                    foreach (QualifierData qd in wmiClass.Qualifiers)
                    {
                        if (qd.Name.Equals("dynamic") || qd.Name.Equals("static"))
                        {
                            classList.Add(wmiClass["__CLASS"].ToString());
                            if (wmiClass.Methods.Count > 0)
                            {
                                classMethodList.Add(wmiClass["__CLASS"].ToString());
                            }
                        }

                    }
                }

            }
            catch
            {
            }

            return (classList.ToArray(), classMethodList.ToArray(), classEventList.ToArray());
        }

        public static void SendProperties(WMIClassInfo wmi,Guid aId)
        {
            Task.Run(() =>
            {
                Telnet.Instance.Send(new CommandContent(Common.ApiCommand.WMI_Props, aId, Global.CurrentClient.Id, GetProperties(wmi)));
            });
        }

        public static WMIObjectCollection GetProperties(WMIClassInfo wmi)
        {
       
            WMIObjectCollection collection = new WMIObjectCollection()
            {
                Namespace = wmi.Namespace,
                Class = wmi.Class
            };
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmi.Namespace,
                "select * from " + wmi.Class);
            int i = 0;
            ManagementObjectCollection result = null;
            try
            {
                 result = searcher.Get();
                foreach (ManagementObject obj in result)
                {
                    WMIPropertyCollection props = new WMIPropertyCollection()
                    {
                        Index = ++i,
                        Name = obj.GetPropertyValue("Name").ToString(),
                    };
                    collection.Add(props);
                    foreach (PropertyData prop in obj.Properties)
                    {
                        string value = null;
                        if(prop.Name=="MUILanguages")
                        {

                        }
                        if(prop.Value is string[])
                        {
                            value = string.Join(" | ", prop.Value as string[]);
                        }
                        else
                        {
                            value = prop?.Value?.ToString();
                        }
                        props.Add(new WMIPropertyInfo() { Name = prop.Name, Value =value});
                    }
                }
            }
            catch(Exception ex)
            {
                collection.Add(new WMIPropertyCollection() { Name = "发生异常：" + ex.Message });
            }
           
            return collection;
        }
    }
}
