namespace MaterialSkin.Controls
{
    using MaterialSkin.Animations;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Text;
    using System.Globalization;
    using System.Linq;
    using System.Windows.Forms;

    public class MaterialTabSelector : Control, IMaterialControl
    {
        [Browsable(false)]
        public int Depth { get; set; }

        [Browsable(false)]
        public MaterialSkinManager SkinManager => MaterialSkinManager.Instance;

        [Browsable(false)]
        public MouseState MouseState { get; set; }

        //[Browsable(false)]
        public enum CustomCharacterCasing
        {
            [Description("Text will be used as user inserted, no alteration")]
            Normal,
            [Description("Text will be converted to UPPER case")]
            Upper,
            [Description("Text will be converted to lower case")]
            Lower,
            [Description("Text will be converted to Proper case (aka Title case)")]
            Proper
        }

        TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

        private MaterialTabControl _baseTabControl;

        [Category("Material Skin"), Browsable(true)]
        public MaterialTabControl BaseTabControl
        {
            get { return _baseTabControl; }
            set
            {
                _baseTabControl = value;
                if (_baseTabControl == null) return;

                UpdateTabRects();

                _previousSelectedTabIndex = _baseTabControl.SelectedIndex;
                _baseTabControl.Deselected += (sender, args) =>
                {
                    _previousSelectedTabIndex = _baseTabControl.SelectedIndex;
                };
                _baseTabControl.SelectedIndexChanged += (sender, args) =>
                {
                    _animationManager.SetProgress(0);
                    _animationManager.StartNewAnimation(AnimationDirection.In);
                };
                _baseTabControl.ControlAdded += delegate
                {
                    Invalidate();
                };
                _baseTabControl.ControlRemoved += delegate
                {
                    Invalidate();
                };
            }
        }

        private int _previousSelectedTabIndex;

        private Point _animationSource;

        private readonly AnimationManager _animationManager;

        private List<Rectangle> _tabRects;
        private Rectangle _removeRect;
        private int? _tabRoundedCornerRadius;
        private bool? _drawTabIndicator;
        private bool? _lastTabUnremovable;
        private Color? _primaryColor;
        private Color? _textColor;
        private Brush _tabBackBrush;
        private Brush _tabHoverBrush;

        /// <summary>
        /// Indicate primary color.
        /// </summary>
        public Color PrimaryColor
        {
            get
            {
                return _primaryColor.GetValueOrDefault(this.SkinManager.ColorScheme.PrimaryColor);
            }
            set
            {
                this._primaryColor = value;
            }
        }
        /// <summary>
        /// Indicate tab text color.
        /// </summary>
        public Color TextColor
        {
            get
            {
                return _textColor.GetValueOrDefault(this.SkinManager.ColorScheme.TextColor);
            }
            set
            {
                this._textColor = value;
            }
        }
        /// <summary>
        /// Indicate tab background brush.
        /// </summary>
        public Brush TabBackBrush
        {
            get
            {
                return this._tabBackBrush == null ?
                    new SolidBrush(this.PrimaryColor) :
                    this._tabBackBrush;
            }
            set
            {
                this._tabBackBrush = value;
            }
        }
        /// <summary>
        /// Indicate selected tab brush.
        /// </summary>
        public Brush TabSelectedBrush
        {
            get;
            set;
        }
        /// <summary>
        /// Indicate hovered tab brush.
        /// </summary>
        public Brush TabHoverBrush
        {
            get
            {
                return this._tabHoverBrush == null ?
                    this.SkinManager.BackgroundHoverBrush :
                    this._tabHoverBrush;
            }
            set
            {
                this._tabHoverBrush = value;
            }
        }
        /// <summary>
        /// If set, a remove tab button which user specified will draw on tab.
        /// </summary>
        public int? RemoveButtonImageIndex
        {
            get;
            set;
        }
        /// <summary>
        /// If set, a tab indicator will be drawn.
        /// </summary>
        public bool DrawTabIndicator
        {
            get
            {
                return this._drawTabIndicator.GetValueOrDefault(true);
            }
            set
            {
                this._drawTabIndicator = value;
            }
        }
        /// <summary>
        /// Indicate the last tab allow to be removed.
        /// </summary>
        public bool IsLastTabUnremovable
        {
            get
            {
                return this._lastTabUnremovable.GetValueOrDefault(false);
            }
            set
            {
                this._lastTabUnremovable = value;
            }
        }
        /// <summary>
        /// Indicate each tab's boundary.
        /// </summary>
        public IReadOnlyCollection<Rectangle> TabBounds
        {
            get { return this._tabRects; }
        }
        /// <summary>
        /// Indicate the tab rectangle with round corner by the spcified radius.
        /// </summary>
        public int? TabRoundedCornerRadius
        {
            get
            {
                return this._tabRoundedCornerRadius;
            }
            set
            {
                if (value.HasValue &&
                    value * 2 > this.Height)
                    this._tabRoundedCornerRadius = this.Height / 2;
                else
                    this._tabRoundedCornerRadius = value;
            }
        }

