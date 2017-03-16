using System;
using Assets.Scripts.Debug;
using Assets.Scripts.Environment;
using Assets.Scripts.Environment.Reactive;
using Assets.Scripts.Plugins;
using UnityEngine;
using UtyDepend;
using UtyMap.Unity;
using UtyMap.Unity.Data;
using UtyMap.Unity.Infrastructure.Config;
using UtyMap.Unity.Infrastructure.Diagnostic;
using UtyMap.Unity.Infrastructure.IO;
using UtyRx;

namespace Assets.Scripts.Scene
{
    /// <summary> Provides the way to initialize the map. </summary>
    internal static class MapInitTask
    {
        #region Initialization

        /// <summary> Run library initialization logic. </summary>
        public static CompositionRoot Run(bool isDebug = false)
        {
            const string fatalCategoryName = "Fatal";

            // create default container which should not be exposed outside to avoid Service Locator pattern.
            var container = new Container();

            // create trace to log important messages
            var trace = new DebugConsoleTrace();

            // create debug console if necessary
            ShowDebugConsole(trace, isDebug);

            // utymap requires some files/directories to be precreated.
            InstallationApi.EnsureFileHierarchy(trace);

            // setup RX configuration.
            UnityScheduler.SetDefaultForUnity();

            // subscribe to unhandled exceptions in RX
            MainThreadDispatcher.RegisterUnhandledExceptionCallback(ex => trace.Error(fatalCategoryName, ex, "Unhandled exception"));

            try
            {
                var compositionRoot = BuildCompositionRoot(container, trace);
                ExtendDebugConsole(container);
                SubscribeOnMapData(compositionRoot, trace);

                return compositionRoot;
            }
            catch (Exception ex)
            {
                trace.Error(fatalCategoryName, ex, "Cannot setup object graph.");
                throw;
            }
        }

        /// <summary> Builds instance responsible for composing object graph. </summary>
        private static CompositionRoot BuildCompositionRoot(IContainer container, ITrace trace)
        {
            // create configuration from default overriding some properties
            var config = ConfigBuilder.GetDefault()
                .SetStringIndex("Index/")
                .SetSpatialIndex("Index/")
                .Build();

            // create entry point for utymap functionallity
            var compositionRoot = new CompositionRoot(container, config)
                // override default services with unity specific implementation
                .RegisterAction((c, _) => c.RegisterInstance<ITrace>(trace))
                .RegisterAction((c, _) => c.Register(UtyDepend.Component.For<IPathResolver>().Use<UnityPathResolver>()))
                .RegisterAction((c, _) => c.Register(UtyDepend.Component.For<INetworkService>().Use<UnityNetworkService>()))
                // register scene specific services (plugins)
                .RegisterAction((c, _) => c.Register(UtyDepend.Component.For<UnityModelBuilder>().Use<UnityModelBuilder>()))
                .RegisterAction((c, _) => c.Register(UtyDepend.Component.For<MaterialProvider>().Use<MaterialProvider>()))
                // register default mapcss
                .RegisterAction((c, _) => c.Register(UtyDepend.Component.For<Stylesheet>().Use<Stylesheet>(@"MapCss/default/default.mapcss")));

            // setup object graph
            compositionRoot.Setup();

            return compositionRoot;
        }

        /// <summary> Shows debug console in scene. </summary>
        /// <remarks> Console is way to debug/investigate app behavior on real devices when regular debugger is not applicable. </remarks>
        private static void ShowDebugConsole(DebugConsoleTrace trace, bool show)
        {
            if (!show) return;

            // NOTE DebugConsole is based on some adapted solution found in Internet
            var consoleGameObject = new GameObject("_DebugConsole_");
            var console = consoleGameObject.AddComponent<DebugConsole>();
            console.IsOpen = true;
            trace.SetConsole(console);
        }

        /// <summary> Adds extra console commands using container. </summary>
        private static void ExtendDebugConsole(IContainer container)
        {
            var console = GameObject.FindObjectOfType<DebugConsole>();
            if (console != null)
            {
                // that is not nice, but we need to use commands registered in DI with their dependencies
                console.SetContainer(container);
                console.IsOpen = true;
            }
        }

        /// <summary> Starts listening for mapdata from core library to convert it into unity game objects. </summary>
        private static void SubscribeOnMapData(CompositionRoot compositionRoot, ITrace trace)
        {
            const string traceCategory = "mapdata";
            var modelBuilder = compositionRoot.GetService<UnityModelBuilder>();
            compositionRoot.GetService<IMapDataStore>()
               .SubscribeOn<MapData>(Scheduler.ThreadPool)
               .ObserveOn(Scheduler.MainThread)
               .Where(r => !r.Tile.IsDisposed)
               .Subscribe(r => r.Variant.Match(
                               e => modelBuilder.BuildElement(r.Tile, e),
                               m => modelBuilder.BuildMesh(r.Tile, m)),
                          ex => trace.Error(traceCategory, ex, "cannot process mapdata."),
                          () => trace.Warn(traceCategory, "stop listening mapdata."));
        }

        #endregion
    }
}
