using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace uLipSync
{
    public class ULipSyncGameObject : MonoBehaviour
    {
        [System.Serializable]
        public class GameObjectInfo
        {
            public string phoneme;
            public GameObject gameObject;
        }

        public UpdateMethod updateMethod = UpdateMethod.LateUpdate;
        public List<GameObjectInfo> gameObjects = new();
        public float minVolume = -2.5f;
        [Range(0f, 1f)] public float minDuration = 0.1f;
        private LipSyncInfo _info;
        readonly List<string> _phonemeHistory = new();
        private Dictionary<string, int> _phonemeCountTable = new();
        private float _keepTimer;
        private bool _lipSyncUpdated;
        private float _volume = -100f;
        private string _phoneme = "";
        [SerializeField] private GameObject _initialGameObject;
        
        
        
        public GameObject InitialGameObject
        {
            get => _initialGameObject ? _initialGameObject : gameObject;
            private set => _initialGameObject = value;
        }

        public void OnLipSyncUpdate(LipSyncInfo info)
        {
            _info = info;
            _lipSyncUpdated = true;

            if (updateMethod != UpdateMethod.LipSyncUpdateEvent) return;
            UpdateLipSync();
            Apply();
        }

        private void Update()
        {
            if (updateMethod != UpdateMethod.LipSyncUpdateEvent)
            {
                UpdateLipSync();
            }

            if (updateMethod == UpdateMethod.Update)
            {
                Apply();
            }
        }

        private void LateUpdate()
        {
            if (updateMethod == UpdateMethod.LateUpdate)
            {
                Apply();
            }
        }

        private void FixedUpdate()
        {
            if (updateMethod == UpdateMethod.FixedUpdate)
            {
                Apply();
            }
        }

        private void UpdateLipSync()
        {
            UpdateVolume();
            UpdateVowels();
            _lipSyncUpdated = false;
        }

        private void UpdateVolume()
        {
            _volume = 0f;

            if (!_lipSyncUpdated) return;

            if (_info.rawVolume > 0f)
            {
                _volume = Mathf.Log10(_info.rawVolume);
            }
        }

        private void UpdateVowels()
        {
            if (_lipSyncUpdated && _volume > minVolume)
            {
                _phonemeHistory.Add(_info.phoneme);
            }
            else
            {
                _phonemeHistory.Add("");
            }

            var minFrame = (int)Mathf.Max(minDuration / Time.deltaTime, 1f);
            while (_phonemeHistory.Count > minFrame && _phonemeHistory.Count > 0)
            {
                _phonemeHistory.RemoveAt(0);
            }

            _phonemeCountTable.Clear();

            for (int i = 0; i < _phonemeHistory.Count; i++)
            {
                _phonemeHistory[i] ??= "";
            }

            foreach (var key in _phonemeHistory)
            {
                if (_phonemeCountTable.ContainsKey(key))
                {
                    ++_phonemeCountTable[key];
                }
                else
                {
                    _phonemeCountTable.Add(key, 0);
                }
            }

            _keepTimer += Time.deltaTime;
            if (_keepTimer < minDuration) return;

            var maxCount = 0;
            var phoneme = "";
            foreach (KeyValuePair<string, int> kv in _phonemeCountTable.Where(kv => kv.Value > maxCount))
            {
                phoneme = kv.Key;
                maxCount = kv.Value;
            }

            if (_phoneme == phoneme) return;
            _keepTimer = 0f;
            _phoneme = phoneme;
        }

        private void Apply()
        {
            if (!InitialGameObject) return;
            
            foreach (GameObjectInfo info in gameObjects.Where(info => info.gameObject != null))
            {
                info.gameObject.SetActive(info.phoneme == _phoneme);
            }
        }
    }
}
