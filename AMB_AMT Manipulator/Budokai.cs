using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
//using nQuant;
using Rainbow.ImgLib.Encoding;
using System.Windows.Forms;
using static Naruto_CCS_Text_Editor.Bin;

//Budokai Class and struct by Bit.Raiden
//Base struct of AMB and AMT by SusmuelDBZMA&M
//Do not comercialize

namespace AMB_AMT_Manipulator
{
    public class Budokai
    {
        public class AMB
        {
            public string FileName;
            public byte[] Container;

            public byte[] Header;
            public byte[] Section;

            public int FileCount;
            public int Version, Unk, SectionSize;

            public List<FileEntry> Entries;

            public AMB(byte[] input, string name = "FILE")
            {
                FileName = name;
                Container = input;
                if (Encoding.Default.GetString(ReadBlock(input, 0, 4)) != "#AMB")
                    return;//Not AMB case

                uint headersize = (uint)ReadUInt(input, 4, Int.UInt32);
                Header = ReadBlock(input, 0, headersize);

                Unk = (int)ReadUInt(Header, 8, Int.UInt32);
                Version = (int)ReadUInt(Header, 0xC, Int.UInt32);
                FileCount = (int)ReadUInt(Header, 0x10, Int.UInt32);
                SectionSize = (int)ReadUInt(Header, 0x18, Int.UInt32);

                Section = ReadBlock(input, 0, (uint)SectionSize);

                #region Read FileEntries
                if (SectionSize > headersize)
                {
                    Entries = new List<FileEntry>();
                    uint offset = headersize;
                    for (int i = 0; i < FileCount; i++)
                    {
                        byte[] entryb = ReadBlock(input, offset, 0x10);
                        Entries.Add(new FileEntry(entryb, i));
                        offset += 0x10;
                    }
                }
                #endregion
            }
            public class FileEntry
            {
                public int Offset;
                public int Size;
                public Type FileType = Type.Unk;
                //and a null value
                //implemented for security
                public int Unk;
                public int Index;

                public FileEntry(byte[] entryblock, int indx)
                {
                    Offset = (int)ReadUInt(entryblock, 0, Int.UInt32);
                    Size = (int)ReadUInt(entryblock, 4, Int.UInt32);
                    int filt = (int)ReadUInt(entryblock, 8, Int.UInt32);
                    if (filt < 3)
                    {
                        FileType = (Type)filt;
                    }
                    Unk = (int)ReadUInt(entryblock, 0xC, Int.UInt32);
                    Index = indx;
                }
                public enum Type
                {
                    Unk,
                    AMO,
                    AMT,
                };
            }
            public void ExtractContainer(string savefolder, bool extractamts = false)
            {
                var sb = new StringBuilder();
                foreach (var file in Entries)
                {
                    string namesave = savefolder + FileName + "_" + file.Index.ToString() + "." + file.FileType.ToString();
                    byte[] fileBIN = ReadBlock(Container, (uint)file.Offset, (uint)file.Size);
                    if (extractamts && file.FileType == FileEntry.Type.AMT)
                    {
                        AMT amtx = new AMT(fileBIN);

                        string amtpatj = savefolder + "AMT_" + file.Index;

                        if (!Directory.Exists(amtpatj))
                            Directory.CreateDirectory(amtpatj);
                        amtpatj += @"\";
                        int index = 0;
                        foreach (var tex in amtx.Textures)
                        {
                            string saveng = amtpatj + tex.texinfo.Bpp + "Bpp";
                            if (!Directory.Exists(saveng))
                                Directory.CreateDirectory(saveng);
                            saveng += @"\";

                            tex.GetPNG().Save(saveng + @"\texture_" + index.ToString() + ".png");
                            index++;
                        }
                    }
                    else if (file.FileType.ToString() == "-1")
                        namesave = savefolder + FileName + "_" + file.Index.ToString() + ".UNK";

                    File.WriteAllBytes(namesave, fileBIN);
                    sb.Append(Path.GetFileName(namesave) + "\r\n");
                }
                sb.Remove(sb.Length - 2, 2);//CR LF at final
                File.WriteAllBytes(savefolder + "pack.amb", Section);//AMB Section for repack
                File.WriteAllText(savefolder+"filelist.txt", sb.ToString());//AMB filelist
            }
            public static bool RemakeContainer(string openfolder, string savepath, bool repackamts = false)
            {
                DirectoryInfo info = new DirectoryInfo(openfolder);
                string name = info.Name;

                if (Directory.EnumerateFiles(openfolder).Count() == 0)
                    return false;//Error

                var outFile = new List<byte>();
                var outAMB = new List<byte>();

                byte[] AMB = File.ReadAllBytes(openfolder + "pack.amb");
                string[] filelist = File.ReadAllLines(openfolder + "filelist.txt");

                int i = 0;
                int offset = AMB.Length;
                foreach (string filexc in filelist)
                {
                    string file = openfolder + filexc;

                    byte[] filex = File.ReadAllBytes(file);
                    if(repackamts&&Path.GetExtension(file).ToUpper()==".AMT")
                    {
                        if (Directory.Exists(openfolder+"AMT_" + i.ToString()))
                        {
                            string amtpath = openfolder + "AMT_" + i.ToString() + @"\";
                            AMT remount = new AMT(filex);
                            for(int p =0;p<remount.TextureCount;)
                            {
                                string path = amtpath + remount.Textures[p].texinfo.Bpp + "Bpp" + @"\";
                                var png = Image.FromFile(path + "texture_" + p.ToString()+".png");
                                if (remount.SetfromPNG(png, p))
                                    p++;
                                else
                                {
                                    png.Dispose();
                                    return false;
                                }
                                png.Dispose();
                            }
                            filex = remount.AMTB;//Re-packed AMT
                        }
                    }

                    outAMB.AddRange(filex);
                    #region Save pointers on AMB
                    Array.Copy(BitConverter.GetBytes((UInt32)offset), 0, AMB, (i * 0x10) + 0x20, 4);//Offset
                    Array.Copy(BitConverter.GetBytes((UInt32)filex.Length), 0, AMB, (i * 0x10) + 0x24, 4);//Size
                    #endregion
                    offset += filex.Length;
                    i++;
                }
                outFile.AddRange(AMB);
                outFile.AddRange(outAMB.ToArray());
                File.WriteAllBytes(savepath, outFile.ToArray());//Save output
                return true;
            }
        }
        public class AMT
        {
            public string FileName;

