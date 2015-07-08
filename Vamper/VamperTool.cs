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
    public enum BuildValueType 
    {
        Incremental,
        JDate,
        FullDate
    }

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
        [DefaultCommandLineArgument(ValueHint="VERSION_FILE", Description="Version file")]
        public ParsedPath VersionFile { get; set; }

        Dictionary<string, string> tags = new Dictionary<string, string>();
        IEnumerable<string> fileList;
        BuildValueType buildValueType;
        TimeZoneInfo timeZoneInfo = TimeZoneInfo.Utc;
        DateTime today;

        public BuildValueType BuildValueType 
        {
            get { return buildValueType; }
        }
        public TimeZoneInfo TimeZone 
        {
            get { return timeZoneInfo; }
        }
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
        public int Patch
        {
            get 
            {
                string patch;

                if (tags.TryGetValue("Patch", out patch))
                    return int.Parse(patch);
                else
                    return 0;
            }
            set { tags["Patch"] = value.ToString(); }
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
            get 
            { 
                string startYear;

                if (tags.TryGetValue("StartYear", out startYear))
                    return int.Parse(startYear);
                else
                    return this.today.Year;
            }
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

            var versionFile = this.VersionFile;
            
            if (String.IsNullOrEmpty(versionFile))
            {
                var files = DirectoryUtility.GetFiles(new ParsedFilePath("*.version"), SearchScope.RecurseParentDirectories);
            
                if (files.Count == 0)
                {
                    WriteError("Unable to find a .version file in this or parent directories.");
                    return;
                }

                versionFile = files[0];
            }
            else if (!File.Exists(versionFile))
            {
                WriteError("Version file '{0}' does not exist", versionFile);
                return;
            }
            
            string projectName = versionFile.File;
            var versionConfigFile = versionFile.WithExtension(".version.config");

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
                Patch = 0;
                Build = 0;
                Revision = 0;
                StartYear = DateTime.Now.Year;

                fileList = new string[] { };
            }

            switch (buildValueType)
            {
            case BuildValueType.JDate:
                int jDateBuild = GetJDate();
                
                if (Build != jDateBuild)
                {
                    Revision = 0;
                    Build = jDateBuild;
                }
                else
                {
                    Revision++;
                }
                break;
            case BuildValueType.FullDate:
                int fullDateBuild = GetFullDate();

                if (Build != fullDateBuild)
                {
                    Revision = 0;
                    Build = fullDateBuild;
                }
                else
                {
                    Revision++;
                }
                break;
            case BuildValueType.Incremental:
                Build++;
                Revision = 0;
                break;
            }

            StringBuilder sb = new StringBuilder("Version data is:");

            foreach (var pair in tags)
            {
                sb.AppendFormat("\n  {0}=\"{1}\"", pair.Key, pair.Value);
            }

            WriteMessage(sb.ToString());
           
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
    
                                contents = Regex.Replace(contents, update.Item1, update.Item2, 
                                    RegexOptions.Multiline | RegexOptions.ExplicitCapture);

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

        private List<FileType> ReadVersionConfigFile(ParsedPath versionConfigFileName)
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

        private bool ReadVersionFile(ParsedPath versionFileName)
        {
            XDocument versionDoc = XDocument.Load(versionFileName);
            var buildValueTypeAttr = versionDoc.Root.Attribute("BuildValueType");

            if (buildValueTypeAttr == null)
            {
                this.buildValueType = BuildValueType.JDate;
            }
            else
            {
                this.buildValueType = (BuildValueType)Enum.Parse(typeof(BuildValueType), buildValueTypeAttr.Value, ignoreCase: true);
            }

            tags = versionDoc.Root.Elements().Where(x => x.Name != "Files").ToDictionary<XElement, string, string>(
                x => x.Name.ToString(), x => x.Value);

            if (!tags.ContainsKey("Major") || !tags.ContainsKey("Minor") || 
                !tags.ContainsKey("Build") || !tags.ContainsKey("Revision"))
            {
                WriteError("Version file must at least contain at least Major, Minor, Build, Revision tags");
                return false;
            }

            string timeZoneId; 

            if (tags.TryGetValue("TimeZone", out timeZoneId))
            {
                try
                {
                    this.timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                }
                catch (Exception ex)
                {
                    throw new ApplicationException("TimeZone '{0}' was not found".CultureFormat(timeZoneId), ex);
                }
            }

            this.today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, this.timeZoneInfo);

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
                    new XAttribute("BuildValueType", this.buildValueType.ToString()),
                    new XElement("Files", fileList.Select(f => new XElement("File", f)).ToArray()),
                    tags.Select(t => new XElement(t.Key, t.Value)).ToArray()
                );

            doc.Save(versionFileName);
        }

        private int GetFullDate()
        {
            return today.Year * 10000 + today.Month * 100 + today.Day;
        }

        private int GetJDate()
        {
            return (((today.Year - this.StartYear + 1) * 10000) + (today.Month * 100) + today.Day);
        }
    }
}

