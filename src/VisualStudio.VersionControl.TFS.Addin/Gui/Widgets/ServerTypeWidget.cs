// ServerTypeWidget.cs
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

using System;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Widgets
{
	public class ServerTypeWidget : Components.RoundedFrameBox
	{
		HBox _contentBox;
		ImageView _imageView;
		VBox _textBox;
		readonly Label _titleLabel;
		readonly Label _descriptionLabel;

		Xwt.Drawing.Image _image;
		string _title;
		string _description;
		bool _isSelected;

		public ServerTypeWidget()
		{
			InnerBackgroundColor = Ide.Gui.Styles.BaseBackgroundColor;
			BorderWidth = 1;
			CornerRadius = new BorderCornerRadius(6, 6, 6, 6);
			HeightRequest = 48;
			Padding = new WidgetSpacing(12, 6, 12, 6);

			_contentBox = new HBox();

			_imageView = new ImageView
			{
				HeightRequest = 36,
				WidthRequest = 36
			};

			_contentBox.PackStart(_imageView);

			_textBox = new VBox
			{
				HorizontalPlacement = WidgetPlacement.Start
			};

			_titleLabel = new Label();
			_descriptionLabel = new Label();
			_descriptionLabel.Font = _descriptionLabel.Font.WithScaledSize(0.95);
			_descriptionLabel.TextColor = Ide.Gui.Styles.SecondaryTextColor;
			_textBox.PackStart(_titleLabel);
			_textBox.PackEnd(_descriptionLabel);
			_contentBox.PackStart(_textBox);

			Content = _contentBox;
		}

		public Xwt.Drawing.Image Icon
        {
            get { return _image; }
            set
            {
                _image = value;
                UpdateImage();
            }
        }

		public string Title
		{
			get { return _title; }
			set
			{
				_title = value;
				UpdateTitle();
			}
		}

		public string Description
        {
			get { return _description; }
            set
            {
				_description = value;
                UpdateDescription();
            }
        }

		public bool IsSelected
        {
            get { return _isSelected; }
			set
			{
				_isSelected = value;
				UpdateIsSelected();
			}
        }

		protected override void OnButtonReleased(ButtonEventArgs args)
        {
            base.OnButtonReleased(args);

            if (args.Button == PointerButton.Left)
            {
				IsSelected = true;
            }
        }

		protected override void OnMouseEntered(EventArgs args)
        {
            base.OnMouseEntered(args);

			InnerBackgroundColor = Ide.Gui.Styles.BackgroundColor;  
        }

        protected override void OnMouseExited(EventArgs args)
        {
			base.OnMouseExited(args);
           
			InnerBackgroundColor = Ide.Gui.Styles.BaseBackgroundColor;
        }

		void UpdateImage()
        {
			if (_image == null)
            {
                return;
            }

			_imageView.Image = _image.WithSize(36, 36);
        }

		void UpdateTitle()
		{
			if (string.IsNullOrEmpty(_title))
            {
                return;
            }

			_titleLabel.Text = _title;
		}

		void UpdateDescription()
        {
			if (string.IsNullOrEmpty(_description))
            {
                return;
            }

			_descriptionLabel.Text = _description;
        }

        void UpdateIsSelected()
		{
			if (IsSelected)
			{
				InnerBackgroundColor = Ide.Gui.Styles.BaseSelectionBackgroundColor;
				_titleLabel.TextColor = Ide.Gui.Styles.BaseSelectionTextColor;
                _descriptionLabel.TextColor = Ide.Gui.Styles.BaseSelectionTextColor;
			}
			else
			{
				InnerBackgroundColor = Ide.Gui.Styles.BaseBackgroundColor;
				_titleLabel.TextColor = Ide.Gui.Styles.BaseForegroundColor;
				_descriptionLabel.TextColor = Ide.Gui.Styles.SecondaryTextColor;
			}
		}
	}
}