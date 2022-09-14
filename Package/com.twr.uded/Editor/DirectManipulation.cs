using UnityEditor;
using UnityEngine;

namespace Uded
{
    static class DirectManipulation
    {
        private static Vector2 StartPosition;
        private static bool s_IsDragging;
        private static PickingElement element;
        private static UdedCore uded;
        public static bool IsDragging => s_IsDragging;
        
        public static void StartDrag(UdedCore _uded, PickingElement _element)
        {
            uded = _uded;
            element = _element;
            StartPosition = _uded.Vertexes[_uded.Edges[_element.index].vertexIndex]._value;
            s_IsDragging = true;
        }

        public static void UpdateDrag()
        {
            if (element.t == ElementType.vertex)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay (Event.current.mousePosition);
                new Plane(Vector3.up, element.PickPoint).Raycast(ray, out float enter);
                var offset = (ray.origin+ray.direction*enter) - element.PickPoint;
                uded.Vertexes[uded.Edges[element.index].vertexIndex]._value =
                    StartPosition + new Vector2(offset.x, offset.z);
            }
        }

        public static void EndDrag()
        {
            s_IsDragging = false;
        }
    }
}
