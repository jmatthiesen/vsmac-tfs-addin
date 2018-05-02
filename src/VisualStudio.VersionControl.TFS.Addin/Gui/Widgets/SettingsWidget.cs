// SettingsWidget.cs
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

using Autofac;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.TFS.Models;
using MonoDevelop.VersionControl.TFS.Services;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Widgets
{
    public class SettingsWidget : VBox
    {
        ComboBox _lockLevelBox;
        CheckBox _debugModeBox;

        TeamFoundationServerVersionControlService _service;

        public SettingsWidget()
        {
            Init();
            BuildGui();
        }

        /// <summary>
		/// Init SettingsWidget.
        /// </summary>
        void Init()
        {
            _service = DependencyContainer.Container.Resolve<TeamFoundationServerVersionControlService>();

            _lockLevelBox = CreateLockLevelComboBox();     
            _debugModeBox = new CheckBox(GettextCatalog.GetString("Debug Mode"));
        }

		/// <summary>
		/// Builds the SettingsWidget GUI.
		/// </summary>
		void BuildGui()
		{
			PackStart(new Label(GettextCatalog.GetString("Lock Level:")));
			PackStart(_lockLevelBox);

			_debugModeBox.AllowMixed = false;
			_debugModeBox.Active = _service.DebugMode;

#if DEBUG
			PackStart(_debugModeBox);
#endif
		}

        public void ApplyChanges()
        {
            _service.CheckOutLockLevel = (LockLevel)_lockLevelBox.SelectedItem;
            _service.DebugMode = _debugModeBox.Active;
        }

        /// <summary>
        /// Creates the lock level combo box.
		/// More info: https://docs.microsoft.com/en-us/vsts/tfvc/understand-lock-types?view=vsts
        /// </summary>
        /// <returns>The lock level combo box.</returns>
        ComboBox CreateLockLevelComboBox()
        {
            ComboBox lockLevelBox = new ComboBox();

            lockLevelBox.Items.Add(LockLevel.Unchanged, "Keep any existing lock.");
            lockLevelBox.Items.Add(LockLevel.CheckOut, "Prevent other users from checking out and checking in");
            lockLevelBox.Items.Add(LockLevel.Checkin, "Prevent other users from checking in but allow checking out");

            if (_service.CheckOutLockLevel == LockLevel.Unchanged)
                lockLevelBox.SelectedItem = LockLevel.CheckOut;
            else
                lockLevelBox.SelectedItem = _service.CheckOutLockLevel;
            
            return lockLevelBox;
        }
    }
}