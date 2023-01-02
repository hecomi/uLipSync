using UnityEngine;

namespace uLipSync
{

public static class Util
{
    public static Transform FindChildRecursively(Transform tform, string name)
    {
        if (tform.name == name) return tform;

        for (int i = 0; i < tform.childCount; i++)
        {
            var child = tform.GetChild(i);
            var result = FindChildRecursively(child, name);
            if (result) return result;
        }

        return null;
    }

    public static int GetBlendShapeIndex(SkinnedMeshRenderer smr, string name)
    {
        var mesh = smr.sharedMesh;
        for (int i = 0; i < mesh.blendShapeCount; ++i)
        {
            var bs = mesh.GetBlendShapeName(i);
            if (bs == name) return i;
        }
        return -1;
    }
}

}
