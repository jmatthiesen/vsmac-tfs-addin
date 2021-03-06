﻿// LoggingService.cs
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
using System.IO;
using System.Text;
using MonoDevelop.Core.Logging;

namespace MonoDevelop.VersionControl.TFS.Services
{
	/// <summary>
    /// Logging service.
    /// </summary>
    sealed class LoggingService : ILoggingService
    {
        readonly static object locker = new object();
        readonly Configuration _configuration;

        public LoggingService(IConfigurationService configurationService)
        {
            _configuration = configurationService.Load();
        }

        /// <summary>
		/// Logs to debug. Create a MonoDevelop.VersionControl.TFS.Debug.log file.
        /// </summary>
        /// <param name="message">Message.</param>
        public void LogToDebug(string message)
        {
            if (_configuration.DebugMode)
            {
                lock (locker)
                {
                    var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "MonoDevelop.VersionControl.TFS.Debug.log");
                    File.AppendAllText(path, message, Encoding.UTF8);
                }
            }
        }

        /// <summary>
        /// Logs info.
        /// </summary>
        /// <param name="message">Message.</param>
        public void LogToInfo(string message)
        {
            Core.LoggingService.Log(LogLevel.Info, message);
        }

        /// <summary>
        /// Logs an error.
        /// </summary>
        /// <param name="message">Message.</param>
        public void LogToError(string message)
        {
            Core.LoggingService.Log(LogLevel.Error, message);
        }
    }
}
