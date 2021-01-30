using UnityEngine;

namespace uLipSync
{

public static class Util
{
    public static float CalcNextValue(float current, float target, float smoothness)
    {
        int frameRate = Application.targetFrameRate;
        if (frameRate < 0f) frameRate = 60;
        float a = Mathf.Pow(1f - smoothness, frameRate / 60f);
        a = Mathf.Clamp(a, 0f, 1f);
        return Mathf.Lerp(current, target, a);
    }
}

}