            public byte[] Header;
            public byte[] AMTB;
            public int TextureCount;

            public int Version, Unk, Unk2, Unk3, SectionSize;

            public List<int> TexBlocksOffsets;
            public List<TexBlock> TexBlocks;
            public List<Texture> Textures;

            public AMT(byte[] input, string name = "AMT")
            {
                FileName = name;
                AMTB = input;
                if (Encoding.Default.GetString(ReadBlock(input, 0, 4)) != "#AMT")
                    return;//Not AMT case

                uint headersize = (uint)ReadUInt(input, 4, Int.UInt32);
                Header = ReadBlock(input, 0, headersize);

                Unk = (int)ReadUInt(Header, 8, Int.UInt32);
                Version = (int)ReadUInt(Header, 0xC, Int.UInt32);
                TextureCount = (int)ReadUInt(Header, 0x10, Int.UInt32);
                Unk2 = (int)ReadUInt(Header, 0x18, Int.UInt32);
                Unk3 = (int)ReadUInt(Header, 0x1C, Int.UInt32);

                #region TexBlocks Reading
                //TexBlocks Offsets
                TexBlocksOffsets = new List<int>();
                for (int i = 0; i < TextureCount; i++)
                    TexBlocksOffsets.Add((int)ReadUInt(input, (int)(headersize + (i * 4)), Int.UInt32));

                //TexBlocks
                TexBlocks = new List<TexBlock>();
                foreach (var offblock in TexBlocksOffsets)
                {
                    if (offblock != 0)
                    {
                        byte[] entryblock = ReadBlock(input, (uint)offblock, 0x30);
                        TexBlocks.Add(new TexBlock(entryblock));
                    }
                }
                #endregion
                #region Textures Reading
                Textures = new List<Texture>();
                foreach (var texb in TexBlocks)
                {
                    Textures.Add(new Texture(input, texb));
                }
                #endregion
            }
            public bool SetfromPNG(Image input, int index, bool nquant=false)
            {
                //var quant = new WuQuantizer();
                var tex = Textures[index];
                var colors = new HashSet<Color>();
                byte[] coresbyte;
                Color[] cores;
                Bitmap bit = new Bitmap(input);
                //if (nquant)
                   // bit = new Bitmap(quant.QuantizeImage(bit, 10, 70, tex.Clt.Length / 4));
                bit.RotateFlip(RotateFlipType.Rotate180FlipX);
                int colorcount = 0;
                #region Obter cores no eixo cartesiano 2D        
                for (int y = 0; y < bit.Height; y++)
                {
                    for (int x = 0; x < bit.Width; x++)
                    {
                        colors.Add(bit.GetPixel(x, y));
                    }
                }
                colorcount = colors.ToArray().Length;
                #region Calcular quantia de cores
                if (colorcount <= 256)
                    colorcount = 256;
                else if (colorcount <= 16)
                    colorcount = 16;
                else
                {
                    MessageBox.Show("The image has too much colors!\n" +
                    "Expected: " + (tex.texinfo.PalArraySize/4).ToString() + " Colors, " + tex.texinfo.Bpp.ToString() + " Bpp\n" +
                    "Got: " + colorcount.ToString() + " Colors, " + bit.PixelFormat.ToString() + "\n\n" +
                    "Please use Photoshop or OptipixIMGStudio to reduce colors and try again!", "There's something wrong...");
                    return false;
                }
                cores = new Color[256];
                Array.Copy(colors.ToArray(), 0, cores, 0, colors.Count);
                if (tex.Interleave)
                    cores = Texture.swizzlePalette(cores);
                #endregion
                #region Separar cores para array
                coresbyte = new byte[colorcount * 4];//1024 bytes = 256 cores
                for (int i = 0; i < coresbyte.Length; i += 4)
                {
                    if ((i / 4) < cores.Length)
                    {
                        coresbyte[i] = cores[i / 4].R;
                        coresbyte[i + 1] = cores[i / 4].G;
                        coresbyte[i + 2] = cores[i / 4].B;
                        coresbyte[i + 3] = cores[i / 4].A;
                        if (cores[i / 4].A <= 255)
                            coresbyte[i + 3] = (byte)((cores[i / 4].A * 128) / 255);
                    }

                }
                #endregion
                #endregion
                #region Obter índices de pixel no eixo cartesiano 2D
                var pixeldata = new List<byte>();
                Color c1,c2;
                int flagx = bit.Width;
                if (tex.texinfo.Bpp == 4)
                    flagx /= 2;
                for (int y = 0; y < bit.Height; y++)
                    for (int x = 0; x < flagx; x++)
                    {
                        if (tex.texinfo.Bpp == 4)
                        {
                            c1 = bit.GetPixel(x * 2 + 1, bit.Height - y - 1);
                            c2 = bit.GetPixel(x * 2, bit.Height - y - 1);
                            pixeldata.Add((byte)((Texture.FindColorIndex(c1, colors.ToArray()) << 4) + Texture.FindColorIndex(c2, colors.ToArray())));

                        }
                        else
                        {
                            c1 = bit.GetPixel(x, bit.Height - y - 1);
                            pixeldata.Add(Texture.FindColorIndex(c1, colors.ToArray()));
                        }
                    }
                #endregion
                tex.Tex = pixeldata.ToArray();
                tex.Clt = coresbyte;

                Array.Copy(tex.Tex, 0, AMTB, tex.texinfo.TexArrayOffset + 0x20, tex.texinfo.TexArraySize);//Tex array
                Array.Copy(tex.Clt, 0, AMTB, tex.texinfo.PalArrayOffset + 0x20, tex.texinfo.PalArraySize);//Palette array
                return true;
            }
            public class TexBlock
            {
                public int Bpp = 8;

