using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UtyMap.Unity.Infrastructure.Diagnostic;

namespace Assets.Scripts.Environment
{
    /// <summary> Provides platform specific way to extract embedded data to persistent storage. </summary>
    internal static class InstallationApi
    {
        private const string TraceCategory = "install";
        private const string MarkerFileName = "install_log.txt";

        /// <summary> Ensures required filesystem structure. Should be called from main thread. </summary>
        public static void EnsureFileHierarchy(ITrace trace)
        {
            trace.Info(TraceCategory, "checking installed version..");
            if (IsInstalled(trace)) return;

            try
            {
// NOTE On unity editor is easier for development to use original files.
#if !UNITY_EDITOR
                trace.Info(TraceCategory, "creating directories..");
                Directory.CreateDirectory(Path.Combine(EnvironmentApi.ExternalDataPath, "index/cache"));
                Directory.CreateDirectory(Path.Combine(EnvironmentApi.ExternalDataPath, "index/data"));
                Directory.CreateDirectory(Path.Combine(EnvironmentApi.ExternalDataPath, "index/import"));
                
                trace.Info(TraceCategory, "copying assets..");
                CopyFiles(GetMapCssFileNames(), trace);
                CopyFiles(GetLsysFileNames(), trace);
#endif
                MarkAsInstalled();
                trace.Info(TraceCategory, "assets are copied.");
            }
            catch (Exception ex)
            {
                trace.Error(TraceCategory, ex, "cannot copy assets.");
                throw;
            }
        }

        #region Hard coded file/directory names

        // NOTE Android platform does not support getting file list from jar.
        private static IEnumerable<string> GetMapCssFileNames()
        {
            return new List<string>()
            {
                "mapcss",
                "orbit.mapcss",
                "orbit-nodes.mapcss",
                "orbit-ways.mapcss",
                "orbit-areas.mapcss",

                "surface.mapcss",
                "surface-nodes.mapcss",
                "surface-ways.mapcss",
                "surface-areas.mapcss",

                "detail.txt",
                "detail.mapcss",
                "detail-buildings.mapcss",
                "detail-misc.mapcss",
                "detail-poi.mapcss",
                "detail-roads.mapcss",
                "detail-terrain.mapcss",
                "detail-water.mapcss"
            }.Select(f => "mapcss/default/default." + f);
        }

        private static IEnumerable<string> GetLsysFileNames()
        {
            return new List<string>()
            {
                "conifer.lsys",
                "lamp.lsys",
                "tree.lsys"
            }.Select(f => "mapcss/default/" + f);
        }

        #endregion

        private static bool IsInstalled(ITrace trace)
        {
            var file = Path.Combine(EnvironmentApi.ExternalDataPath, MarkerFileName);
            if (!File.Exists(file))
            {
                trace.Info(TraceCategory, "no previous versions detected: fresh install.");
                return false;
            }

            string version = File.ReadAllText(file);
            if (version == EnvironmentApi.Version)
            {
                trace.Info(TraceCategory, "found actual version: {0}.", EnvironmentApi.Version);
                return true;
            }

            trace.Info(TraceCategory, "found old version: {0}; current is {1}.", version, EnvironmentApi.Version);
            return false;
        }

        private static void MarkAsInstalled()
        {
            File.WriteAllText(Path.Combine(EnvironmentApi.ExternalDataPath, MarkerFileName), EnvironmentApi.Version);
        }

        private static void CopyFiles(IEnumerable<string> files, ITrace trace)
        {
            foreach (var fileName in files)
                CopyStreamingAsset(fileName, EnvironmentApi.ExternalDataPath, trace);
        }

        private static void CopyStreamingAsset(string srcAssetRelativePath, string destAbsoluteDirPath, ITrace trace)
        {
            string destAbsolutePath = Path.Combine(destAbsoluteDirPath, srcAssetRelativePath);

            // ensure all parent directories exist
            var parentDirectory = Path.GetDirectoryName(destAbsolutePath);
            Directory.CreateDirectory(parentDirectory);

            // read asset
            string srcAssetAbsolutePath = Path.Combine(Application.streamingAssetsPath, srcAssetRelativePath);
            WWW reader = new WWW(srcAssetAbsolutePath);
            while (!reader.isDone) { }

            trace.Info(TraceCategory, string.Format("copy from {0} to {1} bytes:{2}", srcAssetAbsolutePath, destAbsolutePath, reader.bytes.Length));

            File.WriteAllBytes(destAbsolutePath, reader.bytes);
        }
    }
}
