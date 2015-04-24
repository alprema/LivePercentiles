using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("LivePercentiles")]
[assembly: ComVisible(false)]
[assembly: Guid("849b3690-0ebf-4e72-a162-a9877201883f")]

[assembly: AssemblyProduct("LivePercentiles")]
[assembly: AssemblyDescription("A library to compute percentiles on the fly")]
[assembly: AssemblyCopyright("Copyright © 2015")]

[assembly: AssemblyVersion("0.1.1")]
[assembly: AssemblyFileVersion("0.1.1")]
[assembly: AssemblyInformationalVersion("0.1.1")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: InternalsVisibleTo("LivePercentiles.Tests")]