                public int Index, Unk, Unk2;

                public int UnkTex, UnkPal;

                public int Width, Height;
                public int TexArrayOffset, TexArraySize;

                public int Unk3, Unk4;

                public int PalArrayOffset, PalArraySize, BlockID;

                public TexBlock(byte[] entryblock)
                {
                    Index = (int)ReadUInt(entryblock, 0, Int.UInt32);
                    Unk = (int)ReadUInt(entryblock, 4, Int.UInt32);
                    Unk2 = (int)ReadUInt(entryblock, 8, Int.UInt32);

                    UnkTex = (int)ReadUInt(entryblock, 0xC, Int.UInt16);
                    UnkPal = (int)ReadUInt(entryblock, 0xE, Int.UInt16);

                    Width = (int)ReadUInt(entryblock, 0x10, Int.UInt16);
                    Height = (int)ReadUInt(entryblock, 0x12, Int.UInt16);

                    TexArrayOffset = (int)ReadUInt(entryblock, 0x14, Int.UInt32);
                    TexArraySize = (int)ReadUInt(entryblock, 0x18, Int.UInt32);
                    Unk3 = (int)ReadUInt(entryblock, 0x1C, Int.UInt32);
                    Unk4 = (int)ReadUInt(entryblock, 0x20, Int.UInt32);

                    PalArrayOffset = (int)ReadUInt(entryblock, 0x24, Int.UInt32);
                    PalArraySize = (int)ReadUInt(entryblock, 0x28, Int.UInt32);

                    BlockID = (int)ReadUInt(entryblock, 0x2C, Int.UInt32);

                    if (PalArraySize < 0x400)
                        Bpp = 4;
                }
            }
            public class Texture
            {

