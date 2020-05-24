using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using MethodDecorator.Fody.Interfaces;
using TokenGenLib;

[assembly: EmbeddedTokenRepo] //Register this Fody Extended Attribute.

namespace TokenGenLib
{
  [AttributeUsage(AttributeTargets.Method | /*AttributeTargets.Constructor |*/ AttributeTargets.Assembly | AttributeTargets.Module)]
  public class EmbeddedTokenRepoAttribute : Attribute, IMethodDecorator
  {
    // instance, method and args can be captured here and stored in attribute instance fields
    // for future usage in OnEntry/OnExit/OnException
    public void Init(object instance, MethodBase method, object[] args)
    {
      //From the instance of the attribute, get the servername, and locate and memorize the API Server token manger
    }

    public void OnEntry()
    {
    }

    public void OnExit()
    {
    }

    public void OnException(Exception exception)
    {
    }
  }
}
