using UnityEditor;
using UnityEngine;

namespace Uded
{
    public class SectorHandle
    {
        int sectorHandle = "Free2DMoveHandle".GetHashCode();
        public int controlId { get; private set; }
        private bool selected;
        private bool hovered;
        public float offsetAmount;
        public Vector3 DrawHandle(Vector3 position, float size)
        {
            return DrawHandle(EditorGUIUtility.GetControlID(sectorHandle, FocusType.Keyboard), position, size);
        }

        public Vector3 DrawHandle(int controlId, Vector3 position, float size)
        {
            this.controlId = controlId;
            selected = GUIUtility.hotControl == controlId || GUIUtility.keyboardControl == controlId;
            hovered = HandleUtility.nearestControl == controlId;
            var e = Event.current;
            var offset = Vector3.up*offsetAmount;
            var offsetHandle = position+offset;
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == controlId && e.button == 0)
                    {
                        GUIUtility.hotControl = controlId;
                        GUIUtility.keyboardControl = controlId;
                        e.Use();
                    }
                    break ;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId && (e.button == 0 || e.button == 2))
                    {
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }
                    break ;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != controlId)
                        break;
                    // construct the plane of the drag line
                    var guiWorldRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                    if (Utility.ClosestPointsOnTwoLines(out Vector3 closestPointLine1, out Vector3 closestPointLine2,
                        position, Vector3.up, guiWorldRay.origin,
                        guiWorldRay.direction))
                    {
                        position = closestPointLine1 - offset;
                    }
                    e.Use();
                    break ;
                case EventType.Repaint:
                    Handles.DrawDottedLine(position, offsetHandle, 3);
                    var drawposition = Handles.matrix.MultiplyPoint(offsetHandle);
                    Vector3 vector3_1 = Camera.current.transform.right * size;
                    Vector3 vector3_2 = Camera.current.transform.up * size;
                    var verts = new Vector3[4];
                    var handleSize		= UnityEditor.HandleUtility.GetHandleSize(drawposition)*0.1f;
                    verts[0] = drawposition + (vector3_1 + vector3_2)*handleSize*0.5f;
                    verts[1] = drawposition + (vector3_1 - vector3_2)*handleSize*0.5f;
                    verts[2] = drawposition - (vector3_1 + vector3_2)*handleSize*0.5f;
                    verts[3] = drawposition - (vector3_1 - vector3_2)*handleSize*0.5f;
                    Handles.DrawSolidRectangleWithOutline(verts, Color.white, Color.black);
                    break ;
                case EventType.Layout:
                    if (e.type == EventType.Layout)
                        SceneView.RepaintAll();
                    var distance = HandleUtility.DistanceToRectangle(position, Camera.current.transform.rotation, size);
                    HandleUtility.AddControl(controlId, distance);
                    break ;
            }

            return position;

        }
    }
}