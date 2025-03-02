using System;

namespace Punk.Hotsy
{
    public interface IRecipeMaker
    {
        // События для добавления и удаления файлов
        event EventHandler<FileEventArgs> SourceFileAdded;
        event EventHandler<FileEventArgs> SourceFileRemoved;
        event EventHandler<FileEventArgs> AssemblyFileAdded;
        event EventHandler<FileEventArgs> AssemblyFileRemoved;
        event EventHandler<FileEventArgs> ResourceFileAdded;
        event EventHandler<FileEventArgs> ResourceFileRemoved;

        // События для добавления и удаления директорий
        event EventHandler<DirectoryEventArgs> DirectoryAdded;
        event EventHandler<DirectoryEventArgs> DirectoryRemoved;

        // События для добавления и удаления сборок из GAC
        event EventHandler<GacAssemblyEventArgs> GacAssemblyAdded;
        event EventHandler<GacAssemblyEventArgs> GacAssemblyRemoved;
    }

    public class FileEventArgs : EventArgs
    {
        public string FilePath { get; }

        public FileEventArgs(string filePath)
        {
            FilePath = filePath;
        }
    }

    public class DirectoryEventArgs : EventArgs
    {
        public string DirectoryPath { get; }

        public DirectoryEventArgs(string directoryPath)
        {
            DirectoryPath = directoryPath;
        }
    }

    public class GacAssemblyEventArgs : EventArgs
    {
        public string AssemblyName { get; }

        public GacAssemblyEventArgs(string assemblyName)
        {
            AssemblyName = assemblyName;
        }
    }

    public class SourceDescription
    {
        public string Filter { get; }
        public List<string> Files { get; }

        public SourceDescription(string filter = null, List<string> files = null)
        {
            Filter = filter;
            Files = files ?? new List<string>();
        }

        public bool IsFileIncluded(string filePath)
        {
            if (Filter != null)
            {
                // Проверка по фильтру
                string fileName = Path.GetFileName(filePath);
                bool matchesFilter = fileName != null && fileName.Contains(Filter);
                bool isExcluded = Files.Contains(filePath);
                return matchesFilter && !isExcluded;
            }
            else
            {
                // Проверка по списку вхождений
                return Files.Contains(filePath);
            }
        }
    }
}
