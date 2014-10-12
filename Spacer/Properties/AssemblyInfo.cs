using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyTitle("C# Spacer Tool")]
[assembly: AssemblyDescription("Text file tab/space reporter and fixer. " +
                               "The tool reports on beginning-of-line tabs/spaces. " + 
                               "All tabs not at the beginning of a line are replaced with spaces. " + 
                               "Spaces/tabs inside C# multi-line strings are ignored.")]
[assembly: AssemblyCompany("John Lyon-Smith")]
[assembly: AssemblyProduct("C# Tools")]
[assembly: AssemblyCopyright("Copyright (c) John Lyon-Smith 2013")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("2.1.0.0")]
[assembly: AssemblyFileVersion("2.1.11012.0")]
