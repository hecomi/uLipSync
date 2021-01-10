uLipSync
========

**uLipSync** is an Unity asset to do a realtime lipsync (now supports only **A**, **I**, **U**, **E**, and **O**).

- Fast calculation using Job and Burst compiler
- No native plugin / No dependency


Demo
----

<img src="https://raw.githubusercontent.com/wiki/hecomi/uLipSync/Unity-Chan.gif" width="640" />


Environment
-----------
- I've created this asset using Unity 2020.1.17f1 on Windows 10 (not tested with other versions and operation systems yet)
- **Burst** and **Mathematics** should be installed in the Package Manager

<img src="https://raw.githubusercontent.com/wiki/hecomi/uLipSync/Package.png" width="640" />


Get started
-----------
1. Download the latest package from the [Releases](https://github.com/hecomi/uLipSync/releases) page and add it into your project.
2. Attach `uLipSync` component to the GameObject that has `AudioSource` and plays voice sounds.

<img src="https://raw.githubusercontent.com/wiki/hecomi/uLipSync/uLipSync-Default-UI.png" width="640" />

3. Attach `uLipSyncBlendshape` component to the GameObject of your character.
4. Set parameters of the component. Checking `Find From Children` helps you find the target `SkinnedMeshRenderer` and blendshapes of the character's mouth.

<img src="https://raw.githubusercontent.com/wiki/hecomi/uLipSync/uLipSync-BlendShape.png" width="640" />

5. Register `uLipSyncBlendshape.OnLipSyncUpdate` to `uLipSync` in Callback section.

<img src="https://raw.githubusercontent.com/wiki/hecomi/uLipSync/Register-Callback.png" width="640" />

6. Choose a profile from `Man` or `Woman` in LipSync Profile section.

<img src="https://raw.githubusercontent.com/wiki/hecomi/uLipSync/Profiles.png" width="640" />

7. Play!


Copmonents
----------
### `uLipSync`
This is a core component to calculate lipsync. `uLipSync` gets sound buffers from `MonoBehaviour.OnAudioFilterRead()` so you have to attach this component to the same GameObject that has `AudioSource` to play voice. In `LateUpdate()` phase, this component schedules a job to calculate lipsync parameters, then retrives the result in the future `LateUpdate()` timing when the calculation finishes (typically, the calculation is ~1ms so next frame). All calculation is optimized by Burst compiler.

### `uLipSyncBlendShape`
Update BlendShapes of `SkinnedMeshRenderer` by registering `OnLipSyncUpdate()` to the event handler of `uLipSync` components as described in the above section.

### `uLipSyncMicrophone`

<img src="https://raw.githubusercontent.com/wiki/hecomi/uLipSync/uLipSync-Microphone.png" width="640" />

Create `AudioClip` that plays Mic input and set it to `AudioSource`. Please attach this component to the same GameObject that has `uLipSync`. You can start / stop the recording from the Script by calling `StartRecord()` / `StopRecord()`. And you can change the input source by changing `index`. `uLipSync.MicUtil.GetDeviceList()` helps you find the desired input.


Parameters
----------

<img src="https://raw.githubusercontent.com/wiki/hecomi/uLipSync/Section-Parameters.png" width="640" />

- **Volume**
  - **Normalized Volume**
    - Shows the normalized volume calculated using Min Volume and Max Volume
  - **Min Volume**
    - Ignore sounds whose volume is lower than this threshold
  - **Max Volume**
    - Make blendshape value 100 with this threshold (If Auto Volume is checked, this parameter is hidden)
  - **Auto Volume**
    - Calculate Max volume automatically from recent inputs
  - **Auto Volume Amp**
    - Max Volume = current input volume * amp
  - **Auto Volume Filter**
    - Decrease max volume by this filter every frame (MaxVolume *= filter)
- **Smoothness**
  - **Open Smoothness**
    - Speed to open mouth (instant 0.0 <-> 1.0 smooth)
  - **Close Smoothness**
    - Speed to close mouth (instant 0.0 <-> 1.0 smooth)
  - **Vowel Transition Smoothness**
    - Speed to change the shape of mouth (instant 0.0 <-> 1.0 smooth)
- **Output**
  - **Output Sound Gain**
    - Change the volume of output sound
    - If you use `uLipSync` with microphone and don't want to output microphone sound in Unity, please set the gain zero.


Profile
-------

uLipSync detects frequencies of the 1st and the 2nd formants from short sound inputs and guesses the vowel from them.

- https://en.wikipedia.org/wiki/Formant

The preset `Man` and `Woman` profiles have typical frequencies of all vowels for men and women. But these frequencies depend on person, so you can create your profiles by cliking `Create` button if you want to optimize the lipsync for the voice you use.

<img src="https://raw.githubusercontent.com/wiki/hecomi/uLipSync/Asset-Profile.png" width="640" />

- **Formant**
  - **F1**
    - the frequency of the 1st formant (Hz)
  - **F2**
    - the frequency of the 2nd formant (Hz)
- **Tips**
  - Typical frequencies are written here
- **Visualizer**
  - X-axis is the 1st formant, and Y-axis is the 2nd formant
  - Please check each formant is not so close
- **Settings**
  - **Use Error Range**
    - If you want to remove the result outer the range in the graph, please check this
  - **Max Error Range**
    - Maximum range from formants (= Radius in the graph)
  - **Min Log 10H**
    - Do not use the formant if the spectrum value is lower than this threshold


Callback
--------

<img src="https://raw.githubusercontent.com/wiki/hecomi/uLipSync/Section-Callback.png" width="640" />

When lipsync process finishes, the result is notified by this event. The event handler should have a `LipSyncInfo` argument like this:

```c
using UnityEngine;
using uLipSync;

public class DebugPrintLipSyncInfo : MonoBehaviour
{
    public float threshVolume = 0.01f;
    public bool outputLog = true;

    public void OnLipSyncUpdate(LipSyncInfo info)
    {
        if (info.volume > threshVolume && outputLog)
        {
            Debug.LogFormat("MAIN VOWEL: {0}, [ A:{1} I:{2}, U:{3} E:{4} O:{5} N:{6} ], VOL: {7}, FORMANT: {8}, {9}",
                info.mainVowel,
                info.volume,
                info.vowels[Vowel.A],
                info.vowels[Vowel.I],
                info.vowels[Vowel.U],
                info.vowels[Vowel.E],
                info.vowels[Vowel.O],
                info.vowels[Vowel.None],
                info.formant.f1,
                info.formant.f2);
        }
    }
}
```


Config
------

<img src="https://raw.githubusercontent.com/wiki/hecomi/uLipSync/Section-Config.png" width="640" />

The lipsync process calculates LPC spectral envelope, and parameters for the calculation is defined in this section. You can create your parameter set in the same way as `Profile`. I recommend you NOT to change the default settings if you don't know the details of the calculation.

- **Config**
  - **Default**
    - A recommended setting
  - **Calibration**
    - Needs long time (~ 1 sec) but you can see formants more clearly so that you can check frequencies of formants.
  - **Create**
    - Create a new `Config` asset
- **Sample Count**
  - The number of sound data
- **Lpc Order**
  - LPC (Linear predictive coding) order
- **Frequency Resolution**
  - The division number of LPC spectral envelope (0 Hz ~ Max Frequency)
- **Max Frequency**
  - The maximum frequency to check formants (The highest formant is the 2nd one of vowel I, it's about 3000 Hz)
- **Window Func**
  - Window function to calculate the spectral envelope
    - None: Not apply window
    - Hann: Hanning window
    - Blackman-Harris: Blackman-Harris window
    - Gauss_4_5: Gauss window (sigma = 4.5)
- **Check Second Derivative**
  - To guess formants, the second derivative of the spectral envelope are considered. This is useful when the second formant of O is unclear, but sometimes has a bad influence on other vowels.
- **Check Third Formant**
  - Consider also the third formant to guess vowels. This is useful when an undesired peak appears but also has bad effect sometimes.
- **Filter H**
  - The spectral envelope changes slowly to avoid the noise. This is useful when you want to check formant peaks when adjusting formant values in profile.


Visualizer
----------

<img src="https://raw.githubusercontent.com/wiki/hecomi/uLipSync/visualizer.gif" width="640" />

Visualizer is useful when you want to check whether the input sound is correctly analyzed with given profile.

- **Draw On Every Frame**
  - To update graphs in realtime, editor script needs to call `Repaint()` every frame. This causes performance issues in the game, so I recommend to check this only when you really want to see them in realtime.
- **Formant Map**
  - X-axis is the 1st formant frequency, and Y-axis is the 2nd formant one.
  - In runtime, white circle indicates the latest result and the ellipse of the vowel gets brighter by it.
- **LPC Spectral Envelope**
  - Draws a graph of LPC spectral envelope and you can see some related information by selecting following checkboxes:
  - **LPC**
    - LPC spectral envelope
  - **dLPC**
    - The second derivative of the envelope
  - **FFT**
    - FFT result
  - **Formant**
    - Current

UnityChan
---------
Examples include Unity-chan assets (Release packages don't include them).

© Unity Technologies Japan/UCL


License
-------
The MIT License (MIT)

Copyright (c) 2021 hecomi

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
