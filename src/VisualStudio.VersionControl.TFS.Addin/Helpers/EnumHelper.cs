// EnumHelper.cs
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

using System;
using MonoDevelop.VersionControl.TFS.Models;

namespace MonoDevelop.VersionControl.TFS.Helpers
{
    public static class EnumHelper
    {
        public static ChangeType ParseChangeType(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return ChangeType.None;
			if (Enum.TryParse<ChangeType>(value.Replace(" ", ","), true, out ChangeType changeType))
				return changeType;
			else
				return ChangeType.None;
		}

        public static ItemType ParseItemType(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return ItemType.Any;
			if (Enum.TryParse<ItemType>(value.Replace(" ", ","), true, out ItemType itemType))
				return itemType;
			else
				return ItemType.Any;
		}

        public static ConflictType ParseConflictType(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
				throw new ArgumentNullException(nameof(value));
			if (Enum.TryParse<ConflictType>(value.Replace(" ", ","), true, out ConflictType conflictType))
				return conflictType;
			else
				throw new ArgumentException("Unknown Conflict Type", nameof(value));
		}

        public static RequestType ParseRequestType(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return RequestType.None;
			if (Enum.TryParse<RequestType>(value.Replace(" ", ","), true, out RequestType requestType))
				return requestType;
			else
				return RequestType.None;
		}

        public static LockLevel ParseLockLevel(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return LockLevel.None;
			if (Enum.TryParse<LockLevel>(value.Replace(" ", ","), true, out LockLevel lockType))
				return lockType;
			else
				return LockLevel.None;
		}

        public static SeverityType ParseSeverityType(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
				throw new ArgumentNullException(nameof(value));
			
			if (Enum.TryParse(value.Replace(" ", ","), true, out SeverityType severityType))
				return severityType;
			else
				throw new ArgumentException("Unknown Severity Type", nameof(value));
		}
    }
}

