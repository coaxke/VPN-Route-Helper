using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.NetworkInformation;
using System.Xml;
using System.Diagnostics;

namespace VPNRouteHelper
{
    class SetRoute
    {
        public int setroutes(string LocalConfigFile)
        {
            //Fetch Default gateway
            string DefaultGateway = GetVPNDefaultGateway(LocalConfigFile);

            if (String.IsNullOrEmpty(DefaultGateway))
            {
                Console.WriteLine("Could not retrieve a default gateway for some reason; We're going to have to stop here, we can't set a route");
                return 0;
            }
            else
            {
                //Parse Route XML File
                Dictionary<string, string> routes = ParseRoutes(LocalConfigFile);

                //Go ahead and set Route's now by calling "route add"
                try
                {
                    foreach (KeyValuePair<string, string> route in routes)
                    {
                        Console.WriteLine("Setting route using following Params: add " + route.Key + " mask " + route.Value + " " + DefaultGateway);
                        Process SetRoute = new Process();
                        SetRoute.StartInfo.UseShellExecute = false;
                        SetRoute.StartInfo.FileName = "route ";
                        SetRoute.StartInfo.Arguments = ("add " + route.Key + " mask " + route.Value + " " + DefaultGateway);
                        //SetRoute.StartInfo.RedirectStandardOutput = true;
                        //SetRoute.StartInfo.StandardErrorEncoding = Encoding.ASCII;
                        SetRoute.Start();
                        // Console.WriteLine(SetRoute.StandardOutput.ReadToEnd());
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error when setting non-persistant static-route \n {0}\n{1}", e.Message, e.StackTrace);

                }
                return 1;
            }
        }


        private Dictionary<string, string> ParseRoutes(string LocalConfigFile)
        {
            //Instantiate Dictionary to store Netmask & Subnet.
            Dictionary<string, string> routes = new Dictionary<string, string>();

            //Load XML Doc and Select routes using Xpath.
            XmlDocument VpnconfigDoc = new XmlDocument();
            VpnconfigDoc.Load(LocalConfigFile);
            XmlNodeList XMLRoutes = VpnconfigDoc.SelectNodes("/VPN/Routes/Route");

            //Itterate over each route in XML file and add to Routes Dictionary
            foreach (XmlNode XMLRoute in XMLRoutes)
            {
                XmlAttribute netmask = XMLRoute.Attributes["netmask"];
                XmlAttribute subnet = XMLRoute.Attributes["subnet"];
                XmlAttribute desciption = XMLRoute.Attributes["description"];

                //Add Each route (Netmask and subnet) to a new Index in Dictionary
                routes.Add(netmask.Value.ToString(), subnet.Value.ToString());
            }

            //Return our Dictionary of Routes
            return routes;
        }


        private string GetVPNDefaultGateway(string LocalConfigFile)
        {
            // Setup a <List> of IP addresses as we dont know how many PPP interfaces the users will have active.
            List<string> VpnIPAddresses = new List<string>();
            int IPaddressCount = 0;

            //Loop through network interfaces present on the system (both Physical and virtual)
            foreach (NetworkInterface NetInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                //Test if Interface is a PPP adaptor (VPN Adaptor)
                if (NetInterface.NetworkInterfaceType == NetworkInterfaceType.Ppp)
                    foreach (UnicastIPAddressInformation ip in NetInterface.GetIPProperties().UnicastAddresses)
                    {
                        //Test if we have an IPv4 address returned to us
                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            //Add Interface to <List> and increment the count.
                            Console.WriteLine("Found potential IP an address for VPN Tunnel : {0}", ip.Address.ToString());
                            VpnIPAddresses.Add(ip.Address.ToString());
                            IPaddressCount++;
                        }
                    }
            }

            Console.WriteLine("Found {0} PPP Interface IP Address(es)...", IPaddressCount.ToString());

            if (IPaddressCount < 1)
            {
                //No VPN Sessiosn found, we cannot continue.
                Console.WriteLine("VPN has not been dialed - Please Dial VPN before attempting to set static routes.");
                return null;
            }

            XmlDocument VpnconfigDoc = new XmlDocument();
            VpnconfigDoc.Load(LocalConfigFile);
            XmlNodeList DefaultGateways = VpnconfigDoc.SelectNodes("/VPN/DefaultGateways/DefaultGateway");

            foreach (string VPNIPaddress in VpnIPAddresses)
            {

                foreach (XmlNode DefaultGateway in DefaultGateways)
                {
                    XmlAttribute VpnSubnetLower = DefaultGateway.Attributes["VPNSubnetLower"];
                    XmlAttribute VpnSubnetUpper = DefaultGateway.Attributes["VPNSubnetUpper"];
                    XmlAttribute DG = DefaultGateway.Attributes["DefaultGateway"];
                    XmlAttribute SubnetDescription = DefaultGateway.Attributes["SubnetDescription"];

                    Console.WriteLine("Checking to see if assigned VPN falls inside the range {0} - {1}", VpnSubnetLower.Value.ToString(), VpnSubnetUpper.Value.ToString());

                    //Check that the address we have assigned from VPN DHCP server falls in one of the defined subnet ranges - First one wins.

                    IPAddress AssignedVPNAddress = IPAddress.Parse(VPNIPaddress);
                    IPAddress LowerVPNSubnet = IPAddress.Parse(VpnSubnetLower.Value.ToString());
                    IPAddress UpperVPNSubnet = IPAddress.Parse(VpnSubnetUpper.Value.ToString());

                    //Cast Lower and upper ends of subnet into Byte-arrays so we can enumerate each Octet-group 
                    byte[] lowerAddressBytes = LowerVPNSubnet.GetAddressBytes();
                    byte[] upperAddressBytes = UpperVPNSubnet.GetAddressBytes();

                    //Check that we're working with two of the same IP Types (IPv4/IPv6) - If not; there's no point in continuing 
                    if (AssignedVPNAddress.AddressFamily != LowerVPNSubnet.AddressFamily)
                    {
                        Console.WriteLine("Assigned VPN address does not fall in same Address Family (IPv4/IPv6).");
                        return null;
                    }
                    else
                    {

                        if (TestIPInRange(AssignedVPNAddress, LowerVPNSubnet, UpperVPNSubnet))
                        {
                            Console.WriteLine("Assigned VPN IP Address falls inside Pre-defined range ({0}) - Setting Default Gateway to {1}", SubnetDescription.ToString(), DG.Value.ToString());
                            return DG.Value.ToString();
                        }
                    }
                }
            }
            //Default Return
            return null;
        }


        private bool TestIPInRange(IPAddress AssignedVPNAddress, IPAddress LowerVPNSubnet, IPAddress UpperVPNSubnet)
        {
            byte[] lowerAddressBytes = LowerVPNSubnet.GetAddressBytes();
            byte[] upperAddressBytes = UpperVPNSubnet.GetAddressBytes();
            byte[] addressBytes = AssignedVPNAddress.GetAddressBytes();
            bool lowerBoundary = true, upperBoundary = true;

            for (int i = 0; i < lowerAddressBytes.Length && (lowerBoundary || upperBoundary); i++)
            {
                if ((lowerBoundary && addressBytes[i] < lowerAddressBytes[i]) || (upperBoundary && addressBytes[i] > upperAddressBytes[i]))
                {
                    Console.WriteLine("Assigned VPN address does not fall in this range, trying next address subnet (if any).");
                    lowerBoundary = false;
                    upperBoundary = false;

                    return false;
                }
                lowerBoundary &= (addressBytes[i] == lowerAddressBytes[i]);
                upperBoundary &= (addressBytes[i] == upperAddressBytes[i]);
            }
            //The address falls inside the Range.
            return true;
        }
    }
}