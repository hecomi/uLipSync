using UnityEngine;
using System.Collections.Generic;

namespace uLipSync
{

public struct MicDevice
{
    public string name;
    public int index;
    public int minFreq;
    public int maxFreq;
}

public static class MicUtil
{
    public static List<MicDevice> GetDeviceList()
    {
        var list = new List<MicDevice>();

        for (int i = 0; i < Microphone.devices.Length; ++i)
        {
            var info = new MicDevice();
            info.name = Microphone.devices[i];
            info.index = i;
            Microphone.GetDeviceCaps(info.name, out info.minFreq, out info.maxFreq);
            list.Add(info);
        }

        return list;
    }
}

}
