using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Resources;
using System.Linq;
using ToolBelt;

namespace Tools
{
    [CommandLineTitle("Strongly Typed String Resource Class Wrapper Generator")]
    [CommandLineDescription("Generates wrapper methods to make parameterized string resources safer")]
    [CommandLineCopyright("Copyright (c) John Lyon-Smith 2015")]
    public class StrapperTool : ToolBase
    {
        #region Classes

        private class ResourceItem
        {
            #region Fields

            private Type dataType;
            private string name;
            private string valueString;

            #endregion

            #region Constructors

            public ResourceItem(string name, string stringResourceValue)
               : this(name, typeof(string))
            {
                this.valueString = stringResourceValue;
            }

            public ResourceItem(string name, Type dataType)
            {
                this.name = name;
                this.dataType = dataType;
            }

            #endregion

            #region Properties

            public Type DataType
            {
                get
                {
                    return this.dataType;
                }
            }

            public string Name
            {
                get
                {
                    return this.name;
                }
            }

            public string ValueString
            {
                get
                {
                    return this.valueString;
                }
            }

            #endregion
        }

        #endregion

        #region Fields
        private List<ResourceItem> resources = new List<ResourceItem>();
        private StreamWriter writer;
        private readonly char[] InvalidNameCharacters = new char[] { '.', '$', '*', '{', '}', '|', '<', '>' };
        private bool haveDrawingResources = false;
        #endregion

        [DefaultCommandLineArgument(ValueHint="INPUTFILE", Description="Input .resx or .strings file.")]
        public string InputFileName { get; set; }
        [CommandLineArgument("resources", ShortName="r", ValueHint="RESFILE", Description="Specify different name for .resources file.")]
        public string ResourcesFileName { get; set; }
        [CommandLineArgument("output", ShortName="o", ValueHint="CSFILE", Description="Specify different name for .cs file.")]
        public string CsFileName { get; set; }
        [CommandLineArgument("namespace", ShortName="n", ValueHint="NAMESPACE", Description="Namespace to use in generated C#.")]
        public string Namespace { get; set; }
        [CommandLineArgument("basename", ShortName="b", Description="The root name of the resource file without its extension but including any " +
            "fully qualified namespace name. For example, the root name for the resource file named MyApplication.MyResource.en-US.resources " +
            "is MyApplication.MyResource. See ResourceManager constructor documentation for details.  If not provided the fully qualified type name of the " +
            "generated class is used as the file name of the resource. For example, given a type named MyCompany.MyProduct.MyType, the resource manager looks " +
            "for a .resources file named MyCompany.MyProduct.MyType.resources in the assembly that defines MyType.")]
        public string BaseName { get; set; }
        [CommandLineArgument("access", ShortName="a", Description="Access modifier of the wrapper class. Must be internal or public. Default is public.",
            Initializer=typeof(StrapperTool), MethodName="ParseModifier")]
        public string AccessModifier { get; set; }
        [CommandLineArgument("help", ShortName="?", Description="Shows this help")]
        public bool ShowUsage { get; set; }
        [CommandLineArgument("incremental", ShortName="i", Description="Only update output files if the input files have changed")]
        public bool Incremental { get; set; }

        public static string ParseModifier(string arg)
        {
            if (arg == "public" || arg == "internal")
                return arg;

            throw new CommandLineArgumentException("Modifier must be public or internal");
        }

        #region Methods

