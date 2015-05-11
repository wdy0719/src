using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Xml.Serialization;

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

        public static IDictionary<string, string[]> GetEnvironmentMachineMap(string environmentSetting)
        {
            var dict = new Dictionary<string, string[]>();
            foreach (var envSetting in environmentSetting.Split(','))
            {
                var apcluster = envSetting.Split('|')[0].Trim();
                var cosmoscluter = envSetting.Split('|')[1].Trim();
                var machineFunction = envSetting.Split('|')[2].Trim();
                dict.Add(cosmoscluter, GetFunctionMachines(apcluster, cosmoscluter, machineFunction));
            }

            return dict;
        }

        public static string[] GetFunctionMachines(string apcluster, string cosmoscluter, string machineFunction)
        {
            var machinesCSV = Path.Combine(Settings.ApGoldSrcRoot, "autopilotservice", apcluster, cosmoscluter, "Machines.csv");

            return File.ReadAllLines(machinesCSV)
                  .Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith("#") && l.Split(',')[2] == machineFunction)
                  .Select(line => line.Split(',')[0]).ToArray();
        }

        public static T XmlDeserialize<T>(string xmlString) where T : class
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xmlString)))
            {
                return serializer.Deserialize(stream) as T;
            }
        }

        public static string XmlSerialize<T>(T entity) where T : class
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            using (var stream = new MemoryStream())
            using (var streamReader = new StreamReader(stream))
            {
                serializer.Serialize(stream, entity, ns);
                stream.Flush();
                return streamReader.ReadToEnd();
            }
        }
    }
}