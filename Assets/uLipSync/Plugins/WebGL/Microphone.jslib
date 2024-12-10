// source : https://github.com/bnco-dev/unity-webgl-microphone
var Microphone = {
    // Setup the audiocontext and all required objects. Should be called before
    // any of the other js microphone interface functions in this file. If this
    // returns true, it is possible to start an audio recording with Start()
    JS_Microphone_InitOrResumeContext: function() {
        if (!WEBAudio || WEBAudio.audioWebEnabled == 0) {
            // No WEBAudio object (Unity version changed?)
            return false;
        }

        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            navigator.getUserMedia =
                navigator.getUserMedia || navigator.webkitGetUserMedia ||
                navigator.mozGetUserMedia || navigator.msGetUserMedia;
            if (!navigator.getUserMedia) {
                return false;
            }
        }

        var ctx = document.unityMicrophoneInteropContext;
        if (!ctx) {
            document.unityMicrophoneInteropContext = {};
            ctx = document.unityMicrophoneInteropContext;
        }

        if (!ctx.audioContext || ctx.audioContext.state == "closed"){
            ctx.audioContext = new AudioContext();
        }

        if (ctx.audioContext.state == "suspended") {
            ctx.audioContext.resume();
        }

        if (ctx.audioContext.state == "suspended") {
            return false;
        }

        return true;
    },

    // Returns the index of the most recently created audio clip so we can
    // write to it. Should be called immediately after creating the clip and
    // the value stored for indexing purposes.
    // Relies on undocumented/unexposed js code within Unity's WebGL code,
    // so may break with later versions
    JS_Microphone_GetBufferInstanceOfLastAudioClip: function() {
        if (WEBAudio && WEBAudio.audioInstanceIdCounter) {
            return WEBAudio.audioInstanceIdCounter;
        }
        return -1;
    },

    JS_Microphone_IsRecording: function(deviceIndex) {
        var ctx = document.unityMicrophoneInteropContext;
        if (ctx && ctx.stream) {
            return true;
        } else {
            return false;
        }
    },

    // Get the current index of the last recorded sample
    JS_Microphone_GetPosition: function(deviceIndex) {
        var ctx = document.unityMicrophoneInteropContext;
        if (ctx && ctx.stream) {
            return ctx.stream.currentPosition;
        }
        return -1;
    },

    // Get the sample rate for this device
    // According to https://www.w3.org/TR/webaudio/ WebAudio implementations
    // must support 8khz to 96khz. In practice seems to be best to let the
    // browser pick the sample rate it prefers to avoid audio glitches
    JS_Microphone_GetSampleRate: function(deviceIndex) {
        var ctx = document.unityMicrophoneInteropContext;
        if (ctx && ctx.audioContext.state == "running") {
            return ctx.audioContext.sampleRate;
        }
        return -1;
    },

    // Note samplesPerUpdate balances performance against latency, must be one of:
    // 256, 512, 1024, 2048, 4096, 8192, 16384
    // Note also that the clip sample count for the buffer instance should be
    // a multiple of samplesPerUpdate
    JS_Microphone_Start: function(deviceIndex,bufferInstance,samplesPerUpdate) {
        var ctx = document.unityMicrophoneInteropContext;
        if (ctx && ctx.stream) {
            // We are already recording
            return false;
        }

        var sound = WEBAudio.audioInstances[bufferInstance];
        if (!sound || !sound.buffer) {
            // No buffer for the given bufferInstance (Unity version changed?)
            return false;
        }

        var handleStream = function (userMediaStream) {
            var stream = {};
            stream.userMediaStream = userMediaStream;
            stream.microphoneSource = ctx.audioContext.createMediaStreamSource(userMediaStream);
            stream.processorNode = ctx.audioContext.createScriptProcessor(samplesPerUpdate, 1, 1);
            stream.currentPosition = 0;
            stream.processorNode.onaudioprocess = function(event) {

                // Simple version for minimum delay
                // Assumes clip length is a multiple of samplesPerUpdate
                var outputArray = sound.buffer.getChannelData(0);
                var inputArray = event.inputBuffer.getChannelData(0);
                var pos = stream.currentPosition;
                outputArray.set(inputArray,pos);
                pos = (pos + samplesPerUpdate) % outputArray.length;
                stream.currentPosition = pos;
            }

            stream.microphoneSource.connect(stream.processorNode);

            // Add a zero gain node and connect to destination
            // Some browsers seem to ignore a solo processor node
            stream.gainNode = new GainNode(ctx.audioContext,{gain:0});
            stream.processorNode.connect(stream.gainNode);
            stream.gainNode.connect(ctx.audioContext.destination);

            ctx.stream = stream;
        };

        //var outputArray = sound.buffer.getChannelData(0);
        //var outputArrayLen = outputArray.length;

        if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
            navigator.mediaDevices.getUserMedia({audio:true})
                .then(function(umStream) { handleStream(umStream); })
                .catch(function (e) { console.error(e.name + ": " + e.message); });
        } else {
            navigator.getUserMedia({audio:true},
                function(umStream) { handleStream(umStream); },
                function(e) { console.error(e.name + ": " + e.message); });
        }

        // navigator.getUserMedia(
        //     {audio:true},
        //     function(userMediaStream) {
        //         var stream = {};
        //         stream.userMediaStream = userMediaStream;
        //         stream.microphoneSource = ctx.audioContext.createMediaStreamSource(userMediaStream);
        //         stream.processorNode = ctx.audioContext.createScriptProcessor(samplesPerUpdate, 1, 1);
        //         stream.currentPosition = 0;
        //         stream.processorNode.onaudioprocess = function(event) {

        //             // Simple version for minimum delay
        //             // Assumes clip length is a multiple of samplesPerUpdate
        //             var outputArray = sound.buffer.getChannelData(0);
        //             var inputArray = event.inputBuffer.getChannelData(0);
        //             var pos = stream.currentPosition;
        //             outputArray.set(inputArray,pos);
        //             pos = (pos + samplesPerUpdate) % outputArray.length;
        //             stream.currentPosition = pos;
        //         }

        //         stream.microphoneSource.connect(stream.processorNode);

        //         // Add a zero gain node and connect to destination
        //         // Some browsers seem to ignore a solo processor node
        //         stream.gainNode = new GainNode(ctx.audioContext,{gain:0});
        //         stream.processorNode.connect(stream.gainNode);
        //         stream.gainNode.connect(ctx.audioContext.destination);

        //         ctx.stream = stream;
        //     },
        //     function(e) {
        //         alert('Error capturing audio.');
        //     }
        // );
    },

    JS_Microphone_End: function(deviceIndex) {
        var ctx = document.unityMicrophoneInteropContext;
        if (ctx && ctx.stream) {
            ctx.stream.userMediaStream.getTracks().forEach(
                function(track) {
                    track.stop();
                }
            );

            ctx.stream.gainNode.disconnect();
            ctx.stream.processorNode.disconnect();
            ctx.stream.microphoneSource.disconnect();

            delete ctx.stream;
        }
    },
};

mergeInto(LibraryManager.library, Microphone);