        private const int ICON_SIZE = 24;
        private const int FIRST_TAB_PADDING = 50;
        private const int TAB_HEADER_PADDING = 24;
        private const int TAB_WIDTH_MIN = 160;
        private const int TAB_WIDTH_MAX = 264;

        private int _tab_over_index = -1;
        private bool _is_in_tab_remove_rect = false;

        private CustomCharacterCasing _characterCasing;

        [Category("Appearance")]
        public CustomCharacterCasing CharacterCasing
        {
            get => _characterCasing;
            set
            {
                _characterCasing = value;
                _baseTabControl.Invalidate();
                Invalidate();
            }
        }
        private int _tab_indicator_height;

        [Category("Material Skin"), Browsable(true), DisplayName("Tab Indicator Height"), DefaultValue(2)]
        public int TabIndicatorHeight 
        {
            get { return _tab_indicator_height; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException("Tab Indicator Height", value, "Value should be > 0");
                else
                {
                    _tab_indicator_height = value;
                    Refresh();
                }
            }
        }

        public enum TabLabelStyle
        {
            Text,
            Icon,
            IconAndText,
        }

        private TabLabelStyle _tabLabel;
        [Category("Material Skin"), Browsable(true), DisplayName("Tab Label"), DefaultValue(TabLabelStyle.Text)]
        public TabLabelStyle TabLabel
        {
            get { return _tabLabel; }
            set
            {
                _tabLabel = value;
                if (_tabLabel == TabLabelStyle.IconAndText)
                    Height = 72;
                else
                    Height = 48;
                UpdateTabRects();
                Invalidate();
            }
        }


        public MaterialTabSelector()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.OptimizedDoubleBuffer, true);
            TabIndicatorHeight = 2;
            TabLabel = TabLabelStyle.Text;

            Size = new Size(480, 48);

            _animationManager = new AnimationManager
            {
                AnimationType = AnimationType.EaseOut,
                Increment = 0.04
            };
            _animationManager.OnAnimationProgress += sender => Invalidate();
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            Font = SkinManager.getFontByType(MaterialSkinManager.fontType.Body1);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            g.Clear(this.PrimaryColor);

            if (_baseTabControl == null) return;

            if (!_animationManager.IsAnimating() || _tabRects == null || _tabRects.Count != _baseTabControl.TabCount)
                UpdateTabRects();

            if (this._tabRects.Count == 0)
                return;

            var animationProgress = _animationManager.GetProgress();

