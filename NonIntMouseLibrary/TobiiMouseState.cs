using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NonIntMouseLibrary
{

    class TobiiMouseState
    {
        // Set to true when right control is being held down.
        public bool rctrlDown = false;
        // The last registered gaze point.
        public PointF currentGazePoint = new Point();
        // Set to true when space bar is being held down while rctrl is down.
        public bool spaceDown = false;
        // Screen dimensions for the monitor that tobii works with.
        public Rectangle screenSize;
        // User gaze point when the spacebar was depressed. When the space bar is released, this stores
        // the precision point generated.
        public PointF spacePressedLockPoint = new Point();
        // Set to true if the user presses the spacebar to refine a gaze point while holding rctrl.
        // The desired behavior here is to prevent rctrl from macro-moving the cursor anymore.
        public bool precisionPointFound = false;

        // List of macro lock points to refined points, to be used in some sort of calibration 
        // algorithm.
        public List<PointF[]> gazeToFinal = new List<PointF[]>();

        public TobiiMouseState(Rectangle screenSize)
        {
            this.screenSize = screenSize;
        }

        public void reset()
        {
            rctrlDown = false;
            currentGazePoint = new PointF();
            spaceDown = false;
            spacePressedLockPoint = new PointF();
            precisionPointFound = false;
        }

        public Point toScreenPoint(PointF p)
        {
            return new Point((int)(p.X * screenSize.Width), (int)(p.Y * screenSize.Height));
        }
    }
}
