
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Punk.Hotsy
{
    public class HotRecipe : IDisposable
    {
        private readonly Dictionary<string, SourcePack> _sourcePacks = new Dictionary<string, SourcePack>();
        private readonly List<string> _gacAssemblies = new List<string>();

        private readonly IRecipeMaker _recipeMaker;

        public HotRecipe(IRecipeMaker recipeMaker)
        {
            _recipeMaker = recipeMaker;
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            _recipeMaker.SourceFileAdded += OnSourceFileAdded;
            _recipeMaker.SourceFileRemoved += OnSourceFileRemoved;
            _recipeMaker.AssemblyFileAdded += OnAssemblyFileAdded;
            _recipeMaker.AssemblyFileRemoved += OnAssemblyFileRemoved;
            _recipeMaker.ResourceFileAdded += OnResourceFileAdded;
            _recipeMaker.ResourceFileRemoved += OnResourceFileRemoved;

            _recipeMaker.DirectoryAdded += OnDirectoryAdded;
            _recipeMaker.DirectoryRemoved += OnDirectoryRemoved;

            _recipeMaker.GacAssemblyAdded += OnGacAssemblyAdded;
            _recipeMaker.GacAssemblyRemoved += OnGacAssemblyRemoved;
        }

        private void UnsubscribeFromEvents()
        {
            _recipeMaker.SourceFileAdded -= OnSourceFileAdded;
            _recipeMaker.SourceFileRemoved -= OnSourceFileRemoved;
            _recipeMaker.AssemblyFileAdded -= OnAssemblyFileAdded;
            _recipeMaker.AssemblyFileRemoved -= OnAssemblyFileRemoved;
            _recipeMaker.ResourceFileAdded -= OnResourceFileAdded;
            _recipeMaker.ResourceFileRemoved -= OnResourceFileRemoved;

            _recipeMaker.DirectoryAdded -= OnDirectoryAdded;
            _recipeMaker.DirectoryRemoved -= OnDirectoryRemoved;

            _recipeMaker.GacAssemblyAdded -= OnGacAssemblyAdded;
            _recipeMaker.GacAssemblyRemoved -= OnGacAssemblyRemoved;
        }

        private void OnSourceFileAdded(object sender, FileEventArgs e)
        {
            AddFileToSourcePack(e.FilePath, FileType.Source);
        }

        private void OnSourceFileRemoved(object sender, FileEventArgs e)
        {
            RemoveFileFromSourcePack(e.FilePath, FileType.Source);
        }

        private void OnAssemblyFileAdded(object sender, FileEventArgs e)
        {
            AddFileToSourcePack(e.FilePath, FileType.Assembly);
        }

        private void OnAssemblyFileRemoved(object sender, FileEventArgs e)
        {
            RemoveFileFromSourcePack(e.FilePath, FileType.Assembly);
        }

        private void OnResourceFileAdded(object sender, FileEventArgs e)
        {
            AddFileToSourcePack(e.FilePath, FileType.Resource);
        }

        private void OnResourceFileRemoved(object sender, FileEventArgs e)
        {
            RemoveFileFromSourcePack(e.FilePath, FileType.Resource);
        }

        private void OnDirectoryAdded(object sender, DirectoryEventArgs e)
        {
            CreateSourcePackForDirectory(e.DirectoryPath);
        }

        private void OnDirectoryRemoved(object sender, DirectoryEventArgs e)
        {
            RemoveSourcePackForDirectory(e.DirectoryPath);
        }

        private void OnGacAssemblyAdded(object sender, GacAssemblyEventArgs e)
        {
            _gacAssemblies.Add(e.AssemblyName);
        }

        private void OnGacAssemblyRemoved(object sender, GacAssemblyEventArgs e)
        {
            _gacAssemblies.Remove(e.AssemblyName);
        }

        private void AddFileToSourcePack(string filePath, FileType fileType)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!_sourcePacks.ContainsKey(directory))
            {
                CreateSourcePackForDirectory(directory);
            }

            var sourcePack = _sourcePacks[directory];
            switch (fileType)
            {
                case FileType.Source:
                    sourcePack.SourceFiles.Files.Add(filePath);
                    break;
                case FileType.Assembly:
                    sourcePack.AssemblyFiles.Files.Add(filePath);
                    break;
                case FileType.Resource:
                    sourcePack.ResourceFiles.Files.Add(filePath);
                    break;
            }
        }

        private void RemoveFileFromSourcePack(string filePath, FileType fileType)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (_sourcePacks.ContainsKey(directory))
            {
                var sourcePack = _sourcePacks[directory];
                switch (fileType)
                {
                    case FileType.Source:
                        sourcePack.SourceFiles.Files.Remove(filePath);
                        break;
                    case FileType.Assembly:
                        sourcePack.AssemblyFiles.Files.Remove(filePath);
                        break;
                    case FileType.Resource:
                        sourcePack.ResourceFiles.Files.Remove(filePath);
                        break;
                }

                if (sourcePack.IsEmpty())
                {
                    RemoveSourcePackForDirectory(directory);
                }
            }
        }

        private void CreateSourcePackForDirectory(string directoryPath)
        {
            if (!_sourcePacks.ContainsKey(directoryPath))
            {
                var sourcePack = new SourcePack(directoryPath);
                _sourcePacks[directoryPath] = sourcePack;
                sourcePack.FileChanged += OnFileChanged;
            }
        }

        private void RemoveSourcePackForDirectory(string directoryPath)
        {
            if (_sourcePacks.ContainsKey(directoryPath))
            {
                var sourcePack = _sourcePacks[directoryPath];
                sourcePack.FileChanged -= OnFileChanged;
                _sourcePacks.Remove(directoryPath);
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            RebuildRequested?.Invoke(this, EventArgs.Empty);
        }

        public List<string> GetDirectories()
        {
            return new List<string>(_sourcePacks.Keys);
        }

        public List<string> GetAllSourceFiles()
        {
            var sourceFiles = new List<string>();
            foreach (var pack in _sourcePacks.Values)
            {
                sourceFiles.AddRange(pack.GetSourceFiles());
            }
            return sourceFiles;
        }

        public List<string> GetAllAssemblyFiles()
        {
            var assemblyFiles = new List<string>();
            foreach (var pack in _sourcePacks.Values)
            {
                assemblyFiles.AddRange(pack.GetAssemblyFiles());
            }
            return assemblyFiles;
        }

        public List<string> GetAllResourceFiles()
        {
            var resourceFiles = new List<string>();
            foreach (var pack in _sourcePacks.Values)
            {
                resourceFiles.AddRange(pack.GetResourceFiles());
            }
            return resourceFiles;
        }

        public List<string> GetGacAssemblies()
        {
            return _gacAssemblies;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("HotRecipe:");

            foreach (var pack in _sourcePacks.Values)
            {
                sb.AppendLine($"  Directory: {pack.DirectoryPath}");
                sb.AppendLine("    Source Files:");
                foreach (var file in pack.GetSourceFiles())
                {
                    sb.AppendLine($"      - {Path.GetFileName(file)}");
                }
                sb.AppendLine("    Assembly Files:");
                foreach (var file in pack.GetAssemblyFiles())
                {
                    sb.AppendLine($"      - {Path.GetFileName(file)}");
                }
                sb.AppendLine("    Resource Files:");
                foreach (var file in pack.GetResourceFiles())
                {
                    sb.AppendLine($"      - {Path.GetFileName(file)}");
                }
            }

            if (_gacAssemblies.Count > 0)
            {
                sb.AppendLine("  GAC Assemblies:");
                foreach (var assembly in _gacAssemblies)
                {
                    sb.AppendLine($"    - {assembly}");
                }
            }

            return sb.ToString();
        }

        public void Dispose()
        {
            UnsubscribeFromEvents();
        }

        public event EventHandler RebuildRequested;

        private enum FileType
        {
            Source,
            Assembly,
            Resource
        }
    }
}
