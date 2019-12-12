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
using Tobii.StreamEngine;

namespace TobiiNoMouse
{
    public partial class MainForm : Form
    {
        private LowLevelKeyboardHook keyboardHook;
        private TobiiStreamer streamer;
        private bool rctrlDown = false;
        private Dispatcher uiDispatcher;

        public MainForm()
        {
            InitializeComponent();
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
                Console.WriteLine("rctrl pressed");
                rctrlDown = true;
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
                Console.WriteLine("rctrl released");
                rctrlDown = false;
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
