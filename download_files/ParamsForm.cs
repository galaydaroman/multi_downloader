using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace download_files
{
    public partial class ParamsForm : Form
    {
        public static DownloadParams OpenDialog()
        {
            ParamsForm form = new ParamsForm();
            if (form.ShowDialog() == DialogResult.OK)
            {
                return new DownloadParams
                {
                    Links = form.textBox1.Lines,
                    DestinationFolder = form.textBox2.Text,
                    ThreadCount = Convert.ToInt32(form.numericUpDown1.Value)
                };
            }
            return null;
        }

        public ParamsForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                textBox1.Text = System.IO.File.ReadAllText(openFileDialog1.FileName);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                textBox2.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }
    }

    public class DownloadParams
    {
        public string[] Links { set; get; }
        public string DestinationFolder { get; set; }
        public int ThreadCount { get; set; }
    }
}
