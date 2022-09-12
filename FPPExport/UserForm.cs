using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FPPExport
{
    public partial class UserForm : Form
    {
        public UserForm(string filePath)
        {
            InitializeComponent();
            textBoxFileName.Text = filePath.Trim();
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            var fileSaveDialog = new SaveFileDialog();
            fileSaveDialog.Filter = "FPP file|*.fseq";
            fileSaveDialog.Title = "Select where to export to";
            fileSaveDialog.FileName = textBoxFileName.Text;
            fileSaveDialog.InitialDirectory = Vixen.Paths.SequencePath;

            if (fileSaveDialog.ShowDialog() == DialogResult.OK)
                textBoxFileName.Text = fileSaveDialog.FileName;
        }

        public string FilePath
        {
            get { return textBoxFileName.Text; }
        }
    }
}
