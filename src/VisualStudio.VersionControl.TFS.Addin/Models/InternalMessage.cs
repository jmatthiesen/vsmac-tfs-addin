// InternalMessage.cs
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

using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace MonoDevelop.VersionControl.TFS.Models
{
    sealed class Message 
	{
		const string SoapEnvelopeNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
		const string MessageNamespace =	"http://schemas.microsoft.com/TeamFoundation/2005/06/WorkItemTracking/ClientServices/03";
		
        WebRequest _request;
		XmlTextWriter _xtw;
		string _methodName;

		public XmlTextWriter Body
		{
			get { return _xtw; }
		}	 

		public WebRequest Request
		{
			get { return _request; }
		}	 

		public string MethodName 
		{
			get { return _methodName; }
		}

		public XmlReader ResponseReader(HttpWebResponse response)
		{
			StreamReader sr = new StreamReader(response.GetResponseStream(), new UTF8Encoding (false), false);
			XmlReader reader = new XmlTextReader(sr);

			string resultName = MethodName + "Result";
			while (reader.Read())
			{
				if (reader.NodeType == XmlNodeType.Element && reader.Name == resultName)
					break;
			}

			return reader;
		}

		public Message(WebRequest request, string methodName)
		{
			_methodName = methodName;
			_request = request;
			_request.Method = "POST";

			// PreAuth can't be used with NTLM afaict
			//this.request.PreAuthenticate=true;

			// Must allow buffering for NTLM authentication
			HttpWebRequest req = request as HttpWebRequest;
			req.AllowWriteStreamBuffering = true;
			_request.ContentType = "text/xml; charset=utf-8";
			_request.Headers.Add ("X-TFS-Version", "1.0.0.0");
			_request.Headers.Add ("accept-language", "en-US");
			_request.Headers.Add ("X-VersionControl-Instance", "ac4d8821-8927-4f07-9acf-adbf71119886");

			_xtw = new XmlTextWriter (request.GetRequestStream(), new UTF8Encoding (false));
			
			_xtw.WriteStartDocument();
			_xtw.WriteStartElement("soap", "Envelope", SoapEnvelopeNamespace);
			_xtw.WriteAttributeString("xmlns", "xsi", null, XmlSchema.InstanceNamespace);
			_xtw.WriteAttributeString("xmlns", "xsd", null, XmlSchema.Namespace);

			_xtw.WriteStartElement("soap", "Body", SoapEnvelopeNamespace);
			_xtw.WriteStartElement("", methodName, MessageNamespace);
		}

		public void End ()
		{
			_xtw.WriteEndElement(); // methodName
			_xtw.WriteEndElement(); // soap:body
			_xtw.WriteEndElement(); // soap:Envelope
			_xtw.Flush();
			_xtw.Close();
		}
	}
}