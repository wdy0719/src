using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FirewallRulesTest
{
    /*
     *  This test class used to verify the firewall rules settings on remote computer.
     *  To run the tests:
     *      1. Enable PS remoting:  Enable-PSRemoting
     *      2. Add trusted hosts to local computer:  Set-Item WSMan:\localhost\Client\TrustedHosts *
     *      3. Update app settings
     * 
     */
    [TestClass]
    public class FirewallRuleTests
    {
        /// <summary>
        /// Powershell runspace used for tests.
        /// </summary>
        static Runspace psRunspace;

        /// <summary>
        /// Test context.
        /// </summary>
        public TestContext TestContext
        { get; set; }

        /// <summary>
        ///  Test suite setup.
        /// </summary>
        [ClassInitialize]
        public static void ClassInitialize(TestContext TestContext)
        {
            InitPSRunspace();

            ForceGPUpdate();
        }

        /// <summary>
        /// Test suite clean up.
        /// </summary>
        [ClassCleanup]
        public static void ClassCleanup()
        {
            if (psRunspace != null)
            {
                psRunspace.Close();
                psRunspace.Dispose();
            }
        }

        /// <summary>
        /// Check remote computer firewall state.
        /// </summary>
        [TestMethod]
        public void CheckFirewallState()
        {
            // http://msdn.microsoft.com/en-us/library/windows/desktop/aa364724(v=vs.85).aspx
            int NET_FW_PROFILE2_DOMAIN = 0x0001;
            int NET_FW_PROFILE2_PRIVATE = 0x0002;
            int NET_FW_PROFILE2_PUBLIC = 0x0004;
            int NET_FW_ACTION_BLOCK = 0;
            int NET_FW_ACTION_ALLOW = 1;

            Action<int> verifyFirewallState = (int profileType) => {
                var psCmd = string.Format("$fw = New-Object -ComObject HNetCfg.FWPolicy2 ; $fw.FirewallEnabled({0})", profileType);
                Assert.AreEqual(true, (bool)PSExec(psCmd).First().BaseObject, "Firewall is not enabled! Firewall profile type:{0}", profileType);
            };

            Action<int> verifyDefaultInboundAction = (int profileType) =>
            {
                var psCmd = string.Format("$fw = New-Object -ComObject HNetCfg.FWPolicy2 ; $fw.DefaultInboundAction({0})", profileType);
                Assert.AreEqual(NET_FW_ACTION_BLOCK, (int)PSExec(psCmd).First().BaseObject, "Default inbound action should be 'Block'! Firewall profile type:{0}", profileType);
            };

            verifyFirewallState(NET_FW_PROFILE2_DOMAIN);
            verifyFirewallState(NET_FW_PROFILE2_PRIVATE);
            verifyFirewallState(NET_FW_PROFILE2_PUBLIC);

            verifyDefaultInboundAction(NET_FW_PROFILE2_DOMAIN);
            verifyDefaultInboundAction(NET_FW_PROFILE2_PRIVATE);
            verifyDefaultInboundAction(NET_FW_PROFILE2_PUBLIC);
        }

        [DataSource("System.Data.Odbc",
            "Driver={Microsoft Excel Driver (*.xls)};DriverId=790;Dbq=D:\\Features\\x-boundary\\SPOD_Cross_Boundary_Application_Flow_final_copy.xls;DefaultDir=.;", "Consolidate$",
            DataAccessMethod.Sequential),
        TestMethod]
        public void CheckInboundRules()
        {
            var td_Name = TestContext.DataRow["Name"].ToString();
            var td_port = TestContext.DataRow["DEST Port"].ToString();
            var td_protocol = TestContext.DataRow["Protocol"].ToString();

            // Get expected inbound firewall rules by name.
            var psCmd = string.Format("$fw = New-Object -ComObject HNetCfg.FWPolicy2 ; $fw.rules | ?{$_.enabled -eq $true -and $_.direction -eq 1 $_.name -eq '{0}'}", td_Name);
            var rules = PSExec(psCmd).Select(rule => new
            {
                Name = rule.Members["Name"].Value.ToString(),
                Protocol = (int)rule.Members["Protocol"].Value,
                LocalPorts = rule.Members["LocalPorts"].Value.ToString()
            });

            
            /*
             * Mapping protocol numbers: http://msdn.microsoft.com/en-us/library/windows/desktop/aa364720(v=vs.85).aspx
             */
            // Verify protocol:
            var NET_FW_IP_PROTOCOL_TCP = 6;
            var NET_FW_IP_PROTOCOL_UDP = 17;
            var protocols = rules.Select(r => r.Protocol);
            switch (td_protocol.ToUpperInvariant())
            {
                case "TCP":
                    Assert.AreEqual(1, protocols.Count(), "Firewall rule not found or more than one firewall rule found by name!");
                    Assert.AreEqual(NET_FW_IP_PROTOCOL_TCP, protocols.First(), "Unexpected protocol!");
                    break;
                case "UDP":
                    Assert.AreEqual(1, protocols.Count(), "Firewall rule not found or more than one firewall rule found by name!");
                    Assert.AreEqual(NET_FW_IP_PROTOCOL_UDP, protocols.First(), "Unexpected protocol!");
                    break;
                case "TCP/UDP":
                    Assert.AreEqual(2, protocols.Count(), "Unexpected number of firewall rules found by name!");
                    Assert.IsTrue(protocols.Contains(NET_FW_IP_PROTOCOL_TCP) && protocols.Contains(NET_FW_IP_PROTOCOL_UDP), "Unexpected protocol!");
                    break;
                default:
                    Assert.Inconclusive("Unexpected test data format for protocol:{0}", td_protocol);
                    break;
            }

            // Verify local ports are equal to the expected ports:
            foreach (var r in rules)
            {
                Assert.IsTrue(r.LocalPorts.OrderByDescending(c => c).SequenceEqual(td_port.OrderByDescending(c => c)),
                    "Unexpected local ports! expected:'{0}',  actual:'{1}'.", td_port, r.LocalPorts);
            }
        }

        // Init settings.
        static void InitPSRunspace()
        {
            var RemoteComputer = ConfigurationManager.AppSettings["RemoteComputer"];
            var RemoteCred_User = ConfigurationManager.AppSettings["RemoteCred_User"];
            var RemoteCred_Password = ConfigurationManager.AppSettings["RemoteCred_Password"];
            var SecPassword = new SecureString();
            RemoteCred_Password.Select(c => { SecPassword.AppendChar(c); return c; }).ToArray();
            var PSCredential = new PSCredential(RemoteCred_User, SecPassword);
            string shellUri = "http://schemas.microsoft.com/powershell/Microsoft.PowerShell";
            var WSManConnectionInfo = new WSManConnectionInfo(false, RemoteComputer, 5985, "/wsman", shellUri, PSCredential);

            psRunspace = RunspaceFactory.CreateRunspace(WSManConnectionInfo);
            psRunspace.Open();
        }

        // Execute PS command on remote computer.
        static Collection<PSObject> PSExec(string script)
        {
            using (Pipeline Pipeline = psRunspace.CreatePipeline())
            {
                Pipeline.Commands.AddScript(script);
                return Pipeline.Invoke();
            }
        }

        // Force update remote computer's group policy and verify if sucessfully.
        static void ForceGPUpdate()
        {
            var str = PSExec("gpupdate /target:computer /force");
            Assert.IsTrue(str.Any(s => ((string)s.ImmediateBaseObject).Contains("Computer Policy update has completed successfully.")),
                "Force update group policy on remote computer failed!");
        }
    }
}
