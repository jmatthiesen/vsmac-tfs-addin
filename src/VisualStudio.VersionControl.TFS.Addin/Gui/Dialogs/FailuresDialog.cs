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
    public class FailuresDialog : Dialog
    {
		VBox _view;
		ListView _failuresView;
		DataField<string> _typeField;
		DataField<string> _messageField;
		DataField<Failure> _failureField;
        ListStore _failuresStore;

        public FailuresDialog(IEnumerable<Failure> failures)
        {
            Init();
            BuildGui();
            GetData(failures);
        }

        void Init()
        {
			_view = new VBox();
			_failuresView = new ListView();
			_typeField = new DataField<string>();
			_messageField = new DataField<string>();
			_failureField = new DataField<Failure>();
			_failuresStore = new ListStore(_typeField, _messageField, _failureField);   
		}

        void BuildGui()
        {
            Title = GettextCatalog.GetString("Failures");
            
            var titleLabel = new Label(Title + ":");
            
			_view.PackStart(titleLabel, false, false);
            _failuresView.WidthRequest = 800;
            _failuresView.HeightRequest = 200;
			_failuresView.Columns.Add(new ListViewColumn("Type", new TextCellView(_typeField) { Editable = false }));
			_failuresView.Columns.Add(new ListViewColumn("Message", new TextCellView(_messageField) { Editable = false }));           
			_failuresView.DataSource = _failuresStore;

			_view.PackStart(_failuresView, true, true);
            
			HBox buttonBox = new HBox();

            var closeButton = new Button(GettextCatalog.GetString("Close"))
            {
				HorizontalPlacement = WidgetPlacement.End,
                MinWidth = GuiSettings.ButtonWidth
            };
            closeButton.Clicked += (sender, e) => Respond(Command.Close);
			buttonBox.PackEnd(closeButton);

			_view.PackEnd(buttonBox);
			Content = _view;
			Resizable = false;
        }

        void GetData(IEnumerable<Failure> failures)
        {
            _failuresStore.Clear();

            foreach (var item in failures)
            {
				var row = _failuresStore.AddRow();
				_failuresStore.SetValue(row, _typeField, item.SeverityType.ToString());
				_failuresStore.SetValue(row, _messageField, item.Message);
				_failuresStore.SetValue(row, _failureField, item);
            }
        }
    }
}