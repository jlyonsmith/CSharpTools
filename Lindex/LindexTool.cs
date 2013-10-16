using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;

namespace Tools
{
    public class LindexTool
    {
        public bool HasOutputErrors { get; set; }

        public bool ShowUsage;
        public string InputFile { get; set; }

        public LindexTool()
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
            
            if (String.IsNullOrEmpty(InputFile))
            {
                WriteError("No input text file specified");
                return;
            }

            string outputFile = InputFile + ".lines";

            using (StreamReader reader = new StreamReader(InputFile))
            {
                using (BinaryWriter writer = new BinaryWriter(new FileStream(outputFile, FileMode.Create)))
                {
                    long offset = 0;
                    int c;

                    writer.Write(offset);

                    while (true)
                    {
                        c = reader.Read();
                        if (c == -1)
                            break;
                        offset++;

                        if (c == (int)'\r')
                        {
                            if (reader.Peek() == (int)'\n')
                            {
                                c = reader.Read();
                                if (c == -1)
                                    break;
                                offset++;
                            }

                            writer.Write(offset);
                        }
                        else if (c == (int)'\n')
                        {
                            writer.Write(offset);
                        }
                    }
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
                else if (String.IsNullOrEmpty(InputFile))
                {
                    InputFile = arg;
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
