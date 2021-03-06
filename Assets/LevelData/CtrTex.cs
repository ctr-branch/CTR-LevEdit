﻿using CTRFramework.Shared;
using System;
using System.Collections.Generic;

namespace CTRFramework
{
    public class CtrTex
    {
        public TextureLayout[] midlods = new TextureLayout[3];
        public TextureLayout[] hi = new TextureLayout[16];
        public List<TextureLayout> animframes = new List<TextureLayout>();

        public uint ptrHi;
        public bool isAnimated = false;

        public CtrTex(BinaryReaderEx br)
        {
            Read(br);
        }

        public void Read(BinaryReaderEx br)
        {
            int pos = (int)br.BaseStream.Position;

            if ((pos & 2) > 0)
            {
                Console.WriteLine("!!!");
                Console.ReadKey();
            }

            //this apparently defines animated texture, really
            if ((pos & 1) == 1)
            {
                isAnimated = true;

                br.BaseStream.Position -= 1;

                uint texpos = br.ReadUInt32();
                int numFrames = br.ReadInt16();
                int whatsthat = br.ReadInt16();

                if (br.ReadUInt32() != 0)
                    Helpers.Panic(this, "not 0!");

                uint[] ptrs = br.ReadArrayUInt32(numFrames);

                foreach (uint ptr in ptrs)
                {
                    br.Jump(ptr);
                    animframes.Add(TextureLayout.FromStream(br));
                }

                br.Jump(texpos);
            }

            for (int i = 0; i < 3; i++)
                midlods[i] = TextureLayout.FromStream(br);

            //Console.WriteLine(br.BaseStream.Position.ToString("X8"));
            //Console.ReadKey();
            
            ptrHi = br.ReadUInt32();

            //loosely assume we got a valid pointer
            if (ptrHi > 0x30000 && ptrHi < 0xB0000)
            {
                br.Jump(ptrHi);

                for (int i = 0; i < 16; i++)
                {
                    hi[i] = TextureLayout.FromStream(br);
                }
            }

            /*
            if (ptrHi != 0 && ptrHi < br.BaseStream.Position)
            {
                br.Jump(ptrHi);

                ptrHi = br.ReadUInt32();
                if (ptrHi != 0 && ptrHi < br.BaseStream.Position)
                {

                    for (int i = 0; i < 4; i++)
                    {
                        try
                        {
                            hi[i] = TextureLayout.FromStream(br);
                        }
                        catch
                        {
                            Console.WriteLine("fail");
                            Console.ReadKey();
                        }
                    }
                }
                else
                {
                    ptrHi -= 4;

                    for (int i = 0; i < 4; i++)
                    {
                        try
                        {
                            hi[i] = TextureLayout.FromStream(br);
                        }
                        catch
                        {
                            Console.WriteLine("fail");
                            Console.ReadKey();
                        }
                    }

                }
            }
            else
            {
                // Console.WriteLine("not a hi res texture");
            }

    */

        }
    }
}
