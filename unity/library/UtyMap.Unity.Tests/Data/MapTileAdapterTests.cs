using System.Runtime.InteropServices;
using Moq;
using NUnit.Framework;
using UtyMap.Unity.Data;
using UtyRx;

namespace UtyMap.Unity.Tests.Data
{
    [TestFixture]
    public class MapTileAdapterTests
    {
        private Mock<IObserver<MapData>> _observer;
        private Tile _tile;
            
        [TestFixtureSetUp]
        public void Setup()
        {
            _tile = new Tile(new QuadKey(), new Mock<Stylesheet>("").Object, new Mock<IProjection>().Object, ElevationDataType.Flat);
            _observer = new Mock<IObserver<MapData>>();

            MapDataAdapter.Add(_tile);
            MapDataAdapter.Add(_observer.Object);
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            MapDataAdapter.Clear();
        }

        [TestCase("building")]
        public void CanAdaptTheSameNonTerrainMeshOnlyOnce(string name)
        {
            name += ":42";
            var vertices = new double[] {.0, 0, 0};
            GCHandle verticesPinned = GCHandle.Alloc(vertices, GCHandleType.Pinned);
            var triangles = new int[] { 0, 0, 0 };
            GCHandle trianglesPinned = GCHandle.Alloc(triangles, GCHandleType.Pinned);
            var colors = new int[] { 0, 0, 0 };
            GCHandle colorsPinned = GCHandle.Alloc(colors, GCHandleType.Pinned);
            var uvs = new double[] { .0, 0, 0, .0, 0, 0 };
            GCHandle uvsPinned = GCHandle.Alloc(uvs, GCHandleType.Pinned);
            var uvMap = new int[0] {};
            GCHandle uvMapPinned = GCHandle.Alloc(uvMap, GCHandleType.Pinned);

            for (int i = 0; i < 2; ++i)
                MapDataAdapter.AdaptMesh(_tile.GetHashCode(),
                    name,
                    verticesPinned.AddrOfPinnedObject(), vertices.Length,
                    trianglesPinned.AddrOfPinnedObject(), triangles.Length,
                    colorsPinned.AddrOfPinnedObject(), colors.Length,
                    uvsPinned.AddrOfPinnedObject(), uvs.Length,
                    uvMapPinned.AddrOfPinnedObject(), uvMap.Length);

            _observer.Verify(o => o.OnNext(It.IsAny<MapData>()), Times.Once);
        }
    }
}
