using Assets.Scripts.Core;
using Assets.Scripts.Core.Plugins;
using Assets.Scripts.Scenes.ThirdPerson.Tiling;
using UnityEngine;
using UtyRx;
using UtyMap.Unity;
using UtyMap.Unity.Data;
using UtyMap.Unity.Utils;
using Component = UtyDepend.Component;

/// <summary> Demonstrates third person use case. </summary>
public class ThirdPersonBehaviour : MonoBehaviour
{
    private const int LevelOfDetail = 16;
    // NOTE change to Grid type for having non-flat terrain.
    private const ElevationDataType ElevationType = ElevationDataType.Flat;

    public GameObject CharacterTarget;
    public GameObject TileContainer;

    public double StartLatitude = 52.5317429;
    public double StartLongitude = 13.3871987;

    private CompositionRoot _compositionRoot;

    private TileController _tileController;

    void Start()
    {
        // init utymap library
        _compositionRoot = InitTask.Run((container, config) =>
        {
            container
                .Register(Component.For<Stylesheet>().Use<Stylesheet>(@"mapcss/default/index.mapcss"))
                .Register(Component.For<MaterialProvider>().Use<MaterialProvider>())
                .Register(Component.For<GameObjectBuilder>().Use<GameObjectBuilder>())
                .Register(Component.For<IElementBuilder>().Use<PlaceElementBuilder>().Named("place"));
        });

        // initial geo position and quad key of character
        var coordinate = new GeoCoordinate(StartLatitude, StartLongitude);
        var quadKey = GeoUtils.CreateQuadKey(coordinate, LevelOfDetail);

        // init tile controller which is responsible for tile processing
        _tileController = new TileController(
            _compositionRoot.GetService<IMapDataStore>(),
            _compositionRoot.GetService<Stylesheet>(),
            ElevationType,
            coordinate,
            LevelOfDetail);
        
        // freeze target till initial tile is loaded
        var rigidbody = CharacterTarget.transform.GetComponent<Rigidbody>();
        rigidbody.isKinematic = true;
        // TODO unsubscribe listener
        _compositionRoot
            .GetService<IMapDataStore>()
            .Subscribe<Tile>(tile =>
            {
                if (!quadKey.Equals(tile.QuadKey)) return;

                Observable.Start(() =>
                {
                    // get elevation at current position
                    var elevation = _compositionRoot
                        .GetService<IMapDataLibrary>()
                        .GetElevation(ElevationType, quadKey, coordinate);
                    // move character accordingly
                    CharacterTarget.transform.localPosition = new Vector3(
                        CharacterTarget.transform.localPosition.x,
                        (float) elevation + 5f,
                        CharacterTarget.transform.localPosition.z);
                    rigidbody.isKinematic = false;
                }, Scheduler.MainThread).Subscribe();
            });
    }

    void Update()
    {
        _tileController.Update(TileContainer.transform, CharacterTarget.transform.localPosition);
    }
}
