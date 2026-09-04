using System.Collections.Generic;
using JadedBelles.Util.RoomTemplates;
using UnityEditor;
using UnityEngine;

namespace JadedBelles.Util.EditorTools
{
    /// <summary>
    /// Editor window that fills a <see cref="RoomTemplate"/> with prefabs into the scene,
    /// preventing overlaps via <c>Physics.OverlapBox</c>, and can snapshot the
    /// <see cref="SlotZoneAuthoring"/> children of an origin into a new <see cref="RoomTemplate"/>
    /// asset. Open from <c>Tools &gt; JadedBelles &gt; Room Randomizer</c>.
    /// </summary>
    public class RoomRandomizerWindow : EditorWindow
    {
        private RoomTemplate template;
        private Transform origin;
        private Transform container;
        private LayerMask overlapLayers = ~0;
        private bool useSeed = false;
        private int seed = 12345;
        private int lastPlacedCount = 0;
        private int lastFailedCount = 0;

        [MenuItem("Tools/JadedBelles/Room Randomizer")]
        public static void Open()
        {
            GetWindow<RoomRandomizerWindow>("Room Randomizer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Room Template Randomizer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1) Author zones: drop SlotZoneAuthoring components (BoxCollider) under an origin Transform, set slotName + candidate prefabs, size the box.\n" +
                "2) Snapshot into a RoomTemplate asset (below).\n" +
                "3) Randomize: pick a template + origin + container, hit Randomize. Overlaps are rejected via Physics.OverlapBox.",
                MessageType.Info);

            EditorGUILayout.Space();
            GUILayout.Label("Randomize", EditorStyles.boldLabel);

            template = (RoomTemplate)EditorGUILayout.ObjectField("Template", template, typeof(RoomTemplate), false);
            origin = (Transform)EditorGUILayout.ObjectField("Origin", origin, typeof(Transform), true);
            container = (Transform)EditorGUILayout.ObjectField("Container (parent for spawns)", container, typeof(Transform), true);

            LayerMask newMask = EditorGUILayout.MaskField("Overlap Layers", overlapLayers, InternalEditorLayerNames());
            overlapLayers = newMask;

            useSeed = EditorGUILayout.Toggle("Use Fixed Seed", useSeed);
            if (useSeed) seed = EditorGUILayout.IntField("Seed", seed);

            using (new EditorGUI.DisabledScope(template == null || origin == null))
            {
                if (GUILayout.Button("Randomize"))
                {
                    RunRandomize();
                }
            }

            if (GUILayout.Button("Clear Container"))
            {
                ClearContainer();
            }

            if (lastPlacedCount > 0 || lastFailedCount > 0)
            {
                EditorGUILayout.HelpBox($"Last run: placed {lastPlacedCount}, failed {lastFailedCount}.", MessageType.None);
            }

            EditorGUILayout.Space();
            GUILayout.Label("Snapshot Zones \u2192 Template Asset", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Reads every SlotZoneAuthoring under Origin and writes/updates the Template asset above.", MessageType.None);

            using (new EditorGUI.DisabledScope(origin == null))
            {
                if (GUILayout.Button("Snapshot Into Existing Template"))
                {
                    if (template == null)
                    {
                        EditorUtility.DisplayDialog("Room Randomizer", "Assign a Template asset first, or use 'Create New Template From Origin'.", "OK");
                    }
                    else
                    {
                        SnapshotInto(template);
                    }
                }

                if (GUILayout.Button("Create New Template From Origin\u2026"))
                {
                    CreateNewTemplateFromOrigin();
                }
            }
        }

        private void RunRandomize()
        {
            var opts = new RoomRandomizerOptions
            {
                template = template,
                origin = origin,
                container = container,
                overlapLayers = overlapLayers,
                seed = useSeed ? seed : (int?)null,
            };

            int placed = 0, failed = 0;
            opts.onPlaced = (go, slot) =>
            {
                Undo.RegisterCreatedObjectUndo(go, "Randomize Room");
                placed++;
            };
            opts.onPlacementFailed = (slot) => failed++;

            List<GameObject> results = RoomRandomizer.Randomize(opts);
            lastPlacedCount = placed;
            lastFailedCount = failed;
            Debug.Log($"[RoomRandomizer] '{template.templateName}': placed {placed}, failed {failed}.", template);

            if (results.Count > 0)
            {
                Selection.objects = results.ToArray();
            }
        }

        private void ClearContainer()
        {
            if (container == null) return;
            // Iterate backwards; DestroyImmediate mutates the child list.
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(container.GetChild(i).gameObject);
            }
        }

