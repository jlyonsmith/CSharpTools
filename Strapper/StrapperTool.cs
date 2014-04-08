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

namespace Tools
{
    public class StrapperTool
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
        public string InputFileName;
        public string ResourcesFileName;
        public string CsFileName;
        public string Namespace;
        public string BaseName;
        public string WrapperClass;
        public string Modifier;
        public bool ShowUsage;
        public bool Incremental;

        public bool HasOutputErrors { get; set; }

        #endregion

        #region Constructors

        public StrapperTool()
        {
        }

        #endregion

        #region Methods

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
          <file>                   Input .resx or .strings file.
          [-o:<output-cs>]         Specify different name for .cs file.
          [-r:<output-resources>]  Specify different name for .resources file.
          [-n:<namespace>]         Namespace to use in generated C#.
          [-b:<basename>]          The root name of the resource file without its extension 
                                   but including any fully qualified namespace name. For 
                                   example, the base name for MyNamespace.MyClass.en-US.resources
                                   would be MyNamespace.MyClass. See ResourceManager constructor 
                                   documentation for details.  If not provided the typeof the 
                                   generated class is used.
          [-w:<wrapper-class>]     String wrapper class. See Message.cs for details.
          [-a:<access>]            Access modifier for properties and methods.
          [-i]                     Incremental build. Create outputs only if out-of-date.
          [-h] or [-?]             Show help.
");
                return;
            }

            if (InputFileName == null)
            {
                WriteError("A .resx file must be specified");
                return;
            }
           
            if (WrapperClass == null)
            {
                WriteError("A string wrapper class must be specified");
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

            if (String.IsNullOrEmpty(Modifier))
            {
                Modifier = "public";
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
                Modifier, 
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

                if ((string.Equals(WrapperClass, "String", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(WrapperClass, "System.String", StringComparison.Ordinal)))
                {
                    WriteStringMethodBody(item, Path.GetFileNameWithoutExtension(CsFileName), 
                        builder.ToString(), paramCount, parameters, parametersWithTypes);
                }
                else
                {
                    WriteMessageMethodBody(item, Path.GetFileNameWithoutExtension(CsFileName), 
                        builder.ToString(), paramCount, parameters, parametersWithTypes);
                }
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

        private void WriteStringMethodBody(
            ResourceItem item, 
            string resourceClassName, 
            string methodName, 
            int paramCount, 
            string parameters, 
            string parametersWithTypes)
        {
            string str = "temp";
            if (parameters.Length > 0)
            {
                str = "string.Format(CultureInfo.CurrentCulture, temp, " + parameters + ")";
            }
            if (paramCount == 0)
            {
                writer.Write(string.Format(CultureInfo.InvariantCulture, 
                    @"
    public static {0} {1}
    {{
        get
        {{
            string temp = ResourceManager.GetString(""{2}"", CultureInfo.CurrentUICulture);
            return {3};
        }}
    }}
"
                   , WrapperClass, methodName, item.Name, str));
            }
            else
            {
                writer.Write(string.Format(CultureInfo.InvariantCulture, 
                    @"
    public static {0} {1}({2})
    {{
        string temp = ResourceManager.GetString(""{3}"", CultureInfo.CurrentUICulture);
        return {4};
    }}
"
                   , WrapperClass, methodName, parametersWithTypes, item.Name, str));
            }
        }

        private void WriteMessageMethodBody(
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
    public static {0} {1}
    {{
        get
        {{
            return new {0}(""{2}"", typeof({3}), ResourceManager, null);
        }}
    }}
"
                   , WrapperClass, methodName, item.Name, resourceClassName));
            }
            else
            {
                writer.Write(string.Format(CultureInfo.InvariantCulture, 
                    @"
    public static {0} {1}({2})
    {{
        Object[] o = {{ {4} }};
        return new {0}(""{3}"", typeof({5}), ResourceManager, o);
    }}
"
                   , WrapperClass, methodName, parametersWithTypes, item.Name, parameters, resourceClassName));
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
                    case 'i':
                        Incremental = true;
                        continue;
                    case 'b':
                        CheckAndSetArgument(arg, ref BaseName);
                        break;
                    case 'o':
                        CheckAndSetArgument(arg, ref CsFileName); 
                        break;
                    case 'r':
                        CheckAndSetArgument(arg, ref ResourcesFileName); 
                        break;
                    case 'n':
                        CheckAndSetArgument(arg, ref Namespace); 
                        break;
                    case 'w':
                        CheckAndSetArgument(arg, ref WrapperClass); 
                        break;
                    case 'm':
                        CheckAndSetArgument(arg, ref Modifier); 
                        if (Modifier != "public" && Modifier != "internal")
                            throw new ApplicationException(string.Format("Wrapper class must be public or internal"));
                        break;
                    default:
                        throw new ApplicationException(string.Format("Unknown argument '{0}'", arg[1]));
                    }
                }
                else if (String.IsNullOrEmpty(InputFileName))
                {
                    InputFileName = arg;
                }
                else
                {
                    throw new ApplicationException("Only one .resx file can be specified");
                }
            }
        }

        private void CheckAndSetArgument(string arg, ref string val)
        {
            if (arg[2] != ':')
            {
                throw new ApplicationException(string.Format("Argument {0} is missing a colon", arg[1]));
            }
   
            if (string.IsNullOrEmpty(val))
            {
                val = arg.Substring(3);
            }
            else
            {
                throw new ApplicationException(string.Format("Argument {0} has already been set", arg[1]));
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

        #endregion
    }
}
