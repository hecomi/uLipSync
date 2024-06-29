using UnityEngine;
using UnityEngine.UI;

public class uLipSyncPhonemeUI : MonoBehaviour
{
    public InputField phonemeInputField;
    public RawImage mfccImage;

    public uLipSyncProfileUI profileUi { get; set; }
    public uLipSync.Profile profile { get; set; }
    public int index { get; set; }
    public string phoneme
    {
        get => phonemeInputField?.text;
        set { if (phonemeInputField) phonemeInputField.text = value; }
    }

    bool _isCalibrating = false;
    Texture2D _texture = null;

    void OnEnable()
    {
        phonemeInputField.onEndEdit.AddListener(OnPhonemeEndEdit);
    }

    void OnDisable()
    {
        phonemeInputField.onEndEdit.RemoveListener(OnPhonemeEndEdit);
    }

    void OnPhonemeEndEdit(string phoneme)
    {
        var mfcc = profile.mfccs[index];
        mfcc.name = phoneme;
    }

    void Update()
    {
        if (_isCalibrating) UpdateCalibration();
    }

    public void UpdateMfccTexture()
    {
        if (!mfccImage) return;

        _texture = uLipSync.TextureCreator.CreateMfccTexture(_texture, profile, index);
        mfccImage.texture = _texture;
    }

    void SwapMfccs(int i0, int i1)
    {
        (profile.mfccs[i0], profile.mfccs[i1]) = (profile.mfccs[i1], profile.mfccs[i0]);
    }

    public void OnUpButtonDown()
    {
        if (index == 0) return;
        SwapMfccs(index, index - 1);
        profileUi?.OnProfileChanged();
    }

    public void OnDownButtonDown()
    {
        if (index == profile.mfccs.Count - 1) return;
        SwapMfccs(index, index + 1);
        profileUi?.OnProfileChanged();
    }

    public void OnRemoveButtonDown()
    {
        profile.mfccs.RemoveAt(index);
        profileUi?.OnProfileChanged();
    }

    public void OnCalibButtonDown()
    {
        _isCalibrating = true;
    }

    public void OnCalibButtonUp()
    {
        _isCalibrating = false;
        profileUi?.OnProfileChanged();
    }

    void UpdateCalibration()
    {
        profileUi?.target?.RequestCalibration(index);
        UpdateMfccTexture();
    }
}
