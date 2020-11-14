using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Rendering;

namespace Uded
{
    public class SnapControl
    {
        // todo: add support for snapping to vert
        // todo: add support for snapping to edge
        // todo: add support for changing grid size and anchor position 
        
        public static Vector3 SnapToGrid(Vector3 input)
        {
            return new Vector3(Mathf.Round(input.x), Mathf.Round(input.y), Mathf.Round(input.z));
        }
    }
    [EditorTool("Add Rect", typeof(UdedCore))]
    public class RectTool : EditorTool
    {
        private int drawingStage;
        private Vector3 firstPoint;
        private Vector3 secondPoint;
        void OnEnable()
        {
            EditorTools.activeToolChanged += ActiveToolDidChange;
            drawingStage = 0;
        }

        void OnDisable()
        {
            EditorTools.activeToolChanged -= ActiveToolDidChange;
        }
        void ActiveToolDidChange()
        {
            if (!EditorTools.IsActiveTool(this))
                return;

        }
        static readonly int kDrawRectModeHash		= "DrawRectMode".GetHashCode();

        public override void OnToolGUI(EditorWindow window)
        {
            var sceneView = window as SceneView;
            if (sceneView == null)
                return;
            var dragArea = sceneView.position;

            var evt = Event.current;
            Ray ray = HandleUtility.GUIPointToWorldRay (Event.current.mousePosition);
            float rayEnter;
            if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out rayEnter))
            {
                var intersectionPoint = ray.GetPoint(rayEnter);
                Handles.DrawSolidDisc(intersectionPoint, Vector3.up, 0.1f);
                var defaultID = GUIUtility.GetControlID(kDrawRectModeHash, FocusType.Keyboard, dragArea);
                HandleUtility.AddDefaultControl(defaultID);
                if (evt.type == EventType.MouseDown)
                {
                    if (drawingStage == 0)
                    {
                        firstPoint = intersectionPoint;
                        drawingStage++;
                    }
                    else if(drawingStage == 1)
                    {
                        secondPoint = intersectionPoint;
                        var bounds = new Bounds(firstPoint, Vector3.zero);
                        bounds.Encapsulate(secondPoint);
                        firstPoint = bounds.min;
                        secondPoint = bounds.max;
                        var uded = target as UdedCore;
                        uded.AddRect(
                            new Vertex(firstPoint.x, firstPoint.z),
                            new Vertex(secondPoint.x, firstPoint.z),
                            new Vertex(secondPoint.x, secondPoint.z),
                            new Vertex(firstPoint.x, secondPoint.z));
                        drawingStage = 0;
                        uded.Rebuild();
                        Undo.RecordObject(uded, "Add Rect");
                    }
                }
            }
            EditorUtility.SetDirty(target);
        }
    }
    
    [EditorTool("Add Line", typeof(UdedCore))]
    public class LineTool : EditorTool
    {
        private List<Vector3> linePoints = new List<Vector3>(); 
        GUIContent m_IconContent;

        [SerializeField]
        Texture2D m_ToolIcon;

        public override GUIContent toolbarIcon => m_IconContent;

        void OnEnable()
        {
            EditorTools.activeToolChanged += ActiveToolDidChange;
            m_IconContent = new GUIContent()
            {
                text = "Platform Tool",
                tooltip = "Platform Tool"
            };
        }

        void OnDisable()
        {
            EditorTools.activeToolChanged -= ActiveToolDidChange;
        }
        void ActiveToolDidChange()
        {
            if (!EditorTools.IsActiveTool(this))
                return;

        }
        static readonly int kDrawLineModeHash		= "DrawLineMode".GetHashCode();

        public override void OnToolGUI(EditorWindow window)
        {
            var sceneView = window as SceneView;
            if (sceneView == null)
                return;
            var dragArea = sceneView.position;
            var evt = Event.current;
            Ray ray = HandleUtility.GUIPointToWorldRay (Event.current.mousePosition);
            float rayEnter;
            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Return)
            {
                SubmitCurrentPoints();
            }
            if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out rayEnter))
            {
                var intersectionPoint = SnapControl.SnapToGrid(ray.GetPoint(rayEnter));
                Handles.DrawSolidDisc(intersectionPoint, Vector3.up, 0.1f);
                var defaultID = GUIUtility.GetControlID(kDrawLineModeHash, FocusType.Keyboard, dragArea);
                HandleUtility.AddDefaultControl(defaultID);
                if (evt.type == EventType.MouseDown && evt.button == 0)
                {
                    if (linePoints.Count == 0)
                    {
                        linePoints = new List<Vector3>();
                        linePoints.Add(intersectionPoint);
                    }
                    else if(intersectionPoint.Equals(linePoints[0]))
                    {
                        // should also submit points if the intersection point is equal to an existing vertex
                        // or lands or a border line
                        SubmitCurrentPoints();
                    }
                    else
                    {
                        linePoints.Add(intersectionPoint);
                    }
                }

                if (evt.type == EventType.Repaint && linePoints.Count > 0)
                {
                    for (int i = 0; i < linePoints.Count-1; i++)
                    {
                        var firstPoint = linePoints[i];
                        var secondPoint = linePoints[i + 1];
                        Handles.DrawLine(firstPoint, secondPoint);
                    }
                    Handles.DrawLine(linePoints[linePoints.Count-1], intersectionPoint);
                }
            }
            EditorUtility.SetDirty(target);
        }

        private void SubmitCurrentPoints()
        {
            if (linePoints.Count < 2)
            {
                linePoints.Clear();
                return;
            }
            var uded = target as UdedCore;
            for (int i = 0; i < linePoints.Count; i++)
            {
                var firstPoint = linePoints[i];
                var secondPoint = linePoints[(i + 1) % linePoints.Count];
                uded.AddLine(
                    new Uded.Vertex(firstPoint.x, firstPoint.z),
                    new Uded.Vertex(secondPoint.x, secondPoint.z));
            }

            linePoints.Clear();
            uded.Rebuild();
            Undo.RecordObject(uded, "Add Line");
        }
    }
}
