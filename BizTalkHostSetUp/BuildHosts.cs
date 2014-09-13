using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using BizTalkSetUp;
using System.Xml;
using System.Management;
using Microsoft.BizTalk.ExplorerOM;

namespace BizTalkSetUp
{
    public partial class BuildHosts : Form
    {
        public BuildHosts()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // Load the Config XML
                XmlDocument oDoc = new XmlDocument();
                oDoc.Load(@"..\..\HostConfig.xml");
                //oDoc.Load(@"..\..\HostConfig-Demo.xml");
                
                // Get the list of Hosts
                XmlNodeList oNodes = oDoc.SelectNodes(@"BizTalkHostConfig/MakeHosts/Host");

                // Loop over the nodes
                foreach(XmlNode oNode in oNodes)
                {
                    string hostName = oNode["HostName"].InnerText;
                    string hostType = oNode["Type"].InnerText;
                    string hostNTGroup = oNode["NTGroup"].InnerText;
                    bool authTrusted = Convert.ToBoolean(oNode["AuthTrusted"].InnerText);
                    textBox1.Text += WMIMethods.MakeHost(hostName, hostType, hostNTGroup, authTrusted);

                    if (oNode["InstallServers"].GetAttribute("Action") == "true")
                    {
                        // Get the list of Servers - Not tested with remote servers
                        XmlNodeList oNodeServers = oNode["InstallServers"].ChildNodes;
                        foreach (XmlNode oNodeServer in oNodeServers)
                        {
                            string serverName = oNodeServer["ServerName"].InnerText;
                            string userName = oNodeServer["UserName"].InnerText;
                            string password = "";
                            bool startHost = Convert.ToBoolean(oNodeServer["ServerName"].GetAttribute("Start"));

                            if (oNodeServer["Password"].GetAttribute("Prompt") == "true")
                            {
                                PasswordInput inputForm = new PasswordInput();
                                inputForm.label1.Text = "Enter password for " + userName;
                                if (inputForm.ShowDialog(this) == DialogResult.OK)
                                    password = inputForm.textPassword.Text;
                            }
                            else
                                password = oNodeServer["Password"].InnerText;
                            
                            textBox1.Text += WMIMethods.InstallHosts(serverName, hostName, userName, password, startHost);
                        }  
                    }

                    if (oNode["SetAdapters"].GetAttribute("Action") == "true")
                    {
                        // Get the list of Servers - Not tested with remote servers
                        XmlNodeList oNodeAdapters = oNode["SetAdapters"].ChildNodes;
                        foreach (XmlNode oNodeAdapter in oNodeAdapters)
                        {
                            string adapterName = oNodeAdapter["AdapterName"].InnerText;

                            if(oNodeAdapter["AdapterName"].GetAttribute("Type") == "Receive")
                                textBox1.Text += WMIMethods.AddReceiveHostHandler(adapterName, hostName);
                            else
                                textBox1.Text += WMIMethods.AddSendHostHandler(adapterName, hostName);
                        }
                    }
                }   
            }
            catch (Exception ex)
            {
                textBox1.Text += "\r\n Error: " + ex.Message;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Load the Config XML
            XmlDocument oDoc = new XmlDocument();
            oDoc.Load(@"..\..\HostConfig.xml");

            string defaulInProcessHost = oDoc.DocumentElement.GetAttribute("defaultHost").ToString();
            string defaulIsoHost = oDoc.DocumentElement.GetAttribute("defaultIsoHost").ToString();

            try
            {
                PutOptions options = new PutOptions();
                options.Type = PutType.UpdateOnly;

                //Look for the target WMI Class MSBTS_ReceiveHandler instance
                string strWQL = "SELECT * FROM MSBTS_ReceiveHandler";
                ManagementObjectSearcher searcherReceiveHandler = new ManagementObjectSearcher(new ManagementScope("root\\MicrosoftBizTalkServer"), new WqlObjectQuery(strWQL), null);

                string recName;
                string recHost;
                string sndName;
                string sndHost;

                if (searcherReceiveHandler.Get().Count > 0)
                    foreach (ManagementObject objReceiveHandler in searcherReceiveHandler.Get())
                    {
                        //Get the Adapter Name
                        recName = objReceiveHandler["AdapterName"].ToString();

                        // Get the Current Host
                        recHost = objReceiveHandler["HostName"].ToString();


                        // Find the Host Type
                        string strWQLHost = "SELECT * FROM MSBTS_HostInstanceSetting where HostName = '" + recHost + "'";
                        ManagementObjectSearcher searcherHostHandler = new ManagementObjectSearcher(new ManagementScope("root\\MicrosoftBizTalkServer"), new WqlObjectQuery(strWQLHost), null);

                        foreach (ManagementObject objHostHandler in searcherHostHandler.Get())
                        {
                            // Type 1 is In Process
                            if (objHostHandler["HostType"].ToString() == "1")
                            {
                                objReceiveHandler.SetPropertyValue("HostNameToSwitchTo", defaulInProcessHost);
                                objReceiveHandler.Put();
                            }
                            // Otherwise it is Isolated
                            else
                            {
                                objReceiveHandler.SetPropertyValue("HostNameToSwitchTo", defaulIsoHost);
                                objReceiveHandler.Put();
                            }
                        }

                        textBox1.Text += "Receive Adapters: - " + recName + " \r\n";
                    }

                //Look for the target WMI Class MSBTS_SendHandler instance
                string strWQLsnd = "SELECT * FROM MSBTS_SendHandler2";
                ManagementObjectSearcher searcherSendHandler = new ManagementObjectSearcher(new ManagementScope("root\\MicrosoftBizTalkServer"), new WqlObjectQuery(strWQLsnd), null);

                if (searcherSendHandler.Get().Count > 0)
                    foreach (ManagementObject objSendHandler in searcherSendHandler.Get())
                    {
                        //Get the Adapter Name
                        sndName = objSendHandler["AdapterName"].ToString();

                        // Get the Current Host
                        sndHost = objSendHandler["HostName"].ToString();

                        // Find the Host Type
                        string strWQLHost = "SELECT * FROM MSBTS_HostInstanceSetting where HostName = '" + sndHost + "'";
                        ManagementObjectSearcher searcherHostHandler = new ManagementObjectSearcher(new ManagementScope("root\\MicrosoftBizTalkServer"), new WqlObjectQuery(strWQLHost), null);

                        foreach (ManagementObject objHostHandler in searcherHostHandler.Get())
                        {
                            // Type 1 is In Process
                            if (objHostHandler["HostType"].ToString() == "1")
                            {
                                objSendHandler.SetPropertyValue("HostNameToSwitchTo", defaulInProcessHost);
                                objSendHandler.Put();
                            }
                            // Otherwise it is Isolated
                            else
                            {
                                objSendHandler.SetPropertyValue("HostNameToSwitchTo", defaulIsoHost);
                                objSendHandler.Put();
                            }
                        }

                        textBox1.Text += "Send Adapters: - " + sndName + " \r\n";
                    }

                textBox1.Text += "Done";
            }
            catch (Exception ex)
            {
                textBox1.Text += ex.Message;
            }
 
       }
    } 
}