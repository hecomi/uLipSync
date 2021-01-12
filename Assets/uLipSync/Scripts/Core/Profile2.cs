using UnityEngine;
using Unity.Collections;

namespace uLipSync
{

[System.Serializable]
public class SerializedSpectralData
{
    public float[] spectral;
    public NativeArray<float> spectralNativeArray;
#if UNITY_EDITOR
    public NativeArray<float> spectralNativeArrayForEditor;
#endif
}

[CreateAssetMenu(menuName = Common.assetName + "/Profile2")]
public class Profile2 : ScriptableObject
{
    [Min(256)] public int sampleCount = 1024;
    [Min(16)] public int lpcOrder = 64;
    [Min(32)] public int frequencyResolution = 128;
    [Min(3000)] public int maxFrequency = 4000;
    public WindowFunc windowFunc = WindowFunc.Hann;

    public SerializedSpectralData a = new SerializedSpectralData();
    public SerializedSpectralData i = new SerializedSpectralData();
    public SerializedSpectralData u = new SerializedSpectralData();
    public SerializedSpectralData e = new SerializedSpectralData();
    public SerializedSpectralData o = new SerializedSpectralData();

    bool isDirty_ = false;

    void OnEnable()
    {
        Allocate();
    }

    void OnDisable()
    {
        Deallocate();
    }

    public void Set(Vowel vowel, NativeArray<float> H)
    {
        switch (vowel)
        {
            case Vowel.A: Set(ref a.spectral, H); break;
            case Vowel.I: Set(ref i.spectral, H); break;
            case Vowel.U: Set(ref u.spectral, H); break;
            case Vowel.E: Set(ref e.spectral, H); break;
            case Vowel.O: Set(ref o.spectral, H); break;
            default: break;
        }
        isDirty_ = true;
    }

    void Set(ref float[] array, NativeArray<float> H)
    {
        if (array.Length != H.Length)
        {
            array = new float[H.Length];
        }
        H.CopyTo(array);
    }

    SerializedSpectralData Get(Vowel vowel)
    {
        switch (vowel)
        {
            case Vowel.A: return a;
            case Vowel.I: return i;
            case Vowel.U: return u;
            case Vowel.E: return e;
            case Vowel.O: return o;
            default: return null;
        }
    }

    public NativeArray<float> GetNativeArray(Vowel vowel)
    {
        return Get(vowel).spectralNativeArray;
    }

#if UNITY_EDITOR
    public NativeArray<float> GetNativeArrayForEditor(Vowel vowel)
    {
        return Get(vowel).spectralNativeArrayForEditor;
    }
#endif

    NativeArray<float> CreateNativeArrayFrom(float[] array)
    {
        var nativeArray = new NativeArray<float>(frequencyResolution, Allocator.Persistent);
        if (nativeArray.Length == array.Length)
        {
            nativeArray.CopyFrom(array);
        }
        return nativeArray;
    }

    void Allocate()
    {
        for (int i = (int)Vowel.A; i <= (int)Vowel.O; ++i)
        {
            var data = Get((Vowel)i);
            data.spectralNativeArray = CreateNativeArrayFrom(data.spectral);
#if UNITY_EDITOR
            data.spectralNativeArrayForEditor = CreateNativeArrayFrom(data.spectral);
#endif
        }
    }

    void Deallocate()
    {
        for (int i = (int)Vowel.A; i <= (int)Vowel.O; ++i)
        {
            var data = Get((Vowel)i);
            data.spectralNativeArray.Dispose();
#if UNITY_EDITOR
            data.spectralNativeArrayForEditor.Dispose();
#endif
        }
    }

    public void RebuildIfNeeded()
    {
        if (!isDirty_) return;
        Deallocate();
        Allocate();
        isDirty_ = false;
    }
}

}
