using System;
using System.Linq;
using System.Text;
using System.Management;
using System.Collections.Generic;
using System.Collections.Specialized;
using nekoMonitor.SMART;

namespace nekoMonitor.Hardware
{
    class HDD
    {
        public static List<Dictionary<string, string>> GetSMART()
        {
            var list = new List<Dictionary<string, string>>();
            try
            {
                var dicDrives = Reader.read();
                foreach (var drive in dicDrives)
                {
                    var status = new Dictionary<string, string>();
                    status["smart.serial"] = drive.Value.Serial;
                    status["smart.model"] = drive.Value.Model;
                    status["smart.type"] = drive.Value.Type;
                    status["smart.status"] = (drive.Value.IsOK) ? "OK" : "BAD";

                    foreach (var attr in drive.Value.Attributes)
                    {
                        if (attr.Value.HasData)
                        {
                            status["smart.attr." + attr.Value.Attribute + ".name"] = attr.Value.Attribute;
                            status["smart.attr." + attr.Value.Attribute + ".Data"] = attr.Value.Data.ToString();
                            status["smart.attr." + attr.Value.Attribute + ".Current"] = attr.Value.Current.ToString();
                            status["smart.attr." + attr.Value.Attribute + ".Worst"] = attr.Value.Worst.ToString();
                            status["smart.attr." + attr.Value.Attribute + ".Threshold"] = attr.Value.Threshold.ToString();
                            status["smart.attr." + attr.Value.Attribute + ".status"] = (attr.Value.IsOK) ? "OK" : "BAD";
                        }
                    }
                    list.Add(status);
                }
                return list;
            }
            catch (ManagementException e)
            {
                return list;
            }
        }
    }
}