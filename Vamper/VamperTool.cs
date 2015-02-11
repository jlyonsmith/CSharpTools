using System;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using ToolBelt;

namespace Tools
{
    [CommandLineTitle("Version Stamper")]
    [CommandLineDescription("Stamps versions into project files")]
    [CommandLineCopyright("Copyright (c) John Lyon-Smith 2014")]
    public class VamperTool : ToolBase
    {
        private class FileType
        {
            public string name;
            public Regex[] fileSpecs;
            public Tuple<string, string>[] updates;
            public string write;
        }

        [CommandLineArgument("help", ShortName="?", Description="Shows this help")]
        public bool ShowUsage { get; set; }
        [CommandLineArgument("update", ShortName="u", Description="Increment the build number and update all files")]
        public bool DoUpdate { get; set; }

        public string VersionFile;
        Dictionary<string, string> tags = new Dictionary<string, string>();
        private IEnumerable<string> fileList;

        public int Major
        {
            get { return int.Parse(tags["Major"]); }
            set { tags["Major"] = value.ToString(); }
        }
        public int Minor
        {
            get { return int.Parse(tags["Minor"]); }
            set { tags["Minor"] = value.ToString(); }
        }
        public int Build
        {
            get { return int.Parse(tags["Build"]); }
            set { tags["Build"] = value.ToString(); }
        }
        public int Revision
        {
            get { return int.Parse(tags["Revision"]); }
            set { tags["Revision"] = value.ToString(); }
        }
        public int StartYear
        {
            get { return int.Parse(tags["StartYear"]); }
            set { tags["StartYear"] = value.ToString(); }
        }

        public override void Execute()
        {
            if (ShowUsage)
            {
                WriteMessage(this.Parser.LogoBanner);
                WriteMessage(this.Parser.Usage);
                return;
            }

            string versionFile = this.VersionFile;
            
            if (String.IsNullOrEmpty(versionFile))
            {
                versionFile = FindVersionFile();
            
                if (versionFile == null)
                {
                    WriteError("Unable to find a .version file in this or parent directories.");
                    return;
                }
            }
            else if (!File.Exists(versionFile))
            {
                WriteError("Version file '{0}' does not exist", versionFile);
                return;
            }
            
            string versionFileName = Path.GetFileName(versionFile);
            string projectName = versionFileName.Substring(0, versionFileName.IndexOf('.'));
            string versionConfigFile = versionFile + ".config";

            WriteMessage("Version file is '{0}'", versionFile);
            WriteMessage("Version config file is '{0}'", versionConfigFile);
            WriteMessage("Project name is '{0}'", projectName);

            if (File.Exists(versionFile))
            {
                if (!ReadVersionFile(versionFile))
                    return;
            }
            else
            {
                Major = 1;
                Minor = 0;
                Build = 0;
                Revision = 0;
                StartYear = DateTime.Now.Year;

                fileList = new string[] { };
            }
            
            int jBuild = ProjectDate(StartYear);
            
            if (Build != jBuild)
            {
                Revision = 0;
                Build = jBuild;
            }
            else
            {
                Revision++;
            }

            WriteMessage("New version {0} be {1}.{2}.{3}.{4}", this.DoUpdate ? "will" : "would", Major, Minor, Build, Revision);
           
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
            IEnumerable<string> expandedFileList = fileList.Select(x => ReplaceTags(x));

            foreach (string file in expandedFileList)
            {
                string path = Path.Combine(Path.GetDirectoryName(versionFile), file);
                string fileOnly = Path.GetFileName(file);
                bool match = false;

                foreach (var fileType in fileTypes)
                {
                    // Find files of this type
                    foreach (var fileSpec in fileType.fileSpecs)
                    {
                        if (fileSpec.IsMatch(fileOnly))
                        {
                            match = true;
                            break;
                        }
                    }

                    if (!match)
                        // We did not find one, ignore it
                        continue;

                    // Are we just writing a file or updating an existing one?
                    if (String.IsNullOrEmpty(fileType.write))
                    {
                        if (!File.Exists(path))
                        {
                            WriteError("File '{0}' does note exist to update", path);
                            return;
                        }
                        
                        if (DoUpdate)
                        {
                            foreach (var update in fileType.updates)
                            {
                                string contents = File.ReadAllText(path);
    
                                contents = Regex.Replace(contents, update.Item1, update.Item2);

                                File.WriteAllText(path, contents);
                            }
                        }
                    }
                    else
                    {
                        string dir = Path.GetDirectoryName(path);
                        
                        if (!Directory.Exists(dir))
                        {
                            WriteError("Directory '{0}' does not exist to write file '{1}'", dir, Path.GetFileName(path));
                            return;
                        }
                        
                        if (DoUpdate)
                            File.WriteAllText(path, fileType.write);
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

        private string ReplaceTags(string input)
        {
            return StringUtility.ReplaceTags(input, "${", "}", tags, TaggedStringOptions.LeaveUnknownTags);
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
                    x => new Tuple<string, string>((string)x.Element("Search"), ReplaceTags((string)x.Element("Replace")))).ToArray();
                fileType.write = ReplaceTags((string)fileTypeElement.Element("Write"));

                fileTypes.Add(fileType);
            }

            return fileTypes;
        }

        private bool ReadVersionFile(string versionFileName)
        {
            XDocument versionDoc = XDocument.Load(versionFileName);

            tags = versionDoc.Root.Elements().Where(x => x.Name != "Files").ToDictionary<XElement, string, string>(
                x => x.Name.ToString(), x => x.Value);

            if (!tags.ContainsKey("Major") || !tags.ContainsKey("Minor") || 
                !tags.ContainsKey("Build") || !tags.ContainsKey("Revision") || 
                !tags.ContainsKey("StartYear"))
            {
                WriteError("Version file must at least contain Major, Minor, Build, Revision and StartYear tags");
                return false;
            }

            var filesNode = versionDoc.Root.Element("Files");

            if (filesNode == null)
            {
                WriteError("Version file must contain a Files node");
                return false;
            }

            fileList = filesNode.Descendants().Select(x => (string)x).ToArray();

            return true;
        }

        private void WriteVersionFile(string versionFileName, IEnumerable<string> fileList)
        {
            XElement doc = 
                new XElement("Version",
                    new XElement("Files", fileList.Select(f => new XElement("File", f)).ToArray()),
                    tags.Select(t => new XElement(t.Key, t.Value)).ToArray()
                );

            doc.Save(versionFileName);
        }

        private string FindVersionFile()
        {
            var fileSpec = "*.version";
            string dir = Environment.CurrentDirectory;

            do
            {
                string[] files = Directory.GetFiles(dir, fileSpec);
            
                if (files.Length > 0)
                {
                    return files[0];
                }
            
                int i = dir.LastIndexOf(Path.DirectorySeparatorChar);
            
                if (i <= 0)
                    break;
            
                dir = dir.Substring(0, i);
            }
            while (true);

            return null;
        }

        static private int ProjectDate(int startYear)
        {
            DateTime today = DateTime.Today;
            
            return (((today.Year - startYear + 1) * 10000) + (today.Month * 100) + today.Day);
        }
    }
}

