using CM3D2.Toolkit.Guest4168Branch.Arc;
using CM3D2.Toolkit.Guest4168Branch.Arc.Entry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CM3D2.Toolkit.Guest4168Branch.MultiArcLoader
{
    public class MultiArcLoader
    {
        //Constructor
        string[] directories;
        int threadCount;
        Exclude exclusions;
        bool heirarchyOnly;
        bool keepDupes;
        LoadMethod loadMethod;

        //Instance
        List<List<string>> arcFilePathsDivided;
        List<ArcFileSystem> arcFilePathsDividedArcs;
        public ArcFileSystem arc;

        public MultiArcLoader(string[] dirs, int threads, LoadMethod loadingMethod = LoadMethod.Single, bool loadFileContent = true, bool keepDuplicates = false, Exclude exclude = Exclude.None)
        {
            directories = dirs;
            threadCount = Math.Max(1, threads);
            exclusions = exclude;
            heirarchyOnly = !loadFileContent;
            keepDupes = keepDuplicates;
            loadMethod = loadingMethod;
        }

        public void LoadArcs()
        {
            //Combine all file paths
            List<string> arcFilePaths = new List<string>();
            foreach (string dir in directories)
            {
                if (dir.EndsWith(".arc"))
                {
                    string fileName = Path.GetFileName(dir);
                    if (includeArc(fileName))
                    {
                        arcFilePaths.Add(dir);
                    }
                }
                else
                {
                    string[] files = Directory.GetFiles(dir, "*.arc", SearchOption.AllDirectories);
                    foreach (string file in files)
                    {
                        string fileName = Path.GetFileName(file);
                        if (includeArc(fileName))
                        {
                            arcFilePaths.Add(file);
                        }
                    }
                }
            }

            //Divide work up based on size
            //Could have used some object, but i'm lazy so multiple lists
            arcFilePathsDivided = new List<List<string>>();
            arcFilePathsDividedArcs = new List<ArcFileSystem>();
            List<long> arcFilePathsDividedSizes = new List<long>();
            for (int i = 0; i < threadCount; i++)
            {
                arcFilePathsDivided.Add(new List<string>());
                arcFilePathsDividedArcs.Add(null);
                arcFilePathsDividedSizes.Add(0);
            }

            //Loop all arcs
            foreach (string filePath in arcFilePaths)
            {
                FileInfo nextFile = new FileInfo(filePath);

                //Smallest collection gets next arc
                int threadIndex = arcFilePathsDividedSizes.IndexOf(arcFilePathsDividedSizes.Min());
                arcFilePathsDivided[threadIndex].Add(filePath);
                arcFilePathsDividedSizes[threadIndex] = arcFilePathsDividedSizes[threadIndex] + nextFile.Length;
            }

            //Tasks
            Task[] tasks = new Task[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                tasks[i] = Task.Factory.StartNew(loadArcsInTask, i.ToString());
            }
            Task.WaitAll(tasks);


            //After finishing threads, copy everything to single ARC
            arc = arcFilePathsDividedArcs[0];
            for (int i = 1; i < threadCount; i++)
            {
                arc.MergeCopy(arcFilePathsDividedArcs[i].Root, arc.Root);
            }
        }

        private bool includeArc(string arcFileName)
        {
            arcFileName = arcFileName.ToLower().Trim();

            if ((exclusions & Exclude.None) == Exclude.None)
            {
                return true;
            }

            if ((exclusions & Exclude.BG) == Exclude.BG && arcFileName.StartsWith("bg"))
            {
                return false;
            }
            if ((exclusions & Exclude.CSV) == Exclude.CSV && arcFileName.StartsWith("csv"))
            {
                return false;
            }
            if ((exclusions & Exclude.Motion) == Exclude.Motion && arcFileName.StartsWith("motion"))
            {
                return false;
            }
            if ((exclusions & Exclude.Parts) == Exclude.Parts && arcFileName.StartsWith("parts"))
            {
                return false;
            }
            if ((exclusions & Exclude.PriorityMaterial) == Exclude.PriorityMaterial && arcFileName.StartsWith("prioritymaterial"))
            {
                return false;
            }
            if ((exclusions & Exclude.Script) == Exclude.Script && arcFileName.StartsWith("script"))
            {
                return false;
            }
            if ((exclusions & Exclude.Sound) == Exclude.Sound && arcFileName.StartsWith("sound"))
            {
                return false;
            }
            if ((exclusions & Exclude.System) == Exclude.System && arcFileName.StartsWith("system"))
            {
                return false;
            }
            if ((exclusions & Exclude.Voice) == Exclude.Voice && arcFileName.StartsWith("voice"))
            {
                return false;
            }

            return true;
        }
        private void loadArcsInTask(System.Object i)
        {
            int index = Int32.Parse(i as string);
            List<string> arcFilePaths = arcFilePathsDivided[index];

            ArcFileSystem afs = new ArcFileSystem("root", heirarchyOnly, keepDupes);

            if (arcFilePaths.Count > 0)
            {
                //Loop paths
                foreach (String arcFilePath in arcFilePaths)
                {
                    //Load into the next arc
                    //Console.WriteLine("Task " + index + ": " + arcFilePath);

                    switch (loadMethod)
                    {
                        case LoadMethod.Single:
                            {
                                string arcName = Path.GetFileNameWithoutExtension(arcFilePath);
                                ArcDirectoryEntry dir = afs.CreateDirectory(arcName, afs.Root);
                                afs.LoadArc(arcFilePath, dir, true);
                                break;
                            }
                        case LoadMethod.MiniTemps:
                            {
                                ArcFileSystem afsTemp = new ArcFileSystem(heirarchyOnly, keepDupes);
                                afsTemp.LoadArc(arcFilePath, afsTemp.Root, true);

                                //Combine to shared arc
                                afs.MergeCopy(afsTemp.Root, afs.Root);
                                break;
                            }
                            //case LoadMethod.SingleIgnoreArcNames:
                            //    {
                            //        afs.LoadArc(arcFilePath, afs.Root);
                            //        break;
                            //    }
                    }
                }
            }

            //Copy out
            arcFilePathsDividedArcs[index] = afs;
        }

        //Post Load Methods
        public string[] GetFileListAtExtension(string extension)
        {
            extension = (extension.Contains(".") ? extension.Remove('.') : extension).Trim();

            List<string> data = new List<string>();
            if (arc != null)
            {
                foreach (ArcFileEntry arcFile in arc.Files.Values)
                {
                    if (arcFile.Name.EndsWith("." + extension))
                    {
                        data.Add(arcFile.Name);
                    }
                }
            }

            return data.ToArray();
        }

        public string GetContentsArcFilePath(ArcEntryBase content)
        {
            if (content == null)
            {
                return null;
            }

            if (content.IsFile())
            {
                if (content.Parent != null)
                {
                    return this.GetContentsArcFilePath(content.Parent);
                }

                return null;
            }
            else
            {
                if (((ArcDirectoryEntry)content).Depth == 1)
                {
                    return ((ArcDirectoryEntry)content).ArcPath;
                }

                if (((ArcDirectoryEntry)content).Parent != null)
                {
                    return this.GetContentsArcFilePath(content.Parent);
                }

                return null;
            }
        }

        [System.Flags]
        public enum Exclude
        {
            None = (2 ^ 1),
            BG = (2 ^ 2),
            CSV = (2 ^ 3),
            Motion = (2 ^ 4),
            Parts = (2 ^ 5),
            PriorityMaterial = (2 ^ 6),
            Script = (2 ^ 7),
            Sound = (2 ^ 8),
            System = (2 ^ 0),
            Voice = (2 ^ 10)
        }

        public enum LoadMethod
        {
            Single = 0,
            MiniTemps = 1,
            //SingleIgnoreArcNames = 2
        }
    }
}