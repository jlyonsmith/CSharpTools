using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;

namespace Tools
{
    public class DoozerTool
    {
        public bool HasOutputErrors { get; set; }

        public bool ShowUsage;

        private Regex todoRegex = new Regex(@"// *(TODO*.*$)", RegexOptions.Singleline);

        public DoozerTool()
        {
        }

        public void Execute()
        {
            if (ShowUsage)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string name = assembly.FullName.Substring(0, assembly.FullName.IndexOf(','));
                object[] attributes = assembly.GetCustomAttributes(true);
                string version = ((AssemblyFileVersionAttribute)attributes.First(x => x is AssemblyFileVersionAttribute)).Version;
                string copyright = ((AssemblyCopyrightAttribute)attributes.First(x => x is AssemblyCopyrightAttribute)).Copyright;
                string title = ((AssemblyTitleAttribute)attributes.First(x => x is AssemblyTitleAttribute)).Title;
                string description = ((AssemblyDescriptionAttribute)attributes.First(x => x is AssemblyDescriptionAttribute)).Description;

                WriteMessage("{0}. Version {1}", title, version);
                WriteMessage("{0}.\n", copyright);
                WriteMessage("{0}\n", description);
                WriteMessage("Usage: mono {0}.exe ...\n", name);
                WriteMessage(@"Arguments:
    [-h] or [-?]            Show help.
");
                return;
            }

            ProcessDirectory(Environment.CurrentDirectory);
        }

        private void ProcessDirectory(string dir)
        {
            string[] files = Directory.GetFiles(dir);

            foreach (var file in files)
            {
                if (Path.GetExtension(file) == ".cs")
                    ScanFile(file);
            }

            string[] dirs = Directory.GetDirectories(dir);

            foreach (var subDir in dirs)
            {
                ProcessDirectory(subDir);
            }
        }

        private void ScanFile(string fileName)
        {
            using (StreamReader reader = new StreamReader(fileName))
            {
                string line;
                int lineNum = 1;

                while ((line = reader.ReadLine()) != null)
                {
                    Match m = todoRegex.Match(line);

                    if (m.Success)
                    {
                        WriteMessage("{0}({1}): {2}", fileName, lineNum, m.Groups[0].Value);
                    }

                    lineNum++;
                }
            }
        }

        public void ProcessCommandLine(string[] args)
        {
            foreach (var arg in args)
            {
                if (arg.StartsWith("-"))
                {
                    switch (arg[1])
                    {
                    case 'h':
                    case '?':
                        ShowUsage = true;
                        return;
                    default:
                        throw new ApplicationException(string.Format("Unknown argument '{0}'", arg[1]));
                    }
                }
                else
                {
                    throw new ApplicationException("Only one file can be specified");
                }
            }
        }

        private void WriteError(string format, params object[] args)
        {
            Console.Write("error: ");
            Console.WriteLine(format, args);
            this.HasOutputErrors = true;
        }

        private void WriteWarning(string format, params object[] args)
        {
            Console.Write("warning: ");
            Console.WriteLine(format, args);
            this.HasOutputErrors = true;
        }

        private void WriteMessage(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }
    }
}
