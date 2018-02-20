using System.Collections.Generic;
using Microsoft.TeamFoundation.VersionControl.Client.Objects;
using MonoDevelop.Core;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Dialogs
{
    public class FailuresDialog : Gtk.Dialog
    {
        Gtk.TreeView _failuresView;
        Gtk.ListStore _failuresStore;

        public FailuresDialog(List<Failure> failures)
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

        void GetData(List<Failure> failures)
        {
            _failuresStore.Clear();

            foreach (var item in failures)
            {
                _failuresStore.AppendValues(item.SeverityType.ToString(), item.Message, item);
            }
        }
    }
}