        public override void Execute()
        {
            if (ShowUsage)
            {
                WriteMessage(this.Parser.LogoBanner);
                WriteMessage(this.Parser.Usage);
                return;
            }

            if (InputFileName == null)
            {
                WriteError("A .resx file must be specified");
                return;
            }
           
            if (CsFileName == null)
            {
                CsFileName = Path.ChangeExtension(InputFileName, ".cs");
            }

            if (ResourcesFileName == null)
            {
                ResourcesFileName = Path.ChangeExtension(InputFileName, ".resources");
            }

            if (String.IsNullOrEmpty(AccessModifier))
            {
                AccessModifier = "public";
            }

            DateTime resxLastWriteTime = File.GetLastWriteTime(InputFileName);

            if (Incremental &&
                resxLastWriteTime < File.GetLastWriteTime(CsFileName) &&
                resxLastWriteTime < File.GetLastWriteTime(ResourcesFileName))
            {
                return;
            }

            string ext = Path.GetExtension(InputFileName);

            if (ext == ".resx")
                ReadResXResources();
            else if (ext == ".strings")
                ReadStringsFile();
            else
            {
                WriteError("Expected file ending in .strings or .resx");
                return;
            }

            WriteMessage("Read file '{0}'", InputFileName);

            using (this.writer = new StreamWriter(CsFileName, false, Encoding.ASCII))
            {
                WriteCsFile();
            }

            WriteResourcesFile();
            WriteMessage("Generated file '{0}'", ResourcesFileName);
        }

        private void WriteResourcesFile()
        {
            IResourceWriter resourceWriter = new ResourceWriter(ResourcesFileName);

            foreach (var resource in resources)
            {
                resourceWriter.AddResource(resource.Name, resource.ValueString);
            }

            resourceWriter.Close();
        }

        private void WriteCsFile()
        {
            WriteNamespaceStart();
            WriteClassStart();

            int num = 0;

            foreach (ResourceItem item in resources)
            {
                if (item.DataType.IsPublic)
                {
                    num++;
                    this.WriteResourceMethod(item);
                }
                else
                {
                    WriteWarning("Resource skipped. Type {0} is not public.", item.DataType);
                }
            }
            WriteClassEnd();
            WriteNamespaceEnd();

            WriteMessage("Generated wrapper class '{0}' for {1} resource(s)", CsFileName, num);
        }

        private void WriteNamespaceStart()
        {
            DateTime now = DateTime.Now;
            writer.Write(String.Format(CultureInfo.InvariantCulture, 
                @"//
// This file genenerated by the Buckle tool on {0} at {1}. 
//
// Contains strongly typed wrappers for resources in {2}
//

"
               , now.ToShortDateString(), now.ToShortTimeString(), Path.GetFileName(InputFileName)));
           
            if ((Namespace != null) && (Namespace.Length != 0))
            {
                writer.Write(String.Format(CultureInfo.InvariantCulture, 
                    @"namespace {0} {{
"
                   , Namespace));
            }
            writer.Write(
                @"using System;
using System.Reflection;
using System.Resources;
using System.Diagnostics;
using System.Globalization;
"
            );

            if (haveDrawingResources)
            {
                writer.Write(
                    @"using System.Drawing;
"
                );
            }

            writer.WriteLine();
        }

        private void WriteNamespaceEnd()
        {
            if ((Namespace != null) && (Namespace.Length != 0))
            {
                writer.Write(
                    @"}
"
                );
            }
        }

