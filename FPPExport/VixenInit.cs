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
            var exporter = new FSEQGenerator(sequence);
            exporter.ExportSequence(Paths.SequencePath);
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
