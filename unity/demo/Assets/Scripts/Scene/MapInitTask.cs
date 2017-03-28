using System;
using Assets.Scripts.Environment;
using Assets.Scripts.Environment.Reactive;
using Assets.Scripts.Plugins;
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
        /// <summary> Run library initialization logic. </summary>
        public static CompositionRoot Run()
        {
            const string fatalCategoryName = "Fatal";

            // create default container which should not be exposed outside to avoid Service Locator pattern.
            var container = new Container();

            // create trace to log important messages
            var trace = new UnityLogTrace();

            // utymap requires some files/directories to be precreated.
            InstallationApi.EnsureFileHierarchy(trace);

            // setup RX configuration.
            UnityScheduler.SetDefaultForUnity();

            // subscribe to unhandled exceptions in RX
            MainThreadDispatcher.RegisterUnhandledExceptionCallback(ex => trace.Error(fatalCategoryName, ex, "Unhandled exception"));

            try
            {
                var compositionRoot = BuildCompositionRoot(container, trace);
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
                .RegisterAction((c, _) => c.Register(Component.For<IPathResolver>().Use<UnityPathResolver>()))
                .RegisterAction((c, _) => c.Register(Component.For<INetworkService>().Use<UnityNetworkService>()))
                // register scene specific services (plugins)
                .RegisterAction((c, _) => c.Register(Component.For<UnityModelBuilder>().Use<UnityModelBuilder>()))
                .RegisterAction((c, _) => c.Register(Component.For<MaterialProvider>().Use<MaterialProvider>()))
                // register default mapcss
                .RegisterAction((c, _) => c.Register(Component.For<Stylesheet>().Use<Stylesheet>(@"MapCss/default/default.mapcss")));

            // setup object graph
            compositionRoot.Setup();

            return compositionRoot;
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
    }
}
