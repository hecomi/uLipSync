using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Collections.Generic;

namespace uLipSync.Timeline
{

public class uLipSyncMixer : PlayableBehaviour
{
    public uLipSyncTrack track { get; set; }
    public TimelineClip[] clips { get; set; }
    uLipSyncTimelineEvent _target = null;
    Dictionary<string, float> _phonemeRatio = new Dictionary<string, float>();

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        if (_target) _target.OnStop();
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        _target = playerData as uLipSyncTimelineEvent;
        if (!_target) return;

        float volume = 0f;
        _phonemeRatio.Clear();

        for (int i = 0; i < clips.Length; i++)
        {
            var clip = clips[i];
            var asset = clips[i].asset as uLipSyncClip;
            var behaviour = asset.behaviour;
            var weight = playable.GetInputWeight(i);

            volume += behaviour.frame.volume * asset.volume * weight;
            foreach (var phoneme in behaviour.frame.phonemes)
            {
                if (!_phonemeRatio.ContainsKey(phoneme.phoneme))
                {
                    _phonemeRatio.Add(phoneme.phoneme, 0f);
                }
                _phonemeRatio[phoneme.phoneme] += phoneme.ratio * weight;
            }
        }

        var frame = BakedFrame.zero;
        frame.volume = volume;
        foreach (var kv in _phonemeRatio)
        {
            frame.phonemes.Add(new BakedPhonemeRatio() {
                phoneme = kv.Key, 
                ratio = kv.Value,
            });
        }

        _target.OnFrame(frame);
    }
}

}