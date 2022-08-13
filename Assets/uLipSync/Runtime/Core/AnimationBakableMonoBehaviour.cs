using UnityEngine;
using System.Collections.Generic;

namespace uLipSync
{

abstract public class AnimationBakableMonoBehaviour : MonoBehaviour
{
#if UNITY_EDITOR
    public abstract GameObject target { get; }
    public abstract List<string> GetPropertyNames();
    public abstract List<float> GetPropertyWeights();
    public abstract void OnAnimationBakeStart();
    public abstract void OnAnimationBakeUpdate(LipSyncInfo info, float dt);
    public abstract void OnAnimationBakeEnd();
    public virtual float maxWeight { get { return 1f; } }
    public virtual float minWeight { get { return 0f; } }
#endif
}

}

