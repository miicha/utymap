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

            for (int i = 0; i < 2; ++i)
                MapDataAdapter.AdaptMesh(_tile.GetHashCode(), 
                    name,
                    new[] { .0, 0, 0 }, 3,
                    new[] { 0, 0, 0 }, 3,
                    new[] { 0, 0, 0 }, 3,
                    new[] {.0, 0, 0, .0, 0, 0}, 6,
                    new int[0], 0);

            _observer.Verify(o => o.OnNext(It.IsAny<MapData>()), Times.Once);
        }
    }
}