        private void WriteClassStart()
        {
            writer.Write(string.Format(CultureInfo.InvariantCulture, 
                @"
/// <summary>
/// Strongly typed resource wrappers generated from {0}.
/// </summary>
{1} class {2}
{{
    internal static readonly ResourceManager ResourceManager = new ResourceManager("
                ,
                Path.GetFileName(InputFileName), 
                AccessModifier, 
                Path.GetFileNameWithoutExtension(CsFileName)));

            if (String.IsNullOrWhiteSpace(this.BaseName))
            {
                writer.Write(string.Format(CultureInfo.InvariantCulture,
                    @"typeof({0}));
"
                    , 
                    Path.GetFileNameWithoutExtension(CsFileName)));
            }
            else
            {
                writer.Write(string.Format(CultureInfo.InvariantCulture,
                    @"""{0}"", Assembly.GetExecutingAssembly());
"
                    , 
                    this.BaseName));
            }
        }

        private void WriteClassEnd()
        {
            writer.Write(
                @"}
");
        }

        private void CheckNameCharacters(string name)
        {
            for (int i = 0; i < name.Length; i++)
            {
                if (name.IndexOfAny(InvalidNameCharacters) != -1)
                    throw new FormatException(String.Format("Characters '{0}' are invalid in name", InvalidNameCharacters));
            }
        }

        private void WriteResourceMethod(ResourceItem item)
        {
            StringBuilder builder = new StringBuilder(item.Name);

            if (item.DataType == typeof(string))
            {
                string parametersWithTypes = string.Empty;
                string parameters = string.Empty;
                int paramCount = 0;
                try
                {
                    paramCount = this.GetNumberOfParametersForStringResource(item.ValueString);
                }
                catch (ApplicationException exception)
                {
                    WriteError("Resource has been skipped: {0}", exception.Message);
                }
                for (int j = 0; j < paramCount; j++)
                {
                    string str3 = string.Empty;
                    if (j > 0)
                    {
                        str3 = ", ";
                    }
                    parametersWithTypes = parametersWithTypes + str3 + "object param" + j.ToString(CultureInfo.InvariantCulture);
                    parameters = parameters + str3 + "param" + j.ToString(CultureInfo.InvariantCulture);
                }
               
                if ((item.ValueString != null) && (item.ValueString.Length != 0))
                {
                    writer.Write(
                        @"
    /// <summary>"
                    );
                    foreach (string str4 in item.ValueString.Replace("\r", string.Empty).Split("\n".ToCharArray()))
                    {
                        writer.Write(string.Format(CultureInfo.InvariantCulture, 
                            @"
    /// {0}"
                           , str4));
                    }
                    writer.Write(
                        @"
    /// </summary>"
                    );
                }

                WriteMethodBody(item, Path.GetFileNameWithoutExtension(CsFileName), 
                    builder.ToString(), paramCount, parameters, parametersWithTypes);
            }
            else
            {
                writer.Write(string.Format(CultureInfo.InvariantCulture, 
                    @"
    public static {0} {1}
    {{
        get
        {{
            return ({0})ResourceManager.GetObject(""{2}"");
        }}
    }}"
                   , item.DataType, builder, item.Name));
            }
        }

        private void WriteMethodBody(
            ResourceItem item, 
            string resourceClassName, 
            string methodName, 
            int paramCount, 
            string parameters, 
            string parametersWithTypes)
        {
            if (paramCount == 0)
            {
                writer.Write(string.Format(CultureInfo.InvariantCulture, 
                    @"
    public static string {0}
    {{
        get
        {{
            return ResourceManager.GetString(""{1}"", CultureInfo.CurrentUICulture);
        }}
    }}
"
                   , methodName, item.Name));
            }
            else
            {
                writer.Write(string.Format(CultureInfo.InvariantCulture, 
                    @"
    public static string {0}({1})
    {{
        string format = ResourceManager.GetString(""{2}"", CultureInfo.CurrentUICulture);
        return string.Format(CultureInfo.CurrentCulture, format, {3});
    }}
"
                   , methodName, parametersWithTypes, item.Name, parameters));
            }
        }

        public int GetNumberOfParametersForStringResource(string resourceValue)
        {
            string input = resourceValue.Replace("{{", "").Replace("}}", "");
            string pattern = "{(?<value>[^{}]*)}";
            int num = -1;
            for (Match match = Regex.Match(input, pattern); match.Success; match = match.NextMatch())
            {
                string str3 = null;
                try
                {
                    str3 = match.Groups["value"].Value;
                }
                catch
                {
                }
                if ((str3 == null) || (str3.Length == 0))
                {
                    throw new ApplicationException(
                        string.Format(CultureInfo.InvariantCulture, "\"{0}\": Empty format string at position {1}", new object[] {
                            input,
                            match.Index
                        }));
                }
                string s = str3;
                int length = str3.IndexOfAny(",:".ToCharArray());
                if (length > 0)
                {
                    s = str3.Substring(0, length);
                }
                int num3 = -1;
                try
                {
                    num3 = (int)uint.Parse(s, CultureInfo.InvariantCulture);
                }
                catch (Exception exception)
                {
                    throw new ApplicationException(string.Format(CultureInfo.InvariantCulture, "\"{0}\": {1}: {{{2}}}", new object[] {
                        input,
                        exception.Message,
                        str3
                    }), exception);
                }
                if (num3 > num)
                {
                    num = num3;
                }
            }
            return (num + 1);
        }

        private void ReadResXResources()
        {
            XmlDocument document = new XmlDocument();
           
            document.Load(this.InputFileName);

            Dictionary<string, string> assemblyDict = new Dictionary<string, string>();

            foreach (XmlElement element in document.DocumentElement.SelectNodes("assembly"))
            {
                assemblyDict.Add(element.GetAttribute("alias"), element.GetAttribute("name"));
            }

            resources.Clear();

            foreach (XmlElement element in document.DocumentElement.SelectNodes("data"))
            {
                string name = element.GetAttribute("name");

                // This can happen...
                if ((name == null) || (name.Length == 0))
                {
                    WriteWarning("Resource skipped. Empty name attribute: {0}", element.OuterXml);
                    continue;
                }

                CheckNameCharacters(name);
               
                Type dataType = null;
                string typeName = element.GetAttribute("type");

                if ((typeName != null) && (typeName.Length != 0))
                {
                    string[] parts = typeName.Split(',');

                    // Replace assembly alias with full name
                    typeName = parts[0] + ", " + assemblyDict[parts[1].Trim()];

                    try
                    {
                        dataType = Type.GetType(typeName, true);
                    }
                    catch (Exception exception)
                    {
                        WriteWarning("Resource skipped. Could not load type {0}: {1}", typeName, exception.Message);
                        continue;
                    }
                }
               
                ResourceItem item = null;
               
                // String resources typically have no type name
                if ((dataType == null) || (dataType == typeof(string)))
                {
                    string stringResourceValue = null;
                    XmlNode node = element.SelectSingleNode("value");
                    if (node != null)
                    {
                        stringResourceValue = node.InnerXml;
                    }
                    if (stringResourceValue == null)
                    {
                        WriteWarning("Resource skipped.  Empty value attribute: {0}", element.OuterXml);
                        continue;
                    }
                    item = new ResourceItem(name, stringResourceValue);
                }
                else
                {
                    if (dataType == typeof(Icon) || dataType == typeof(Bitmap))
                        haveDrawingResources = true;

                    item = new ResourceItem(name, dataType);
                }
                this.resources.Add(item);
            }
        }

        private void ReadStringsFile()
        {
            resources.Clear();

            using (StreamReader reader = new StreamReader(InputFileName))
            {
                string line;
                int lineNum = 1;

                while ((line = reader.ReadLine()) != null)
                {
                    int i = 0;

                    while (i < line.Length && line[i] == ' ' || line[i] == '\t')
                        i++;

                    if (i >= line.Length)
                        // Ignore empty lines
                        continue;

                    if (line.StartsWith("#"))
                        continue;

                    int j = line.IndexOf(':');

                    if (j == -1)
                        throw new FormatException(String.Format("Expected a colon at line {0}", lineNum));

                    string name = line.Substring(i, j - i);

                    CheckNameCharacters(name);

                    i = j + 1;

                    while (i < line.Length && line[i] != '"')
                        i++;

                    if (i >= line.Length)
                        throw new FormatException(String.Format("Missing opening quote at line {0}", lineNum));

                    i++; 
                    StringBuilder value = new StringBuilder();

                    while (i < line.Length)
                    {
                        if (line[i] == '\\')
                        {
                            i++;

                            if (i >= line.Length)
                                break;

                            value.Append(line[i++]);

                            continue;
                        }
                        else if (line[i] == '"')
                            break;
                        
                        value.Append(line[i++]);
                    }

                    if (i >= line.Length)
                        throw new FormatException(String.Format("String is missing closing quote at line {0}", lineNum));

                    resources.Add(new ResourceItem(name, value.ToString()));
                }
            }
        }

        #endregion
    }
}
