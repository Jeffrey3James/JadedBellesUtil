using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;

namespace JadedBelles.Util.EditorTools
{
    /// <summary>
    /// Scene View toolbar dropdown that lists every scene enabled in Build Settings and switches to
    /// the picked one. The active scene's name appears as the dropdown label. Registered
    /// automatically once the package is referenced — no menu item needed. Prompts to save unsaved
    /// changes before switching, so a stray click never loses work.
    /// </summary>
    /// <remarks>
    /// Extracted from SyntyGameJam. The toolbar id changed from <c>"Scene Tools/Scene Switcher"</c>
    /// to <c>"JadedBelles/Scene Switcher"</c> so all package tools group under one namespace.
    /// </remarks>
    [EditorToolbarElement(SceneSwitcherToolbar.ID, typeof(SceneView))]
    public class SceneSwitcherToolbar : EditorToolbarDropdown
    {
        public const string ID = "JadedBelles/Scene Switcher";

        public SceneSwitcherToolbar()
        {
            UpdateSceneName();

            tooltip = "Switch Scenes";

            clicked += ShowSceneMenu;

            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            UpdateSceneName();
        }

        private void UpdateSceneName()
        {
            if (EditorSceneManager.GetActiveScene().IsValid())
            {
                text = EditorSceneManager.GetActiveScene().name;
            }
            else
            {
                text = "Scenes";
            }
        }

        private void ShowSceneMenu()
        {
            GenericMenu menu = new GenericMenu();

            var scenes = EditorBuildSettings.scenes;

            foreach (var scene in scenes)
            {
                if (!scene.enabled) continue;

                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scene.path);

                bool isCurrent = EditorSceneManager.GetActiveScene().path == scene.path;

                menu.AddItem(
                    new GUIContent(sceneName),
                    isCurrent,
                    () => OpenScene(scene.path)
                );
            }

            menu.ShowAsContext();
        }

        private void OpenScene(string path)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(path);
            }
        }
    }
}
