using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Web;
using System.Web.Configuration;

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

        public static IDictionary<string, string[]> GetEnvironmentMachineMap()
        {
            return GetEnvironmentMachineMap(Constants.ApCluster, Constants.Environments);
        }

        public static IDictionary<string, string[]> GetEnvironmentMachineMap(string cluster, string[] environments)
        {
            var dict = new Dictionary<string, string[]>();

            foreach (var env in environments)
            {
                var machinesCSV = Path.Combine(Constants.ApGoldSrcRoot, "autopilotservice", cluster, env, "Machines.csv");


                var machines = File.ReadAllLines(machinesCSV)
                    .Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith("#") && l.Split(',')[2] == "IKFE")
                    .Select(line => line.Split(',')[0]).ToArray();

                dict.Add(env, machines);
            }

            return dict;
        }

        public static string[] GetFunctionMachines(string cluster, string environment, string machineFunction)
        {
            var machinesCSV = Path.Combine(Constants.ApGoldSrcRoot, "autopilotservice", cluster, environment, "Machines.csv");
            return File.ReadAllLines(machinesCSV)
                  .Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith("#") && l.Split(',')[2] == machineFunction)
                  .Select(line => line.Split(',')[0]).ToArray();
        }

        public static T XmlDeserialize<T>(string xmlString) where T : class
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(Entities.JobStates));
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xmlString)))
            {
                return serializer.Deserialize(stream) as T;
            }
        }

        public static string XmlSerialize(object entity)
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(Entities.JobStates));
            using (var stream = new MemoryStream())
            using (var streamReader = new StreamReader(stream))
            {
                serializer.Serialize(stream, entity);
                stream.Flush();
                return streamReader.ReadToEnd();
            }
        }

        public static T[] ParseCsvLog<T>(string csvText)
        {
            if (string.IsNullOrEmpty(csvText))
            {
                throw new ArgumentNullException();
            }

            using (TextReader sr = new StringReader(csvText))
            using (var reader = new CsvHelper.CsvReader(sr))
            {
                return reader.GetRecords<T>().ToArray();
            }
        }
    }
}