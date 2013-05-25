using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyTitle("Spacer Tool")]
[assembly: AssemblyDescription("Text file tab/space reporter and fixer")]
[assembly: AssemblyCompany("John Lyon-Smith")]
[assembly: AssemblyProduct("C# Tools")]
[assembly: AssemblyCopyright("Copyright (c) John Lyon-Smith 2013")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.7.0.0")]
