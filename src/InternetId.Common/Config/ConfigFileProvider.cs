using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;

namespace InternetId.Common.Config
{
    public class ConfigFileProvider : IDisposable
    {
        /// <summary>
        /// 10 KB
        /// </summary>
        private const int maxReadCacheFileSize = 10 * 1024;

        private readonly Dictionary<string, byte[]?> readCache = new Dictionary<string, byte[]?>();
        private PhysicalFileProvider fileProvider;
        private readonly FileSystemWatcher fileSystemWatcher;

        public static ConfigFileProvider Singleton { get; } = Create();

        public ConfigFileProvider(string root)
        {
            fileProvider = new PhysicalFileProvider(root);
            fileSystemWatcher = new FileSystemWatcher(root, "**/*");
            fileSystemWatcher.Changed += FileSystemWatcher_Changed;
            fileSystemWatcher.Created += FileSystemWatcher_Changed;
            fileSystemWatcher.Deleted += FileSystemWatcher_Changed;
            fileSystemWatcher.Renamed += FileSystemWatcher_Changed;
        }

        public static ConfigFileProvider Create()
        {
            if (Environment.GetEnvironmentVariable("CONFIGROOT") is string configRoot && !string.IsNullOrWhiteSpace(configRoot))
            {
                // Relative paths are acceptable and relative to the current working directory.
                configRoot = Path.GetFullPath(configRoot);
            }
            else
            {
                configRoot = Path.GetFullPath("../config");
            }

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                Directory.CreateDirectory(configRoot);
            }
            else
            {
                throw new DirectoryNotFoundException($"The CONFIGROOT directory ({configRoot}) does not exist. It can be changed by setting the CONFIGROOT environment variable.");
            }

            ConfigFileProvider configFileManager = new ConfigFileProvider(configRoot);
            return configFileManager;
        }

        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            // Reset the cache if any files change
            readCache.Clear();
        }

        public byte[]? ReadAllBytes(string subpath)
        {
            if (readCache.TryGetValue(subpath, out var value))
            {
                return value;
            }

            IFileInfo fileInfo = fileProvider.GetFileInfo(subpath);

            value = fileInfo.Exists ? File.ReadAllBytes(fileInfo.PhysicalPath) : null;

            if (value != null && value.Length < maxReadCacheFileSize)
            {
                readCache.TryAdd(subpath, value);
            }

            return value;
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            return fileProvider.GetFileInfo(subpath);
        }

        public void Dispose()
        {
            fileProvider?.Dispose();
        }
    }
}
