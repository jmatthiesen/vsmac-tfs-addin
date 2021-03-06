﻿// CheckInResult.cs
// 
// Authors:
//       Ventsislav Mladenov
//       Javier Suárez Ruiz
// 
// The MIT License (MIT)
// 
// Copyright (c) 2013-2018 Ventsislav Mladenov, Javier Suárez Ruiz
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using MonoDevelop.VersionControl.TFS.Helpers;

namespace MonoDevelop.VersionControl.TFS.Models
{
    public class CheckInResult
    {
        CheckInResult()
        {
            
        }

        public static CheckInResult FromXml(XElement el)
        {
            var ns = el.Name.Namespace;
            var result = new CheckInResult();
            result.ChangeSet = el.GetIntAttribute("cset");
            result.UndoneServerItems = new List<string>();
          
            if (el.Element(ns + "UndoneServerItems") != null)
            {
                result.UndoneServerItems.AddRange(el.Element(ns + "UndoneServerItems").Elements(ns + "string").Select(x => x.Value));
            }
           
            result.LocalVersionUpdates = new List<GetOperation>();
          
            if (el.Element(ns + "LocalVersionUpdates") != null)
            {
                result.LocalVersionUpdates.AddRange(el.Element(ns + "LocalVersionUpdates").Elements(ns + "GetOperation").Select(GetOperation.FromXml));
            }

            return result;
        }

        public int ChangeSet { get; set; }

        internal List<GetOperation> LocalVersionUpdates { get; set; }

        public List<Failure> Failures { get; set; }

        public List<string> UndoneServerItems { get; set; }
    }
}