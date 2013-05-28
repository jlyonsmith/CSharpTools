using System;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Tools
{
    public class VamperTool
    {
        public bool HasOutputErrors { get; set; }
        public bool ShowUsage { get; set; }
        public bool DoUpdate { get; set; }

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
            
            WriteMessage("Version file is '{0}'", versionFile);
            
            int major;
            int minor;
            int build;
            int revision;
            int startYear;
            string[] fileList;
            
            if (File.Exists(versionFile))
                ReadVersionFile(versionFile, out fileList, out major, out minor, out build, out revision, out startYear);
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
            
            string versionBuildAndRevision = String.Format("{0}.{1}", build, revision);
            string versionMajorAndMinor = String.Format("{0}.{1}", major, minor);
            string versionMajorMinorAndBuild = String.Format("{0}.{1}.{2}", major, minor, build);
            string versionFull = String.Format("{0}.{1}.{2}.{3}", major, minor, build, revision);
            string versionFullCsv = versionFull.Replace('.', ',');
            
            WriteMessage("New version will be {0}", versionFull);
           
            if (this.DoUpdate)
                WriteMessage("Updating version information:");

            foreach (string file in fileList)
            {
                string path = Path.Combine(Path.GetDirectoryName(projectSln), file);
                
                if (!File.Exists(path))
                {
                    WriteMessage("File '{0}' does not exist", path);
                    continue;
                }

                if (!this.DoUpdate)
                    continue;

                switch (Path.GetExtension(path))
                {
                case ".cs":
                    UpdateCSVersion(path, versionMajorAndMinor, versionFull);
                    break;
                    
                case ".rc":
                    UpdateRCVersion(path, versionFull, versionFullCsv);
                    break;
                    
                case ".wxi":
                    UpdateWxiVersion(path, versionMajorAndMinor, versionBuildAndRevision);
                    break;
                    
                case ".wixproj":
                case ".proj":
                    UpdateProjVersion(path, versionFull, projectName);
                    break;
                    
                case ".vsixmanifest":
                    UpdateVsixManifestVersion(path, versionFull);
                    break;
                    
                case ".config":
                    UpdateConfigVersion(path, versionMajorAndMinor);
                    break;
                    
                case ".svg":
                    UpdateSvgContentVersion(path, versionMajorMinorAndBuild);
                    break;
                    
                case ".xml":
                    if (Path.GetFileNameWithoutExtension(file) == "WMAppManifest")
                        UpdateWMAppManifestContentVersion(path, versionMajorAndMinor);
                    break;
                }
                
                WriteMessage(path);
            }

            if (this.DoUpdate)
                WriteVersionFile(versionFile, fileList, major, minor, build, revision, startYear);
        }

        private static void ReadVersionFile(
            string versionFileName, out string[] fileList, out int major, out int minor, out int build, out int revision, out int startYear)
        {
            XDocument versionDoc = XDocument.Load(versionFileName);
            fileList = (from e in versionDoc.Descendants("File") select e).Select(x => x.Value).ToArray();
            major = (int)(from e in versionDoc.Descendants("Major") select e).First();
            minor = (int)(from e in versionDoc.Descendants("Minor") select e).First();
            build = (int)(from e in versionDoc.Descendants("Build") select e).First();
            revision = (int)(from e in versionDoc.Descendants("Revision") select e).First();
            startYear = (int)(from e in versionDoc.Descendants("StartYear") select e).First();
        }

        private static void WriteVersionFile(string versionFileName, string[] fileList, int major, int minor, int build, int revision, int startYear)
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

        static void UpdateSvgContentVersion(string file, string versionMajorMinorBuild)
        {
            string contents = File.ReadAllText(file);
            
            contents = Regex.Replace(
                contents,
                @"(?'before'VERSION )([0-9]+\.[0-9]+\.[0-9]+)",
                "${before}" + versionMajorMinorBuild);
            
            File.WriteAllText(file, contents);
        }
        
        static void UpdateWMAppManifestContentVersion(string file, string versionMajorMinor)
        {
            string contents = File.ReadAllText(file);
            
            contents = Regex.Replace(
                contents,
                @"(?'before'Version="")([0-9]+\.[0-9]+)(?'after'\.[0-9]+\.[0-9]+"")",
                "${before}" + versionMajorMinor + "${after}");
            
            File.WriteAllText(file, contents);
        }
        
        static void UpdateCSVersion(string file, string versionMajorMinor, string version)
        {
            string contents = File.ReadAllText(file);
            
            // Note that we use named substitutions because otherwise Regex gets confused.  "$1" + "1.0.0.0" = "$11.0.0.0".  There is no $11.
            
            contents = Regex.Replace(
                contents,
                @"(?'before'AssemblyVersion\("")([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)(?'after'""\))",
                "${before}" + versionMajorMinor + ".0.0${after}");
            
            contents = Regex.Replace(
                contents,
                @"(?'before'AssemblyFileVersion\("")([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)(?'after'""\))",
                "${before}" + version + "${after}");
            
            File.WriteAllText(file, contents);
        }
        
        static void UpdateRCVersion(string file, string version, string versionCsv)
        {
            string contents = File.ReadAllText(file);
            
            contents = Regex.Replace(
                contents,
                @"(?'before'FILEVERSION )([0-9]+,[0-9]+,[0-9]+,[0-9]+)",
                "${before}" + versionCsv);
            
            contents = Regex.Replace(
                contents,
                @"(?'before'PRODUCTVERSION )([0-9]+,[0-9]+,[0-9]+,[0-9]+)",
                "${before}" + versionCsv);
            
            contents = Regex.Replace(
                contents,
                @"(?'before'""FileVersion"",[ \t]*"")([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)(?'after'"")",
                "${before}" + version + "${after}");
            
            contents = Regex.Replace(
                contents,
                @"(?'before'""ProductVersion"",[ \t]*"")([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)(?'after'"")",
                "${before}" + version + "${after}");
            
            File.WriteAllText(file, contents);
        }
        
        static void UpdateWxiVersion(string file, string versionMajorMinor, string versionBuildAndRevision)
        {
            string contents = File.ReadAllText(file);
            
            contents = Regex.Replace(
                contents,
                @"(?'before'ProductVersion = "")([0-9]+\.[0-9]+)(?'after'"")",
                "${before}" + versionMajorMinor + "${after}");
            
            contents = Regex.Replace(
                contents,
                @"(?'before'ProductBuild = "")([0-9]+\.([0-9]|[1-9][0-9]))(?'after'"")",
                "${before}" + versionBuildAndRevision + "${after}");
            
            File.WriteAllText(file, contents);
        }
        
        static void UpdateConfigVersion(string file, string versionMajorMinor)
        {
            // In .config files we are looking for the section that contains an assembly reference 
            // for the section handler.
            string contents = File.ReadAllText(file);
            
            contents = Regex.Replace(
                contents,
                @"(?'before', +Version=)\d+\.\d+(?'after'\.0\.0 *,)",
                "${before}" + versionMajorMinor + "${after}");
            
            File.WriteAllText(file, contents);
        }
        
        static void UpdateProjVersion(string file, string version, string projectName)
        {
            string contents = File.ReadAllText(file);
            
            contents = Regex.Replace(
                contents,
                @"(?'before'<OutputName>" + projectName + @"_)([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)(?'after'</OutputName>)",
                "${before}" + version + "${after}");
            
            File.WriteAllText(file, contents);
        }
        
        static void UpdateVsixManifestVersion(string file, string version)
        {
            string contents = File.ReadAllText(file);
            
            contents = Regex.Replace(
                contents,
                @"(?'before'<Version>)([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)(?'after'</Version>)",
                "${before}" + version + "${after}");
            
            File.WriteAllText(file, contents);
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

