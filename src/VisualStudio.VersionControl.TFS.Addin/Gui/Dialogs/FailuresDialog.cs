// FailuresDialog.cs
// 
// Author:
//       Javier Suárez Ruiz
// 
// The MIT License (MIT)
// 
// Copyright (c) 2018 Javier Suárez Ruiz
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
using MonoDevelop.Core;
using MonoDevelop.VersionControl.TFS.Models;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Dialogs
{
    public class FailuresDialog : Gtk.Dialog
    {
        Gtk.TreeView _failuresView;
        Gtk.ListStore _failuresStore;

        public FailuresDialog(IEnumerable<Failure> failures)
        {
            Init();
            BuildGui();
            GetData(failures);
        }

        void Init()
        {
            _failuresView = new Gtk.TreeView();
            _failuresStore = new Gtk.ListStore(typeof(string), typeof(string), typeof(Failure));     }

        void BuildGui()
        {
            Title = GettextCatalog.GetString("Failures");

            var lbl = new Gtk.Label(Title + ":");
            var align = new Gtk.Alignment(0, 0, 0, 0);
            lbl.Justify = Gtk.Justification.Left;
            align.Add(lbl);

            VBox.PackStart(align, false, false, 0);
            _failuresView.WidthRequest = 300;
            _failuresView.HeightRequest = 200;
            _failuresView.AppendColumn("Type", new Gtk.CellRendererText(), "text", 0);
            _failuresView.AppendColumn("Message", new Gtk.CellRendererText(), "text", 1);
            _failuresView.HasTooltip = true;
            _failuresView.Model = _failuresStore;

            VBox.PackStart(_failuresView, true, true, 0);
            AddButton(Gtk.Stock.Ok, Gtk.ResponseType.Ok);

            ShowAll();
        }

        void GetData(IEnumerable<Failure> failures)
        {
            _failuresStore.Clear();

            foreach (var item in failures)
            {
                _failuresStore.AppendValues(item.SeverityType.ToString(), item.Message, item);
            }
        }
    }
}