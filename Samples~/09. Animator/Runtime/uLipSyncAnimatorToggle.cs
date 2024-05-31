using UnityEngine;

namespace uLipSync
{

public class uLipSyncAnimatorToggle : MonoBehaviour
{
    public KeyCode toggleKey = KeyCode.Space;
    public Animator animator;
    public string triggerON = "On";
    public string triggerOff = "Off";
    bool _isOn = true;

    void Awake()
    {
        if (animator) return;

        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            Toggle();
        }
    }

    void Toggle()
    {
        _isOn = !_isOn;

        if (!animator) return;

        if (_isOn)
        {
            animator.SetTrigger(triggerON);
        }
        else
        {
            animator.SetTrigger(triggerOff);
        }
    }
}

}