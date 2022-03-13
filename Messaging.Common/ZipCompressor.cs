using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Messaging.Common
{
    public class ZipCompressor
    {
        private IList<Tuple<string, string>> m_Directories = new List<Tuple<string, string>>();
        private IList<string> m_Files = new List<string>();
        public virtual void AddDirectoryToZip(string path, string targetNameWithZipExtension)
        {
            m_Directories.Add(new Tuple<string, string>(path, targetNameWithZipExtension));
            // Cleanup.AddFile(targetNameWithZipExtension);
        }

        public virtual void AddFileToZip(string path)
        {
            m_Files.Add(path);
        }

        public virtual void ZipEverything(string outputZipPath)
        {
            foreach (var tuple in m_Directories)
            {
                if (File.Exists(tuple.Item2))
                    File.Delete(tuple.Item2);

                ZipFile.CreateFromDirectory(tuple.Item1, tuple.Item2);
                // Cleanup.AddFile(tuple.Item2);

                m_Files.Add(tuple.Item2);
            }

            if (File.Exists(outputZipPath))
                File.Delete(outputZipPath);

            using (var archive = ZipFile.Open(outputZipPath, ZipArchiveMode.Create))
            {
                // Cleanup.AddFile(outputZipPath);

                foreach (var path in m_Files)
                    archive.CreateEntryFromFile(path, Path.GetFileName(path));
            }

            m_Directories.Clear();
            m_Files.Clear();

            // Cleanup.AddFile(outputZipPath);
        }
    }
}
