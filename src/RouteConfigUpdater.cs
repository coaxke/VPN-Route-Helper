using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Reflection;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace VPNRouteHelper
{
    class RouteConfigUpdater
    {        
       public int CheckLocalVersion (string WebConfigURL, string LocalConfigFile)
       {
           try
           {
                //Test that VPN website is alive - Loading XML Config will fail otherwise.
                WebRequest ConfigRequest = WebRequest.Create(WebConfigURL);

               using (HttpWebResponse ConfigResponse = (HttpWebResponse)ConfigRequest.GetResponse())
               {
                   if (ConfigResponse.StatusCode == HttpStatusCode.OK)
                   {
                       //Check if RouteConfig file exists on machine
                       Console.WriteLine("Checking existance of VPNConfig file on local machine...");

                       if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "VPNConfig.xml")))
                       {
                           //Create a blank config file and download latest copy if config file does not exist.
                           Console.WriteLine("VPNConfig does not exist - Creating new file and attempting to download the latest config from VPN Site");

                           try
                           {  
                               //Validate Remote XML Document no-matter what - so we know our source of truth is valid.
                               if (ValidateXMLDocument(WebConfigURL))
                               {
                                   File.Create(LocalConfigFile).Close();
                                   LoadConfigFile(WebConfigURL).Save(LocalConfigFile);
                               }
                               else
                               {
                                   MessageBox.Show("Unable to parse/load VPN Route config file for destination networks fom Web Server. " +
                                                    "VPN Will connect with existing configuration file (if present), otherwise you will be connected using " +
                                                    "default routes supplied by the VPN Concentrator. \n",
                                                    "Error Loading VPN Config file.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                               }
                           }
                           catch (Exception e)
                           {
                               Console.WriteLine("Error when creating blank file, perhaps we dont have permission to write to this Dir. See below exception \n {0}\n{1}", e.Message, e.StackTrace);
                               return 0;
                           }

                           return 1;
                   
                       }
                       else
                       {
                           //Check if We have the latest version of the file.
                           Console.WriteLine("VPNConfig file exists - Checking to see if we have the latest version...");

                           try
                           {
                               //Check that base-config file is even valid before we save it to the users machine
                               //so we dont get in a weird state where the user has a buggered version even when the server
                               //version has been fixed.

                               if (ValidateXMLDocument(WebConfigURL))
                               {
                                   XmlDocument ExtranetVPNConfigDocument = LoadConfigFile(WebConfigURL);

                                   XmlNode ExtranetVersion = ExtranetVPNConfigDocument.SelectSingleNode("/VPN/@Version");

                                   XmlDocument LocalVPNConfigDocument = LoadConfigFile(LocalConfigFile);
                                   XmlNode LocalVersion = LocalVPNConfigDocument.SelectSingleNode("/VPN/@Version");

                                   if (Int32.Parse(ExtranetVersion.Value.ToString()) > (Int32.Parse(LocalVersion.Value.ToString())))
                                   {
                                       ExtranetVPNConfigDocument.Save(LocalConfigFile);
                                       Console.WriteLine("Config File Updated to version {0}", ExtranetVersion.Value.ToString());
                                   }
                                   else
                                   {
                                       Console.WriteLine("Lastest version of the config already present on system, continuing...");
                                   }
                               }
                               else
                               {
                                   MessageBox.Show("Unable to parse/load VPN Route config file for destination networks fom Web Server. " +
                                                    "VPN Will connect with existing configuration file (if present), otherwise you will be connected using " +
                                                    "default routes supplied by the VPN Concentrator. \n",                                                   
                                                    "Error Loading VPN Config file.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                               }
                           }
                           catch (Exception e)
                           {
                               Console.WriteLine("Error when comparing version from VPN webiste - Document is likely not valid. \n {0}\n{1}", e.Message, e.StackTrace);
                               return 0;
                           }
                           
                           return 1;
                         }
                   }
                   else
                   {
                       MessageBox.Show("Cannot reach {0} to retrieve config file, VPN website may be offline, We will have to use default routes from VPN concentrator or local copy of config file", WebConfigURL);
                       return 0;
                   }
                 }
                }
                catch (WebException e)
                {
                    Console.WriteLine ("We got an error when we attempted to retrieve the config file - The error was {0} : {1}", e.Message, e.InnerException);
                    return 0;
                }
            }

       private XmlDocument LoadConfigFile (string FilePath)
       {
           XmlDocument VpnconfigDoc = new XmlDocument();
           VpnconfigDoc.Load(FilePath);

           return VpnconfigDoc;
       }

        public bool ValidateXMLDocument (string XMLPath)
        {
            Assembly RouteHelperAssembly = Assembly.GetExecutingAssembly();

            XmlSchemaSet RouteHelperSchema = new XmlSchemaSet();
            Stream RouteHelpberSchemaStream = RouteHelperAssembly.GetManifestResourceStream("VPNRouteHelper.VPN_Config_Files.VPNConfig.xsd");
            XmlReader RouteHelperSchemaReader = XmlReader.Create(RouteHelpberSchemaStream);
            RouteHelperSchema.Add("", RouteHelperSchemaReader);

            XmlReader ConfigFileXMLReader;
            try
            {
                ConfigFileXMLReader = XmlReader.Create(XMLPath);

            }
            catch
            {
                //We Couldnt load the XML File for some reason - lets just abort and handle outside of this function.
                return false;
            }

            XmlReaderSettings ReaderSettings = new XmlReaderSettings()
            {
                Schemas = RouteHelperSchema,
                ValidationType = ValidationType.Schema,
                IgnoreComments = true,
                IgnoreWhitespace = true
            };

            XmlReader VerifySchema = XmlReader.Create(ConfigFileXMLReader, ReaderSettings);

            try
            {
                while (VerifySchema.Read()) { }
            }
            catch (Exception ex)
            {               
                Console.WriteLine("Downloaded XML Config appears to not match bundled schema - Check error for more info: {0} - {1}0", ex.Message, ex.InnerException);

                VerifySchema.Close();

                return false;
            }

            VerifySchema.Close();

            return true;
        }
    }
}
