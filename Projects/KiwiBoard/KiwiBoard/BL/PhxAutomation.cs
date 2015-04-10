using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
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

        private PowerShell psInstance = null;

        public PhxAutomation()
        {
            psInstance = System.Management.Automation.PowerShell.Create(RunspaceMode.NewRunspace);

            psInstance.AddScript("Set-ExecutionPolicy -ExecutionPolicy Bypass -Force");
            psInstance.AddScript(string.Format(@"Import-Module '{0}'", Constants.CoreXTAutomationModule));
            psInstance.AddScript(string.Format(@"Import-Module '{0}'", Constants.PhxAutomationModule));
            psInstance.AddScript(@"Set-ApGoldRoot -Path " + Constants.ApGoldSrcRoot);
            psInstance.Invoke();
            if (psInstance.Streams.Error.Count > 0)
            {
                throw new PhxAutomationException(PhxAutomationErrorCode.InitError, "Initiliaze Phx Automation Powershell Failed. {0}", psInstance.Streams.Error.ToString());
            }
        }

        public string FetchIscopeJobStateXml(string machineName, string runtime)
        {
            var script = string.Format("\"{0}\" | Read-PhxFile \"data\\iscopehost\\{1}\\state.xml\"", machineName, runtime);

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
                    errorMessage = "Cannot find the machine in PHX domain.";
                }

                throw new PhxAutomationException(errorCode, errorMessage, ex);
            }
            catch (Exception ex)
            {
                throw new PhxAutomationException(PhxAutomationErrorCode.Unknown, ex.Message, ex);
            }
        }

        private string RunScript(string script)
        {
            lock (this)
            {
                StringBuilder result = new StringBuilder();
                psInstance.AddScript("$error.clear();");
                psInstance.AddScript(script);
                var output = psInstance.Invoke();

                foreach (PSObject outputItem in output)
                {
                    if (outputItem != null)
                    {
                        result.AppendLine(outputItem.ToString());
                    }
                }

                return result.ToString();
            }
        }

        public void Dispose()
        {
            if (psInstance != null)
            {
                psInstance.Dispose();
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