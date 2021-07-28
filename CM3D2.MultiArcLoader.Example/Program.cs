using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CM3D2.Toolkit.Guest4168Branch.Arc.Entry;
using CM3D2.Toolkit.Guest4168Branch.MultiArcLoader;

namespace CM3D2.MultiArc.Example
{
    class Program
    {
        static void Main(string[] args)
        {
            //Time
            Stopwatch stopWatch = Stopwatch.StartNew();

            //Load Arcs
            List<List<string>> data = new List<List<string>>();
            MultiArcLoader sal = new MultiArcLoader(new string[] { @"C:\KISS\CM3D2\GameData",
                                                                   @"C:\KISS\COM3D2\GameData",
                                                                   @"C:\KISS\COM3D2\GameData_20",
                                                                   //@"C:\KISS\COM3D2\Mod\[Warps]"
                                                                 }, 5, MultiArcLoader.LoadMethod.Single, false, true, MultiArcLoader.Exclude.None | MultiArcLoader.Exclude.BG);
            sal.LoadArcs();

            //Get only the anm files
            string[] files = sal.GetFileListAtExtension(".nei");
            foreach (string file in files)
            {
                string id = "X".PadLeft(10, '0');
                string name = file.Split(new string[] { ".nei" }, System.StringSplitOptions.RemoveEmptyEntries)[0];
                data.Add(new List<string>() { id, name });
            }

            //Time
            stopWatch.Stop();

            //Base Folders???
            List<ArcDirectoryEntry> level0 = sal.arc.Directories.Where(dir => dir.Depth == 1).ToList();
            foreach (ArcFileEntry file in sal.arc.Files.Values)
            {
                if (!(sal.GetContentsArcFilePath(file) == null || sal.GetContentsArcFilePath(file).Trim().Equals("")))
                {
                    //Console.WriteLine(sal.GetContentsArcFilePath(file));
                }
                else
                {
                    Console.WriteLine("WHERE IS MY MOMMY " + file.FullName);
                }
            }
            Console.WriteLine("ANM Files:" + data.Count + " Time:" + (stopWatch.ElapsedMilliseconds) + " Total Files:" + sal.arc.Files.Count);
            Console.ReadLine();
        }
    }
}
