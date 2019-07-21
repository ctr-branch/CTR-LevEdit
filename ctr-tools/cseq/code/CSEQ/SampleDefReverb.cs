﻿using System;
using System.ComponentModel;
using System.IO;
using CTRFramework;
using CTRtools.Helpers;

namespace CTRtools.CSEQ
{
    public class SampleDefReverb : SampleDef, IRead, IWrite
    {
        [CategoryAttribute("General"), DescriptionAttribute("Unknown FF80.")]
        public short UnknownFF80
        {
            get { return unknownFF80; }
            set { unknownFF80 = value; }
        }

        [CategoryAttribute("General"), DescriptionAttribute("Unknown FF80.")]
        public byte Reverb
        {
            get { return reverb; }
            set { reverb = value; }
        }

        [CategoryAttribute("General"), DescriptionAttribute("Unknown FF80.")]
        public byte Reverb2
        {
            get { return reverb2; }
            set { reverb2 = value; }
        }

        private byte magic1;
        private short always0;
        private short unknownFF80;
        private byte reverb;     //maybe reverb is 2 bytes? mostly 193
        private byte reverb2;    //unknown value, mostly 31


        public override void Read(BinaryReader br)
        {
            magic1 = br.ReadByte();
            Volume = br.ReadByte();
            always0 = br.ReadInt16();
            Pitch = br.ReadUInt16();
            SampleID = br.ReadUInt16();
            unknownFF80 = br.ReadInt16();
            reverb = br.ReadByte();
            reverb2 = br.ReadByte();

            if (magic1 != 1)
                throw new Exception(String.Format("SampleDef magic1 = {0}", magic1));

            if (always0 != 0)
                throw new Exception(String.Format("SampleDef always0 = {0} ", always0));
        }

        public override void Write(BinaryWriter bw)
        {
            bw.Write((byte)magic1);
            bw.Write((byte)Volume);
            bw.Write((short)always0);
            bw.Write((short)Pitch);
            bw.Write((short)SampleID);
            bw.Write((short)unknownFF80);
            bw.Write((byte)reverb);
            bw.Write((byte)reverb2);
        }
    }
}
