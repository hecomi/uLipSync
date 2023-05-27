using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class uLipSyncInformationUI : MonoBehaviour
{
    [Header("Messages")]
    public Text infoText;
    public Text warningText;
    public Text errorText;
    public Text successText;

    public void ClearTexts()
    {
        infoText.text = "";
        warningText.text = "";
        errorText.text = "";
        successText.text = "";
    }

    public void ClearTextsAfter(float time = 3f)
    {
        StartCoroutine(_ClearTextsAfter(time));
    }

    IEnumerator _ClearTextsAfter(float time)
    {
        yield return new WaitForSeconds(time);
        ClearTexts();
    }

    public void Info(string msg)
    {
        ClearTexts();
        infoText.text = msg;
        ClearTextsAfter();
    }

    public void Warn(string msg)
    {
        ClearTexts();
        warningText.text = msg;
        ClearTextsAfter();
    }

    public void Error(string msg)
    {
        ClearTexts();
        errorText.text = msg;
        ClearTextsAfter();
    }

    public void Success(string msg)
    {
        ClearTexts();
        successText.text = msg;
        ClearTextsAfter();
    }
}
