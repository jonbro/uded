﻿using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;

namespace Uded
{
    [EditorTool("Add Line", typeof(UdedCore))]
    public class LineTool : EditorTool
    {
        private List<Vector3> linePoints = new();
        GUIContent m_IconContent;

        [SerializeField] Texture2D m_ToolIcon;

        public override GUIContent toolbarIcon => m_IconContent;
        private float pickingPlaneHeight = 0;
        public override bool gridSnapEnabled => true;

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
            linePoints.Clear();
        }
        
        [Shortcut("Uded/Line Tool", typeof(SceneView), KeyCode.A)]
        static public void StartLineTool()
        {
            if (Selection.objects[0] is GameObject go && go.GetComponent<UdedCore>())
            {
                ToolManager.SetActiveTool(typeof(LineTool));
            }
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
            var worldSpaceEdges = UdedEditorUtility.GetWorldSpaceEdges(uded);
            var worldSpaceVertexLines = UdedEditorUtility.GetWorldSpaceVertexLines(uded);
            foreach (var edge in worldSpaceVertexLines)
            {
                Vector3 offset = Random.insideUnitSphere*0.01f;
                offset.y = 0;
                Handles.DrawLine(edge.Item2+offset, edge.Item3+offset);
            }

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
                bool foundSnap = false;
                // snap to nearby verts that are in the current point list
                foreach (var linePoint in linePoints)
                {
                    float snapSize = HandleUtility.GetHandleSize(intersectionPoint) * 0.1f;
                    if (Vector3.Distance(intersectionPoint, linePoint) < snapSize)
                    {
                        intersectionPoint = linePoint;
                        foundSnap = true;
                    }
                }
                // snap to nearby worldspace edges
                foreach (var edge in worldSpaceEdges)
                {
                    var edgeRay = new Ray(edge.Item1, (edge.Item2 - edge.Item1).normalized);
                    if (UdedEditorUtility.PointOnRay(ray, edgeRay, out var distanceOnRay1, out var distanceOnRay2))
                    {
                        var distanceClamped = Mathf.Clamp(distanceOnRay2, 0, Vector3.Distance(edge.Item1, edge.Item2));
                        var pointOnLine = edgeRay.GetPoint(distanceClamped);
                        float snapSize = HandleUtility.GetHandleSize(pointOnLine) * 0.1f;
                        if (Vector3.Distance(pointOnLine, ray.GetPoint(distanceOnRay1)) < snapSize)
                        {
                            Handles.DrawLine(edge.Item1, edge.Item2);
                            intersectionPoint = pointOnLine;
                            foundSnap = true;
                            break;
                        }
                    }
                }

                if (EditorSnapSettings.gridSnapActive && !foundSnap)
                {
                    intersectionPoint = Snapping.Snap(ray.GetPoint(rayEnter), EditorSnapSettings.gridSize);
                }
                Handles.DrawWireDisc(intersectionPoint, Vector3.up, HandleUtility.GetHandleSize(intersectionPoint)*0.1f);
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
                    for (int i = 0; i < linePoints.Count; i++)
                    {
                        Handles.DrawWireDisc(linePoints[i], Vector3.up, HandleUtility.GetHandleSize(linePoints[i])*0.1f);
                    }
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