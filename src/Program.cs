using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace VPNRouteHelper
{
    class Program
    {
        static void Main(string[] args)
        {
            //If No Switch is passed (anything) then we will hide the console window from users view.
            if (args.Length == 0)
            {
                IntPtr winHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                HideWindow(winHandle, 0);
            }

            //Generate GUID to beat Caching by XmlDocument.Load();
            string WebRequestGUID = Guid.NewGuid().ToString();
            string WebConfigURL = "http://extranet.resdevops.com/VPNConfig.xml?=" + WebRequestGUID;
            string LocalConfigFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VPNConfig.xml");

            Console.WriteLine("========================================================");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("VPN Route Helper Tool");
            Console.ResetColor();
            Console.WriteLine("========================================================\n\n");

            //Check if we need to update Config Files
            Console.WriteLine("Begining Check for File Versions\n");
            RouteConfigUpdater objCheckLocalVersion = new RouteConfigUpdater();
            int versioncheck = objCheckLocalVersion.CheckLocalVersion(WebConfigURL, LocalConfigFile);

            //CHECK config file exists before executing the next statement.
            if (versioncheck == 1)
            {
                if (!File.Exists(LocalConfigFile))
                {
                    Console.WriteLine("No local VPN Route Config file exists - We're going to have to connect using default routes supplied by the VPN concentrator - Some subnets may be inaccessable");
                }
                else
                {
                    //Check the local version of the file regardless...
                    if (objCheckLocalVersion.ValidateXMLDocument(LocalConfigFile).Item1)
                    {
                        //Set routes for VPN Connection
                        Console.WriteLine("Setting Routes");
                        SetRoute objSetRoute = new SetRoute();
                        int RoutesSet = objSetRoute.setroutes(LocalConfigFile);

                        if (RoutesSet == 1)
                        {
                            //Return a MoTD to the user if one exists.
                            MoTD objMoTD = new MoTD();
                            objMoTD.ShowMoTd(LocalConfigFile);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Unable to parse/load local VPN Route config file for destination networks. \n" +
                                  "VPN Will be connected with default routes supplied by the VPN Concentrator. \n",
                                  "Error Loading VPN Config file.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Unable to parse/load VPN Route config file for destination networks. \n" +
                          "VPN Will be connected with default routes supplied by the VPN Concentrator. \n",
                           "Error Loading VPN Config file.", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Console.WriteLine("All Done.");
        }

        //Import user32.dll to allow us to hide the Console window after program invoked.
        [DllImport("user32.dll")]
        public static extern bool HideWindow(IntPtr hWnd, int nCmdShow);
    }
}
