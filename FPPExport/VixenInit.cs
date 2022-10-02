using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Vixen;

namespace FPPExport
{
    public class VixenInit : IAddIn
    {
        public LoadableDataLocation DataLocationPreference => LoadableDataLocation.Application;

        public string Name => "Export FSEQ";

        public string Author => "Chris Maloney";

        public string Description => "Exports a Vixen Sequence to a FSEQ file";

        public bool Execute(EventSequence sequence)
        {
            if (sequence == null)
            {
                System.Windows.Forms.MessageBox.Show("Please open a sequence to export.");
                return false;
            }
            var exporter = new FSEQGenerator(sequence);
            exporter.ExportSequence(Paths.ImportExportPath);
            return false;

        }

        public void Loading(XmlNode dataNode)
        {
        }

        public void Unloading()
        {
        }
    }
}
