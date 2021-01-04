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


Details
-------
(writing...)


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
