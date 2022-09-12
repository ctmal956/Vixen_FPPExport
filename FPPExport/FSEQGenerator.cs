using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Vixen;

namespace FPPExport
{
    internal class FSEQGenerator
    {
        EventSequence _sequence;
        public FSEQGenerator(EventSequence sequence)
        {
            _sequence = sequence;
        }

        public void ExportSequence(string folder)
        {

            // Initialize necessary variables
            const ushort fixedHeaderLength = 28;
            const ushort mediaHeaderSize = 5;

            var mediaFileName = _sequence.Audio != null ? _sequence.Audio.FileName ?? "" : "";
            var mediaHeaderTotalLength = mediaFileName.Length == 0 ? 0 : mediaFileName.Length + mediaHeaderSize;
            var offsetToChannelData = RoundUshortTo4((ushort)(fixedHeaderLength + mediaHeaderTotalLength));          
            string fseqFile = Path.ChangeExtension(_sequence.FileName, "fseq");

            fseqFile = Path.Combine(Vixen.Paths.SequencePath, fseqFile);
            var dialog = new UserForm(fseqFile);

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                fseqFile = dialog.FilePath;
            }
            else
            {
                return;
            }
            

            //save the sequence
            using (var fileStream = new FileStream(fseqFile, FileMode.Create))
            {
                using (var binaryWriter = new BinaryWriter(fileStream))
                {
                    // 0 - 3 - file identifier, must be 'PSEQ'
                    binaryWriter.Write("PSEQ".ToCharArray());
                    // 4 - 5 - Offset to start of channel data
                    binaryWriter.Write(offsetToChannelData);
                    // 6 - minor version, should be 0
                    binaryWriter.Write((byte)0);
                    // 7 - major version, should be 1
                    binaryWriter.Write((byte)1);
                    // 8 - 9 - fixed header length/ index to first variable header
                    binaryWriter.Write(fixedHeaderLength);
                    // 10 - 13 - channel count per frame
                    binaryWriter.Write((uint)_sequence.ChannelCount);
                    // 14 - 17 - number of frames
                    binaryWriter.Write((uint)_sequence.TotalEventPeriods);
                    // 18 - step time in ms, usually 25 or 50
                    binaryWriter.Write((byte)_sequence.EventPeriod);
                    // 19 - bit flags / reserved should be 0
                    binaryWriter.Write((byte)0);
                    // 20 - 21 - universe count, ignored by FPP
                    binaryWriter.Write((ushort)0);
                    // 22 - 23 - universe size, ignored by FPP
                    binaryWriter.Write((ushort)0);
                    // 24 - gamma, should be 1, ignored by FPP
                    binaryWriter.Write((byte)1);
                    // 25 - color encoding, 2 for RGB, ignored by FPP
                    binaryWriter.Write((byte)2);
                    // 26 - 27 - reserved, should be 0
                    binaryWriter.Write((ushort)0);
                    // media data
                    if (mediaHeaderTotalLength > 0)
                    {
                        binaryWriter.Write((ushort)(mediaHeaderTotalLength));
                        binaryWriter.Write("mf".ToCharArray());
                        binaryWriter.Write(mediaFileName.ToCharArray());
                    }

                    // (pad to nearest 4)
                    var padSize = offsetToChannelData - (fixedHeaderLength + mediaHeaderTotalLength);

                    for (var pad = 0; pad < padSize; pad++)
                    {
                        binaryWriter.Write((byte)0);
                    }

                    // Write the event data
                    for (var period = 0; period < _sequence.TotalEventPeriods; period++)
                    {
                        for (var channel = 0; channel < _sequence.ChannelCount; channel++)
                        {
                            binaryWriter.Write(_sequence.EventValues[channel, period]);
                        }
                    }

                    //finish binary save
                    binaryWriter.Close();

                    System.Windows.Forms.MessageBox.Show("Export complete.");
                }
            }


        }

        // thanks to Vixen+ for these helper functions
        private static uint RoundUIntTo4(uint i)
        {
            return (i % 4 == 0) ? i : i + 4 - (i % 4);
        }


        private static ushort RoundUshortTo4(ushort i)
        {
            return (ushort)(i % 4 == 0 ? i : i + 4 - (i % 4));
        }

        /*
         Format from github documentation:
        v1.0 spec
    0-3 - file identifier, must be 'PSEQ'
    4-5 - Offset to start of channel data
    6   - minor version, should be 0
    7   - major version, should be 1
    8-9 - fixed header length/index to first variable header
    10-13 - channel count per frame
    14-17 - number of frames
    18  - step time in ms, usually 25 or 50
    19  - bit flags/reserved should be 0
    20-21 - universe count, ignored by FPP
    22-23 - universe size, ignored by FPP
    24 - gamma, should be 1, ignored by FPP
    25 - color encoding, 2 for RGB, ignored by FPP
    26-27 - reserved, should be 0

    v2.0 spec
    0-3 - file identifier, must be 'PSEQ'
    4-5 - Offset to start of channel data
    6   - minor version, should be 0
    7   - major version, should be 2
    8-9 - standard header length/index to first variable header
    10-13 - channel count per frame (*)
    14-17 - number of frames
    18  - step time in ms, usually 25 or 50
    19  - bit flags/reserved should be 0
    20 bits 0-3 - compression type 0 for uncompressed, 1 for zstd, 2 for libz/gzip
    20 bits 4-7 - number of compression blocks, upper 4 bits - introduced in FSEQ 2.1
    21  - number of compression blocks, 0 if uncompressed, lower 8 bits.  Total 12 bits.
    22  - number of sparse ranges, 0  if none
    23  - bit flags/reserved, unused right now, should be 0
    24-31 - 64bit unique identifier, likely a timestamp or uuid
    numberOfBlocks*8 - compress block index
       0-3 - frame number
       4-7 - length of block
    numberOfSparseRanges*6 - sparse range definitions
       0-2 - start channel number
       3-5 - number of channels

    (*) The channel count is per frame within this file which may not
    be the full number of channels needed to output.  For example, if there
    is a single "sparse range" of start channel 5000 with lengh 50, the
    channel count in bytes 10-13 will be "50" as there will only be 50
    channels per frame within the file.   If there are multiple sparse
    ranges, each range is appended one after another into the frame
    with the channel count being the total lengths of the ranges.


    Variable Length Headers in FSEQ  spec
    - v1.0+
      - 'mf' - Media Filename
        vh[0] = low byte of variable header length
        vh[1] = high byte of variable header length
        vh[2] = 'm'
        vh[3] = 'f'
        vh[4-Len] = NULL terminated media filename
      - 'sp' - Sequence Producer
        vh[0] = low byte of variable header length
        vh[1] = high byte of variable header length
        vh[2] = 's'
        vh[3] = 'p'
        vh[4-Len] = NULL terminated string of producer of the fseq file
                   ex: "xLights Macintosh 2019.22"

         */
    }
}
