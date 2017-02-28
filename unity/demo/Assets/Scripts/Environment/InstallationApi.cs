using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UtyMap.Unity.Infrastructure.Diagnostic;

namespace Assets.Scripts.Environment
{
    /// <summary> Provides the way to extract embedded data to persistent storage. </summary>
    internal static class InstallationApi
    {
        private const string MarkerFileName = "install_log.txt";

        /// <summary> Ensures required filesystem structure. Should be called from main thread. </summary>
        public static void EnsureFileHierarchy(ITrace trace)
        {
            if (IsInstalled())
                return;

            // NOTE On unity editor is easier for development to use original files.
#if !UNITY_EDITOR
            CopyFiles(GetMapCssFileNames(), trace);
            CopyFiles(GetNaturalEarthFileNames(), trace);
            CopyFiles(GetOsmFileNames(), trace);
            CopyFiles(GetLsysFileNames(), trace);
#endif
            CreateDirectories(GetDirectories());

            MarkAsInstalled();
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
            }.Select(f => "MapCss/default/default." + f);
        }

        private static IEnumerable<string> GetLsysFileNames()
        {
            return new List<string>()
            {
                "conifer.lsys",
                "lamp.lsys",
                "tree.lsys"
            }.Select(f => "MapCss/default/" + f);
        }

        private static IEnumerable<string> GetNaturalEarthFileNames()
        {
            return new List<string>()
            {
                // NOTE use mapzen data so far
            }.SelectMany(f => new List<string>()
            {
                String.Format("{0}.dbf",Path.Combine("NaturalEarth", f)),
                String.Format("{0}.prj",Path.Combine("NaturalEarth", f)),
                String.Format("{0}.shp",Path.Combine("NaturalEarth", f)),
                String.Format("{0}.shx",Path.Combine("NaturalEarth", f))
            });
        }

        private static IEnumerable<string> GetOsmFileNames()
        {
            return new List<string>()
            {
            }.Select(f => "Osm/" + f);
        }

        private static IEnumerable<string> GetDirectories()
        {
            yield return "Cache";
            yield return "Index";

            for (int i = 1; i <= 16; ++i)
                yield return Path.Combine("Index", i.ToString());
        }

        #endregion

        private static bool IsInstalled()
        {
            var file = Path.Combine(EnvironmentApi.ExternalDataPath, MarkerFileName);
            return File.Exists(file) && File.ReadAllText(file) == EnvironmentApi.Version;
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

        private static void CreateDirectories(IEnumerable<string> directories)
        {
            foreach (var directoryName in directories)
                Directory.CreateDirectory(Path.Combine(EnvironmentApi.ExternalDataPath, directoryName));
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

            trace.Info("install", string.Format("copy from {0} to {1} bytes:{2}", srcAssetAbsolutePath, destAbsolutePath, reader.bytes.Length));

            File.WriteAllBytes(destAbsolutePath, reader.bytes);
        }
    }
}
