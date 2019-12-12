using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using Karna.Magnification;

namespace TobiiNoMouse
{
    public partial class MainForm : Form
    {
        // Future configuration constants
        const float MAGNIGIFER_MAGNIFICATION = 2f;
        Size MAGNIFIER_WINDOW_SIZE = new Size(400, 400);


        private LowLevelKeyboardHook keyboardHook;
        private TobiiStreamer streamer;
        private bool rctrlDown = false;
        private Dispatcher uiDispatcher;
        private Form magnifierForm;
        private Magnifier magnifier;

        public MainForm()
        {
            InitializeComponent();

            magnifierForm = new Form();
            magnifierForm.FormBorderStyle = FormBorderStyle.None;
            magnifierForm.Visible = false;
            magnifierForm.Size = MAGNIFIER_WINDOW_SIZE;
            magnifier = new Magnifier(magnifierForm, MAGNIGIFER_MAGNIFICATION);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            uiDispatcher = Dispatcher.CurrentDispatcher;
            streamer = new TobiiStreamer();
            streamer.init();
            streamer.gazePointEvent += gazePointEvent;
            keyboardHook = new LowLevelKeyboardHook();
            keyboardHook.onKeyPress = new LowLevelKeyboardHook.OnKeyPressedDelegate(globalKeyPressHandler);
            keyboardHook.onKeyRelease = new LowLevelKeyboardHook.OnKeyReleasedDelegate(globalKeyReleaseHandler);
        }

        private void gazePointEvent(object sender, TobiiStreamer.GazeEventArgs args)
        {
            if(Dispatcher.CurrentDispatcher != uiDispatcher)
            {
                uiDispatcher.BeginInvoke(new EventHandler<TobiiStreamer.GazeEventArgs>(gazePointEvent), sender, args);
                return;
            }
            if(rctrlDown)
            {
                Rectangle resolution = Screen.PrimaryScreen.Bounds;
                this.Cursor = new Cursor(Cursor.Current.Handle);
                Cursor.Position = new Point((int)(resolution.Width * args.x), (int)(resolution.Height * args.y));
                magnifierForm.Location = new Point(Cursor.Position.X - magnifierForm.Size.Width / 2,
                    Cursor.Position.Y - magnifierForm.Size.Height / 2);
            }
        }

        private void bStart_Click(object sender, EventArgs e)
        {
            Console.WriteLine("Listening for key presses");
            keyboardHook.HookKeyboard();
            streamer.start();
        }

        private void bStop_Click(object sender, EventArgs e)
        {
            Console.WriteLine("Key press listener stopped");
            keyboardHook.UnHookKeyboard();
            rctrlDown = false;
            streamer.stop();
        }

        bool globalKeyPressHandler(Keys e)
        {
            if(e == Keys.RControlKey)
            {
                rctrlDown = true;
                magnifierForm.Visible = true;
            }
            else if (rctrlDown)
            {
                Console.WriteLine("Key press with rctrl: " + e.ToString());
                return false;
            }
            return true;
        }

        bool globalKeyReleaseHandler(Keys e)
        {
            if (e == Keys.RControlKey)
            {
                rctrlDown = false;
                //magnifierForm.Visible = false;
            }
            else if (rctrlDown)
            {
                Console.WriteLine("Key release with rctrl: " + e.ToString());
                return false;
            }
            return true;
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            streamer.cleanup();
        }
    }
}
