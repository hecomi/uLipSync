using UnityEngine;
using UnityEngine.UI;

public class uLipSyncProfileUI : MonoBehaviour
{
    [Header("Main")]
    public uLipSync.uLipSync target;
    public GameObject phonemePrefab;
    public GameObject canvas;

    [Header("UI")]
    public GameObject content;
    public InputField profileInputField;
    public uLipSyncInformationUI infoUi;

    [Header("Control")]
    public KeyCode modifierKey = KeyCode.LeftShift;
    public KeyCode toggleKey = KeyCode.Space;

    string path { get => profileInputField.text; }
    public uLipSync.Profile profile 
    { 
        get => target?.profile; 
        private set { target.profile = value; }
    }

    void Start()
    {
        infoUi?.ClearTexts();
        OnProfileChanged();
    }

    void Update()
    {
        UpdateToggle();
    }

    void UpdateToggle()
    {
        if (!canvas) return;

        if (!Input.GetKeyDown(toggleKey)) return;

        if (modifierKey == KeyCode.None || Input.GetKey(modifierKey))
        {
            if (canvas.activeSelf) 
            {
                Hide();
            }
            else 
            {
                Show();
            }
        }
    }

    public void Create()
    {
        profile = uLipSync.Profile.Create();

        if (!profile.Export(path))
        {
            infoUi?.Error("Create failed.");
            return;
        }

        infoUi?.Success($"Create profile \"{path}\"");
        OnProfileChanged();
    }

    public void Load()
    {
        profile = uLipSync.Profile.Create();

        if (!profile.Import(path))
        {
            infoUi?.Error("Load failed.");
            return;
        }

        infoUi?.Success($"Load profile from \"{path}\"");
        OnProfileChanged();
    }

    public void Save()
    {
        if (!profile)
        {
            infoUi?.Warn("No profile.");
            return;
        }

        if (!profile.Export(path))
        {
            infoUi?.Error("Save failed.");
            return;
        }

        infoUi?.Success($"Save profile to \"{path}\"");
    }

    public void AddPhoneme()
    {
        profile.AddMfcc("New Phoneme");
        OnProfileChanged();
    }

    void RemoveAllChildren()
    {
        foreach (Transform child in content.transform)
        {
            Destroy(child.gameObject);
        }
    }

    public void OnProfileChanged()
    {
        if (!profile) return;
        if (!phonemePrefab) return;

        RemoveAllChildren();

        int index = 0;
        foreach (var mfcc in profile.mfccs)
        {
            var go = Instantiate(phonemePrefab, content.transform);
            var ui = go.GetComponent<uLipSyncPhonemeUI>();
            ui.profileUi = this;
            ui.profile = profile;
            ui.index = index++;
            ui.phoneme = mfcc.name;
            ui.UpdateMfccTexture();
        }
    }

    public void Show()
    {
        canvas?.SetActive(true);
    }

    public void Hide()
    {
        canvas?.SetActive(false);
    }
}
