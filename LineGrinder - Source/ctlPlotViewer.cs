using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LineGrinder
{
    public partial class ctlPlotViewer : UserControl
    {

        // this determines the view we use to display the plot
        private DisplayModeEnum displayMode = DisplayModeEnum.DisplayMode_GERBERONLY;

        // this is the gerber file we display
        private GerberFile gerberFileToDisplay = new GerberFile();
        // this is the excellon file we display
        private ExcellonFile excellonFileToDisplay = new ExcellonFile();
        // this is the display IsoPlotBuilder it should have been built from the gerberFileToDisplay
        private IsoPlotBuilder isoplotBuilderToDisplay = null;
        // this is the display gcode file it should have been built from the isoplotBuilderToDisplay
        private GCodeFile gcodeFileToDisplay = null;

        // this, if false, disables all plot displays
        public const bool DEFAULT_SHOW_PLOT = true;
        private bool showPlot = DEFAULT_SHOW_PLOT;

        public const bool DEFAULT_SHOW_GERBERONGCODE = false;
        private bool showGerberOnGCode = DEFAULT_SHOW_GERBERONGCODE;

        public const bool DEFAULT_SHOW_ORIGIN = false;
        private bool showOrigin = DEFAULT_SHOW_ORIGIN;

        public const bool DEFAULT_SHOW_GCODE_ORIGIN = false;
        private bool showGCodeOrigin = DEFAULT_SHOW_GCODE_ORIGIN;

        public const bool DEFAULT_SHOW_FLIP_AXIS = false;
        private bool showFlipAxis = DEFAULT_SHOW_FLIP_AXIS;

        public const bool DEFAULT_GCODE_AXIS_IS_IN_CENTER = true;
        private bool gcodeOriginAtCenter = DEFAULT_GCODE_AXIS_IS_IN_CENTER;

        // these are the values we add to the plot origin in order
        // to find the true origin of the plot display. This is the 
        // origin actually in the Gerber or Excellon file
        private float plotXOriginLocation = 0;
        private float plotYOriginLocation = 0;

        // NOTE: In general, if a coordinate is an int it has been scaled and it represents
        //       a value in plot coordinates. If it is a float it represents an unscaled
        //       value from the gerber file or gCode file

        private float minPlotXCoord = 0;
        private float minPlotYCoord = 0;
        private float maxPlotXCoord = 0;
        private float maxPlotYCoord = 0;
        private float midPlotXCoord = 0;
        private float midPlotYCoord = 0;

        private PointF workingOrigin = new PointF(0, 0);
        private Matrix viewportMatrix = new Matrix();// fmfcd
        private const int DEFAULT_PADDING_LEFT = 10;
        private const int DEFAULT_PADDING_TOP = 10;
        private const int DEFAULT_PADDING_RIGHT = 10;
        private const int DEFAULT_PADDING_BOTTOM = 10;
        Padding plotPadding = new Padding(DEFAULT_PADDING_LEFT, DEFAULT_PADDING_TOP, DEFAULT_PADDING_TOP, DEFAULT_PADDING_BOTTOM);
        public const int DEFAULT_PLOT_WIDTH = 1000;
        public const int DEFAULT_PLOT_HEIGHT = 850;

        private const int DEFAULT_PLOT_PADDING_TOP = 20;
        private const int DEFAULT_PLOT_PADDING_RIGHT = 20;

        // this is the size of the virtual GerberPlot
        Size virtualPlotSize = new Size(DEFAULT_PLOT_WIDTH, DEFAULT_PLOT_HEIGHT);
        // this is the size of the virtual GerberPlot including padding
        Size virtualScreenSize = new Size(DEFAULT_PLOT_WIDTH + DEFAULT_PADDING_LEFT + DEFAULT_PADDING_RIGHT, DEFAULT_PLOT_HEIGHT + DEFAULT_PADDING_RIGHT + DEFAULT_PADDING_BOTTOM);

        private float isoPlotPointsPerAppUnit = ApplicationImplicitSettings.DEFAULT_ISOPLOTPOINTS_PER_APPUNIT_IN;
        private ApplicationUnitsEnum screenUnits = ApplicationImplicitSettings.DEFAULT_APPLICATION_UNITS;

        // this bitmap is used to display temp iso plot steps
        Bitmap backgroundBitmap = null;
        // this is the displayMode the background bitmap is appropriate for
        private DisplayModeEnum bitmapMode = DisplayModeEnum.DisplayMode_GERBERONLY;

        // this is the default magnification level we return to whenever we open a new file
        // these are percents values *1.00 is 100%)
        public const float DEFAULT_MAGNIFICATION_LEVEL = 1.00f;
        // this is the zero based index in the DEFAULT_MAGNIFICATION_LEVELS of
        // the DEFAULT_MAGNIFICATION_LEVEL
        public const int DEFAULT_MAGNICATION_LEVEL_INDEX = 6;
        // these are the possible default scale levels, the user can specify values between these manually
        public static float[] DEFAULT_MAGNIFICATION_LEVELS = { 0.25f, 0.33f, 0.50f, 0.66f, 0.75f, 0.87f, 1.00f, 1.25f, 1.50f, 2.00f, 3.00f, 4.00f, 5.00f, 6.00f, 7.00f, 8.00f, 10.00f, 12.00f, 16.00f, 20.00f, 30.00f, 40.00f };
        // this is the currently operational level of magnification
        private float magnificationLevel = DEFAULT_MAGNIFICATION_LEVEL;

        // these are the dots per inch on the screen. We get them once in the first paint
        // in order to avoid things that don't need a graphics object having to obtain one
        // these are never used directly since the application units may be MM. Always
        // access these values through DotsPerAppUnitX and DotsPerAppUnitY which does the conversion.
        private bool _dpiHasBeenSet = false;
        private float _dpiX = 96f;
        private float _dpiY = 96f;

        // there are 25.4 mm to the inch
        private const int INCHTOMMSCALERx10 = 254;

        //TODO when using the scroll wheel to scale, adjust the x and  offset so the 
        // pixel under the mouse stays in roughly the same place

        private Point lastMouseDownPosition;
        private PointF workingOriginAtMouseDown = new PointF(0, 0);
        private bool panningActive = false;

        private TextBox mouseCursorDisplayControl = null;
        //private Matrix lastTransformMatrix = null;

        // fmfcd selection
        enum TA_Action { TA_AUCUNE, TA_SELECTION };
        TA_Action action = TA_Action.TA_AUCUNE;
        private Point currentMouseMovePosition;
        // direct graphics
        BufferedGraphicsContext currentGC;
        BufferedGraphics buffer;

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Constructor
        /// </summary>
        public ctlPlotViewer()
        {
            InitializeComponent();


        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the control we use to display the mouse cursor position
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TextBox MouseCursorDisplayControl
        {
            get
            {
                return mouseCursorDisplayControl;
            }
            set
            {
                mouseCursorDisplayControl = value;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// An invalidate routine for this control
        /// </summary>
        public new void Invalidate()
        {

            base.Invalidate();
            // invalidate every control we possess
            foreach (Control conObj in this.Controls)
            {
                conObj.Invalidate();
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets whether we show the GCode cut lines when plotting GCodes
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowGerberOnGCode
        {
            get
            {
                return showGerberOnGCode;
            }
            set
            {
                showGerberOnGCode = value;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets whether we show the (0,0) origin on the display
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowOrigin
        {
            get
            {
                return showOrigin;
            }
            set
            {
                showOrigin = value;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets whether we show the GCode origin on the display
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowGCodeOrigin
        {
            get
            {
                return showGCodeOrigin;
            }
            set
            {
                showGCodeOrigin = value;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets whether we show the GCode origin in the center of the plot
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool GcodeOriginAtCenter
        {
            get
            {
                return gcodeOriginAtCenter;
            }
            set
            {
                gcodeOriginAtCenter = value;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets whether we show the FlipAxis on the display
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowFlipAxis
        {
            get
            {
                return showFlipAxis;
            }
            set
            {
                showFlipAxis = value;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the GerberFile to display. Will never set or get a null value.
        /// </summary>
        [Browsable(false)]
        [DefaultValue(null)]
        [ReadOnlyAttribute(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public GerberFile GerberFileToDisplay
        {
            set
            {
                gerberFileToDisplay = value;
                if (gerberFileToDisplay == null) gerberFileToDisplay = new GerberFile();
            }
            get
            {
                if (gerberFileToDisplay == null) gerberFileToDisplay = new GerberFile();
                return gerberFileToDisplay;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the ExcellonFile to display. Will never set or get a null value.
        /// </summary>
        [Browsable(false)]
        [DefaultValue(null)]
        [ReadOnlyAttribute(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ExcellonFile ExcellonFileToDisplay
        {
            set
            {
                excellonFileToDisplay = value;
                if (excellonFileToDisplay == null) excellonFileToDisplay = new ExcellonFile();
            }
            get
            {
                if (excellonFileToDisplay == null) excellonFileToDisplay = new ExcellonFile();
                return excellonFileToDisplay;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the isoplotbuilder to display. Can set or get a null value.
        /// </summary>
        [BrowsableAttribute(false)]
        [DefaultValue(null)]
        [ReadOnlyAttribute(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IsoPlotBuilder IsoPlotBuilderToDisplay
        {
            get
            {
                return isoplotBuilderToDisplay;
            }
            set
            {
                isoplotBuilderToDisplay = value;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the GCodeFile to display. Can set or get a null value.
        /// </summary>
        [BrowsableAttribute(false)]
        [DefaultValue(null)]
        [ReadOnlyAttribute(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public GCodeFile GCodeFileToDisplay
        {
            get
            {
                return gcodeFileToDisplay;
            }
            set
            {
                gcodeFileToDisplay = value;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the background bitmap which represents the interim GCode calculation stages
        /// </summary>
        [Browsable(false)]
        [DefaultValue(null)]
        [ReadOnlyAttribute(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Bitmap BackgroundBitmap
        {
            get
            {
                return backgroundBitmap;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Sets the background bitmap which represents the interim GCode calculation stages
        /// </summary>
        public void SetBackgroundBitmap(Bitmap bitmapIn, DisplayModeEnum displayModeIn)
        {
            backgroundBitmap = bitmapIn;
            bitmapMode = displayModeIn;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the display mode the current background bitmap is appropriate for
        /// </summary>
        [Browsable(false)]
        [DefaultValue(null)]
        [ReadOnlyAttribute(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DisplayModeEnum BitmapMode
        {
            get
            {
                return bitmapMode;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the which plot of the display we show
        /// </summary>
        [Browsable(false)]
        [DefaultValue(null)]
        [ReadOnlyAttribute(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DisplayModeEnum DisplayMode
        {
            get
            {
                return displayMode;
            }
            set
            {
                displayMode = value;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Resets the display
        /// </summary>
        public void Reset()
        {
            showPlot = DEFAULT_SHOW_PLOT;
            plotXOriginLocation = 0;
            plotYOriginLocation = 0;
            minPlotXCoord = 0;
            minPlotYCoord = 0;
            maxPlotXCoord = 0;
            maxPlotYCoord = 0;
            workingOrigin = new PointF(0, 0);

            magnificationLevel = DEFAULT_MAGNIFICATION_LEVEL;
            plotPadding = new Padding(DEFAULT_PADDING_LEFT, DEFAULT_PADDING_TOP, DEFAULT_PADDING_RIGHT, DEFAULT_PADDING_BOTTOM);
            virtualPlotSize = new Size(DEFAULT_PLOT_WIDTH, DEFAULT_PLOT_HEIGHT);
            virtualScreenSize = new Size(DEFAULT_PLOT_WIDTH + DEFAULT_PADDING_LEFT + DEFAULT_PADDING_RIGHT, DEFAULT_PLOT_HEIGHT + DEFAULT_PADDING_RIGHT + DEFAULT_PADDING_BOTTOM);
            // we do not have these
            IsoPlotBuilderToDisplay = null;
            GCodeFileToDisplay = null;
            workingOrigin.X = 0;
            workingOrigin.Y = 0;

            // fmfcd selection 
            action = TA_Action.TA_AUCUNE;
            Invalidate();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the virtual plot size. There is no set accessor - this value is 
        /// set when the gerber file is plotted. Never returns a value with a
        /// height or width of zero
        /// </summary>
        [Browsable(false)]
        [DefaultValue(null)]
        [ReadOnlyAttribute(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Size VirtualPlotSize
        {
            get
            {
                if ((virtualPlotSize.Width <= 0) || (virtualPlotSize.Height <= 0))
                {
                    virtualPlotSize = new Size(DEFAULT_PLOT_WIDTH, DEFAULT_PLOT_HEIGHT);
                }
                return virtualPlotSize;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the currently set Screen Units as an enum
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ApplicationUnitsEnum ScreenUnits
        {
            get
            {
                return screenUnits;
            }
            set
            {
                screenUnits = value;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the virtual plot size.  Never gets/sets a value less than
        /// or equal to zero
        /// </summary>
        [Browsable(false)]
        [DefaultValue(null)]
        [ReadOnlyAttribute(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float IsoPlotPointsPerAppUnit
        {
            get
            {
                if (isoPlotPointsPerAppUnit <= 0)
                {
                    if (ScreenUnits == ApplicationUnitsEnum.INCHES)
                    {
                        isoPlotPointsPerAppUnit = ApplicationImplicitSettings.DEFAULT_VIRTURALCOORD_PER_INCH;
                    }
                    else
                    {
                        isoPlotPointsPerAppUnit = ApplicationImplicitSettings.DEFAULT_VIRTURALCOORD_PER_MM;
                    }
                }
                return isoPlotPointsPerAppUnit;
            }
            set
            {
                isoPlotPointsPerAppUnit = value;
                if (isoPlotPointsPerAppUnit <= 0)
                {
                    if (ScreenUnits == ApplicationUnitsEnum.INCHES)
                    {
                        isoPlotPointsPerAppUnit = ApplicationImplicitSettings.DEFAULT_VIRTURALCOORD_PER_INCH;
                    }
                    else
                    {
                        isoPlotPointsPerAppUnit = ApplicationImplicitSettings.DEFAULT_VIRTURALCOORD_PER_MM;
                    }
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Shows whatever objects we have configured on the plot
        /// </summary>
        public void ShowPlot()
        {
            // a reset is assumed to have been done prior to this call
            SetVirtualPlotSize();

        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/sets the current magnification level. Will never get/set a value less
        /// than zero.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float MagnificationLevel
        {
            get
            {
                if (magnificationLevel <= 0) magnificationLevel = DEFAULT_MAGNIFICATION_LEVEL;
                return magnificationLevel;
            }
            set
            {
                magnificationLevel = value;
                if (magnificationLevel <= 0) magnificationLevel = DEFAULT_MAGNIFICATION_LEVEL;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the plotXOriginLocation.  these are the values we add to the 
        /// plot origin in order to find the true origin of the plot display. This 
        /// is the (0,0) origin actually used in the Gerber or Excellon file
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float PlotXOriginLocation
        {
            get
            {
                return plotXOriginLocation;
            }
            set
            {
                plotXOriginLocation = value;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the plotYOriginLocation.  these are the values we add to the 
        /// plot origin in order to find the true origin of the plot display. This 
        /// is the (0,0) origin actually used in the Gerber or Excellon file
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float PlotYOriginLocation
        {
            get
            {
                return plotYOriginLocation;
            }
            set
            {
                plotYOriginLocation = value;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Figures out the virtual plot dimensions using the largest sizes we have
        /// and the scaling factor
        /// </summary>
        public void SetVirtualPlotSize()
        {
            // the virtual plot size must be large enough to take the full size of the GerberPlot to display
            // the minPlotXYCoords should be origin compensated to zero. We just use the maxXY here

            float xSize = maxPlotXCoord;
            float ySize = maxPlotYCoord;

            xSize *= (float)isoPlotPointsPerAppUnit;
            ySize *= (float)isoPlotPointsPerAppUnit;

            // add on a bit of extra space so the objects with the biggest X or Y 
            // we just add on the DEFAULT_PLOT_PADDING      
            ySize += (float)DEFAULT_PLOT_PADDING_TOP;
            xSize += (float)DEFAULT_PLOT_PADDING_RIGHT;

            if (xSize <= 0) xSize = DEFAULT_PLOT_WIDTH;
            if (ySize <= 0) ySize = DEFAULT_PLOT_HEIGHT;

            virtualPlotSize = new Size((int)xSize, (int)ySize);
            virtualScreenSize = new Size((int)xSize + plotPadding.Left + plotPadding.Right, (int)ySize + plotPadding.Top + plotPadding.Bottom);

            //  LogMessage("SetVirtualPlotSize: virtualPlotSize: (0,0) " + virtualPlotSize.ToString()); // à enlever
            //  LogMessage("SetVirtualPlotSize: virtualScreenSize: (0,0) " + virtualScreenSize.ToString()); // à enlever

        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Sets the limits of the plot. This is the limits of the plot used by the 
        /// objects on display. The units will be as determined by
        /// SetPlotUnits
        /// </summary>
        /// <param name="maxXCoordIn">maxX</param>
        /// <param name="maxYCoordIn">maxY</param>
        /// <param name="minXCoordIn">minX</param>
        /// <param name="minYCoordIn">minY</param>
        /// <param name="midXCoordIn">minX</param>
        /// <param name="midYCoordIn">minY</param>
        public int SetPlotObjectLimits(float minXCoordIn, float minYCoordIn, float maxXCoordIn, float maxYCoordIn, float midXCoordIn, float midYCoordIn)
        {
            minPlotXCoord = minXCoordIn;
            minPlotYCoord = minYCoordIn;
            maxPlotXCoord = maxXCoordIn;
            maxPlotYCoord = maxYCoordIn;
            midPlotXCoord = midXCoordIn;
            midPlotYCoord = midYCoordIn;

            //DebugMessage("SetPlotObjectLimits minX=" + minPlotXCoord.ToString() + ", maxX=" + maxPlotXCoord.ToString() + ", minY=" + minPlotYCoord.ToString() + ", maxY=" + maxPlotYCoord.ToString() + ", midX=" + midPlotXCoord.ToString() + ", midY=" + midPlotYCoord.ToString());

            return 0;
        }



        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handles a mouse wheel event
        /// </summary>
        public void HandleMouseWheelEvent(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            int currIndex = 0;
            // Update the drawing based upon the mouse wheel scrolling.
            int numberOfMagLevels = e.Delta * SystemInformation.MouseWheelScrollLines / 120;
            // get our position in the array now
            currIndex = GetCurrentMagLevelsIndexIntoDefaultMagLevelArray();
            // add on where we want to go based on the mouse wheel, we should just add
            // numberOfScaleLevels on to currIndex here but that does not seem to work well
            // so we do it increment by increment
            if (numberOfMagLevels > 0)
            {
                currIndex += 1;
            }
            else if (numberOfMagLevels < 0)
            {
                currIndex -= 1;
            }

            // sanity check
            if (currIndex < 0) currIndex = 0;
            if (currIndex >= DEFAULT_MAGNIFICATION_LEVELS.Count()) currIndex = DEFAULT_MAGNIFICATION_LEVELS.Count() - 1;
            // set the magnification level
            MagnificationLevel = DEFAULT_MAGNIFICATION_LEVELS[currIndex];
            // set the scroll bar

            this.Invalidate();

            //        DebugMessage("MouseWheel, delta=" + e.Delta.ToString() + " MagnificationLevel=" + MagnificationLevel.ToString());
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// returns the index of the entry in the DEFAULT_MAGNIFICATION_LEVELS just
        /// equal to greater than the current magnification level
        /// </summary>
        public int GetCurrentMagLevelsIndexIntoDefaultMagLevelArray()
        {
            for (int index = 0; index < DEFAULT_MAGNIFICATION_LEVELS.Count(); index++)
            {
                if (DEFAULT_MAGNIFICATION_LEVELS[index] >= magnificationLevel) return index;
            }
            // not found - just return the last one
            return DEFAULT_MAGNIFICATION_LEVELS.Count() - 1;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Paints the control according to the currently loaded gerber file
        /// </summary>

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Control_Paint(this, e);
        }
        private void Control_Paint(object sender, PaintEventArgs e)
        {
            Graphics graphics = this.CreateGraphics();

            paint(graphics);

            graphics.Dispose();

            return;
        }
        public class GDI
        {

            [System.Runtime.InteropServices.DllImport("gdi32.dll")]
            internal static extern bool Rectangle(
               IntPtr hdc,
               int ulCornerX, int ulCornerY,
               int lrCornerX, int lrCornerY);
            [System.Runtime.InteropServices.DllImport("gdi32.dll")]
            internal static extern bool LineTo(IntPtr hdc, int x, int y);
            [System.Runtime.InteropServices.DllImport("gdi32.dll")]
            internal static extern bool MoveToEx(IntPtr hdc, int x, int y, IntPtr lppt);
            [System.Runtime.InteropServices.DllImport("gdi32.dll")]
            internal static extern IntPtr CreatePen(int iStyle, int cWidth, int colorRef);
            [System.Runtime.InteropServices.DllImport("gdi32.dll")]
            internal static extern IntPtr SetROP2(IntPtr hdc, int rop2);
        }

        Point endPointAnc = new Point();
        Point startPointAnc = new Point();
        bool bDebutFantome = true;
        void debutFantome()
        {
            bDebutFantome = true;
        }
        protected void PaintFantome()
        {
    
            Graphics graphics = this.CreateGraphics();
            IntPtr hdc = graphics.GetHdc();
            Point endPoint = lastMouseDownPosition;
            Point startPoint = currentMouseMovePosition;
            GDI.SetROP2(hdc, 10);  // inverser les couleurs
                                   // redessiner l'ancien

            if (!bDebutFantome)  // pas au début
            {


                GDI.MoveToEx(hdc, startPointAnc.X, startPointAnc.Y, IntPtr.Zero);
                GDI.LineTo(hdc, endPointAnc.X, startPointAnc.Y);
                GDI.LineTo(hdc, endPointAnc.X, endPointAnc.Y);
                GDI.LineTo(hdc, startPointAnc.X, endPointAnc.Y);
                GDI.LineTo(hdc, startPointAnc.X, startPointAnc.Y);
            }
            bDebutFantome = false;  // une seule fois
            // dessiner le nouveau
            endPointAnc.X = endPoint.X;
            endPointAnc.Y = endPoint.Y;
            startPointAnc.X = startPoint.X;
            startPointAnc.Y = startPoint.Y;
            GDI.MoveToEx(hdc, startPointAnc.X, startPointAnc.Y, IntPtr.Zero);
            GDI.LineTo(hdc, endPointAnc.X, startPointAnc.Y);
            GDI.LineTo(hdc, endPointAnc.X, endPointAnc.Y);
            GDI.LineTo(hdc, startPointAnc.X, endPointAnc.Y);
            GDI.LineTo(hdc, startPointAnc.X, startPointAnc.Y);
            // fin dessin
            graphics.ReleaseHdc(hdc);
        }
        private void paint(Graphics graphicsObj)
        {

            // this is the translation and scaling matrix we use to draw on the panel
            Matrix R1 = null;

            // Note that flipX means that we 
            // flip about the center vertical axis such that the Y values remain constant but 
            // the X values are mirrored around the center between the minimum and maximum

            // this is 0 in normal mode or set to a value to shift the X axis offset if we are
            // X flipping
            int flipXCompensator = 0;
            // normally 1, this gets set to -1 to initiate a flip about the Y axis
            int matrixXFlipInitiator = 1;



            //DebugMessage("Paint called");

            if (_dpiHasBeenSet == false)
            {
                // set this now
                _dpiX = graphicsObj.DpiX;
                _dpiY = graphicsObj.DpiY;
                _dpiHasBeenSet = true;
            }


            if (showPlot == false)
            {
                // just clear the screen
                graphicsObj.Clear(ApplicationColorManager.DEFAULT_PLOT_PANEL_COLOR);
            }
            else
            {
                // if this is true, we are done, screen is automatically cleared
                if (DisplayMode == DisplayModeEnum.DisplayMode_NONE) return;

                if ((GerberFileToDisplay.IsPopulated == true) && (GerberFileToDisplay.FlipMode == FlipModeEnum.X_Flip))
                {
                    // yes, we do want to flip about the Y axis. We will need to adjust some things on the display
                    flipXCompensator = (int)(gerberFileToDisplay.MaxPlotXCoord * isoPlotPointsPerAppUnit);
                    matrixXFlipInitiator = -1;
                }
                else if ((ExcellonFileToDisplay.IsPopulated == true) && (ExcellonFileToDisplay.FlipMode == FlipModeEnum.X_Flip))
                {
                    // yes, we do want to flip about the Y axis. We will need to adjust some things on the display
                    flipXCompensator = (int)(excellonFileToDisplay.MaxPlotXCoord * isoPlotPointsPerAppUnit);
                    matrixXFlipInitiator = -1;
                }
                else { } // leave everyting at defaults

                const int INVERSION_COMPENSATOR_OFFSET = 3;
                // set up the matrix to invert on the Y axis. This
                // puts the origin 0,0 in the lower left hand corner
                // the INVERSION_COMPENSATOR_OFFSET is necessary because
                // the reflection and translation are off slightly. I think
                // this is due to the borders or something, anyhoo this makes
                // it come out right
                R1 = new Matrix(1 * matrixXFlipInitiator, 0, 0, -1, 0, 0);
                R1.Translate(0, this.Height - INVERSION_COMPENSATOR_OFFSET, MatrixOrder.Append);
                // R1.Translate(workingOrigin.X, workingOrigin.Y);
                R1.Translate(workingOrigin.X * matrixXFlipInitiator, workingOrigin.Y);
                // now compensate for the left, and top padding
                R1.Translate(plotPadding.Left, plotPadding.Top);
                // now compensate for the scaling
                float xScreenScale = ConvertMagnificationLevelToXScreenScaleFactor(MagnificationLevel);
                float yScreenScale = ConvertMagnificationLevelToYScreenScaleFactor(MagnificationLevel);
                R1.Scale(xScreenScale, yScreenScale);
                // now translate appropriately. Normally this will be 0,0 but if we are flip X axis 
                // it will have other values. Note this MUST come after the above scaling!!! Note that
                // it goes in negative. The matrix math seems to require this
                R1.Translate(flipXCompensator * matrixXFlipInitiator, 0);

                //DebugMessage("workingOrigin=" + workingOrigin.ToString());


                // DebugMessage("");
                // DebugMessage("MagnificationLevel=" + MagnificationLevel.ToString());
                // DebugMessage("xScreenScale=" + xScreenScale.ToString());
                // DebugMessage("xScreenScale*virtualScreenSize.Width=" + (xScreenScale * virtualScreenSize.Width).ToString());
                // DebugMessage("yScreenScale=" + yScreenScale.ToString());
                // DebugMessage("yScreenScale*virtualScreenSize.Height=" + (yScreenScale * virtualScreenSize.Height).ToString());
                // DebugMessage("");
                // apply it to the graphics object. This means the rest of the code
                // does not need to know about it
                graphicsObj.Transform = R1;
                // draw the background and the border
                DrawBackground(graphicsObj, ApplicationColorManager.DEFAULT_PLOT_BACKGROUND_BRUSH);
                //DebugTODO("make the border and corners options");
                //DrawBorder(graphicsObj, ApplicationColorManager.DEFAULT_PLOT_BORDER_PEN);
                //DrawDiagnosticCornerBoxes(graphicsObj);

                if (DisplayMode == DisplayModeEnum.DisplayMode_GERBERONLY)
                {
                    // Draw the Gerber File
                    if ((GerberFileToDisplay != null) && (GerberFileToDisplay.IsPopulated == true))
                    {
                        GerberFileToDisplay.PlotGerberFile(graphicsObj);
                    }
                    else if ((ExcellonFileToDisplay != null) && (ExcellonFileToDisplay.IsPopulated == true))
                    {
                        ExcellonFileToDisplay.PlotExcellonFile(graphicsObj);
                    }
                }
                else if (DisplayMode == DisplayModeEnum.DisplayMode_ISOSTEP1)
                {
                    // the bitmap will have been set up differently
                    if (backgroundBitmap != null) graphicsObj.DrawImage(backgroundBitmap, 0, 0);
                    // do we want to show the gerber plot anyways?
                    if (ShowGerberOnGCode == true)
                    {
                        // Draw the Gerber File
                        if ((GerberFileToDisplay != null) && (GerberFileToDisplay.IsPopulated == true))
                        {
                            GerberFileToDisplay.PlotGerberFile(graphicsObj);
                        }
                    }
                }
                else if (DisplayMode == DisplayModeEnum.DisplayMode_ISOSTEP2)
                {
                    if (backgroundBitmap != null) graphicsObj.DrawImage(backgroundBitmap, 0, 0);
                    // do we want to show the gerber plot anyways?
                    if (ShowGerberOnGCode == true)
                    {
                        // Draw the Gerber File
                        if ((GerberFileToDisplay != null) && (GerberFileToDisplay.IsPopulated == true))
                        {
                            GerberFileToDisplay.PlotGerberFile(graphicsObj);
                        }
                    }
                }
                else if (DisplayMode == DisplayModeEnum.DisplayMode_ISOSTEP3)
                {
                    if (backgroundBitmap != null) graphicsObj.DrawImage(backgroundBitmap, 0, 0);
                    // do we want to show the gerber plot anyways?
                    if (ShowGerberOnGCode == true)
                    {
                        // Draw the Gerber File
                        if ((GerberFileToDisplay != null) && (GerberFileToDisplay.IsPopulated == true))
                        {
                            GerberFileToDisplay.PlotGerberFile(graphicsObj);
                        }
                    }
                }
                else if (DisplayMode == DisplayModeEnum.DisplayMode_GCODEONLY)
                {
                    // show the GCode File
                    if (GCodeFileToDisplay != null)
                    {
                        GCodeFileToDisplay.PlotGCodeFile(graphicsObj, false);
                    }
                    // do we want to show the gerber plot anyways?
                    if (ShowGerberOnGCode == true)
                    {
                        // Draw the Gerber File
                        if ((GerberFileToDisplay != null) && (GerberFileToDisplay.IsPopulated == true))
                        {
                            GerberFileToDisplay.PlotGerberFile(graphicsObj);
                        }
                        else if ((ExcellonFileToDisplay != null) && (ExcellonFileToDisplay.IsPopulated == true))
                        {
                            ExcellonFileToDisplay.PlotExcellonFile(graphicsObj);
                        }
                    }
                }
                // draw the Flip Axis origin, if enabled
                DrawFlipAxis(graphicsObj, ApplicationColorManager.DEFAULT_PLOT_FLIPAXIS_PEN);
                // draw the GCode origin, if enabled
                DrawGCodeOrigin(graphicsObj, ApplicationColorManager.DEFAULT_PLOT_GCODE_ORIGIN_PEN);
                // draw the origin, if enabled, this always goes over top the GCode origin
                DrawOrigin(graphicsObj, ApplicationColorManager.DEFAULT_PLOT_ORIGIN_PEN);

                // actually this returns a clone it will need to be disposed
                //Matrix R2 = graphicsObj.Transform;
                //R2.Invert();
                //lastTransformMatrix = R2;
                // fmfcd
                switch (action)
                {
                    case TA_Action.TA_SELECTION:
                        paintSelect(graphicsObj);
                        break;
                    default:
                        break;
                }



            }


        }

        public void paintSelect(Graphics graphicsObj)
        {
            // a afficher directement sur le graphics
            Point endPoint = MouseToWorld(lastMouseDownPosition);
            Point startPoint = MouseToWorld(currentMouseMovePosition);
            Rectangle rcSelection = new Rectangle(startPoint.X, startPoint.Y, endPoint.X - startPoint.X, endPoint.Y - startPoint.Y);
            graphicsObj.DrawRectangle(ApplicationColorManager.DEFAULT_PLOT_ORIGIN_PEN, rcSelection);

        }
        public void noAction()  // plus d'action en cours
        {
            action = TA_Action.TA_AUCUNE;
        }
        public void selectElements()
        {
            /// TODO
            Point endPoint = MouseToWorld(lastMouseDownPosition);
            Point startPoint = MouseToWorld(currentMouseMovePosition);
            Rectangle rcSelection = new Rectangle(startPoint.X, startPoint.Y, endPoint.X - startPoint.X, endPoint.Y - startPoint.Y);
            if (gcodeFileToDisplay != null)
            {

                // trouver les élément entièrement dans le rectangle de selection
                List<GCodeCmd> listeGCodeCmd = gcodeFileToDisplay.SourceLines.FindAll(g =>
                {


                    if (g.GetType() == typeof(GCodeCmd_Line))
                    {
                        if (((GCodeCmd_Line)g).X0 > startPoint.X && ((GCodeCmd_Line)g).Y0 > startPoint.Y
                            && ((GCodeCmd_Line)g).X0 < endPoint.X && ((GCodeCmd_Line)g).Y0 < endPoint.Y
                            &&
                             ((GCodeCmd_Line)g).X1 > startPoint.X && ((GCodeCmd_Line)g).Y1 > startPoint.Y
                            && ((GCodeCmd_Line)g).X1 < endPoint.X && ((GCodeCmd_Line)g).Y1 < endPoint.Y)
                            return true;
                    }
                    else if (g.GetType() == typeof(GCodeCmd_Arc))
                    {
                        if (((GCodeCmd_Arc)g).X0 > startPoint.X && ((GCodeCmd_Arc)g).Y0 > startPoint.Y
                            && ((GCodeCmd_Arc)g).X0 < endPoint.X && ((GCodeCmd_Arc)g).Y0 < endPoint.Y
                            &&
                             ((GCodeCmd_Arc)g).X1 > startPoint.X && ((GCodeCmd_Arc)g).Y1 > startPoint.Y
                            && ((GCodeCmd_Arc)g).X1 < endPoint.X && ((GCodeCmd_Arc)g).Y1 < endPoint.Y)
                            return true;

                    }

                    return false;
                });
                // ajouter
                foreach (var gcodeCmd in listeGCodeCmd)
                {
                    int index = gcodeFileToDisplay.SourceLines.IndexOf(gcodeCmd);
                    if (index != -1)
                        AddSelect(index);
                }

                Invalidate();
            }
        }

        private bool rightButtonDown = false;
        private bool leftButtonDown = false;
        //protected override void OnMouseMove(MouseEventArgs e)
        //{
        //    base.OnMouseMove(e);
        private void ctlPlotViewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (!this.Capture)
                return;
            Point convertedPoint = MouseToWorld(e.Location);
            currentMouseMovePosition.X = e.X;
            currentMouseMovePosition.Y = e.Y;

            mouseCursorDisplayControl.Text = string.Format(convertedPoint.X + ":" + convertedPoint.Y);
            if (leftButtonDown)
            {
                switch (action)
                {
                    case TA_Action.TA_SELECTION:
                        PaintFantome();
                        break;
                    default:
                        break;
                }
            }
        }
        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// This handles a mouse down event for the panel
        /// </summary>
        //protected override void OnMouseDown(MouseEventArgs e)
        //{
          //  base.OnMouseDown(e);
        

        private void ctlPlotViewer_MouseDown(object sender, MouseEventArgs e)
        {

            // enable panning
            panningActive = true;
            lastMouseDownPosition.X = e.X;
            lastMouseDownPosition.Y = e.Y;

            workingOriginAtMouseDown = workingOrigin;
            // fmfcd
            Point convertedPoint = MouseToWorld(e.Location);

            if (gcodeFileToDisplay != null)
            {
                action = TA_Action.TA_SELECTION;
                debutFantome(); // pour un dessin correct du fantome
                // recherche d'un élément sous le curseur
                int index = gcodeFileToDisplay.SourceLines.FindIndex(g => {
                    // predicate
                    // TODO à placer dans GCodeCmd, GCodeCmd_Line et GCodeCmd_Arc
                    // selection

                    int dRec = 10; // zone de recherche carré
                    if (g.GetType() == typeof(GCodeCmd_Line))
                    {
                        if ((Math.Abs(((GCodeCmd_Line)g).X0 - convertedPoint.X) < dRec) && (Math.Abs(((GCodeCmd_Line)g).Y0 - convertedPoint.Y) < dRec))
                            return true;
                        if ((Math.Abs(((GCodeCmd_Line)g).X1 - convertedPoint.X) < dRec) && (Math.Abs(((GCodeCmd_Line)g).Y1 - convertedPoint.Y) < dRec))
                            return true;
                        if ((Math.Abs(((GCodeCmd_Line)g).MX - convertedPoint.X) < dRec) && (Math.Abs(((GCodeCmd_Line)g).MY - convertedPoint.Y) < dRec))
                            return true;
                    }
                    else if (g.GetType() == typeof(GCodeCmd_Arc))
                    {
                        if ((Math.Abs(((GCodeCmd_Arc)g).X0 - convertedPoint.X) < dRec) && (Math.Abs(((GCodeCmd_Arc)g).Y0 - convertedPoint.Y) < dRec))
                            return true;
                        if ((Math.Abs(((GCodeCmd_Arc)g).X1 - convertedPoint.X) < dRec) && (Math.Abs(((GCodeCmd_Arc)g).Y1 - convertedPoint.Y) < dRec))
                            return true;

                    }

                    return false;
                });
                // fmfcd
                if (index == -1)
                    mouseCursorDisplayControl.Text = string.Format("no gCode ");
                else
                    AddSelect(index);


            }
            // /fmfcd
            // test the buttons, we only care about left and right at the moment
            if (e.Button == MouseButtons.Left) leftButtonDown = true;
            else if (e.Button == MouseButtons.Right) rightButtonDown = true;
            else return;

            // we require both the left and right buttons to be down
            // at the same time in order to pan
            if (leftButtonDown == false || rightButtonDown == false) return;



        }

        private void AddSelect(int index)
        {
            // fmfcd

            GCodeCmd gCodeCmd = gcodeFileToDisplay.SourceLines[index];
            string sgCodeCmd = gCodeCmd.GetGCodeCmd(gcodeFileToDisplay.StateMachine);
            mouseCursorDisplayControl.Text = string.Format("index" + index + " gCode " + sgCodeCmd);
            // en 2 étapes : sélection puis suppression souligner la ligne sélectionner en rouge
            int iSelect = gcodeFileToDisplay.listeIndexSelection.FindIndex(i => i == index);
            //if (!gcodeFileToDisplay.listeIndexSelection.Contains(index))  // si pas déjà dans la liste
            if (iSelect == -1)
                gcodeFileToDisplay.listeIndexSelection.Add(index); // ajouter aux éléments sélectionné                    
            else
            {
                gcodeFileToDisplay.listeIndexSelection.RemoveAt(iSelect);  // enlever de la sélection
            }
            Invalidate();

        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// This handles a mouse up event for the panel
        /// </summary>
        //protected override void OnMouseUp(MouseEventArgs e)
        //{
        //  base.OnMouseUp(e);
        private void ctlPlotViewer_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                leftButtonDown = false;
            }
            if (e.Button == MouseButtons.Right)
            {
                rightButtonDown = false;
            }
            // any upclick on any button turns off panning
            panningActive = false;
            // fmfcd
            //action = TA_Action.TA_AUCUNE; // fmfcd fin de selction

            switch (action)
            {
                case TA_Action.TA_SELECTION:
                    // sélectionner les éléments dans la zone 
                    selectElements();
                    Invalidate();  // ou dessiner par dessus la screen
                    break;
                default:
                    break;
            }

        }

        Point MouseToWorld(Point location)
        {
            // trouver l'origine du gerber
            //Point pObject = new Point(location.X, this.Height - (location.Y));
            Point[] tPoint = { location };
            viewportMatrix.TransformPoints(tPoint);
            Point pObject = tPoint[0];

            // fmfcd debug mouseCursorDisplayControl.Text = string.Format("X: {0} , Y: {1} lX: {2} , lY: {3}", pObject.X, pObject.Y, location.X, location.Y);
            return pObject;

        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// This returns the number of dots per app unit on the screen. If the 
        /// app units are inches this is the screen resolution in dpi. Otherwise
        /// this is the dpmm
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        private float DotsPerAppUnitX
        {
            get
            {
                if (ScreenUnits == ApplicationUnitsEnum.INCHES)
                {
                    return _dpiX;
                }
                else
                {
                    return ((_dpiX * 10) / INCHTOMMSCALERx10);
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// This returns the number of dots per app unit on the screen. If the 
        /// app units are inches this is the screen resolution in dpi. Otherwise
        /// this is the dpmm
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        private float DotsPerAppUnitY
        {
            get
            {
                if (ScreenUnits == ApplicationUnitsEnum.INCHES)
                {
                    return _dpiY;
                }
                else
                {
                    return ((_dpiY * 10) / INCHTOMMSCALERx10);
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// We have to take the DPI of the screen into account when presenting
        /// the user with a particular magnification level. This does that
        /// </summary>
        ///                       also made it support mm app units
        private float ConvertMagnificationLevelToXScreenScaleFactor(float magLevel)
        {
            if (magLevel >= 1)
            {
                return (DotsPerAppUnitX * magLevel) / isoPlotPointsPerAppUnit;
            }
            else
            {
                return (DotsPerAppUnitX / isoPlotPointsPerAppUnit) * magLevel;
            }

        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// We have to take the DPI of the screen into account when presenting
        /// the user with a particular magnification level. This does that
        /// </summary>
        ///                       also made it support mm app units
        private float ConvertMagnificationLevelToYScreenScaleFactor(float magLevel)
        {
            if (magLevel >= 1)
            {
                return (DotsPerAppUnitY * magLevel) / isoPlotPointsPerAppUnit;
            }
            else
            {
                return (DotsPerAppUnitY / isoPlotPointsPerAppUnit) * magLevel;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Draws the flip axis line - the line we flip around
        /// </summary>
        /// <param name="graphicsObj"> a valid graphics object</param>
        /// <param name="borderPen">a 1 pixel wide pen to draw the border with</param>
        private void DrawFlipAxis(Graphics graphicsObj, Pen originPen)
        {
            float flipAxisLen = (float)(maxPlotYCoord * IsoPlotPointsPerAppUnit);

            if (ShowFlipAxis == false) return;
            if (graphicsObj == null) return;
            if (originPen == null) return;

            //float xScreenScale = ConvertMagnificationLevelToXScreenScaleFactor(MagnificationLevel);
            //float yScreenScale = ConvertMagnificationLevelToYScreenScaleFactor(MagnificationLevel);
            int yLineLen = (int)(flipAxisLen);

            //  DebugMessage("xScreenScale = " + xScreenScale.ToString() + " xLineLen=" + xLineLen.ToString() + " MagnificationLevel=" + MagnificationLevel.ToString());

            // the zero in here just re-inforces that we are offsetting from the (0,0) plot position
            double xOrigin = 0 + Math.Round((this.midPlotXCoord * IsoPlotPointsPerAppUnit)); ;
            // double yOrigin = 0 + Math.Round((plotYOriginLocation * IsoPlotPointsPerAppUnit)); ;

            // DebugMessage("xOrigin = " + xOrigin.ToString() + " xScreenScale=" + xScreenScale.ToString());

            Point startPointY = new Point((int)xOrigin, 0);
            Point endPointY = new Point((int)xOrigin, yLineLen);

            // draw the flip axis line
            graphicsObj.DrawLine(originPen, startPointY, endPointY);

        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Draws the (0,0) position (the origin)
        /// </summary>
        /// <param name="graphicsObj"> a valid graphics object</param>
        /// <param name="borderPen">a 1 pixel wide pen to draw the border with</param>
        private void DrawOrigin(Graphics graphicsObj, Pen originPen)
        {
            const float ORIGIN_CROSSHAIR_LEN = 10;
            //graphicsObj.Transform.Reset();  // !!!! y en 0

            viewportMatrix = graphicsObj.Transform.Clone();  // matrice de transformation unit -> display
            viewportMatrix.Invert();

            if (ShowOrigin == false) return;
            if (graphicsObj == null) return;
            if (originPen == null) return;

            float xScreenScale = ConvertMagnificationLevelToXScreenScaleFactor(MagnificationLevel);
            float yScreenScale = ConvertMagnificationLevelToYScreenScaleFactor(MagnificationLevel);
            int xLineLen = (int)(ORIGIN_CROSSHAIR_LEN / xScreenScale);
            int yLineLen = (int)(ORIGIN_CROSSHAIR_LEN / yScreenScale);

            //  DebugMessage("xScreenScale = " + xScreenScale.ToString() + " xLineLen=" + xLineLen.ToString() + " MagnificationLevel=" + MagnificationLevel.ToString());

            // the zero in here just re-inforces that we are offsetting from the (0,0) plot position
            double xOrigin = 0 + Math.Round((plotXOriginLocation * IsoPlotPointsPerAppUnit)); ;
            double yOrigin = 0 + Math.Round((plotYOriginLocation * IsoPlotPointsPerAppUnit)); ;
            //   DebugMessage("xOrigin = " + xOrigin.ToString() + " xScreenScale=" + xScreenScale.ToString());

            Point startPointX = new Point(((int)xOrigin) + (xLineLen * -1), (int)yOrigin);
            Point endPointX = new Point(((int)xOrigin) + xLineLen, (int)yOrigin);
            Point startPointY = new Point((int)xOrigin, ((int)yOrigin) + (yLineLen * -1));
            Point endPointY = new Point((int)xOrigin, ((int)yOrigin) + yLineLen);

            // draw the cross hair lines
            graphicsObj.DrawLine(originPen, startPointX, endPointX);
            graphicsObj.DrawLine(originPen, startPointY, endPointY);

        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Draws the GCode start position (the origin), can be (0,0) or it can be
        /// elsewhere
        /// </summary>
        /// <param name="graphicsObj"> a valid graphics object</param>
        /// <param name="borderPen">a 1 pixel wide pen to draw the border with</param>
        private void DrawGCodeOrigin(Graphics graphicsObj, Pen originPen)
        {
            const float ORIGIN_CROSSHAIR_LEN = 20;
            const float ORIGIN_CIRCLE_RADIUS = 10;
            double xOrigin = 0;
            double yOrigin = 0;

            if (ShowGCodeOrigin == false) return;
            if (graphicsObj == null) return;
            if (originPen == null) return;

            float xScreenScale = ConvertMagnificationLevelToXScreenScaleFactor(MagnificationLevel);
            float yScreenScale = ConvertMagnificationLevelToYScreenScaleFactor(MagnificationLevel);
            int xLineLen = (int)(ORIGIN_CROSSHAIR_LEN / xScreenScale);
            int yLineLen = (int)(ORIGIN_CROSSHAIR_LEN / yScreenScale);
            int circleRadiusX = (int)(ORIGIN_CIRCLE_RADIUS / xScreenScale);
            int circleRadiusY = (int)(ORIGIN_CIRCLE_RADIUS / yScreenScale);

            //  DebugMessage("xScreenScale = " + xScreenScale.ToString() + " xLineLen=" + xLineLen.ToString() + " MagnificationLevel=" + MagnificationLevel.ToString());

            if (gcodeOriginAtCenter == true)
            {
                float xCenterOffset = (float)(this.midPlotXCoord * IsoPlotPointsPerAppUnit);
                float yCenterOffset = (float)(this.midPlotYCoord * IsoPlotPointsPerAppUnit);

                xOrigin = xCenterOffset + Math.Round((plotXOriginLocation * IsoPlotPointsPerAppUnit)); ;
                yOrigin = yCenterOffset + Math.Round((plotYOriginLocation * IsoPlotPointsPerAppUnit)); ;
            }
            else
            {
                // the zero in here just re-inforces that we are offsetting from the (0,0) plot position
                xOrigin = 0 + Math.Round((plotXOriginLocation * IsoPlotPointsPerAppUnit)); ;
                yOrigin = 0 + Math.Round((plotYOriginLocation * IsoPlotPointsPerAppUnit)); ;
            }

            // DebugMessage("xOrigin = " + xOrigin.ToString() + " xScreenScale=" + xScreenScale.ToString());

            Point startPointX = new Point(((int)xOrigin) + (xLineLen * -1), (int)yOrigin);
            Point endPointX = new Point(((int)xOrigin) + xLineLen, (int)yOrigin);
            Point startPointY = new Point((int)xOrigin, ((int)yOrigin) + (yLineLen * -1));
            Point endPointY = new Point((int)xOrigin, ((int)yOrigin) + yLineLen);

            // draw the cross hair lines
            graphicsObj.DrawLine(originPen, startPointX, endPointX);
            graphicsObj.DrawLine(originPen, startPointY, endPointY);
            // draw the circle, feed it the upper xy corner and the height/width
            graphicsObj.DrawEllipse(originPen, ((int)xOrigin) + (circleRadiusX * -1), ((int)yOrigin) + (circleRadiusY * -1), (circleRadiusX * 2), (circleRadiusY * 2));

        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Draw some diagnostic boxes in the corner of the virtualPlot. We use
        /// these for testing the scroll etc
        /// </summary>
        /// <param name="graphicsObj"> a valid graphics object</param>
        private void DrawDiagnosticCornerBoxes(Graphics graphicsObj)
        {
            const int TEST_LINELEN = 10;

            if (graphicsObj == null) return;

            Pen myPen = new Pen(System.Drawing.Color.Red, 1);
            myPen.Alignment = PenAlignment.Center;


            // box at minx, miny
            graphicsObj.DrawLine(myPen, 0, 0, TEST_LINELEN, 0);
            graphicsObj.DrawLine(myPen, 0, 2, TEST_LINELEN, 2);
            graphicsObj.DrawLine(myPen, 0, 4, TEST_LINELEN, 4);
            graphicsObj.DrawLine(myPen, 0, 6, TEST_LINELEN, 6);
            graphicsObj.DrawLine(myPen, 0, 8, TEST_LINELEN, 8);
            graphicsObj.DrawLine(myPen, 0, 10, TEST_LINELEN, 10);
            graphicsObj.DrawLine(myPen, 0, 0, 0, TEST_LINELEN);

            // box at maxx, miny
            graphicsObj.DrawLine(myPen, virtualPlotSize.Width - TEST_LINELEN, 0, virtualPlotSize.Width, 0);
            graphicsObj.DrawLine(myPen, virtualPlotSize.Width - TEST_LINELEN, 2, virtualPlotSize.Width, 2);
            graphicsObj.DrawLine(myPen, virtualPlotSize.Width - TEST_LINELEN, 4, virtualPlotSize.Width, 4);
            graphicsObj.DrawLine(myPen, virtualPlotSize.Width - TEST_LINELEN, 6, virtualPlotSize.Width, 6);
            graphicsObj.DrawLine(myPen, virtualPlotSize.Width - TEST_LINELEN, 8, virtualPlotSize.Width, 8);
            graphicsObj.DrawLine(myPen, virtualPlotSize.Width - TEST_LINELEN, 10, virtualPlotSize.Width, 10);
            graphicsObj.DrawLine(myPen, virtualPlotSize.Width, 0, virtualPlotSize.Width, TEST_LINELEN);

            // box at minx, maxy
            graphicsObj.DrawLine(myPen, 0, virtualPlotSize.Height - TEST_LINELEN, 0, virtualPlotSize.Height);
            graphicsObj.DrawLine(myPen, 2, virtualPlotSize.Height - TEST_LINELEN, 2, virtualPlotSize.Height);
            graphicsObj.DrawLine(myPen, 4, virtualPlotSize.Height - TEST_LINELEN, 4, virtualPlotSize.Height);
            graphicsObj.DrawLine(myPen, 6, virtualPlotSize.Height - TEST_LINELEN, 6, virtualPlotSize.Height);
            graphicsObj.DrawLine(myPen, 8, virtualPlotSize.Height - TEST_LINELEN, 8, virtualPlotSize.Height);
            graphicsObj.DrawLine(myPen, 10, virtualPlotSize.Height - TEST_LINELEN, 10, virtualPlotSize.Height);
            graphicsObj.DrawLine(myPen, 0, virtualPlotSize.Height, TEST_LINELEN, virtualPlotSize.Height);

            // box at maxx, maxy
            graphicsObj.DrawLine(myPen, virtualPlotSize.Width - TEST_LINELEN + 0, virtualPlotSize.Height - TEST_LINELEN, virtualPlotSize.Width - TEST_LINELEN + 0, virtualPlotSize.Height);
            graphicsObj.DrawLine(myPen, virtualPlotSize.Width - TEST_LINELEN + 2, virtualPlotSize.Height - TEST_LINELEN, virtualPlotSize.Width - TEST_LINELEN + 2, virtualPlotSize.Height);
            graphicsObj.DrawLine(myPen, virtualPlotSize.Width - TEST_LINELEN + 4, virtualPlotSize.Height - TEST_LINELEN, virtualPlotSize.Width - TEST_LINELEN + 4, virtualPlotSize.Height);
            graphicsObj.DrawLine(myPen, virtualPlotSize.Width - TEST_LINELEN + 6, virtualPlotSize.Height - TEST_LINELEN, virtualPlotSize.Width - TEST_LINELEN + 6, virtualPlotSize.Height);
            graphicsObj.DrawLine(myPen, virtualPlotSize.Width - TEST_LINELEN + 8, virtualPlotSize.Height - TEST_LINELEN, virtualPlotSize.Width - TEST_LINELEN + 8, virtualPlotSize.Height);
            graphicsObj.DrawLine(myPen, virtualPlotSize.Width - TEST_LINELEN + 10, virtualPlotSize.Height - TEST_LINELEN, virtualPlotSize.Width - TEST_LINELEN + 10, virtualPlotSize.Height);
            graphicsObj.DrawLine(myPen, virtualPlotSize.Width - TEST_LINELEN + 0, virtualPlotSize.Height, virtualPlotSize.Width - TEST_LINELEN + TEST_LINELEN, virtualPlotSize.Height);

            myPen.Dispose();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Draw a border around the plot
        /// </summary>
        /// <param name="graphicsObj"> a valid graphics object</param>
        /// <param name="borderPen">a 1 pixel wide pen to draw the border with</param>
        private void DrawBorder(Graphics graphicsObj, Pen borderPen)
        {
            if (graphicsObj == null) return;
            if (borderPen == null) return;

            // box 
            graphicsObj.DrawRectangle(borderPen, 0, 0, virtualPlotSize.Width, virtualPlotSize.Height);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Draw the virtualPlot in a different colour
        /// </summary>
        /// <param name="graphicsObj"> a valid graphics object</param>
        private void DrawBackground(Graphics graphicsObj, Brush backgroundBrush)
        {
            if (graphicsObj == null) return;
            if (backgroundBrush == null) return;

            graphicsObj.FillRectangle(backgroundBrush, (float)0, (float)0, (float)virtualPlotSize.Width, (float)virtualPlotSize.Height);
        }

    }

}
