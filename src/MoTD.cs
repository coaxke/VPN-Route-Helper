using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace VPNRouteHelper
{
    class MoTD
    {
        public void ShowMoTd(string LocalConfigFile)
        {
            try
            {
                XmlDocument VpnconfigDoc = new XmlDocument();
                VpnconfigDoc.Load(LocalConfigFile);
                XmlNodeList MoTDs = VpnconfigDoc.SelectNodes("/VPN/Messages/MoTD");

                foreach (XmlNode MoTD in MoTDs)
                {
                    XmlAttribute Display = MoTD.Attributes["Display"];
                    XmlAttribute TitleMessage = MoTD.Attributes["TitleMessage"];
                    XmlAttribute BodyMessage = MoTD.Attributes["BodyMessage"];

                    if (Display.Value.ToString().ToLower() == "true")
                    {
                        Console.WriteLine("Displaying Message of the day to Screen");

                        MessageBox.Show(BodyMessage.Value.ToString(), TitleMessage.Value.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error when showing MoTD: {0} : {1}", e.Message, e.InnerException);
            }
        }
    }
}
