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
using NonIntMouseLibrary;
using Karna.Magnification;

namespace TobiiNoMouse
{
    public partial class MainForm : Form
    {
        NoMouse noMouse;
        public MainForm()
        {
            InitializeComponent();

            noMouse = new NoMouse(this);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            noMouse.init();
        }

        private void bStart_Click(object sender, EventArgs e)
        {
            noMouse.start();
        }

        private void bStop_Click(object sender, EventArgs e)
        {
            noMouse.stop();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            noMouse.stop();
        }
    }
}
