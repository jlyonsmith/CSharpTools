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
[assembly: AssemblyCompany("${Company}")]
[assembly: AssemblyProduct("${Product}")]
[assembly: AssemblyCopyright("${Copyright}")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("2.2.0.0")]
[assembly: AssemblyFileVersion("2.2.20212.0")]
