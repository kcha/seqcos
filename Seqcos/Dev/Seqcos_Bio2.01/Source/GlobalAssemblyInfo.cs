// *********************************************************************
// 
//     Copyright (c) 2011 Microsoft. All rights reserved.
//     This code is licensed under the Apache License, Version 2.0.
//     THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
//     ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
//     IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
//     PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
// 
// *********************************************************************

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyCompany("Microsoft")]
[assembly: AssemblyProduct("Seqcos")]
[assembly: AssemblyDescription("Sequence Quality Control Studio (SeQCoS) is an open source .NET software suite designed to perform quality control of massively parallel sequencing reads.")]
[assembly: AssemblyCopyright("Copyright © 2011 Microsoft")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyVersion("1.0.5.*")]
//[assembly: AssemblyFileVersion("1.0.*")]


namespace GlobalAssemblyAttributes
{
    public static class GlobalAssemblyAttributes
    {
        public static string Copyright
        {
            get
            {
                var attr = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), true);
                if (attr == null || attr.Length == 0) return null;
                return (attr[0] as AssemblyCopyrightAttribute).Copyright;
            }
        }

        public static string Version
        {
            get
            {
                var attr = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
                return attr;
            }
        }
    }
}