            //Click feedback
            if (_animationManager.IsAnimating())
            {
                var rippleBrush = new SolidBrush(Color.FromArgb((int)(51 - (animationProgress * 50)), Color.White));
                var rippleSize = (int)(animationProgress * _tabRects[_baseTabControl.SelectedIndex].Width * 1.75);

                g.SetClip(_tabRects[_baseTabControl.SelectedIndex]);
                g.FillEllipse(rippleBrush, new Rectangle(_animationSource.X - rippleSize / 2, _animationSource.Y - rippleSize / 2, rippleSize, rippleSize));
                g.ResetClip();
                rippleBrush.Dispose();
            }

            // Draw tab with rounded corner
            if (this.TabRoundedCornerRadius.GetValueOrDefault(0) > 0)
            {
                foreach (var rect in this._tabRects)
                {
                    var gp = this.GetTabRoundCornerRegion(rect, this.TabRoundedCornerRadius.Value);

                    if (this.TabBackBrush != null)
                    {
                        var selectedRect = this._tabRects[this._baseTabControl.SelectedIndex];
                        var brush = this.TabBackBrush;

                        if (selectedRect == rect)
                            brush = this.TabSelectedBrush;

                        g.FillPath(brush, gp);

                        if (selectedRect == rect)
                            g.DrawPath(new Pen(Color.Black), gp);
                    }
                    else
                    {
                        g.DrawPath(new Pen(Color.Black), gp);
                    }

                    // Draw remove buttons
                    this.DrawRemoveButtonImage(g, rect);
                }
            }

            //Draw tab headers
            if (this._tab_over_index >= 0 &&
                this._tab_over_index < this._tabRects.Count)
            {
                var hoveredTabRect = this._tabRects[_tab_over_index];

                //Change mouse over tab color
                if (this.TabRoundedCornerRadius.GetValueOrDefault(0) > 0)
                {
                    var gp = this.GetTabRoundCornerRegion(hoveredTabRect, this.TabRoundedCornerRadius.Value);

                    if (this.TabHoverBrush != null)
                        g.FillPath(this.TabHoverBrush, gp);
                    if (this._baseTabControl.SelectedIndex == this._tab_over_index)
                        g.DrawPath(new Pen(Color.Black), gp);
                }
                else
                {
                    g.FillRectangle(
                        this.TabHoverBrush,
                        hoveredTabRect.X,
                        hoveredTabRect.Y,
                        hoveredTabRect.Width,
                        hoveredTabRect.Height - _tab_indicator_height
                        );
                }

                // Draw remove button
                if (this.RemoveButtonImageIndex.GetValueOrDefault(-1) >= 0 &&
                    this.RemoveButtonImageIndex.Value < this._baseTabControl.ImageList.Images.Count)
                {
                    this.UpdateRmoveButtonRect();

                    if (!this.IsLastTabUnremovable ||
                        this._tab_over_index < this._tabRects.Count - 1)
                    {
                        if (this._is_in_tab_remove_rect)
                        {
                            g.FillEllipse(
                                Brushes.Red,
                                this._removeRect
                                );
                        }
                        this.DrawRemoveButtonImage(g, this._tabRects[_tab_over_index]);
                    }
                }
            }

