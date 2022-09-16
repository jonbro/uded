using System.Numerics;
using UnityEditor;
using UnityEngine;
using Plane = UnityEngine.Plane;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Uded
{
    static class DirectManipulation
    {
        private static Vector2 StartPositionVert, StartPositionVert2;
        private static float startPositionHeight;
        private static bool s_IsDragging;
        public static PickingElement element;
        private static UdedCore uded;
        private static Vector2 mouseStart;
        public static bool IsDragging => s_IsDragging;
        
        public static void StartDrag(UdedCore _uded, PickingElement _element)
        {
            uded = _uded;
            element = _element;
            if (element.t is ElementType.wall_mid or ElementType.wall_upper or ElementType.wall_lower)
            {
                StartPositionVert = _uded.Vertexes[_uded.Edges[_element.index].vertexIndex]._value;
                StartPositionVert2 = _uded.Vertexes[_uded.GetTwin(_element.index).vertexIndex]._value;
            }
            else if (element.t == ElementType.vertex)
                StartPositionVert = _uded.Vertexes[_uded.Edges[_element.index].vertexIndex]._value;
            else if (element.t == ElementType.floor)
                startPositionHeight = _uded.Faces[element.index].floorHeight;
            else if (element.t == ElementType.ceiling)
                startPositionHeight = _uded.Faces[element.index].ceilingHeight;
            mouseStart = Event.current.mousePosition;
            s_IsDragging = true;
        }

        public static void UpdateDrag()
        {
            if (element.t is ElementType.wall_mid or ElementType.wall_upper or ElementType.wall_lower)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay (Event.current.mousePosition);
                new Plane(Vector3.up, element.PickPoint).Raycast(ray, out float enter);
                var offset = (ray.origin+ray.direction*enter) - element.PickPoint;
                uded.Vertexes[uded.Edges[element.index].vertexIndex]._value =
                    StartPositionVert + new Vector2(offset.x, offset.z);
                uded.Vertexes[uded.GetTwin(element.index).vertexIndex]._value =
                    StartPositionVert2 + new Vector2(offset.x, offset.z);
            }
            else if (element.t == ElementType.vertex)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay (Event.current.mousePosition);
                new Plane(Vector3.up, element.PickPoint).Raycast(ray, out float enter);
                var offset = (ray.origin+ray.direction*enter) - element.PickPoint;
                uded.Vertexes[uded.Edges[element.index].vertexIndex]._value =
                    StartPositionVert + new Vector2(offset.x, offset.z);
            }
            else if (element.t == ElementType.floor)
            {
                uded.Faces[element.index].floorHeight = startPositionHeight + HandleUtility.CalcLineTranslation(mouseStart, Event.current.mousePosition, element.PickPoint,
                    Vector3.up);
            }
            else if (element.t == ElementType.ceiling)
            {
                uded.Faces[element.index].ceilingHeight = startPositionHeight + HandleUtility.CalcLineTranslation(mouseStart, Event.current.mousePosition, element.PickPoint,
                    Vector3.up);
            }
        }

        public static void EndDrag()
        {
            s_IsDragging = false;
        }
    }
}
