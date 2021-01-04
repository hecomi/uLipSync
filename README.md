uLipSync
========

**uLipSync** is an Unity asset to do a realtime lipsync (now supports only **A**, **I**, **U**, **E**, and **O**).

- Fast calculation using Job and Burst compiler
- No native plugin / No dependency


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
This is a core component to calculate lipsync. `uLipSync` gets sound buffers from `MonoBehaviour.OnAudioFilterRead()` so you have to attach this component to the same GameObject that has `AudioSource` to play voice. In `LateUpdate()` phase, this component schedules a job to calculate lipsync parameters, then retrives the result in the next `LateUpdate()` timing. All calculation is optimized by Burst compiler.

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
  - **Opne Smoothness**
    - Speed to open mouse (instant 0.0 <-> 1.0 smooth)
  - **Opne Smoothness**
    - Speed to close mouse (instant 0.0 <-> 1.0 smooth)
  - **Opne Smoothness**
    - Speed to change the shape of mouse (instant 0.0 <-> 1.0 smooth)
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

Config
------


Callback
--------



Visualizer
----------


UnityChan
---------
Examples of this asset includes Unity-chan assets (Release packages don't include them).

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
