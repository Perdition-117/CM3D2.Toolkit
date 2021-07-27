// --------------------------------------------------
// CM3D2.Toolkit - ArcDirectoryEntry.cs
// --------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace CM3D2.Toolkit.Guest4168Branch.Arc.Entry
{
    /// <summary>
    ///     Arc Directory Entry
    /// </summary>
    public class ArcDirectoryEntry : ArcEntryBase
    {
        private Dictionary<ulong, ArcDirectoryEntry> _directories;
        private Dictionary<string, ArcFileEntry> _files;
        public string ArcPath { get; set; }

        internal override void Invalidate()
        {
            base.Invalidate();
            _directories.Clear();
            _files.Clear();
        }

        /// <summary>
        ///     Dictionary of Sub Directories
        /// </summary>
        public Dictionary<ulong, ArcDirectoryEntry> Directories => _directories;

        /// <summary>
        ///     Directory Count
        /// </summary>
        public int DirectoryCount => Directories.Values.Count();

        /// <summary>
        ///     File Count
        /// </summary>
        public int FileCount => Files.Values.Count();

        /// <summary>
        ///     Dictionary of Files
        /// </summary>
        public Dictionary<string, ArcFileEntry> Files => _files;

        /// <summary>
        ///     Is the <see cref="ArcFileSystem.Root" /> of a <see cref="ArcFileSystem" />
        /// </summary>
        public bool IsRoot => Depth == 0 && FileSystem.Root == this;

        /// <summary>
        ///     Creates a new Directory Entry, pertaining to <paramref name="fileSystem" />
        /// </summary>
        /// <param name="fileSystem">File System</param>
        internal ArcDirectoryEntry(ArcFileSystem fileSystem) : base(fileSystem)
        {
            _directories = new Dictionary<ulong, ArcDirectoryEntry>();
            _files = new Dictionary<string, ArcFileEntry>();
        }

        internal void AddEntry(ArcEntryBase entry)
        {
            var fileEntry = entry as ArcFileEntry;
            if (fileEntry != null)
            {
                AddEntry(fileEntry);
                return;
            }

            var dirEntry = entry as ArcDirectoryEntry;
            if (dirEntry != null)
            {
                AddEntry(dirEntry);
                return;
            }
        }

        internal void AddEntry(ArcFileEntry entry)
        {
            //If not keeping duplicates
            if (!ArcFileSystem.KeepDuplicateFiles)
            {
                //Replace vs Add (replace could be used alone, but this was better for debugging)
                if (_files.ContainsKey(entry.UTF16Hash.ToString()))
                {
                    _files[entry.UTF16Hash.ToString()] = entry;
                }
                else
                {
                    _files.Add(entry.UTF16Hash.ToString(), entry);
                }
            }
            else
            {
                //If the path is the same, just replace, it won't actually be different
                if (_files.ContainsKey(this.FullName + Path.DirectorySeparatorChar + entry.Name))
                {
                    _files[this.FullName + Path.DirectorySeparatorChar + entry.Name] = entry;
                }
                else
                {
                    _files.Add(this.FullName + Path.DirectorySeparatorChar + entry.Name, entry);
                }
            }
        }

        internal void AddEntry(ArcDirectoryEntry entry)
        {
            //Replace vs Add (replace could be used alone, but this was better for debugging)
            if (_directories.ContainsKey(entry.UTF16Hash))
            {
                _directories[entry.UTF16Hash] = entry;
            }
            else
            {
                _directories.Add(entry.UTF16Hash, entry);
            }
        }

        internal override string PreHash(string nameIn)
        {
            //return nameIn;
            return nameIn.ToLower();
        }

        internal void RemoveEntry(ArcEntryBase entry)
        {
            var fileEntry = entry as ArcFileEntry;
            if (fileEntry != null)
            {
                RemoveEntry(fileEntry);
                return;
            }

            var dirEntry = entry as ArcDirectoryEntry;
            if (dirEntry != null)
            {
                RemoveEntry(dirEntry);
                return;
            }
        }

        internal void RemoveEntry(ArcFileEntry entry)
        {
            if (!ArcFileSystem.KeepDuplicateFiles)
            {
                _files.Remove(entry.UTF16Hash.ToString());
            }
            else
            {
                _files.Remove(entry.FullName);
            }
        }

        internal void RemoveEntry(ArcDirectoryEntry entry)
        {
            _directories.Remove(entry.UTF16Hash);
        }

        public override bool IsFile()
        {
            return false;
        }
    }
}
