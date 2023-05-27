using UnityEngine;

public class FrameRateSetter : MonoBehaviour
{
    public int targetFrameRate = 60;
    void Start()
    {
        Application.targetFrameRate = targetFrameRate;
    }
}
