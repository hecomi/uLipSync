using UnityEngine;
using Unity.Mathematics;    
using System.IO;

namespace uLipSync.Debugging
{

[RequireComponent(typeof(uLipSync))]
public class DebugDump : MonoBehaviour
{
    public uLipSync lipsync { get; private set; }

    public string prefix = "";
    public string dataFile = "data.csv";  
    public string spectrumFile = "spectrum.csv";  
    public string melSpectrumFile = "mel-spectrum.csv";  
    public string melCepstrumFile = "mel-cepstrum.csv";  
    public string mfccFile = "mfcc.csv";  
    
    void Start()
    {
        lipsync = GetComponent<uLipSync>();
    }

    public void DumpAll()
    {
        DumpData();
        DumpSpectrum();
        DumpMelSpectrum();
        DumpMelCepstrum();
        DumpMfcc();
    }

    public void DumpData()
    {
#if ULIPSYNC_DEBUG
        if (string.IsNullOrEmpty(dataFile)) return;
        var fileName = $"{prefix}{dataFile}";
        var sw = new StreamWriter(fileName);
        var data = lipsync.data;
        var dt = 1f / (lipsync.profile.targetSampleRate * 2);
        for (int i = 0; i < data.Length; ++i)
        {
            var t = dt * i;
            var val = data[i];
            sw.WriteLine($"{t},{val}");
        }
        sw.Close();
        Debug.Log($"{fileName} was created.");
#endif
    }

    public void DumpSpectrum()
    {
#if ULIPSYNC_DEBUG
        if (string.IsNullOrEmpty(spectrumFile)) return;
        var fileName = $"{prefix}{spectrumFile}";
        var sw = new StreamWriter(fileName);
        var spectrum = lipsync.spectrum;
        var df = (float)lipsync.profile.targetSampleRate / spectrum.Length;
        for (int i = 0; i < spectrum.Length; ++i)
        {
            var f = df * i;
            var val = math.log(spectrum[i]);
            sw.WriteLine($"{f},{val}");
        }
        sw.Close();
        Debug.Log($"{fileName} was created.");
#endif
    }
    
    public void DumpMelSpectrum()
    {
#if ULIPSYNC_DEBUG
        if (string.IsNullOrEmpty(melSpectrumFile)) return;
        var fileName = $"{prefix}{melSpectrumFile}";
        var sw = new StreamWriter(fileName);
        foreach (var x in lipsync.melSpectrum)
        {
            sw.WriteLine(x);
        }
        sw.Close();
        Debug.Log($"{fileName} was created.");
#endif
    }
    
    public void DumpMelCepstrum()
    {
#if ULIPSYNC_DEBUG
        if (string.IsNullOrEmpty(melCepstrumFile)) return;
        var fileName = $"{prefix}{melCepstrumFile}";
        var sw = new StreamWriter(fileName);
        foreach (var x in lipsync.melCepstrum)
        {
            sw.WriteLine(x);
        }
        sw.Close();
        Debug.Log($"{fileName} was created.");
#endif
    }
    
    public void DumpMfcc()
    {
#if ULIPSYNC_DEBUG
        if (string.IsNullOrEmpty(mfccFile)) return;
        var fileName = $"{prefix}{mfccFile}";
        var sw = new StreamWriter(fileName);
        foreach (var x in lipsync.mfcc)
        {
            sw.WriteLine(x);
        }
        sw.Close();
        Debug.Log($"{fileName} was created.");
#endif
    }
}

}