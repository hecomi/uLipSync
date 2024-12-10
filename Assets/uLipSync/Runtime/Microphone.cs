// Source : https://github.com/bnco-dev/unity-webgl-microphone
#if !UNITY_EDITOR && UNITY_WEBGL
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEngine
{
    // Matches standard Unity builtin Microphone class in interface so you can
    // use it for multiplatform projects without preprocessor defines.
    // Not a complete match for functionality:
    //
    // 1) If `Start()` is called before the user has interacted with the web
    // page, it can fail and return null. This is a limitation of the web.
    // Specifically, AudioContexts cannot be started until the first user
    // interaction. One solution is to only call `Start()` after a user must
    // have interacted with the page (i.e., after something has been clicked).
    // You can also safely just keep calling it every few seconds until it
    // returns a clip.
    //
    // 2) Calling `Start()`, `GetPosition()`, `IsRecording()` or
    // `GetDeviceCaps()` will kick off the permissions procedure in the user's
    // browser. During this time `Start()` will provide clips, but they will not
    // be filled with data. You can use `IsRecording()` to check whether data is
    // currently coming in. This is similar to other platforms as there is
    // usually a delay between Audio Clip creation and microphone input. Here
    // though the delay time may take longer, and data may never come in if the
    // user does not give microphone permission. There is currently no way to
    // test if the user has declined permission.
    //
    // 3) This package can only access the default recording device. It's
    // probably possible to target a specific device, but it's not implemented
    // here. Best practice for the web seems to be to use the default device
    // anyway.
    //
    // 4) The recording is always looped and frequency is left to the browser to
    // decide. Though it's possible in theory to specify your desired frequency
    // in the browser, this only led to audio issues when I tested it.
    //
    // 5) `Start()` will set the generated audio clips to the specified length,
    // but they may be up to 512 samples shorter or longer than expected to
    // simplify ring buffer behaviour in the plugin.
    //
    // 6) Unity's audio API has [limitations](https://docs.unity3d.com/Manual/webgl-audio.html) on WebGL.
    // Enjoy :)
    public class Microphone
    {
        [DllImport("__Internal")]
        public static extern bool JS_Microphone_InitOrResumeContext();
        [DllImport("__Internal")]
        public static extern int JS_Microphone_GetSampleRate(int deviceIndex);
        [DllImport("__Internal")]
        public static extern int JS_Microphone_GetPosition(int deviceIndex);
        [DllImport("__Internal")]
        public static extern bool JS_Microphone_IsRecording(int deviceIndex);
        [DllImport("__Internal")]
        public static extern int JS_Microphone_GetBufferInstanceOfLastAudioClip();
        [DllImport("__Internal")]
        public static extern void JS_Microphone_Start(int deviceIndex, int bufferInstance, int samplesPerUpdate);
        [DllImport("__Internal")]
        public static extern void JS_Microphone_End(int deviceIndex);

        private static string[] _devices = {
            // We could get a list of devices with MediaDevices.enumerateDevices()
            // For now just do it the easy way, default device only
            ""
        };
        public static string[] devices { get { return _devices; } }

        public static void GetDeviceCaps(string deviceName, out int minFreq, out int maxFreq)
        {
            // According to https://www.w3.org/TR/webaudio/ WebAudio implementations
            // must support 8khz to 96khz. In practice seems to be best to let the
            // browser pick the sample rate it prefers to avoid audio glitches
            JS_Microphone_InitOrResumeContext();
            minFreq = maxFreq = JS_Microphone_GetSampleRate(0);
        }

        public static int GetPosition(string deviceName)
        {
            JS_Microphone_InitOrResumeContext();
            return JS_Microphone_GetPosition(0);
        }

        public static bool IsRecording(string deviceName)
        {
            JS_Microphone_InitOrResumeContext();
            return JS_Microphone_IsRecording(0);
        }

        // Ignores all arguments and assume the following:
        // deviceName: Only one device is supported, the browser default
        // loop: Always true
        // lengthSec: Sample count is equal to 32 * samplesPerUpdate
        // frequency: Left to the context to decide
        //
        // If Start() is called before the user has interacted with the web page,
        // it can fail and return null. Typical solution is to only call Start()
        // after a user must have interacted with the page (i.e., after something
        // has been clicked). You can also safely just keep calling Start() every
        // few seconds until it returns a clip.
        //
        // Returns a clip only if a new audio recording is started. If existing
        // recording going on, returns null.
        //
        // Client code has responsibility to destroy returned AudioClip.
        public static AudioClip Start(string deviceName, bool loop, int lengthSec, int frequency)
        {
            if (lengthSec <= 0)
            {
                return null;
            }

            if (!JS_Microphone_InitOrResumeContext() || JS_Microphone_IsRecording(0))
            {
                return null;
            }

            const int SAMPLES_PER_UPDATE_HIGHFREQ = 1024;
            const int SAMPLES_PER_UPDATE_LOWFREQ = 512;
            const int LENGTH_SAMPLES_MULTIPLIER = 32;

            var sampleRate = JS_Microphone_GetSampleRate(0);

            if (sampleRate <= 0)
            {
                return null;
            }

            var samplesPerUpdate = sampleRate > 32000
                ? SAMPLES_PER_UPDATE_HIGHFREQ
                : SAMPLES_PER_UPDATE_LOWFREQ;
            var lengthSamples = sampleRate * lengthSec;

            // Make sure clip sample count is a multiple of samplesPerUpdate
            var updatesPerClip = lengthSamples / (float)samplesPerUpdate;
            lengthSamples = Mathf.RoundToInt(updatesPerClip) * samplesPerUpdate;

            var clip = AudioClip.Create(
                name: "Microphone AudioClip",
                lengthSamples: lengthSamples,
                channels: 1,
                frequency: sampleRate,
                stream: false
            );
            var clipIndex = JS_Microphone_GetBufferInstanceOfLastAudioClip();
            JS_Microphone_Start(0,clipIndex,samplesPerUpdate);

            return clip;
        }

        public static void End(string deviceName)
        {
            JS_Microphone_End(0);
        }
    }
}
#endif