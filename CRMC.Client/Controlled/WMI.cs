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
        public static void SendNamespaces(CommandBody cmd)
        {
            if (startGettingNamespaces)
            {
                Telnet.Instance.Send( new CommandBody(Common.ApiCommand.WMI_Namespace,cmd.AId, Global.CurrentClient.ID, namespaces));
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
                    try
                    {
                        foreach (ManagementObject ns in nsClass.GetInstances())
                        {
                            // Add namespaces to the list.
                            string namespaceName = root + "\\" + ns["Name"].ToString();
                            // items.Add(namespaceName);
                            namespaces.Add(namespaceName);
                            Telnet.Instance.Send( new CommandBody(Common.ApiCommand.WMI_Namespace,cmd.AId, Global.CurrentClient.ID, new string[] { namespaceName }));

                            await GetNamespaces(namespaceName);

                        }
                    }
                    catch (Exception ex)
                    {

                    }
                });
            }

        }
        public static void SendWMIClasses(string @namespace, CommandBody cmd)
        {
            Task.Run(() =>
            {
                var items = GetWMIClasses(@namespace).classes;
                var classes = from x in items select new WMIClassInfo() { Namespace = @namespace, Class = x };

                Telnet.Instance.Send( new CommandBody(Common.ApiCommand.WMI_Classes,cmd.AId, Global.CurrentClient.ID,classes.ToArray()));
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

        public static void SendProperties(WMIClassInfo wmi, CommandBody cmd)
        {
            Task.Run(() =>
            {
                Telnet.Instance.Send(new CommandBody(Common.ApiCommand.WMI_Props, cmd.AId, Global.CurrentClient.ID, GetProperties(wmi)));
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
                        if(prop.Name=="Capabilities")
                        {

                        }
                        if (prop.Value != null)
                        {
                            if (prop.IsArray)
                            {
                                switch (prop.Type)
                                {
                                    case CimType.None:
                                        value = null;
                                        break;
                                    case CimType.SInt8:
                                        value = string.Join(" | ", prop.Value as sbyte[]);
                                        break;
                                    case CimType.UInt8:
                                        value = string.Join(" | ", prop.Value as byte[]);
                                        break;
                                    case CimType.SInt16:
                                        value = string.Join(" | ", prop.Value as short[]);
                                        break;
                                    case CimType.UInt16:
                                        value = string.Join(" | ", prop.Value as ushort[]);
                                        break;
                                    case CimType.SInt32:
                                        value = string.Join(" | ", prop.Value as int[]);
                                        break;
                                    case CimType.UInt32:
                                        value = string.Join(" | ", prop.Value as uint[]);
                                        break;
                                    case CimType.SInt64:
                                        value = string.Join(" | ", prop.Value as long[]);
                                        break;
                                    case CimType.UInt64:
                                        value = string.Join(" | ", prop.Value as ulong[]);
                                        break;
                                    case CimType.Real32:
                                        value = string.Join(" | ", prop.Value as float[]);
                                        break;
                                    case CimType.Real64:
                                        value = string.Join(" | ", prop.Value as double[]);
                                        break;
                                    case CimType.Boolean:
                                        value = string.Join(" | ", prop.Value as bool[]);
                                        break;
                                    case CimType.String:
                                        value = string.Join(" | ", prop.Value as string[]);
                                        break;
                                    case CimType.DateTime:
                                        value = string.Join(" | ", prop.Value as DateTime[]);
                                        break;
                                    case CimType.Reference:
                                        value = string.Join(" | ", prop.Value as short[]);
                                        break;
                                    case CimType.Char16:
                                        value = string.Join(" | ", prop.Value as char[]);
                                        break;
                                    case CimType.Object:
                                        value = string.Join(" | ", prop.Value as object[]);
                                        break;
                                }
                            }
                            else
                            {
                                value = prop?.Value?.ToString();
                            }
                        }
                        props.Add(new WMIPropertyInfo() { Name = prop.Name, Value =value});
                    }
                }
            }
            catch (Exception ex)
            {
                collection.Add(new WMIPropertyCollection() { Name = "发生异常：" + ex.Message });
            }
           
            return collection;
        }
    }
}
