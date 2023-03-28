using UnityEngine;
using System.Collections.Generic;

namespace uLipSync
{

public abstract class AnimationBakableMonoBehaviour : MonoBehaviour
{
#if UNITY_EDITOR
    public abstract GameObject target { get; }
    public abstract List<string> GetPropertyNames();
    public abstract List<float> GetPropertyWeights();
    public abstract void OnAnimationBakeStart();
    public abstract void OnAnimationBakeUpdate(LipSyncInfo info, float dt);
    public abstract void OnAnimationBakeEnd();
    public virtual float maxWeight => 1f;
    public virtual float minWeight => 0f;
#endif
}

}

