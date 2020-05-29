using System.Runtime.CompilerServices;
using TokenGenLib.Fody;

[module: ApiCallTracker] //Register this Fody Extended Attribute.
[assembly: InternalsVisibleTo("TokenGen.Ext", AllInternalsVisible = true)]

namespace TokenGenLib
{

  class AssemblyInfo
  {
  }
}
