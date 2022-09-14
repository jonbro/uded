using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Uded
{

    [EditorTool("Add Line", typeof(UdedCore))]
    public class LineTool : EditorTool
    {
        private List<Vector3> linePoints = new List<Vector3>();
        GUIContent m_IconContent;

        [SerializeField] Texture2D m_ToolIcon;

        public override GUIContent toolbarIcon => m_IconContent;
        private float pickingPlaneHeight = 0;
        void OnEnable()
        {
            ToolManager.activeToolChanged += ActiveToolDidChange;
            m_IconContent = new GUIContent()
            {
                text = "Add Line",
                tooltip = "Add Line",
                image = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Packages/com.twr.uded/Editor/Editor Resources/LineTool.png")
            };
        }

        void OnDisable()
        {
            ToolManager.activeToolChanged -= ActiveToolDidChange;
        }

        void ActiveToolDidChange()
        {
            if (!ToolManager.IsActiveTool(this))
                return;
        }

        public override void OnToolGUI(EditorWindow window)
        {
            var sceneView = window as SceneView;
            if (sceneView == null)
                return;
            var dragArea = sceneView.position;
            var evt = Event.current;
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            float rayEnter;
            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Return)
            {
                SubmitCurrentPoints();
            }

            var uded = target as UdedCore;
            if (UdedEditorUtility.GetNearestFace(uded, out var res))
            {
                var face = uded.Faces[res.index];
                if (res.t == ElementType.ceiling)
                {
                    pickingPlaneHeight = face.ceilingHeight;
                }
                else if (res.t == ElementType.floor)
                {
                    pickingPlaneHeight = face.floorHeight;
                }
            }
            if (new Plane(Vector3.up, Vector3.up*pickingPlaneHeight).Raycast(ray, out rayEnter))
            {
                var intersectionPoint = ray.GetPoint(rayEnter); // SnapControl.SnapToGrid(ray.GetPoint(rayEnter));
                // snap to nearby verts that are in the current point list
                foreach (var linePoint in linePoints)
                {
                    float snapSize = HandleUtility.GetHandleSize(intersectionPoint) * 0.08f;
                    if (Vector3.Distance(intersectionPoint, linePoint) < snapSize)
                    {
                        intersectionPoint = linePoint;
                    }
                }
                Handles.DrawSolidDisc(intersectionPoint, Vector3.up, 0.1f);
                var defaultID = GUIUtility.GetControlID(FocusType.Keyboard, dragArea);
                HandleUtility.AddDefaultControl(defaultID);
                if (evt.type == EventType.MouseDown && evt.button == 0)
                {
                    if (linePoints.Count == 0)
                    {
                        linePoints = new List<Vector3>();
                        linePoints.Add(intersectionPoint);
                    }
                    else if (intersectionPoint.Equals(linePoints[0]))
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
                    for (int i = 0; i < linePoints.Count - 1; i++)
                    {
                        var firstPoint = linePoints[i];
                        var secondPoint = linePoints[i + 1];
                        Handles.DrawLine(firstPoint, secondPoint);
                    }

                    Handles.DrawLine(linePoints[linePoints.Count - 1], intersectionPoint);
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
            Undo.RegisterCompleteObjectUndo(uded, "Add Lines");
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
        }
    }
}