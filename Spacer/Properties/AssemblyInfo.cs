using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyTitle("Text File Space/Tab Line Fixer Tool")]
[assembly: AssemblyDescription("Text file tab/space reporter and fixer. " +
                               "The tool reports on beginning-of-line tabs/spaces. " + 
                               "All tabs not at the beginning of a line are replaced with spaces. " + 
                               "Spaces/tabs inside C# multi-line strings are ignored.")]
[assembly: AssemblyCompany("${Company}")]
[assembly: AssemblyProduct("${Product}")]
[assembly: AssemblyCopyright("${Copyright}")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("2.2.0.0")]
[assembly: AssemblyFileVersion("2.2.20212.0")]
