﻿using System;

namespace MonoDevelop.VersionControl.TFS.Models
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public class MonoDevelopVersionAttribute : Attribute
    {
        public MonoDevelopVersionAttribute(string version)
        {
            Version = Version.Parse(version);    
        }

        public Version Version { get; private set; }
    }
}