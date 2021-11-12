using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using static Naruto_CCS_Text_Editor.Bin;

namespace AMB_AMT_Manipulator
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("AMT/AMB Manipulation Tool\n" +
                "Bit.Raiden 2021\n" +
                "Version 1.5\n" +
                "Base AMB and AMT struct by SusmuelDBZMA&M\n\n" +
                "Choose a operation:\n\n" +
                "1 - Extract AMT's Textures\n" +
                "2 - Repack AMT's Textures\n" +
                "3 - Extract container\n" +
                "4 - Repack container\n"+
                "5 - Exit\n\nType the operation code: ");
            switch(Console.ReadLine())
            {
                case "1":
                    ExtractAMTs();
                    break;

                case "2":
                    RepackAMTs();
                    break;

                case "3":
                    ExtractContainer();
                    break;

                case "4":
                    RepackContainer();
                    break;

                case "5":
                    Environment.Exit(0);
                    break;

                default:
                    #region Operação inválida+timer
                    Console.Clear();
                    var time = TimeSpan.FromSeconds((double)10);
                    long tick = time.Ticks;
                    Console.WriteLine("Operação inválida!\nVerifique o código e tente novamente.");
                    while (tick > 0)
                    {
                        tick--;
                    }
                    if (tick == 0)
                    {
                        BackMenu();
                    }
                    #endregion
                    break;
            }
        }

        static ProgressBar progress;
        static void Atualizar()
        {
            Console.WriteLine("Operation concluded!\n" +
                "Want to go back to main menu?\n" +
                "\n(Y)Yes/(N)No: ");
            switch (Console.ReadLine().ToLower())
            {
                case "y":
                    BackMenu();
                    break;
                case "n":
                    Environment.Exit(0);
                    break;
                default:
                    Console.Clear();
                    var time = TimeSpan.FromSeconds((double)10);
                    long tick = time.Ticks;
                    Console.WriteLine("Invalid operation code!\nVerify the code and try again.");
                    while (tick > 0)
                    {
                        tick--;
                    }
                    if (tick == 0)
                    {
                        Atualizar();
                    }
                    break;
            }
        }
        static void ExtractAMTs()
        {
            Console.Clear();
            Console.WriteLine("Open one or multiple AMT files to extract...");
            var open = new OpenFileDialog();
            open.Title = "Open one or multiple AMT files to extract.";
            open.Multiselect = true;
            if (open.ShowDialog() == DialogResult.OK)
            {
                var folder = new FolderBrowserDialog();
                folder.Description = "Select where to save extracted AMTs textures.(It will create a folder named 'ExtractedAMTs')";
                if (folder.ShowDialog() == DialogResult.OK)
                {
                    string savef = folder.SelectedPath + @"\ExtractedAMTs";
                    if (!Directory.Exists(savef))
                        Directory.CreateDirectory(savef);

                    savef += @"\";

                    progress = new ProgressBar();
                    int pro = 0;
                    Console.Clear();
                    Console.WriteLine("Full Progress: ");

                    foreach (string amtfile in open.FileNames)
                    {
                        byte[] AMT = File.ReadAllBytes(amtfile);
                        if (Encoding.Default.GetString(ReadBlock(AMT, 0, 4)) == "#AMT")
                        {
                            string saveamtp = savef + Path.GetFileNameWithoutExtension(amtfile);
                            if (!Directory.Exists(saveamtp))
                                Directory.CreateDirectory(saveamtp);

                            saveamtp += @"\";

                            File.WriteAllBytes(savef + Path.GetFileName(amtfile), AMT);
                            Budokai.AMT intern = new Budokai.AMT(AMT);

                            int i = 0;
                            foreach (var texture in intern.Textures)
                            {
                                string saveng = saveamtp + texture.texinfo.Bpp + "Bpp";
                                if (!Directory.Exists(saveng))
                                    Directory.CreateDirectory(saveng);
                                saveng += @"\";

                                texture.GetPNG().Save(saveng + "AMT_" + i.ToString() + ".png");
                                i++;
                            }
                        }
                        #region ProgressBar
                        progress.Report((double)pro / open.FileNames.Length);
                        Thread.Sleep(20);
                        #endregion
                        pro++;
                    }
                    progress.Dispose();
                    Console.Clear();
                    Console.WriteLine("Files extracted sucessfuly!!\n" +
                        "See output: " + savef);
                    Atualizar();
                }
                else
                    BackMenu();
            }
            else
                BackMenu();
        }
        static void BackMenu()
        {
                Console.Clear();
                Main(new string[0]);
        }
        static void RepackAMTs()
        {
            Console.Clear();
            Console.WriteLine("Open the AMTs's folder path...");
            var folder = new FolderBrowserDialog();
            folder.Description = "Open the AMTs's folder path...";
            if (folder.ShowDialog() == DialogResult.OK)
            {
                Console.Clear();
                Console.WriteLine("Full Progress: ");
                progress = new ProgressBar();
                int pro = 0;
                foreach (var fileamt in Directory.EnumerateFiles(folder.SelectedPath))
                {
                    byte[] amt = File.ReadAllBytes(fileamt);
                    var amtx = new Budokai.AMT(amt);
                    string amtfolder = folder.SelectedPath + @"\" + Path.GetFileNameWithoutExtension(fileamt) + @"\";

                    int i = 0;
                    foreach (var texture in amtx.Textures)
                    {
                        string path = amtfolder + texture.texinfo.Bpp + "Bpp" + @"\";
                        Image im = Image.FromFile(amtfolder + "AMT_" + i.ToString() + ".png");
                        if (amtx.SetfromPNG(im, i))
                        {
                            continue;
                        }
                        else
                        {
                            im.Dispose();
                            break;
                        }
                        im.Dispose();
                        i++;
                    }
                    #region ProgressBar
                    progress.Report((double)pro / Directory.EnumerateFiles(folder.SelectedPath).Count());
                    Thread.Sleep(20);
                    #endregion
                    pro++;
                    File.WriteAllBytes(fileamt, amtx.AMTB);
                }
                progress.Dispose();
                Console.Clear();
                Console.WriteLine("Repacked sucessfully!\n" +
                            "See path: " + folder.SelectedPath);
                Atualizar();
            }
            else
                BackMenu();
        }
        static void ExtractContainer()
        {
            Console.Clear();
            Console.WriteLine("Open one or multiple AMB containers files to extract...");
            var op = new OpenFileDialog();
            op.Title = "Open one or multiple AMB containers files to extract...";
            op.Multiselect = true;
            if (op.ShowDialog() == DialogResult.OK)
            {
                var folder = new FolderBrowserDialog();
                folder.Description = "Select the folder to extract.(It will create a folder named 'Extracted')";
                if (folder.ShowDialog() == DialogResult.OK)
                {
                    string pathX = folder.SelectedPath + @"\Extracted";
                    if (!Directory.Exists(pathX))
                        Directory.CreateDirectory(pathX);
                    pathX += @"\";

                    bool png = false;
                    if (MessageBox.Show("Do you want to extract possible containing AMT's textures?", "Extractor", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        png = true;
                    Console.Clear();
                    Console.WriteLine("Full Progress: ");
                    progress = new ProgressBar();
                    int pro = 0;
                    foreach (var file in op.FileNames)
                    {
                        byte[] filebx = File.ReadAllBytes(file);
                        if (Encoding.Default.GetString(ReadBlock(filebx, 0, 4)) == "#AMB")
                            {
                            string fname = Path.GetFileNameWithoutExtension(file);
                            Budokai.AMB amb = new Budokai.AMB(filebx, fname);

                            string path = pathX + fname + @"\";
                            if (!Directory.Exists(path))
                                Directory.CreateDirectory(path);

                            amb.ExtractContainer(path, png);
                            #region ProgressBar
                            progress.Report((double)pro / op.FileNames.Length);
                            Thread.Sleep(20);
                            #endregion
                        }
                        pro++;
                    }
                    progress.Dispose();
                    Console.Clear();
                    Console.WriteLine("Extracted sucessfully!\n" +
                            "See path: " + pathX);
                    Atualizar();
                }
                else
                    BackMenu();
            }
            else
                BackMenu();
        }
        static void RepackContainer()
        {
            Console.Clear();
            Console.WriteLine("Open the containing AMB containers folder to repack them...");
            var folder = new FolderBrowserDialog();
            folder.Description = "Open the containing AMB containers folder to repack them...";
            if (folder.ShowDialog() == DialogResult.OK)
            {
                string openpack = folder.SelectedPath;
                folder.Description = "Select a folder to save the repacked AMBs.(It will create a folder named 'Repacked')";
                if (folder.ShowDialog() == DialogResult.OK)
                {
                    string savepath = folder.SelectedPath + @"\Repacked";
                    if (!Directory.Exists(savepath))
                        Directory.CreateDirectory(savepath);

                    progress = new ProgressBar();
                    int pro = 0;
                    Console.Clear();
                    Console.WriteLine("Full Progress: ");

                    savepath += @"\";
                    foreach (var dir in Directory.EnumerateDirectories(openpack))
                    {
                        string pathpack = dir + @"\";
                        if (Budokai.AMB.RemakeContainer(pathpack, savepath + Path.GetFileName(dir) + ".bin", true))
                            continue;
                        else
                        {
                            progress.Dispose();
                            Console.Clear();
                            BackMenu();
                        }
                        #region ProgressBar
                        progress.Report((double)pro / Directory.EnumerateDirectories(openpack).Count());
                        Thread.Sleep(20);
                        #endregion
                        pro++;
                    }
                    progress.Dispose();
                    Console.Clear();
                    Console.WriteLine("Repacked sucessfully!\n" +
                                                "See path: " + openpack);
                    Atualizar();
                }
                else
                    BackMenu();
            }
            else
                BackMenu();
        }
    }
}
