using System;

namespace TokenGen.Ext
{
  public static class ServerExtractor
  {
    public static string ServerName(Uri url)
    {
      return $@"{ url.Host}_{url.Port}";
    }
  }
}
