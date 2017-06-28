using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Core.Plugins
{
    /// <summary> Provides unity materials.</summary>
    internal class MaterialProvider
    {
        private readonly Dictionary<string, Material> _sharedMaterials = new Dictionary<string, Material>();

        /// <summary> Gets shared material. </summary>
        /// <remarks> Should be called from UI thread only. </remarks>
        public Material GetSharedMaterial(string key)
        {
            if (!_sharedMaterials.ContainsKey(key))
                _sharedMaterials[key] = Resources.Load<Material>(key);

            return _sharedMaterials[key];
        }
    }
}
