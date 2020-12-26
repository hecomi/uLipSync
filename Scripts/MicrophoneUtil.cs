using UnityEngine;
using System.Collections.Generic;

namespace uLipSync
{

public struct MicrophoneInfo
{
    public string name;
    public int index;
    public int minFreq;
    public int maxFreq;
}

public static class MicrophoneUtil
{
    public static List<MicrophoneInfo> GetMicrophoneList()
    {
        var list = new List<MicrophoneInfo>();

        for (int i = 0; i < Microphone.devices.Length; ++i)
        {
            var info = new MicrophoneInfo();
            info.name = Microphone.devices[i];
            info.index = i;
            Microphone.GetDeviceCaps(info.name, out info.minFreq, out info.maxFreq);
            list.Add(info);
        }

        return list;
    }
}

}
