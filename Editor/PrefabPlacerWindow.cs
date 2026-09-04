using UnityEditor;
using UnityEngine;

namespace JadedBelles.Util.EditorTools
{
    /// <summary>
    /// Editor window for scattering copies of a prefab across a rectangular area around an origin.
    /// Opens from <c>Tools &gt; JadedBelles &gt; Prefab Placer</c>. Placed prefabs can be captured
    /// into a <see cref="PrefabLayoutData"/> asset for reload later.
    /// </summary>
    /// <remarks>
    /// Extracted from SyntyGameJam. Changed from the source:
    /// <list type="bullet">
    ///   <item>The scene tag used to identify placed prefabs is now an inspector field
    ///     (<see cref="placedPrefabTag"/>) instead of the hardcoded string <c>"PlacedPrefab"</c>.
    ///     Set it to any tag your project already defines.</item>
    ///   <item>The menu path moved from <c>Tools/Prefab Placer Tool</c> to
    ///     <c>Tools/JadedBelles/Prefab Placer</c> so all package tools group under one submenu.</item>
    /// </list>
    /// </remarks>
    public class PrefabPlacerWindow : EditorWindow
    {
        private GameObject prefabToPlace;
        private PrefabLayoutData layoutData;

        private int maxCount = 10;
        private int currentCount = 0;
        private float width = 10f;
        private float length = 10f;
        private Transform origin;

        [Tooltip("Scene tag used to find and clear previously placed instances. Must be defined in the project's Tag Manager.")]
        [SerializeField] private string placedPrefabTag = "PlacedPrefab";

        [MenuItem("Tools/JadedBelles/Prefab Placer")]
        public static void Open()
        {
            GetWindow<PrefabPlacerWindow>("Prefab Placer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Prefab Placement Tool", EditorStyles.boldLabel);

            prefabToPlace = (GameObject)EditorGUILayout.ObjectField("Prefab", prefabToPlace, typeof(GameObject), false);
            layoutData = (PrefabLayoutData)EditorGUILayout.ObjectField("Layout Data", layoutData, typeof(PrefabLayoutData), false);

            maxCount = EditorGUILayout.IntField("Max Count", maxCount);
            width = EditorGUILayout.FloatField("Width", width);
            length = EditorGUILayout.FloatField("Length", length);
            origin = (Transform)EditorGUILayout.ObjectField("Origin", origin, typeof(Transform), true);
            placedPrefabTag = EditorGUILayout.TextField("Placed Prefab Tag", placedPrefabTag);

            GUILayout.Space(10);

            if (GUILayout.Button("Spawn Prefab"))
            {
                SpawnPrefab();
            }

            if (GUILayout.Button("Save Scene Objects"))
            {
                SaveSceneObjects();
            }

            if (GUILayout.Button("Clear Spawned (Editor Only)"))
            {
                ClearSpawned();
            }
        }

        private void SpawnPrefab()
        {
            if (prefabToPlace == null) return;
            if (currentCount >= maxCount) return;

            float halfWidth = width * 0.5f;
            float halfLength = length * 0.5f;

            float randomX = Random.Range(-halfWidth, halfWidth);
            float randomZ = Random.Range(-halfLength, halfLength);

            Vector3 center = origin ? origin.position : Vector3.zero;
            Vector3 spawnPos = center + new Vector3(randomX, 0f, randomZ);

            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefabToPlace);
            obj.transform.position = spawnPos;

            currentCount++;
        }

        private void SaveSceneObjects()
        {
            if (layoutData == null) return;
            if (string.IsNullOrEmpty(placedPrefabTag)) return;

            layoutData.placedPrefabs.Clear();

            foreach (GameObject obj in GameObject.FindGameObjectsWithTag(placedPrefabTag))
            {
                layoutData.placedPrefabs.Add(new PlacedPrefabData
                {
                    prefab = prefabToPlace,
                    position = obj.transform.position,
                    rotation = obj.transform.rotation
                });
            }

            EditorUtility.SetDirty(layoutData);
            AssetDatabase.SaveAssets();
        }

        private void ClearSpawned()
        {
            if (string.IsNullOrEmpty(placedPrefabTag)) return;

            foreach (GameObject obj in GameObject.FindGameObjectsWithTag(placedPrefabTag))
            {
                DestroyImmediate(obj);
            }

            currentCount = 0;
        }
    }
}
