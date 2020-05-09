using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SpectrumMonitor
{
    public class Configuration
    {
        public static string MAINUI_REGCONTROL = "REGCONTROL";
        public static string MAINUI_ADDRESSACCESS = "ADDRESSACCESS";

        public static void Initialize()
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(@"C:\ProgramData\SpectrumMonitor\config.xml");
                Simulation = doc.SelectSingleNode("StartupConfig/Simulation").InnerText.ToUpper() == "TRUE"
                    ? true
                    : false;
                MainUI = doc.SelectSingleNode("StartupConfig/MainUI").InnerText.ToUpper();
                NeedLogin = doc.SelectSingleNode("StartupConfig/NeedLogIn").InnerText.ToUpper() == "TRUE"
                    ? true
                    : false;
                string driverOptionstring = MainUI = doc.SelectSingleNode("StartupConfig/DriverOptions").InnerText;
                var optionstrings = driverOptionstring.Split(',');
                foreach (var optionstring in optionstrings)
                {
                    var opt = optionstring.Replace(" ", "");
                    var optpair = opt.Split('=');
                    if (optpair.Length == 2)
                    {
                        DriverOption.Add(optpair[0], optpair[1]);
                    }
                }
            }
            catch (Exception e)
            {
            }
        }


        public static bool Simulation { get; private set; } = false;
        public static string MainUI { get; private set; } = "";
        public static bool NeedLogin { get; private set; } = false;

        public static Dictionary<string,string> DriverOption { get; private set; } = new Dictionary<string, string>();

    }

}
