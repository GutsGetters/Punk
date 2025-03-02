using System;
using System.Collections.Generic;
using System.IO;

namespace Punk.Hotsy
{
    internal class SourcePack
    {
        private readonly FileSystemWatcher _fileWatcher;

        public SourceDescription SourceFiles { get; } = new SourceDescription();
        public SourceDescription AssemblyFiles { get; } = new SourceDescription();
        public SourceDescription ResourceFiles { get; } = new SourceDescription();

        public string DirectoryPath => _fileWatcher.Path;

        public SourcePack(string directoryPath)
        {
            _fileWatcher = new FileSystemWatcher
            {
                Path = directoryPath,
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
            };

            _fileWatcher.Changed += OnFileChanged;
            _fileWatcher.Created += OnFileChanged;
            _fileWatcher.Deleted += OnFileChanged;
            _fileWatcher.Renamed += OnFileChanged;

            _fileWatcher.EnableRaisingEvents = true;
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            FileChanged?.Invoke(this, e);
        }

        public List<string> GetSourceFiles()
        {
            return SourceFiles.Files;
        }

        public List<string> GetAssemblyFiles()
        {
            return AssemblyFiles.Files;
        }

        public List<string> GetResourceFiles()
        {
            return ResourceFiles.Files;
        }

        public bool IsEmpty()
        {
            return SourceFiles.Files.Count == 0 &&
                   AssemblyFiles.Files.Count == 0 &&
                   ResourceFiles.Files.Count == 0;
        }

        public event EventHandler<FileSystemEventArgs> FileChanged;
    }
}