            foreach (TabPage tabPage in _baseTabControl.TabPages)
            {
                var currentTabIndex = _baseTabControl.TabPages.IndexOf(tabPage);

                if (_tabLabel != TabLabelStyle.Icon)
                {
                    // Text
                    using (NativeTextRenderer NativeText = new NativeTextRenderer(g))
                    {
                        Size textSize = TextRenderer.MeasureText(_baseTabControl.TabPages[currentTabIndex].Text, Font);
                        Rectangle textLocation = new Rectangle(_tabRects[currentTabIndex].X+ (TAB_HEADER_PADDING/2), _tabRects[currentTabIndex].Y, _tabRects[currentTabIndex].Width - (TAB_HEADER_PADDING), _tabRects[currentTabIndex].Height);

                        if (_tabLabel == TabLabelStyle.IconAndText)
                        {
                            textLocation.Y = 46;
                            textLocation.Height = 10;
                        }

                        if (((TAB_HEADER_PADDING*2) + textSize.Width < TAB_WIDTH_MAX))
                        {
                            NativeText.DrawTransparentText(
                            CharacterCasing == CustomCharacterCasing.Upper ? tabPage.Text.ToUpper() :
                            CharacterCasing == CustomCharacterCasing.Lower ? tabPage.Text.ToLower() :
                            CharacterCasing == CustomCharacterCasing.Proper ? textInfo.ToTitleCase(tabPage.Text.ToLower()) : tabPage.Text,
                            Font,
                            Color.FromArgb(CalculateTextAlpha(currentTabIndex, animationProgress), this.TextColor),
                            textLocation.Location,
                            textLocation.Size,
                            NativeTextRenderer.TextAlignFlags.Center | NativeTextRenderer.TextAlignFlags.Middle);
                        }
                        else
                        {
                            if (_tabLabel == TabLabelStyle.IconAndText)
                            {
                                textLocation.Y = 40;
                                textLocation.Height = 26;
                            }
                            NativeText.DrawMultilineTransparentText(
                            CharacterCasing == CustomCharacterCasing.Upper ? tabPage.Text.ToUpper() :
                            CharacterCasing == CustomCharacterCasing.Lower ? tabPage.Text.ToLower() :
                            CharacterCasing == CustomCharacterCasing.Proper ? textInfo.ToTitleCase(tabPage.Text.ToLower()) : tabPage.Text,
                            SkinManager.getFontByType(MaterialSkinManager.fontType.Body2),
                            Color.FromArgb(CalculateTextAlpha(currentTabIndex, animationProgress), this.TextColor),
                            textLocation.Location,
                            textLocation.Size,
                            NativeTextRenderer.TextAlignFlags.Center | NativeTextRenderer.TextAlignFlags.Middle);
                        }
                    }
                }

                if (_tabLabel != TabLabelStyle.Text)
                {
                    // Icons
                    if (_baseTabControl.ImageList != null && (!String.IsNullOrEmpty(tabPage.ImageKey) | tabPage.ImageIndex > -1))
                    {
                        Rectangle iconRect = new Rectangle(
                            _tabRects[currentTabIndex].X + (_tabRects[currentTabIndex].Width / 2) - (ICON_SIZE / 2),
                            _tabRects[currentTabIndex].Y + (_tabRects[currentTabIndex].Height / 2) - (ICON_SIZE / 2),
                            ICON_SIZE, ICON_SIZE);
                        if (_tabLabel == TabLabelStyle.IconAndText)
                        {
                            iconRect.Y = 12;
                        }
                        g.DrawImage(!String.IsNullOrEmpty(tabPage.ImageKey) ? _baseTabControl.ImageList.Images[tabPage.ImageKey]: _baseTabControl.ImageList.Images[tabPage.ImageIndex], iconRect);
                    }
                }
            }

