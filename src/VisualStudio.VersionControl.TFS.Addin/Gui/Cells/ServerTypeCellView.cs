using MonoDevelop.VersionControl.TFS.Gui.Dialogs;
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.VersionControl.TFS.Gui.Cells
{
    public class ServerTypeCellView : CanvasCellView
    {
        const int ImagePadding = 2;
        const int CellMargin = 6;
        const int ListViewMargin = 24;
        int TitleFontSize = 12;
        int DescriptionFontSize = 10;

        int _imageSide;
        Size _fontRequiredSize;
 
        public ServerTypeCellView()
        {
            CellWidth = 600 - (ImagePadding * 2) - (ListViewMargin * 2);

            BackgroundColor = Ide.Gui.Styles.BackgroundColor;
            CellBorderColor = Colors.LightGray;
            CellBackgroundColor = Colors.White;
            CellSelectionColor = Colors.SkyBlue;
            CellTextColor = Ide.Gui.Styles.BaseForegroundColor;
            CellTextSelectionColor = new Color(140, 140, 140);
        }

        public double CellWidth { get; internal set; }

        public Color BackgroundColor { get; internal set; }
        public Color CellBorderColor { get; internal set; }
        public Color CellBackgroundColor { get; internal set; }
        public Color CellSelectionColor { get; internal set; }
        public Color CellTextColor { get; internal set; }
        public Color CellTextSelectionColor { get; internal set; }

        public IDataField<CellServerType> CellServerType { get; set; }

        protected override Size OnGetRequiredSize()
        {
            var layout = new TextLayout
            {
                Text = "W"
            };
            layout.Font = layout.Font.WithSize(TitleFontSize);
            _fontRequiredSize = layout.GetSize();

            return new Size(CellWidth, _fontRequiredSize.Height * 3);
        }

        protected override void OnDraw(Context ctx, Rectangle cellArea)
        {
            var isSelected = Selected;

            //Draw the node background
            FillCellBackground(ctx, isSelected, 6);

            // Text color
            FillCellTextColor(ctx, isSelected);

            var cellServerType = GetValue(CellServerType);

            _imageSide = (int)cellArea.Height - (2 * ImagePadding);

            ctx.DrawImage(cellServerType.Icon, cellArea.Left + ImagePadding, cellArea.Top + ImagePadding, _imageSide, _imageSide);
       
            int imageX = _imageSide + ImagePadding + CellMargin;

            var titleTextLayout = new TextLayout();
            titleTextLayout.Font = titleTextLayout.Font.WithSize(TitleFontSize);
            titleTextLayout.Width = cellArea.Width - imageX;
            titleTextLayout.Height = cellArea.Height;
            titleTextLayout.Text = cellServerType.Title;
            ctx.DrawTextLayout(titleTextLayout, cellArea.Left + imageX, cellArea.Top + _fontRequiredSize.Height * .5);

            var descriptionTextLayout = new TextLayout();
            descriptionTextLayout.Font = descriptionTextLayout.Font.WithSize(DescriptionFontSize);
            descriptionTextLayout.Width = cellArea.Width - imageX;
            descriptionTextLayout.Height = cellArea.Height;
            descriptionTextLayout.Text = cellServerType.Description;
            ctx.DrawTextLayout(descriptionTextLayout, cellArea.Left + imageX, cellArea.Top + _fontRequiredSize.Height + (CellMargin * 2));
        }

        void FillCellBackground(Context ctx, bool isSelected, double radius)
        {
            if (isSelected)
            {
                FillCellBackground(ctx, CellSelectionColor, radius);
            }
            else
            {
                FillCellBackground(ctx, CellBackgroundColor, radius);
            }
        }

        void FillCellBackground(Context ctx, Color color, double radius)
        {  
            ctx.Rectangle(BackgroundBounds);
            ctx.SetColor(BackgroundColor);
            ctx.Fill();

            ctx.RoundRectangle(Bounds, radius);
            ctx.SetColor(CellBorderColor);
            ctx.Fill();

            var finalBounds = new Rectangle(Bounds.X + 1, Bounds.Y + 1, Bounds.Width - 2, Bounds.Height - 2);
            ctx.RoundRectangle(finalBounds, radius);
            ctx.SetColor(color);
            ctx.Fill();
        }

        void FillCellTextColor(Context ctx, bool isSelected)
        {
            if (isSelected)
            {
                ctx.SetColor(CellTextSelectionColor);
            }

            ctx.SetColor(CellTextColor);
        }
    }
}