﻿using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Midi;
using System.Diagnostics;
using System.IO;

namespace cseq
{
    public class CTrack
    {
        //meta
        public string name;
        public string address = "";

        public int instrument = 0;
        public bool isDrumTrack = false;

        List<Command> cmd;


        public CTrack()
        {
            cmd = new List<Command>();
        }

        /*
        public static void NOP(double durationSeconds)
        {
            var durationTicks = Math.Round(durationSeconds * Stopwatch.Frequency);
            var sw = Stopwatch.StartNew();

            while (sw.ElapsedTicks < durationTicks)
            {
            }
        }
        */

        public void Read(BinaryReaderEx br)
        {
            address = br.HexPos();

            switch (br.ReadInt16())
            {
                case 0: isDrumTrack = false; break;
                case 1: isDrumTrack = true; break;
                default: Log.WriteLine("drum value not boolean at " + br.HexPos()); break;
            }

            Command cx;

            do
            {
                cx = new Command();
                cx.Read(br);

                if (cx.evt == CSEQEvent.ChangePatch)
                    instrument = cx.pitch;

                cmd.Add(cx);
            }
            while (cx.evt != CSEQEvent.EndTrack && cx.evt != CSEQEvent.EndTrack2);
        }


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(address + "\r\n");

            foreach (Command c in cmd)
                sb.Append(c.ToString());

            return sb.ToString();
        }


        public List<MidiEvent> ToMidiEventList(CSeqHeader header, int channel)
        {
            List<MidiEvent> me = new List<MidiEvent>();
            MidiEvent x;

            int absTime = 0;

            me.Add(new TextEvent(name, MetaEventType.SequenceTrackName, absTime));
            me.Add(new TempoEvent(header.MPQN, absTime));

            foreach (Command c in cmd)
            {
                x = c.ToMidiEvent(absTime, channel);
                if (x != null) me.Add(x);

                absTime += c.wait;
            }

            return me;
        }


        public void WriteBytes(BinaryWriter bw)
        {
            bw.Write(isDrumTrack ? (short)1 : (short)0);

            foreach (Command c in cmd)
            {
                c.WriteBytes(bw);
            }
        }
    }
}
