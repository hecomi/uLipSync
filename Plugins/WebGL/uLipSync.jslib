const uLipSyncPlugin =
{
    $uLipSync:
    {
        unityCsharpCallback: null,
        resumeEventNames: ['keydown', 'mousedown', 'touchstart'],
        userEventCallback: function() {
            Module.dynCall_v(uLipSync.unityCsharpCallback);
            for (const ev of uLipSync.resumeEventNames) {
                window.removeEventListener(ev, uLipSync.userEventCallback);
            }
        }
    },

    OnLoad: function(callback) {
        if (WEBAudio.audioContext.state !== 'suspended') return;
        uLipSync.unityCsharpCallback = callback;
        for (const ev of uLipSync.resumeEventNames) {
            window.addEventListener(ev, uLipSync.userEventCallback);
        }
    },
};

autoAddDeps(uLipSyncPlugin, '$uLipSync');
mergeInto(LibraryManager.library, uLipSyncPlugin);
