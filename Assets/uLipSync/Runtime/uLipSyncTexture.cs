using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace uLipSync
{

public class uLipSyncTexture : MonoBehaviour
{
    [System.Serializable]
    public class TextureInfo
    {
        public string phoneme;
        public Texture texture;
        public Vector2 uvScale = Vector2.one;
        public Vector2 uvOffset = Vector2.zero;
    }

    public UpdateMethod updateMethod = UpdateMethod.LateUpdate;
    public Renderer targetRenderer;
    public List<TextureInfo> textures = new List<TextureInfo>();
    public float minVolume = -2.5f;
    [Range(0f, 1f)] public float minDuration = 0.1f;

    public Texture initialTexture 
    { 
        get 
        { 
            if (_initialTexture) return _initialTexture;
            if (targetRenderer && targetRenderer.sharedMaterial) 
            {
                return targetRenderer.sharedMaterial.mainTexture;
            }
            return Texture2D.whiteTexture;
        }
        private set 
        { 
            _initialTexture = value; 
        }
    }

    Material _mat;
    Texture _initialTexture;
    LipSyncInfo _info = new LipSyncInfo();
    List<string> _phonemeHistory = new List<string>();
    Dictionary<string, int> _phonemeCountTable = new Dictionary<string, int>();
    float _keepTimer = 0f;
    bool _lipSyncUpdated = false;
    float _volume = -100f;
    string _phoneme = "";

    public void OnLipSyncUpdate(LipSyncInfo info)
    {
        _info = info;
        _lipSyncUpdated = true;

        if (updateMethod == UpdateMethod.LipSyncUpdateEvent)
        {
            UpdateLipSync();
            Apply();
        }
    }

    void Update()
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

    void LateUpdate()
    {
        if (updateMethod == UpdateMethod.LateUpdate)
        {
            Apply();
        }
    }

    void FixedUpdate()
    {
        if (updateMethod == UpdateMethod.FixedUpdate)
        {
            Apply();
        }
    }

    void UpdateLipSync()
    {
        UpdateVolume();
        UpdateVowels();
        _lipSyncUpdated = false;
    }

    void UpdateVolume()
    {
        _volume = 0f;

        if (!_lipSyncUpdated) return;

        if (_info.rawVolume > 0f)
        {
            _volume = Mathf.Log10(_info.rawVolume);
        }
    }

    void UpdateVowels()
    {
        if (_lipSyncUpdated && _volume > minVolume)
        {
            _phonemeHistory.Add(_info.phoneme);
        }
        else
        {
            _phonemeHistory.Add("");
        }

        int minFrame = (int)Mathf.Max(minDuration / Time.deltaTime, 1f);
        while (_phonemeHistory.Count > minFrame)
        {
            _phonemeHistory.RemoveAt(0);
        }

        _phonemeCountTable.Clear();

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

        int maxCount = 0;
        string phoneme = "";
        foreach (var kv in _phonemeCountTable)
        {
            if (kv.Value > maxCount)
            {
                phoneme = kv.Key;
                maxCount = kv.Value;
            }
        }

        if (_phoneme != phoneme)
        {
            _keepTimer = 0f;
            _phoneme = phoneme;
        }
    }

    void Apply()
    {
        if (!targetRenderer) return;

        if (!_mat)
        {
            _initialTexture = targetRenderer.sharedMaterial.mainTexture;
            _mat = targetRenderer.material;
        }

        foreach (var item in textures)
        {
            if (_phoneme != item.phoneme) continue;

            var texture = item.texture ?? _initialTexture;
            if (texture && texture != _mat.mainTexture)
            {
                _mat.mainTexture = texture;
            }

            _mat.mainTextureScale = item.uvScale;
            _mat.mainTextureOffset = item.uvOffset;
        }
    }
}

}

