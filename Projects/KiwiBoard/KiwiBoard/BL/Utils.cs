using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Web;

namespace KiwiBoard.BL
{
    public class Utils
    {
        public static int RunProcess(string toolPath, string args, out string standardOutput, out string errorOutput)
        {
            ProcessStartInfo toolStartInfo = new ProcessStartInfo
            {
                FileName = toolPath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (Process toolProcess = new Process())
            {
                toolProcess.StartInfo = toolStartInfo;
                toolProcess.Start();

                toolProcess.StandardInput.WriteLine();
                var outputReader = toolProcess.StandardOutput;
                standardOutput = outputReader.ReadToEnd();
                var errorOutputReader = toolProcess.StandardError;
                errorOutput = errorOutputReader.ReadToEnd();
                toolProcess.WaitForExit();

                return toolProcess.ExitCode;
            }
        }

        public static string RunPsScript(string scriptPath, Expression<Func<object>> parameters)
        {
            string shellPath = "powershell.exe";
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("\"& '{0}'\" ", scriptPath);

            NewExpression n = parameters.Body as NewExpression;

            for (int i = 0; i < n.Members.Count; i++)
            {
                var member = n.Members[i];
                var value = n.Arguments[i];
                string paramValue;
                if (value is MemberExpression)
                {
                    paramValue = Expression.Lambda(value).Compile().DynamicInvoke().ToString();
                }
                else
                {
                    paramValue = value.ToString().Replace("\"", string.Empty);
                }
                sb.AppendFormat(" -{0} {1}", member.Name.Replace("get_", ""), paramValue);
            }

            string output = string.Empty;
            string error = string.Empty;

            RunProcess(shellPath, sb.ToString(), out output, out error);
            return output;
        }
    }
}