            //Animate tab indicator
            if (this.DrawTabIndicator)
            {
                var previousSelectedTabIndexIfHasOne = _previousSelectedTabIndex == -1 ? _baseTabControl.SelectedIndex : _previousSelectedTabIndex;

                if (previousSelectedTabIndexIfHasOne < _tabRects.Count)
                {
                    var previousActiveTabRect = _tabRects[previousSelectedTabIndexIfHasOne];
                    var activeTabPageRect = _tabRects[_baseTabControl.SelectedIndex];

                    var y = activeTabPageRect.Bottom - _tab_indicator_height;
                    var x = previousActiveTabRect.X + (int)((activeTabPageRect.X - previousActiveTabRect.X) * animationProgress);
                    var width = previousActiveTabRect.Width + (int)((activeTabPageRect.Width - previousActiveTabRect.Width) * animationProgress);

                    g.FillRectangle(SkinManager.ColorScheme.AccentBrush, x, y, width, _tab_indicator_height);
                }
            }
            else if (this._tabRects.Count > 0)
            {
                var selectedTab = this._tabRects[_baseTabControl.SelectedIndex];
                var y = selectedTab.Bottom - 1;

                g.DrawLine(new Pen(Color.Black), 0, y, selectedTab.Left + 3, y);
                g.DrawLine(new Pen(Color.Black), selectedTab.Right - 3, y, this.Right, y);
            }
        }

        private int CalculateTextAlpha(int tabIndex, double animationProgress)
        {
            int primaryA = SkinManager.TextHighEmphasisColor.A;
            int secondaryA = SkinManager.TextMediumEmphasisColor.A;

            if (tabIndex == _baseTabControl.SelectedIndex && !_animationManager.IsAnimating())
            {
                return primaryA;
            }
            if (tabIndex != _previousSelectedTabIndex && tabIndex != _baseTabControl.SelectedIndex)
            {
                return secondaryA;
            }
            if (tabIndex == _previousSelectedTabIndex)
            {
                return primaryA - (int)((primaryA - secondaryA) * animationProgress);
            }
            return secondaryA + (int)((primaryA - secondaryA) * animationProgress);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (_tabRects == null)
                UpdateTabRects();

            this.UpdateRmoveButtonRect();

            // Remove tab.
            for (var i = 0; i < _tabRects.Count; i++)
            {
                // Remove tab
                if (this._tabRects[i].Contains(e.Location) &&
                    this._removeRect.Contains(e.Location))
                {
                    if (!this._lastTabUnremovable.GetValueOrDefault(false) ||
                        i < this._tabRects.Count -1)
                    {
                        if (i == this._tabRects.Count - 1 &&
                            i == this._baseTabControl.SelectedIndex)
                            this._baseTabControl.SelectedIndex = i - 1;

                        var tabPage = this._baseTabControl.TabPages[i];
                        this._baseTabControl.TabPages.Remove(tabPage);
                        return;
                    }
                }
            }

            // Select tab changed.
            for (var i = 0; i < _tabRects.Count; i++)
            {
                if (_tabRects[i].Contains(e.Location))
                {
                    _baseTabControl.SelectedIndex = i;
                }
            }

            _animationSource = e.Location;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (DesignMode)
                return;

            if (_tabRects == null)
            {
                UpdateTabRects();
                UpdateRmoveButtonRect();
            }

            int old_tab_over_index = _tab_over_index;
            _tab_over_index = -1;
            for (var i = 0; i < _tabRects.Count; i++)
            {
                if (_tabRects[i].Contains(e.Location))
                {
                    Cursor = Cursors.Hand;
                    _tab_over_index = i;
                    break;
                }
            }
            if (_tab_over_index == -1)
                Cursor = Cursors.Arrow;

            if (old_tab_over_index != _tab_over_index ||
                this._is_in_tab_remove_rect != this._removeRect.Contains(e.Location))
                Invalidate();

            this._is_in_tab_remove_rect = this._removeRect.Contains(e.Location);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (DesignMode)
                return;

            if (_tabRects == null)
                UpdateTabRects();

            Cursor = Cursors.Arrow;
            this._tab_over_index = -1;
            this._is_in_tab_remove_rect = false;
            Invalidate();
        }

        private void UpdateTabRects()
        {
            _tabRects = new List<Rectangle>();

            //If there isn't a base tab control, the rects shouldn't be calculated
            //If there aren't tab pages in the base tab control, the list should just be empty which has been set already; exit the void
            if (_baseTabControl == null || _baseTabControl.TabCount == 0) return;

            //Calculate the bounds of each tab header specified in the base tab control
            using (var b = new Bitmap(1, 1))
            {
                using (var g = Graphics.FromImage(b))
                {
                    using (NativeTextRenderer NativeText = new NativeTextRenderer(g))
                    {
                        for (int i = 0; i < _baseTabControl.TabPages.Count; i++)
                        {
                            Size textSize = TextRenderer.MeasureText(_baseTabControl.TabPages[i].Text, Font);
                            if (_tabLabel == TabLabelStyle.Icon) textSize.Width = ICON_SIZE;

                            int TabWidth = (TAB_HEADER_PADDING * 2) + textSize.Width;
                            if (TabWidth > TAB_WIDTH_MAX)
                                TabWidth = TAB_WIDTH_MAX;
                            else if (TabWidth < TAB_WIDTH_MIN)
                                TabWidth = TAB_WIDTH_MIN;

                            if (i==0)
                                _tabRects.Add(new Rectangle(FIRST_TAB_PADDING - (TAB_HEADER_PADDING), 0, TabWidth, Height));
                            else
                                _tabRects.Add(new Rectangle(_tabRects[i - 1].Right, 0, TabWidth, Height));

                            var max = _tabRects.Max(r => r.Right);
                            if (max > this.Width)
                                this.Width = max;
                        }
                    }
                }
            }
        }

        private void UpdateRmoveButtonRect()
        {
            if (_tab_over_index >= 0)
            {
                if (this.RemoveButtonImageIndex.GetValueOrDefault(-1) >= 0 &&
                    this.RemoveButtonImageIndex.Value < this._baseTabControl.ImageList.Images.Count)
                {
                    var image = this._baseTabControl.ImageList.Images[this.RemoveButtonImageIndex.GetValueOrDefault(0)];
                    var widthUnit = image.Width / 2;
                    var heightUnit = image.Height / 2;
                    var shift = 10 + this.TabRoundedCornerRadius.GetValueOrDefault(0);

                    this._removeRect = new Rectangle(
                        _tabRects[_tab_over_index].Right - (int)Math.Round(widthUnit * 1.5) - shift,
                        (_tabRects[_tab_over_index].Bottom - heightUnit * 2) / 2,
                        widthUnit * 2,
                        heightUnit * 2
                        );
                }
            }
        }

        private GraphicsPath GetTabRoundCornerRegion(Rectangle rect, int radius)
        {
            var diameter = radius * 2;
            var gp = new GraphicsPath();
            gp.AddLine(rect.Left + radius, rect.Bottom, rect.Left, rect.Bottom);
            gp.AddArc(new Rectangle(rect.Left - radius, rect.Bottom - diameter, diameter, diameter), -270, -90);
            gp.AddLine(rect.Left + radius, rect.Bottom - radius, rect.Left + radius, rect.Top + radius);
            gp.AddArc(new Rectangle(rect.Left + radius, rect.Top, diameter, diameter), 180, 90);
            gp.AddLine(rect.Left + diameter, rect.Top, rect.Right - diameter, rect.Top);
            gp.AddArc(new Rectangle(rect.Right - diameter * 3 / 2, rect.Top, diameter, diameter), -90, 90);
            gp.AddLine(rect.Right - radius, rect.Top + radius, rect.Right - radius, rect.Bottom - radius);
            gp.AddArc(new Rectangle(rect.Right - radius, rect.Bottom - diameter, diameter, diameter), 180, -90);

            return gp;
        }

        private void DrawRemoveButtonImage(Graphics g, Rectangle rect)
        {
            if (this.RemoveButtonImageIndex.GetValueOrDefault(-1) < 0 ||
                this._baseTabControl.ImageList.Images.Count <= this.RemoveButtonImageIndex.GetValueOrDefault(-1))
                return;

            var image = this._baseTabControl.ImageList.Images[this.RemoveButtonImageIndex.GetValueOrDefault(0)];
            var widthUnit = image.Width / 2;
            var heightUnit = image.Height / 2;
            var capFromTabRight = 10 + this.TabRoundedCornerRadius.GetValueOrDefault(0);

            g.DrawImage(
                image,
                rect.Right - widthUnit - capFromTabRight,
                (rect.Bottom - heightUnit) / 2,
                widthUnit,
                heightUnit
                );
        }
    }
}
