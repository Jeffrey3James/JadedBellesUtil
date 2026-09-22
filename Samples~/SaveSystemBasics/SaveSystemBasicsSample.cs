using System;
using System.Threading.Tasks;
using JadedBelles.Util.SaveSystem;
using UnityEngine;

namespace JadedBelles.Util.Samples.SaveSystemBasics
{
    /// <summary>Attach to one scene object; bind a Button to HandleSavePressed.</summary>
    public sealed class SaveSystemBasicsSample : MonoBehaviour
    {
        [Header("Optional scene reference")]
        [SerializeField] private Transform _player;
        private SaveManager<SampleSaveData> _saves;
        private SampleSaveData _state;
        private bool _ready;
        private bool _saving;

        private async void Start()
        {
            try
            {
                // All Unity API access, including persistentDataPath, happens here.
                _saves = UnitySaveSystem.Create("save-sample", "guest", "slot-1", SampleSaveData.CreateDefinition());
                var result = await _saves.LoadAsync();
                if (this == null) return; // Scene may have been unloaded while reading.
                foreach (var error in result.Errors) Debug.LogWarning(error.Message, this);
                if (!result.Success)
                {
                    Debug.LogError("Save load failed. Offer recovery/retry; autosave remains disabled.", this);
                    return;
                }
                _state = result.Data;
                _state.Player.RuntimeWorld = _state.World;
                if (_player != null)
                {
                    var position = _state.Player.Position;
                    _player.position = new Vector3(position.X, position.Y, position.Z);
                }
                _ready = true;
            }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }

        public async void HandleSavePressed()
        {
            try { await SaveCurrentAsync(); }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }

        public async Task SaveCurrentAsync()
        {
            if (!_ready || _saving) return;
            _saving = true;
            try
            {
                if (_player != null)
                {
                    var position = _player.position;
                    _state.Player.Position = new PositionData { X = position.x, Y = position.y, Z = position.z };
                }
                var snapshot = _state.CaptureSnapshot();
                await _saves.SaveAsync(snapshot);
                if (this != null) Debug.Log("Save completed.", this);
            }
            finally { _saving = false; }
        }
    }
}
