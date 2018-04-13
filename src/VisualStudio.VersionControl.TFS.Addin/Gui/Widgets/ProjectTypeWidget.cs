﻿using System;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Widgets
{
	public class ProjectTypeWidget : Components.RoundedFrameBox
	{
		HBox _contentBox;
		ImageView _imageView;
		VBox _textBox;
		Label _titleLabel;
		Label _descriptionLabel;

		Xwt.Drawing.Image _image;
		string _title;
		string _description;
		bool _isSelected;

		public ProjectTypeWidget()
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
			_descriptionLabel.Font = _descriptionLabel.Font.WithScaledSize(0.85);
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

			_imageView.Image = _image;
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
			}
			else
			{
				InnerBackgroundColor = Ide.Gui.Styles.BaseBackgroundColor;
			}
		}
	}
}