using UnityEngine;

namespace Uded
{
    public class Utility
    {
        // from https://wiki.unity3d.com/index.php/3d_Math_functions
        public static bool ClosestPointsOnTwoLines(out Vector3 closestPointLine1, out Vector3 closestPointLine2, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2){
 
            closestPointLine1 = Vector3.zero;
            closestPointLine2 = Vector3.zero;
 
            float a = Vector3.Dot(lineVec1, lineVec1);
            float b = Vector3.Dot(lineVec1, lineVec2);
            float e = Vector3.Dot(lineVec2, lineVec2);
 
            float d = a*e - b*b;
 
            //lines are not parallel
            if(d != 0.0f){
 
                Vector3 r = linePoint1 - linePoint2;
                float c = Vector3.Dot(lineVec1, r);
                float f = Vector3.Dot(lineVec2, r);
 
                float s = (b*f - c*e) / d;
                float t = (a*f - c*b) / d;
 
                closestPointLine1 = linePoint1 + lineVec1 * s;
                closestPointLine2 = linePoint2 + lineVec2 * t;
 
                return true;
            }
            return false;
        }
    }
}