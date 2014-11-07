//
// This file genenerated by the Buckle tool on 11/7/2014 at 1:25 PM. 
//
// Contains strongly typed wrappers for resources in Test.resx
//

using System;
using System.Reflection;
using System.Resources;
using System.Diagnostics;
using System.Globalization;


/// <summary>
/// Strongly typed resource wrappers generated from Test.resx.
/// </summary>
public class Test
{
    internal static readonly ResourceManager ResourceManager = new ResourceManager(typeof(Test));

    /// <summary>
    /// A message
    /// </summary>
    public static Message MessageNoArgs
    {
        get
        {
            return new Message("MessageNoArgs", typeof(Test), ResourceManager, null);
        }
    }

    /// <summary>
    /// A message with {0} args
    /// </summary>
    public static Message MessageOneArg(object param0)
    {
        Object[] o = { param0 };
        return new Message("MessageOneArg", typeof(Test), ResourceManager, o);
    }

    /// <summary>
    /// A message {0} with {1} two args
    /// </summary>
    public static Message MessageTwoArgs(object param0, object param1)
    {
        Object[] o = { param0, param1 };
        return new Message("MessageTwoArgs", typeof(Test), ResourceManager, o);
    }
}
