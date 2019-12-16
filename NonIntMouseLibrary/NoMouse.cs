using Karna.Magnification;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using TobiiNoMouse;

namespace NonIntMouseLibrary
{
    public class NoMouse
    {
        // Future configuration constants
        const float MAGNIFIER_MAGNIFICATION = 2f;
        Size MAGNIFIER_WINDOW_SIZE = new Size(900, 900);

        private Form context;
        private LowLevelKeyboardHook keyboardHook;
        private TobiiStreamer streamer;
        private Dispatcher uiDispatcher;
        private Form magnifierForm;
        private Magnifier magnifier;
        TobiiMouseState state;

        public NoMouse(Form context)
        {
            this.context = context;

            state = new TobiiMouseState(Screen.PrimaryScreen.Bounds);
            state.reset();

            magnifierForm = new Form();
            magnifierForm.FormBorderStyle = FormBorderStyle.None;
            magnifierForm.Visible = false;
            magnifierForm.Size = MAGNIFIER_WINDOW_SIZE;
            magnifier = new Magnifier(magnifierForm, MAGNIFIER_MAGNIFICATION);
        }

        public void init()
        {
            uiDispatcher = Dispatcher.CurrentDispatcher;
            streamer = new TobiiStreamer();
            streamer.init();
            streamer.gazePointEvent += gazePointEvent;
            keyboardHook = new LowLevelKeyboardHook();
            keyboardHook.onKeyPress = new LowLevelKeyboardHook.OnKeyPressedDelegate(globalKeyPressHandler);
            keyboardHook.onKeyRelease = new LowLevelKeyboardHook.OnKeyReleasedDelegate(globalKeyReleaseHandler);

            // Necessary to get global cursor? Try removing.
            context.Cursor = new Cursor(Cursor.Current.Handle);
        }

        public void start()
        {
            keyboardHook.HookKeyboard();
            streamer.start();
        }

        public void stop()
        {
            keyboardHook.UnHookKeyboard();
            streamer.stop();
            state.reset();
        }

        private void gazePointEvent(object sender, TobiiStreamer.GazeEventArgs args)
        {
            if (Dispatcher.CurrentDispatcher != uiDispatcher)
            {
                uiDispatcher.BeginInvoke(new EventHandler<TobiiStreamer.GazeEventArgs>(gazePointEvent), sender, args);
                return;
            }
            state.currentGazePoint = new PointF(args.x, args.y);
            if (state.rctrlDown)
            {
                if (state.spaceDown)
                {
                    // We need to do some funky math here. Remember that the user is looking at a zoomed in section
                    // of the screen so we should only shift the cursor over an amount divided by the magnification
                    // to actually show what they are looking at.
                    PointF op = new PointF(state.currentGazePoint.X, state.currentGazePoint.Y);
                    float x = state.spacePressedLockPoint.X + (state.currentGazePoint.X - state.spacePressedLockPoint.X) / MAGNIFIER_MAGNIFICATION;
                    float y = state.spacePressedLockPoint.Y + (state.currentGazePoint.Y - state.spacePressedLockPoint.Y) / MAGNIFIER_MAGNIFICATION;
                    state.currentGazePoint = new PointF(x, y);

                    Console.WriteLine("Adjustment: " + op.X + ", " + op.Y + " to " + state.currentGazePoint.X + ", " + state.currentGazePoint.Y);

                    magnifierForm.Location = new Point(Cursor.Position.X - magnifierForm.Size.Width / 2,
                        Cursor.Position.Y - magnifierForm.Size.Height / 2);
                }

                if (!state.precisionPointFound)
                {
                    Cursor.Position = state.toScreenPoint(state.currentGazePoint);
                }
            }
        }

        bool globalKeyPressHandler(Keys e)
        {
            if (e == Keys.RControlKey)
            {
                state.rctrlDown = true;
            }
            else if (state.rctrlDown)
            {
                switch (e)
                {
                    // When the space is pressed, show a magnification of the screen in the current cursor area that
                    // the user can gaze around in to get a better lock on what they intended to interact with.
                    case Keys.Space:
                        if (!state.spaceDown)
                        {
                            state.spaceDown = true;
                            state.spacePressedLockPoint = state.currentGazePoint;
                            magnifierForm.Visible = true;
                        }
                        break;
                    default: break;
                }
                return false;
            }
            return true;
        }

        bool globalKeyReleaseHandler(Keys e)
        {
            if (e == Keys.RControlKey)
            {
                state.reset();
            }
            else if (state.rctrlDown)
            {
                switch (e)
                {
                    case Keys.Space:
                        state.spaceDown = false;
                        state.precisionPointFound = true;
                        state.spacePressedLockPoint = state.currentGazePoint;
                        magnifierForm.Visible = false;
                        break;
                    default: break;
                }
            }
            return true;
        }

    }
}
