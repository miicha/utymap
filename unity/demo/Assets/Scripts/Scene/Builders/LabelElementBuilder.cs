using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scenes.Orbit.Scripts;
using Assets.Scenes.Surface.Scripts;
using Assets.Scripts.UI;
using UnityEngine;
using UtyMap.Unity;
using UtyMap.Unity.Utils;

namespace Assets.Scripts.Scene.Builders
{
    /// <summary> Builds a label. </summary>
    internal sealed class LabelElementBuilder : IElementBuilder
    {
        /// <inheritdoc />
        public GameObject Build(Tile tile, Element element)
        {
            var gameObject = new GameObject(GetName(element));
            // NOTE should be attached to properly oriented canvas from proper scene.
            gameObject.transform.SetParent(GameObject.Find("Text Visualizer").transform);

            return element.Styles.ContainsKey("type") && element.Styles["type"] == "flat"
                ? BuildFlatText(gameObject, element)
                : BuildSphereText(gameObject, element);
        }

        /// <summary> Builds text on sphere. </summary>
        private GameObject BuildSphereText(GameObject gameObject, Element element)
        {
            var sphereText = gameObject.AddComponent<SphereText>();

            sphereText.Coordinate = element.Geometry[0];
            // NOTE should be in sync with sphere size and offsetted polygons
            sphereText.Radius = OrbitCameraController.TileController.Radius + 25;

            var font = new FontWrapper(element.Styles);
            sphereText.font = font.Font;
            sphereText.fontSize = font.Size;
            sphereText.color = font.Color;
            sphereText.text = GetText(element);
            sphereText.alignment = TextAnchor.MiddleCenter;
           
            return gameObject;
        }

        /// <summary> Builds flat text. </summary>
        private GameObject BuildFlatText(GameObject gameObject, Element element)
        {
            var text = gameObject.AddComponent<TextMesh>();

            // TODO determine correct height and tile controller.
            gameObject.transform.position = SurfaceCameraController.TileController.Projection.Project(element.Geometry[0], 100);
            gameObject.transform.rotation = Quaternion.Euler(90, 0, 0);

            var font = new FontWrapper(element.Styles);
            //text.font = font.Font;
            text.fontSize = font.Size;
            text.color = font.Color;
            text.text = GetText(element);
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;

            gameObject.transform.localScale *= font.Scale;

            return gameObject;
        }

        private static string GetText(Element element)
        {
            return element.Tags["name"];
        }

        private static string GetName(Element element)
        {
            return String.Format("place:{0}[{1}]", element.Id,
                element.Tags.Aggregate("", (s, t) => s += String.Format("{0}={1},", t.Key, t.Value)));
        }

        private struct FontWrapper
        {
            public readonly Font Font;
            public readonly int Size;
            public readonly Color Color;
            public readonly int Scale;

            public FontWrapper(Dictionary<string, string> styles)
            {
                Size = int.Parse(styles["font-size"]);
                Font = UnityEngine.Font.CreateDynamicFontFromOSFont(styles["font-name"], Size);
                Color = ColorUtils.FromUnknown(styles["font-color"]);

                if (!styles.ContainsKey("font-scale") || !int.TryParse(styles["font-scale"], out Scale))
                    Scale = 1;
            }
        }
    }
}
