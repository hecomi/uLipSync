using UnityEngine;
using System.Collections.Generic;

namespace uLipSync
{

public struct MicInfo
{
    public string name;
    public int index;
    public int minFreq;
    public int maxFreq;
}

public static class MicrophoneUtil
{
    public static List<MicInfo> GetList()
    {
        var list = new List<MicInfo>();

        for (int i = 0; i < Microphone.devices.Length; ++i)
        {
            var info = new MicInfo();
            info.name = Microphone.devices[i];
            info.index = i;
            Microphone.GetDeviceCaps(info.name, out info.minFreq, out info.maxFreq);
            list.Add(info);
        }

        return list;
    }
}

}
