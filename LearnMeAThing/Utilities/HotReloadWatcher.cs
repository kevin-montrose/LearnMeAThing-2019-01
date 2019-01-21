using System;
using System.IO;

namespace LearnMeAThing.Utilities
{
    /// <summary>
    /// Helper that monitors a folder (and it's subfolders) for changes to any file.
    /// 
    /// Invokes OnChange whenever a change is detected, at most once every OnChange.
    /// </summary>
    sealed class HotReloadWatcher
    {
        private readonly string Folder;
        private readonly Action<string> OnChange;
        private readonly TimeSpan CheckEvery;

        private DateTime LastCheck;
        private Buffer<string> FoldersChanged;

        public HotReloadWatcher(string folder, TimeSpan checkEvery, Action<string> onChange)
        {
            Folder = folder;
            OnChange = onChange;
            CheckEvery = checkEvery;
            FoldersChanged = new Buffer<string>(8);
        }

        public void Start()
        {
            var watcher = new FileSystemWatcher(Folder);
            watcher.IncludeSubdirectories = true;
            watcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.LastWrite | NotifyFilters.Size;
            watcher.Changed +=
                (o, e) =>
                {
                    var subPath = e.FullPath;
                    subPath = subPath.Substring(Folder.Length + 1);
                    var sepIx = subPath.IndexOf(Path.DirectorySeparatorChar);
                    if (sepIx == -1)
                    {
                        sepIx = subPath.Length;
                    }

                    var folder = subPath.Substring(0, sepIx);

                    lock (this)
                    {
                        if (!FoldersChanged.Contains(folder))
                        {
                            FoldersChanged.Add(folder);
                        }
                    }
                };

            watcher.EnableRaisingEvents = true;
        }

        public void Check()
        {
            var now = DateTime.UtcNow;
            var diff = now - LastCheck;
            if (diff < CheckEvery) return;

            LastCheck = now;

            lock (this)
            {
                for (var i = 0; i < FoldersChanged.Count; i++)
                {
                    var folder = FoldersChanged[i];
                    OnChange(folder);
                }

                FoldersChanged.Clear();
            }
        }
    }
}