        private void SnapshotInto(RoomTemplate target)
        {
            Undo.RecordObject(target, "Snapshot Room Template");
            target.slots.Clear();

            foreach (var authoring in origin.GetComponentsInChildren<SlotZoneAuthoring>(includeInactive: true))
            {
                var col = authoring.GetComponent<BoxCollider>();
                if (col == null) continue;

                Bounds localZone = ComputeLocalZoneRelativeToOrigin(authoring, col, origin);

                target.slots.Add(new RoomSlot
                {
                    slotName = string.IsNullOrEmpty(authoring.slotName) ? authoring.name : authoring.slotName,
                    localZone = localZone,
                    candidatePrefabs = new List<SlotPrefabCandidate>(authoring.candidatePrefabs),
                    countRange = authoring.countRange,
                    rotationMode = authoring.rotationMode,
                    maxPlacementAttempts = authoring.maxPlacementAttempts,
                    overlapPadding = authoring.overlapPadding,
                });
            }

            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            Debug.Log($"[RoomRandomizer] Snapshotted {target.slots.Count} slot(s) into '{target.name}'.", target);
        }

        private void CreateNewTemplateFromOrigin()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Room Template",
                origin.name + "_Template.asset",
                "asset",
                "Choose where to save the new RoomTemplate asset.");
            if (string.IsNullOrEmpty(path)) return;

            var asset = ScriptableObject.CreateInstance<RoomTemplate>();
            asset.templateName = origin.name;
            AssetDatabase.CreateAsset(asset, path);
            SnapshotInto(asset);
            template = asset;
            Selection.activeObject = asset;
        }

        /// <summary>
        /// Convert the SlotZoneAuthoring's BoxCollider (in that object's local space) into a Bounds
        /// expressed in the room origin's local space, so the runtime randomizer can apply
        /// <c>origin.TransformPoint(...)</c> directly without knowing about the authoring hierarchy.
        /// </summary>
        private static Bounds ComputeLocalZoneRelativeToOrigin(SlotZoneAuthoring authoring, BoxCollider col, Transform origin)
        {
            // Sample all 8 corners of the local collider box, push each into origin-local space,
            // and take the AABB of the results. This handles arbitrary rotation of the authoring
            // object relative to the origin. The resulting AABB is axis-aligned in origin space,
            // which is what the runtime randomizer expects.
            Vector3 c = col.center;
            Vector3 h = col.size * 0.5f;

            Vector3[] localCorners = new Vector3[8];
            int idx = 0;
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sy = -1; sy <= 1; sy += 2)
                    for (int sz = -1; sz <= 1; sz += 2)
                        localCorners[idx++] = c + new Vector3(sx * h.x, sy * h.y, sz * h.z);

            Bounds result = new Bounds();
            bool started = false;
            foreach (var lc in localCorners)
            {
                Vector3 world = authoring.transform.TransformPoint(lc);
                Vector3 originLocal = origin.InverseTransformPoint(world);
                if (!started) { result = new Bounds(originLocal, Vector3.zero); started = true; }
                else result.Encapsulate(originLocal);
            }
            return result;
        }

        private static string[] InternalEditorLayerNames()
        {
            var names = new string[32];
            for (int i = 0; i < 32; i++)
            {
                string n = LayerMask.LayerToName(i);
                names[i] = string.IsNullOrEmpty(n) ? $"Layer {i}" : n;
            }
            return names;
        }
    }
}