                public byte[] TexHeader, CltHeader;
                public byte[] Tex, Clt;
                public bool Interleave = false;
                public Color[] Palette;

                public TexBlock texinfo;

                public Texture(byte[] input, TexBlock texBlock)
                {
                    texinfo = texBlock;
                    TexHeader = ReadBlock(input, (uint)texBlock.TexArrayOffset, 0x20);
                    CltHeader = ReadBlock(input, (uint)texBlock.PalArrayOffset, 0x20);

                    if (CltHeader[0x10] != 4)
                        Interleave = true;

                    Tex = ReadBlock(input, (uint)texBlock.TexArrayOffset + 0x20, (uint)texBlock.TexArraySize);
                    Clt = ReadBlock(input, (uint)texBlock.PalArrayOffset + 0x20, (uint)texBlock.PalArraySize);

                    Palette = GetPalette(Clt);
                }
                public Color[] GetPalette(byte[] entries, int rgba = 4)
                {
                    byte[] inptcol = entries;
                    if (Interleave)
                    {
                        //byte[] interleaved = new byte[inptcol.Length];
                        //Array.Copy(inptcol, 0x100, interleaved, 0x200, 0x100);
                        //Array.Copy(inptcol, 0x200, interleaved, 0x100, 0x100);

                        //Array.Copy(inptcol, 0, interleaved, 0, 0x100);
                        //Array.Copy(inptcol, 0x300, interleaved, 0x300, 0x100);
                        //inptcol = interleaved;
                    }

                    var color = new List<Color>();
                    for (int i = 0; i < inptcol.Length; i += rgba)
                    {
                        int r = inptcol[i];
                        int g = inptcol[i + 1];
                        int b = inptcol[i + 2];
                        int a = 0xFF;
                        if (rgba == 4)
                            a = inptcol[i + 3];
                        if (a <= 128)
                            a = (byte)((a * 255) / 128);
                        color.Add(Color.FromArgb(a, r, g, b));
                    }
                    if (Interleave)
                        return unswizzlePalette(color.ToArray());
                    else
                        return color.ToArray();
                }
                public Image GetPNG()
                {
                    var decoder = new ImageDecoderIndexed(Tex, texinfo.Width, texinfo.Height, IndexCodec.FromBitPerPixel(texinfo.Bpp), Palette);
                    return decoder.DecodeImage();
                }

                #region BGR+I
                public static Color[] unswizzlePalette(Color[] palette)
                {
                    if (palette.Length == 256)
                    {
                        Color[] unswizzled = new Color[palette.Length];

                        int j = 0;
                        for (int i = 0; i < 256; i += 32, j += 32)
                        {
                            copy(unswizzled, i, palette, j, 8);
                            copy(unswizzled, i + 16, palette, j + 8, 8);
                            copy(unswizzled, i + 8, palette, j + 16, 8);
                            copy(unswizzled, i + 24, palette, j + 24, 8);
                        }
                        return unswizzled;
                    }
                    else
                    {
                        return palette;
                    }
                }
                public static Color[] swizzlePalette(Color[] palette)
                {
                    if (palette.Length == 256)
                    {
                        Color[] unswizzled = new Color[palette.Length];

                        int j = 0;
                        for (int i = 0; i < 256; i += 32, j += 32)
                        {
                            copySW(palette, i, unswizzled, j, 8);
                            copySW(palette, i + 16, unswizzled, j + 8, 8);
                            copySW(palette, i + 8, unswizzled, j + 16, 8);
                            copySW(palette, i + 24, unswizzled, j + 24, 8);
                        }
                        return unswizzled;
                    }
                    else
                    {
                        return palette;
                    }
                }
                private static void copy(Color[] unswizzled, int i, Color[] swizzled, int j, int num)
                {
                    for (int x = 0; x < num; ++x)
                    {
                        unswizzled[i + x] = swizzled[j + x];
                    }
                }
                private static void copySW(Color[] unswizzled, int i, Color[] swizzled, int j, int num)
                {
                    for (int x = 0; x < num; ++x)
                    {
                        swizzled[j + x] = unswizzled[i + x];
                    }
                }
                #endregion
                public static byte FindColorIndex(Color v, Color[] pal)
                {
                    byte index = 0;
                    for (byte i = 0; i < pal.Length; i++)
                        if (pal[i].R == v.R &&
                            pal[i].G == v.G &&
                            pal[i].B == v.B &&
                            pal[i].A == v.A)
                            return i;

                    return index;
                }

            }
        }
    }
}
