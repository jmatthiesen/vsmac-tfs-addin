// SelectProjectDialog.cs
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
using MonoDevelop.VersionControl.TFS.Services;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Dialogs
{
    public class SelectProjectDialog : Dialog
    {
        ProjectCollection _projectCollection;
        TreeView _treeView;
        TreeStore _treeStore;
        DataField<string> _name;
        DataField<string> _path;

        internal SelectProjectDialog(ProjectCollection projectCollection)
        {
            Init(projectCollection);
            BuildGui();
            GetData();
        }

        public string SelectedPath
        {
            get
            {
                if (_treeView.SelectedRow == null)
                    return string.Empty;
                
                var node = _treeStore.GetNavigatorAt(_treeView.SelectedRow);

                return node.GetValue(_path);
            }
        }

        void Init(ProjectCollection projectCollection)
        {
            _projectCollection = projectCollection;

            _treeView = new TreeView();
            _name = new DataField<string>();
            _path = new DataField<string>();

            _treeStore = new TreeStore(_name, _path);
        }

        void BuildGui()
        {
            Title = GettextCatalog.GetString("Browse for Folder");
          
            VBox content = new VBox();
            content.PackStart(new Label(GettextCatalog.GetString("Team Foundation Server") + ":"));
            content.PackStart(new TextEntry
            { 
                Text = _projectCollection.Server.Name + " - " + _projectCollection.Name, 
                Sensitive = false,
                MinWidth = 300
            });

            content.PackStart(new Label(GettextCatalog.GetString("Folders") + ":"));

            _treeView.Columns.Add(new ListViewColumn("Name", new TextCellView(_name) { Editable = false }));
            _treeView.DataSource = _treeStore;
            _treeView.MinWidth = 400;
            _treeView.MinHeight = 300;
            content.PackStart(_treeView, true, true);

            content.PackStart(new Label(GettextCatalog.GetString("Folder path") + ":"));

			TextEntry folderPathEntry = new TextEntry
			{
				Sensitive = false
			};

			_treeView.SelectionChanged += (sender, e) => folderPathEntry.Text = SelectedPath;
            content.PackStart(folderPathEntry);

            HBox buttonBox = new HBox();

            Button nextButton = new Button(GettextCatalog.GetString("OK"))
            {
                MinWidth = GuiSettings.ButtonWidth
            };
            nextButton.Clicked += (sender, e) => Respond(Command.Ok);
            buttonBox.PackStart(nextButton);

            Button cancelButton = new Button(GettextCatalog.GetString("Cancel"))
            {
                MinWidth = GuiSettings.ButtonWidth
            };
            cancelButton.Clicked += (sender, e) => Respond(Command.Cancel);
            buttonBox.PackEnd(cancelButton);

            content.PackStart(buttonBox);

            Content = content;
            Resizable = false; 
        }

        void GetData()
        {
            _treeStore.Clear();

            var repositoryService = _projectCollection.GetService<RepositoryService>();
            var items = repositoryService.QueryFolders();
            var root = ItemSetToHierarchItemConverter.Convert(items);
            var node = _treeStore.AddNode().SetValue(_name, root.Name).SetValue(_path, root.ServerPath);
            AddChilds(node, root.Children);
            var topNode = _treeStore.GetFirstNode();
            _treeView.ExpandRow(topNode.CurrentPosition, false);
        }

        void AddChilds(TreeNavigator node, List<HierarchyItem> children)
        {
            foreach (var child in children)
            {
                node.AddChild().SetValue(_name, child.Name).SetValue(_path, child.ServerPath);
                AddChilds(node, child.Children);
                node.MoveToParent();
            }
        }
    }
}