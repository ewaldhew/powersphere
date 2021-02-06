using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VectorUtil
{
    public static Vector4 Vector4(Vector3 xyz, float w)
    {
        return new Vector4(xyz.x, xyz.y, xyz.z, w);
    }
}
