//
// WorkItemType.cs
//
// Authors:
//       Ventsislav Mladenov <vmladenov.mladenov@gmail.com>
//       Javier Suárez Ruiz  <javiersuarezruiz@hotmail.com>
//
// Copyright (c) 2018 Ventsislav Mladenov, Javier Suárez Ruiz
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

using Microsoft.TeamFoundation.WorkItemTracking.Client.Metadata;

namespace Microsoft.TeamFoundation.WorkItemTracking.Client.Objects
{
    public class WorkItemType
    {
        [TableFieldName("WorkItemTypeID")]
        public int Id { get; set; }

        [TableFieldName("NameConstantID")]
        public int NameConstantId { get; set; }

        public Constant Name { get; set; }
       
        [TableFieldName("ProjectID")]
        public int ProjectId { get; set; }

        public Project Project { get; set; }
       
        [TableFieldName("fDeleted")]
        public bool IsDeleted { get; set; }
    }
}