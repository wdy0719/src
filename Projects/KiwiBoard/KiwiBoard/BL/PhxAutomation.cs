using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using System.Xml.XPath;

namespace KiwiBoard.BL
{
    public class PhxAutomation : IDisposable
    {
        public static PhxAutomation Instance = null;

        static PhxAutomation()
        {
            Instance = new PhxAutomation();
        }

        private RunspacePool rsPool = null;

        private InitialSessionState defaultSessionState = null;

        public PhxAutomation()
        {
            this.defaultSessionState = System.Management.Automation.Runspaces.InitialSessionState.CreateDefault();
            defaultSessionState.ApartmentState = System.Threading.ApartmentState.MTA;
            defaultSessionState.ThreadOptions = System.Management.Automation.Runspaces.PSThreadOptions.UseNewThread;
            defaultSessionState.ThrowOnRunspaceOpenError = true;
            defaultSessionState.ImportPSModule(new string[] { Settings.CoreXTAutomationModule, Settings.PhxAutomationModule });

            this.rsPool = System.Management.Automation.Runspaces.RunspaceFactory.CreateRunspacePool(defaultSessionState);
            this.rsPool.SetMaxRunspaces(20);
            this.rsPool.Open();
        }

        public string FetchIscopeJobStateXml(string machineName, string runtime)
        {
            return this.FetchIscopeJobStateXml(new string[] { machineName }, runtime);
        }

        public string FetchIscopeJobStateXml(string[] machinesName, string runtime)
        {
            var commands = string.Format("Read-PhxFile \"data\\iscopehost\\{0}\\state.xml\"", runtime);

            return this.RunScriptOnMachines(machinesName, commands);
        }

        public string FetchProfileLog(string environment, string runtime, string runtimeCodeName, string jobId, params string[] machines)
        {
            var commands = string.Format("Read-PhxFile \"data\\JobManagerService\\{0}\\{1}\\{2}\\profile_{{{3}}}.txt\"", environment, runtime, runtimeCodeName, jobId);

            return this.RunScriptOnMachines(machines, commands);
        }

        public string FetchCsLog(DateTime startTime, DateTime endTime, string searchPattern, params string[] machines)
        {
            var CsLogSearchPattern = string.Format("../Cslogs/local/{0}", searchPattern);
            var commands = string.Format("Read-PhxLogs '{0}' -Start '{1}' -End '{2}' -UpdateCache", CsLogSearchPattern, startTime.ToString(), endTime.ToString());

            return this.RunScriptOnMachines(machines, commands);
        }

        public IEnumerable<Entities.CsLog> FetchCsLogEntries(DateTime startTime, DateTime endTime, string searchPattern, params string[] machines)
        {
            var CsLogSearchPattern = string.Format("../Cslogs/local/{0}", searchPattern);
            var commands = string.Format("Read-PhxLogs '{0}' -Start '{1}' -End '{2}' -UpdateCache", CsLogSearchPattern, startTime.ToString(), endTime.ToString());

            var script = string.Format("{0} | {1}", string.Join(",", machines.Select(m => "'" + m + "'")), commands);

            try
            {
                return this.RunScriptAsync<Entities.CsLog>(script);
            }
            catch (RuntimeException ex)
            {
                var errorCode = PhxAutomationErrorCode.Unknown;
                var errorMessage = ex.Message;
                if (ex.Message.Contains("Can't get cluster name for machine"))
                {
                    errorCode = PhxAutomationErrorCode.MachineNotFound;
                    errorMessage = "Cannot find specified machine in PHX domain.";
                }

                throw new PhxAutomationException(errorCode, errorMessage, ex);
            }
            catch (Exception ex)
            {
                throw new PhxAutomationException(PhxAutomationErrorCode.Unknown, ex.Message, ex);
            }
        }

        private string RunScriptOnMachines(string[] machines, string PhxCommands)
        {
            var script = string.Format("{0} | {1}", string.Join(",", machines.Select(m => "'" + m + "'")), PhxCommands);

            try
            {
                return this.RunScript(script);
            }
            catch (RuntimeException ex)
            {
                var errorCode = PhxAutomationErrorCode.Unknown;
                var errorMessage = ex.Message;
                if (ex.Message.Contains("Can't get cluster name for machine"))
                {
                    errorCode = PhxAutomationErrorCode.MachineNotFound;
                    errorMessage = "Cannot find specified machine in PHX domain.";
                }

                throw new PhxAutomationException(errorCode, errorMessage, ex);
            }
            catch (Exception ex)
            {
                throw new PhxAutomationException(PhxAutomationErrorCode.Unknown, ex.Message, ex);
            }
        }

        public string RunScript(string script)
        {
            StringBuilder result = new StringBuilder();
            using (var ps = System.Management.Automation.PowerShell.Create())
            {
                ps.RunspacePool = this.rsPool;
                ps.AddScript(@"Set-Enlistment apgold " + Settings.ApGoldSrcRoot);
                ps.AddScript(script);
                var output = ps.Invoke();
                foreach (PSObject outputItem in output)
                {
                    if (outputItem != null)
                    {
                        result.AppendLine(outputItem.ToString());
                    }
                }
            }
            return result.ToString();
        }

        private IEnumerable<T> RunScriptAsync<T>(string script)
        {
            using (var ps = System.Management.Automation.PowerShell.Create(this.defaultSessionState))
            {
                var result = new List<T>();

                ps.RunspacePool = this.rsPool;
                ps.AddScript(@"Set-ApGoldRoot -Path " + Settings.ApGoldSrcRoot);
                ps.AddScript(script);
                var output = ps.Invoke();

                foreach (PSObject outputItem in output)
                {
                    if (outputItem != null)
                    {
                        var tmp = Activator.CreateInstance<T>();
                        var tmpType = tmp.GetType();

                        foreach (var prop in outputItem.Properties)
                        {
                            tmpType.GetProperty(prop.Name).SetValue(tmp, prop.Value);
                        }

                        result.Add(tmp);
                    }
                }

                return result;
            }
        }

        public void Dispose()
        {
            if (this.rsPool != null)
            {
                this.rsPool.Dispose();
            }
        }
    }

    public class PhxAutomationException : Exception
    {
        public PhxAutomationException(PhxAutomationErrorCode errorCode)
            : base()
        {
            this.ErrorCode = errorCode;
        }

        public PhxAutomationException(PhxAutomationErrorCode errorCode, string description, Exception innerException)
            : base(description, innerException)
        {
            this.ErrorCode = errorCode;
        }

        public PhxAutomationException(PhxAutomationErrorCode errorCode, string description, params string[] parameters)
            : base(string.Format(description, parameters))
        {
            this.ErrorCode = errorCode;
        }

        public PhxAutomationErrorCode ErrorCode { get; private set; }
    }

    public enum PhxAutomationErrorCode
    {
        InitError = 0,
        MachineNotFound,
        Unknown
    }
}