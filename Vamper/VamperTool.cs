using System;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;

namespace Tools
{
    public class VamperTool
    {
        private class FileType
        {
            public string name;
            public Regex[] fileSpecs;
            public Tuple<string, string>[] updates;
            public string[] writes;
        }

        public bool HasOutputErrors { get; set; }
        public bool ShowUsage { get; set; }
        public bool DoUpdate { get; set; }
        public bool WriteDefaultFiles { get; set; }

        private int major;
        private int minor;
        private int build;
        private int revision;
        private int startYear;

        public VamperTool()
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
    [-n]                Write default version files out to current directory.
    [-u]                Actually do the version stamp update.
    [-h] or [-?]        Show this help.
");
                return;
            }

            string projectSln = GetProjectSolution();
            
            if (projectSln == null)
            {
                WriteMessage("Cannot find .sln file to determine project root.");
            }
            
            WriteMessage("Project root is '{0}'", Path.GetDirectoryName(projectSln));
            
            string projectFileName = Path.GetFileName(projectSln);
            string projectName = projectFileName.Substring(0, projectFileName.IndexOf('.'));
            string versionFile = Path.Combine(Path.GetDirectoryName(projectSln), projectName + ".version");
            string versionConfigFile = versionFile + ".config";

            WriteMessage("Version file is '{0}'", versionFile);
            WriteMessage("Version config file is '{0}'", versionConfigFile);

            string[] fileList;
            
            if (File.Exists(versionFile))
                fileList = ReadVersionFile(versionFile);
            else
            {
                major = 1;
                minor = 0;
                build = 0;
                revision = 0;
                startYear = DateTime.Now.Year;
                fileList = new string[] { };
            }
            
            int jBuild = JDate(startYear);
            
            if (build != jBuild)
            {
                revision = 0;
                build = jBuild;
            }
            else 
            {
                revision++;
            }

            WriteMessage("New version will be {0}.{1}.{2}.{3}", major, minor, build, revision);
           
            if (this.DoUpdate)
                WriteMessage("Updating version information:");

            if (!File.Exists(versionConfigFile))
            {
                using (StreamReader reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Tools.Default.version.config")))
                {
                    File.WriteAllText(versionConfigFile, reader.ReadToEnd());
                }
            }

            List<FileType> fileTypes = ReadVersionConfigFile(versionConfigFile);

            foreach (string file in fileList)
            {
                string path = Path.Combine(Path.GetDirectoryName(projectSln), file);
                
                if (!this.DoUpdate)
                    continue;

                bool match = false;

                foreach (var fileType in fileTypes)
                {
                    foreach (var fileSpec in fileType.fileSpecs)
                    {
                        if (fileSpec.IsMatch(path))
                        {
                            match = true;
                            break;
                        }
                    }

                    if (!match)
                        continue;

                    if (fileType.updates.Length > 0 && !File.Exists(path))
                    {
                        WriteMessage("File '{0}' does not exist to update", path);
                        continue;
                    }

                    foreach (var update in fileType.updates)
                    {
                        string contents = File.ReadAllText(path);

                        contents = Regex.Replace(contents, update.Item1, update.Item2);

                        File.WriteAllText(path, contents);
                    }

                    foreach (var write in fileType.writes)
                    {
                        File.WriteAllText(path, write);
                    }

                    break;
                }

                if (!match)
                {
                    WriteError("File '{0}' has no matching file type in the .version.config file", path);
                    return;
                }

                WriteMessage(path);
            }

            if (this.DoUpdate)
                WriteVersionFile(versionFile, fileList);
        }

        private static Regex WildcardToRegex(string pattern)
        {
            return new Regex("^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$"); 
        }

        private string SubstituteVersions(string input)
        {
            StringBuilder sb = new StringBuilder(input);

            sb.Replace("${Major}", major.ToString());
            sb.Replace("${Minor}", minor.ToString());
            sb.Replace("${Build}", build.ToString());
            sb.Replace("${Revision}", revision.ToString());
            sb.Replace("${StartYear}", startYear.ToString());

            return sb.ToString();
        }

        private List<FileType> ReadVersionConfigFile(string versionConfigFileName)
        {
            XDocument versionConfigFile = XDocument.Load(versionConfigFileName);
            var fileTypes = new List<FileType>();

            foreach (var fileTypeElement in versionConfigFile.Descendants("FileType"))
            {
                var fileType = new FileType();

                fileType.name = (string)fileTypeElement.Element("Name");
                fileType.fileSpecs = fileTypeElement.Elements("FileSpec").Select<XElement, Regex>(x => WildcardToRegex((string)x)).ToArray();
                fileType.updates = fileTypeElement.Elements("Update").Select<XElement, Tuple<string, string>>(
                    x => new Tuple<string, string>((string)x.Element("Search"), SubstituteVersions((string)x.Element("Replace")))).ToArray();
                fileType.writes = fileTypeElement.Elements("Write").Select(x => SubstituteVersions((string)x)).ToArray();

                fileTypes.Add(fileType);
            }

            return fileTypes;
        }

        private string[] ReadVersionFile(string versionFileName)
        {
            XDocument versionDoc = XDocument.Load(versionFileName);
            var fileList = versionDoc.Descendants("File").Select(x => (string)x).ToArray();

            major = (int)(versionDoc.Descendants("Major").First());
            minor = (int)(versionDoc.Descendants("Minor").First());
            build = (int)(versionDoc.Descendants("Build").First());
            revision = (int)(versionDoc.Descendants("Revision").First());
            startYear = (int)(versionDoc.Descendants("StartYear").First());

            return fileList;
        }

        private void WriteVersionFile(string versionFileName, string[] fileList)
        {
            XElement doc = 
                new XElement("Version",
                    new XElement("Files", fileList.Select(f => new XElement("File", f)).ToArray()),
                    new XElement("Major", major),
                    new XElement("Minor", minor),
                    new XElement("Build", build),
                    new XElement("Revision", revision),
                    new XElement("StartYear", startYear));

            doc.Save(versionFileName);
        }

        private string GetProjectSolution()
        {
            string fileSpec = "*.sln";

            try
            {
                string dir = Environment.CurrentDirectory;

                do 
                {
                    string[] files = Directory.GetFiles(dir, fileSpec);

                    if (files.Length > 0)
                    {
                        return files[0];
                    }

                    int i = dir.LastIndexOf(Path.DirectorySeparatorChar);

                    if (i == -1)
                        break;

                    dir = dir.Substring(0, i);
                }
                while (true);

                WriteError("Unable to find file '{0}' to determine project root", fileSpec);
            }
            catch (Exception e)
            {
                WriteError("Error looking for file '{0}'. {1}", fileSpec, e.Message);
            }

            return null;
        }
        
        static private int JDate(int startYear)
        {
            DateTime today = DateTime.Today;
            
            return (((today.Year - startYear + 1) * 10000) + (today.Month * 100) + today.Day);
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
                    case 'u':
                        DoUpdate = true;
                        return;
                    default:
                        throw new ApplicationException(string.Format("Unknown argument '{0}'", arg[1]));
                    }
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

