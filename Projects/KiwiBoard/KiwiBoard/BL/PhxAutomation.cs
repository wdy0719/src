using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace KiwiBoard.BL
{
    public class PhxAutomation : IDisposable
    {
        public static PhxAutomation DefaultInstance = null;

        static PhxAutomation()
        {
            DefaultInstance = new PhxAutomation();
        }

        private RunspacePool rsPool = null;

        private InitialSessionState defaultSessionState = null;

        public PhxAutomation()
        {
            this.defaultSessionState = System.Management.Automation.Runspaces.InitialSessionState.CreateDefault();
            defaultSessionState.ApartmentState = System.Threading.ApartmentState.MTA;
            defaultSessionState.ThreadOptions = System.Management.Automation.Runspaces.PSThreadOptions.UseNewThread;
            defaultSessionState.ThrowOnRunspaceOpenError = true;
            defaultSessionState.ImportPSModule(new string[] { Settings.CoreXTAutomationModule, Settings.PhxAutomationModule, Settings.ReadPhxLogs2Location });

            this.rsPool = System.Management.Automation.Runspaces.RunspaceFactory.CreateRunspacePool(defaultSessionState);
            this.rsPool.SetMaxRunspaces(20);
            this.rsPool.Open();
        }

        public XmlDocument[] FetchIscopeJobStateXml(string runtime, params string[] machines)
        {
            var commands = string.Format("Read-PhxFile \"data\\iscopehost\\{0}\\state.xml\" -Xml", runtime);
            var script = string.Format("{0} | {1}", string.Join(",", machines.Select(m => "'" + m + "'")), commands);

            return this.RunScript<XmlDocument>(script).ToArray();
        }

        // returns empty if profile not found in specified machine.
        public string SearchProfileLog(string environment, string runtime, string runtimeCodeName, string jobId, out string profileFileOnMachine, params string[] machines)
        {
            var commands = string.Format("Read-PhxFile \"data\\JobManagerService\\{0}\\{1}\\{2}\\profile_{{{3}}}.txt\"", environment, runtime, runtimeCodeName, jobId);

            foreach (var machine in machines)
            {
                var script = string.Format("'{0}' | {1}", machine, commands);
                var psResult = this.RunScript<string>(script).ToArray();
                if (psResult != null && psResult.Length != 0)
                {
                    profileFileOnMachine = machine;
                    return string.Join(Environment.NewLine, psResult);
                }
            }

            profileFileOnMachine = string.Empty;
            return string.Empty;
        }

        public IEnumerable<Entities.CsLog> FetchCsLogEntries(DateTime startTime, DateTime endTime, string searchPattern, params string[] machines)
        {
            var CsLogSearchPattern = string.Format("../Cslogs/local/{0}", searchPattern);
            var commands = string.Format("Read-PhxLogs2 '{0}' -Start '{1}' -End '{2}' -UpdateCache", CsLogSearchPattern, startTime.ToString(), endTime.ToString());

            var script = string.Format("{0} | {1}", string.Join(",", machines.Select(m => "'" + m + "'")), commands);

            var csLogProperties = typeof(Entities.CsLog).GetProperties();
            foreach (var cslog in this.RunScript<dynamic>(script).Where(a => a is IDictionary<String, Object>).Cast<IDictionary<String, Object>>())
            {
                var log = new Entities.CsLog();
                foreach (var prop in csLogProperties)
                {
                    prop.SetValue(log, cslog[prop.Name]);
                }

                yield return log;
            }
        }

        public IEnumerable<dynamic> ReadPhxFileAsCsv(string path, params string[] machines)
        {
            var commands = string.Format("Read-PhxFile '{0}' -Csv -UpdateCache", path);
            var script = string.Format("{0} | {1}", string.Join(",", machines.Select(m => "'" + m + "'")), commands);
            return RunScript<dynamic>(script);
        }

        public IEnumerable<T> RunScript<T>(string script)
        {
            using (var ps = System.Management.Automation.PowerShell.Create(this.defaultSessionState))
            {
                ps.RunspacePool = this.rsPool;
                // ps.AddScript(@"Set-ApGoldRoot -Path " + Settings.ApGoldSrcRoot);
                ps.AddScript(script);

                System.Collections.ObjectModel.Collection<PSObject> output = null;
                try
                {
                    output = ps.Invoke();
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

                foreach (PSObject outputItem in output)
                {
                    if (outputItem == null)
                    {
                        yield return default(T);
                    }

                    if (outputItem.BaseObject.GetType() != typeof(PSCustomObject))
                    {
                        yield return (T)outputItem.BaseObject;
                    }
                    else
                    {
                        // return dynamic type
                        dynamic sampleObject = new ExpandoObject();
                        foreach (var prop in outputItem.Properties)
                        {
                            ((IDictionary<string, object>)sampleObject).Add(prop.Name, outputItem.Properties[prop.Name].Value);
                        }

                        yield return sampleObject;
                    }
                }
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