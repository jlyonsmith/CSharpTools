using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyTitle("Cleaner C# Project Mega Cleaner")]
[assembly: AssemblyDescription("Deletes all bin/Xxx and obj/Xxx directories in a tree of C# projects")]
[assembly: AssemblyCompany("John Lyon-Smith")]
[assembly: AssemblyProduct("CSharpTools")]
[assembly: AssemblyCopyright("Copyright (c) 2015, John Lyon-Smith")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("3.0.0.0")]
[assembly: AssemblyFileVersion("3.0.10708.1")]
