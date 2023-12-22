#if UNITY_WEBGL && !UNITY_EDITOR
using UnityEngine;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace uLipSync
{

public static class WebGL
{
    static List<uLipSync> instances = new List<uLipSync>();
    static bool isAudioContextResumed = false;

    public static void Register(uLipSync instance)
    {
        if (isAudioContextResumed) return;
        instances.Add(instance);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        OnLoad(OnAuidoContextInitiallyResumed);
    }
    
    [DllImport("__Internal")]
    public static extern void OnLoad(System.Action callback);
    
    [AOT.MonoPInvokeCallback(typeof(System.Action))]
    public static void OnAuidoContextInitiallyResumed()
    {
        foreach (var instance in instances)
        {
            instance.OnAuidoContextInitiallyResumed();
        }
        instances.Clear();
        isAudioContextResumed = true;
    }
}

}
#endif