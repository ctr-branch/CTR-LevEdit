﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CTRFramework.Shared;

namespace CTRFramework
{
    public class Nav : IRead
    {
        public List<uint> ptrs = new List<uint>();
        public List<AIPath> paths = new List<AIPath>();

        public Nav()
        {

        }

        public Nav(BinaryReaderEx br)
        {
            Read(br);
        }
        public void Read(BinaryReaderEx br)
        {
            ptrs = br.ReadListUInt32(3);

            for (int i = 0; i < 3; i++)
            {
                paths.Add(new AIPath(br));
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach (uint u in ptrs)
                sb.AppendLine(u.ToString("X8"));

            foreach (AIPath p in paths)
                sb.AppendLine(p.ToString());

            return sb.ToString();
        }
    }
}