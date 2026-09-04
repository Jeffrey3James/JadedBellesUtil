using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JadedBelles.Util.CharacterCustomization
{
    /// <summary>
    /// UGUI controller for the character customization screen. Wires next/previous buttons to a
    /// <see cref="CharacterCustomization"/> rig and a save button that persists the current
    /// selection then loads a return scene.
    /// </summary>
    /// <remarks>
    /// Assign <see cref="bodyPartButtons"/>, <see cref="characterCustomization"/>, and
    /// <see cref="SaveButton"/> in the inspector. The return scene defaults to "MainMenu"; change
    /// <see cref="returnSceneName"/> per game. Leave it empty to skip the scene load and let the
    /// game handle navigation elsewhere.
    /// </remarks>
    public class CharacterUIController : MonoBehaviour
    {
        [SerializeField] private BodyPartButton[] bodyPartButtons;
        [SerializeField] private CharacterCustomization characterCustomization;
        [SerializeField] private Button SaveButton;

        [Tooltip("Scene loaded after saving. Leave empty to skip the scene load.")]
        [SerializeField] private string returnSceneName = "MainMenu";

        private void Start()
        {
            foreach (var partButton in bodyPartButtons)
            {
                BodyPartButton capturedButton = partButton;
                partButton.button.onClick.AddListener(() =>
                {
                    BodyPart targetPart = GetBodyPartBySlotId(capturedButton.slotId);
                    if (targetPart == null) return;

                    if (capturedButton.next)
                    {
                        characterCustomization.GetNextBodyPart(targetPart, characterCustomization.bodyParts);
                    }
                    else
                    {
                        characterCustomization.GetLastBodyPart(targetPart);
                    }

                    var manager = CharacterOutfitManager.Instance;
                    if (manager != null)
                    {
                        manager.SetSavedIndex(targetPart.slotId, targetPart.currentBodyPartIndex);
                    }
                });
            }

            SaveButton.onClick.AddListener(async () =>
            {
                await characterCustomization.PersistCurrentSelectionAsync();
                if (!string.IsNullOrEmpty(returnSceneName))
                {
                    SceneManager.LoadScene(returnSceneName);
                }
            });
        }

        private BodyPart GetBodyPartBySlotId(string slotId)
        {
            foreach (var part in characterCustomization.bodyParts)
            {
                if (part.slotId == slotId) return part;
            }
            Debug.LogWarning($"[CharacterUIController] No body part slot found with id '{slotId}'.");
            return null;
        }
    }
}
