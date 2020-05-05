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
            XmlDocument doc = new XmlDocument();
            doc.Load(@"C:\ProgramData\SpectrumMonitor\config.xml");
            Simulation = doc.SelectSingleNode("StartupConfig/Simulation").InnerText.ToUpper() == "TRUE" ? true : false;
            MainUI = doc.SelectSingleNode("StartupConfig/MainUI").InnerText.ToUpper();
            NeedLogin = doc.SelectSingleNode("StartupConfig/NeedLogIn").InnerText.ToUpper() == "TRUE" ? true : false;

        }


        public static bool Simulation { get; private set; } = false;
        public static string MainUI { get; private set; } = "";
        public static bool NeedLogin { get; private set; } = false;

    }

}
