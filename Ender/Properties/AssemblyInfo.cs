using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyTitle("Ender Line Ending Fixer")]
[assembly: AssemblyDescription("Reports on and optionally fixes line endings in text files")]
[assembly: AssemblyCompany("John Lyon-Smith")]
[assembly: AssemblyProduct("C# Tools")]
[assembly: AssemblyCopyright("Copyright (c) John Lyon-Smith 2012")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.7.0.0")]
[assembly: AssemblyFileVersion("1.7.20317.4")]