using UnityEngine;

namespace uLipSync
{

[CreateAssetMenu(menuName = "uLipSync/Config")]
public class Config : ScriptableObject
{
    public int lpcOrder = 32;
    public FormantPair formantA = new FormantPair(853f, 2183f);
    public FormantPair formantI = new FormantPair(416f, 2656f);
    public FormantPair formantU = new FormantPair(423f, 2078f);
    public FormantPair formantE = new FormantPair(550f, 2053f);
    public FormantPair formantO = new FormantPair(696f, 3660f);
}

}
