using Corvalius.Membership.Raven;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Web;

// Ensure that the application start code gets called at the start of the application environment.
[assembly: PreApplicationStartMethod(typeof(PreApplicationStartCode), "Start")]
[assembly: AssemblyTitle("corvalius-raven-membership")]
[assembly: AssemblyProduct("Corvalius.Membership.Raven")]