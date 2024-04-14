/********************************************************************
created:    2024-03-16
author:     lixianmin

 https://github.com/ThousandAnt/ta-frustrum-culling.git

Copyright (C) - All Rights Reserved
*********************************************************************/

using Unity.Mathematics;
using UnityEngine;

namespace Unicorn
{
    public static class FrustumTools
    {
        public static bool TestPlanesAABB(UnsafeReadonlyArray<Plane> frustumPlanes, float3 boundsCenter, float3 boundsExtents)
        {
            for (var i = 0; i < frustumPlanes.Length; i++)
            {
                var plane = frustumPlanes[i];
                var normalSign = math.sign(plane.normal);
                var testPoint = boundsCenter + boundsExtents * normalSign;

                var dot = math.dot(testPoint, plane.normal);
                if (dot + plane.distance < 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}