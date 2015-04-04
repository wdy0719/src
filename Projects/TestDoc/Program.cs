using System;
using System.Linq;
namespace TestDoc
{
    using System.IO;
    using System.Reflection;
    using System.Text.RegularExpressions;

    internal class Program
    {
        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            string Dir = @".\";
            string File = @".\TestDoc.xml";

            Action usage = () =>
            {
                Console.WriteLine("Usage: ");
                Console.WriteLine(@"     TestDoc.exe [Source Code Directory] [Test Document Path]");
                Console.WriteLine("Example: ");
                Console.WriteLine(@"     TestDoc.exe D:\SchedulingAutomation D:\TestDoc.xml");
                Console.WriteLine(@"     (Or simply run TestDoc.exe at source code directory to generate test doc with defualt name.)");
            };

            if (args != null && args.Length != 0)
            {
                if (args[0] == "/?" || args[0] == "?" || args[0].ToLower() == "/h" || args[0].ToLower() == "h")
                {
                    usage();
                    return;
                }

                Dir = args[0].Trim();
                File = args[1].Trim();
            }

            if (!Directory.Exists(Dir))
            {
                Console.WriteLine("Directory not exists! \r\n\r\n");
                usage();
                return;
            }

            Utili.TestDocGen(Dir, File);
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (!args.Name.StartsWith("Roslyn."))
            {
                return null;
            }

            var resName = "TestDoc."+args.Name.Split(',')[0] + ".dll";
            var thisAssembly = Assembly.GetExecutingAssembly();
            using (var input = thisAssembly.GetManifestResourceStream(resName))
            {
                return input != null ? Assembly.Load(StreamToBytes(input)) : null;
            }
        }

        static byte[] StreamToBytes(Stream input)
        {
            var capacity = input.CanSeek ? (int)input.Length : 0;
            using (var output = new MemoryStream(capacity))
            {
                int readLength;
                var buffer = new byte[4096];

                do
                {
                    readLength = input.Read(buffer, 0, buffer.Length);
                    output.Write(buffer, 0, readLength);
                }
                while (readLength != 0);

                return output.ToArray();
            }
        }
    }

}
