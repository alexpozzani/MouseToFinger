using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MouseToFinger
{
    public partial class MainForm : Form
    {
        public static bool start = false;

        MouseToFinger mouseToFinger;
        NotifyIcon notifyIcon;
        public MainForm()
        {
            InitializeComponent();

            notifyIcon = new NotifyIcon();
            notifyIcon.ContextMenuStrip = contextMenu;
            
            notifyIcon.Icon = this.Icon;
            notifyIcon.Text = "Mouse to Finger";
            notifyIcon.Visible = true;

            mouseToFinger = new MouseToFinger();
            if (start)
                mouseToFinger.Start();
        }

        private void menuItemStart_Click(object sender, EventArgs e)
        {
            mouseToFinger.Start();
        }

        private void menuItemStop_Click(object sender, EventArgs e)
        {
            mouseToFinger.Stop();
        }

        private void menuItemExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
