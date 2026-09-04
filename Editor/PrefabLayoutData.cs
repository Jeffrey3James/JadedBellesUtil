using System;
using System.Collections.Generic;
using UnityEngine;

namespace JadedBelles.Util.EditorTools
{
    /// <summary>
    /// ScriptableObject asset that stores a list of prefab placements captured from the scene by
    /// <see cref="PrefabPlacerWindow"/>. Save one of these per layout you want to persist and hand
    /// it back to the placer to reload later or diff against the current scene.
    /// </summary>
    [CreateAssetMenu(menuName = "JadedBelles/Prefab Layout Data", fileName = "PrefabLayoutData")]
    public class PrefabLayoutData : ScriptableObject
    {
        public List<PlacedPrefabData> placedPrefabs = new();
    }

    /// <summary>Single captured placement: which prefab, where, and how it was rotated.</summary>
    [Serializable]
    public class PlacedPrefabData
    {
        public GameObject prefab;
        public Vector3 position;
        public Quaternion rotation;
    }
}
