﻿using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Uded
{
    [CustomEditor(typeof(UdedCore))]
    public class UdedEditor : Editor
    {
        private int currentFace = -1;
        private int debugVert = -1;
        private int fixLink = -1;

        private (PickingElement element, Material mat) lastMaterialRollback;
        public override void OnInspectorGUI()
        {
            var uded = (UdedCore) target;
            DrawDefaultInspector();
            if (GUILayout.Button("Rebuild"))
            {
                uded.Rebuild();
                EditorUtility.SetDirty(uded);
            }

            if (GUILayout.Button("Clear"))
            {
                uded.Clear();
                EditorUtility.SetDirty(uded);
            }

            debugVert = EditorGUILayout.IntField(debugVert);
            if (GUILayout.Button("DebugVert") && debugVert > 0 && debugVert < uded.Vertexes.Count)
            {
                uded.DebugExitsForVert(debugVert);
            }

            fixLink = EditorGUILayout.IntField(fixLink);
            if (GUILayout.Button("Fix Link") && fixLink > 0 && fixLink < uded.Edges.Count)
            {
                uded.FixLink(fixLink);
            }
        }

        private PickingElement dragElement;
        private bool dragging;
        void OnSceneGUI()
        {
            // get the chosen game object
            var uded = target as UdedCore;
            if (uded == null)
                return;
            if (uded.displayDebug)
                DisplayDebug(uded);
            // highlight the currently hovered wall
            var nearest = UdedEditorUtility.GetNearestLevelElement(uded);
            Event e = Event.current;
            if (nearest.t is ElementType.vertex)
            {
                Handles.color = Color.green;
                var edge = uded.Edges[nearest.index];
                var face = uded.Faces[edge.face];
                var thisVert = uded.EdgeVertex(edge);
                Handles.DrawLine(thisVert+Vector3.up*face.floorHeight, thisVert+Vector3.up*face.ceilingHeight);
            }
            else if (nearest.t is ElementType.wall_lower or ElementType.wall_mid or ElementType.wall_upper)
            {
                Handles.color = Color.green;
                DrawWall(uded, nearest);
            }
            else if(nearest.t is ElementType.ceiling or ElementType.floor)
            {
                var face = uded.Faces[nearest.index];
                for (int j = 0; j < face.Edges.Count; j++)
                {
                    var edgeIndex = face.Edges[j];
                    var edge = uded.Edges[edgeIndex];
                    Handles.color = Color.green;
                    var twin = uded.GetTwin(edgeIndex);
                    var thisVert = uded.EdgeVertex(edge);
                    var nextVert = uded.EdgeVertex(twin);
                    var height = nearest.t == ElementType.ceiling ? face.ceilingHeight : face.floorHeight;
                    Handles.DrawLine(new Vector3(thisVert.x, height, thisVert.y), new Vector3(nextVert.x, height, nextVert.y));
                }
            }
            if (DirectManipulation.IsDragging)
            {
                var controlId = GUIUtility.GetControlID(FocusType.Passive);
                GUIUtility.hotControl = controlId;
                if (e.type == EventType.MouseUp && e.button == 0)
                {
                    DirectManipulation.EndDrag();
                    e.Use();
                    GUIUtility.hotControl = 0;
                }
                if(e.type == EventType.MouseDrag)
                {
                    // new mouse point
                    DirectManipulation.UpdateDrag();
                    e.Use();
                    uded.Rebuild();
                }
            }

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                DirectManipulation.StartDrag(uded, nearest);
                e.Use();
            }
            if (e.type == EventType.DragExited)
            {
                lastMaterialRollback.element = default;
            }

            if (DragAndDrop.objectReferences.Length == 1 &&
                DragAndDrop.objectReferences[0].GetType() == typeof(Material))
            {
                var mat = DragAndDrop.objectReferences[0] as Material;
                // determine if there is a wall under our cursor
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                DragAndDrop.AcceptDrag();
                if (nearest.t == ElementType.none)
                {
                    RollbackMaterialChange(uded);
                    lastMaterialRollback.element = new PickingElement();
                    uded.Rebuild();
                }
                if (nearest.t == ElementType.ceiling)
                {
                    // run rollback on the last change
                    RollbackMaterialChange(uded);
                    lastMaterialRollback.element = nearest;
                    lastMaterialRollback.mat = uded.Faces[nearest.index].ceilingMat;
                    uded.Faces[nearest.index].ceilingMat = mat;
                    uded.Rebuild();
                }
                if (nearest.t == ElementType.floor)
                {
                    RollbackMaterialChange(uded);
                    lastMaterialRollback.element = nearest;
                    lastMaterialRollback.mat = uded.Faces[nearest.index].floorMat;
                    uded.Faces[nearest.index].floorMat = mat;
                    uded.Rebuild();
                }
                if (nearest.t == ElementType.wall_mid)
                {
                    RollbackMaterialChange(uded);
                    lastMaterialRollback.element = nearest;
                    lastMaterialRollback.mat = uded.Edges[nearest.index].midMat;
                    uded.Edges[nearest.index].midMat = mat;
                    // todo: add a rebuild that only reconstructs the material array
                    uded.Rebuild();
                }
                if (nearest.t == ElementType.wall_upper)
                {
                    RollbackMaterialChange(uded);
                    lastMaterialRollback.element = nearest;
                    lastMaterialRollback.mat = uded.Edges[nearest.index].upperMat;
                    uded.Edges[nearest.index].upperMat = mat;
                    // todo: add a rebuild that only reconstructs the material array
                    uded.Rebuild();
                }
                if (nearest.t == ElementType.wall_lower)
                {
                    RollbackMaterialChange(uded);
                    lastMaterialRollback.element = nearest;
                    lastMaterialRollback.mat = uded.Edges[nearest.index].lowerMat;
                    uded.Edges[nearest.index].lowerMat = mat;
                    // todo: add a rebuild that only reconstructs the material array
                    uded.Rebuild();
                }
            }
        }

        private static void DrawWall(UdedCore uded, PickingElement element)
        {
            var face = uded.Faces[uded.Edges[element.index].face];
            var backface = uded.Faces[uded.GetTwin(element.index).face];
            var edgeIndex = element.index;
            // outline the current edge
            var floorPos = face.floorHeight * Vector3.up;
            var ceilPos = face.ceilingHeight * Vector3.up;
            switch (element.t)
            {
                case ElementType.wall_lower:
                    ceilPos = backface.floorHeight * Vector3.up;
                    break;
                case ElementType.wall_upper:
                    floorPos = backface.ceilingHeight * Vector3.up;
                    break;
            }

            Handles.DrawLine(uded.EdgeVertex(edgeIndex) + floorPos, uded.EdgeVertex(uded.GetTwin(edgeIndex)) + floorPos);
            Handles.DrawLine(uded.EdgeVertex(edgeIndex) + ceilPos, uded.EdgeVertex(edgeIndex) + floorPos);
            Handles.DrawLine(uded.EdgeVertex(uded.GetTwin(edgeIndex)) + floorPos,
                uded.EdgeVertex(uded.GetTwin(edgeIndex)) + ceilPos);
            Handles.DrawLine(uded.EdgeVertex(edgeIndex) + ceilPos, uded.EdgeVertex(uded.GetTwin(edgeIndex)) + ceilPos);
            SceneView.RepaintAll();
        }

        private void RollbackMaterialChange(UdedCore uded)
        {
            switch (lastMaterialRollback.element.t)
            {
                case ElementType.ceiling:
                    uded.Faces[lastMaterialRollback.element.index].ceilingMat = lastMaterialRollback.mat;
                    break;
                case ElementType.floor:
                    uded.Faces[lastMaterialRollback.element.index].floorMat = lastMaterialRollback.mat;
                    break;
                case ElementType.wall_mid:
                    uded.Edges[lastMaterialRollback.element.index].midMat = lastMaterialRollback.mat;
                    break;
                case ElementType.wall_upper:
                    uded.Edges[lastMaterialRollback.element.index].upperMat = lastMaterialRollback.mat;
                    break;
                case ElementType.wall_lower:
                    uded.Edges[lastMaterialRollback.element.index].lowerMat = lastMaterialRollback.mat;
                    break;
            }
        }
        private static void DisplayDebug(UdedCore uded)
        {
            HashSet<HalfEdge> displayedEdges = new HashSet<HalfEdge>();
            // display all edges
            int count = 0;
            for (int i = 0; i < uded.Edges.Count; i++)
            {
                var edge = uded.Edges[i];
                if (displayedEdges.Contains(edge))
                    continue;
                Handles.color = Color.black;
                // offset to the left
                var forward = (Vector3) uded.EdgeVertex(uded.GetTwin(i)) - (Vector3) uded.EdgeVertex(edge);
                var forwardRot = Quaternion.LookRotation(forward);
                var arrowRot = forwardRot * Quaternion.AngleAxis(190, Vector3.up);
                var center = Vector3.Lerp((Vector3) (Vector3) uded.EdgeVertex(edge),
                    (Vector3) uded.EdgeVertex(uded.GetTwin(i)), 0.5f);
                var size = HandleUtility.GetHandleSize(center) * 0.05f;
                Vector3 left = forwardRot * Vector3.left * size;
                Handles.DrawLine((Vector3) (Vector3) uded.EdgeVertex(edge) + left,
                    (Vector3) uded.EdgeVertex(uded.GetTwin(i)) + left);
                // arrow displaying orientation
                Handles.DrawLine((Vector3) uded.EdgeVertex(uded.GetTwin(i)) + left,
                    (Vector3) uded.EdgeVertex(uded.GetTwin(i)) + left + arrowRot * Vector3.forward * 0.1f);
                Handles.Label(center + left * 3, "" + count++);
                displayedEdges.Add(edge);
            }

            if (uded.Vertexes.Count > 0)
            {
                count = 0;
                // display all verts
                foreach (var vertex in uded.Vertexes)
                {
                    Handles.color = Color.white;
                    Handles.color = Color.black;
                    Handles.DrawWireDisc((Vector3) vertex, Vector3.up, 0.01f);
                    Handles.Label((Vector3) vertex, "" + count++);
                }

                Handles.Label((Vector3) uded.Vertexes[0], "edges: " + uded.Edges.Count);
            }
        }

        [MenuItem("GameObject/Uded", false, 10)]
        public static void CreateNewUded(MenuCommand menuCommand)
        {
            var go = new GameObject("Uded");
            go.AddComponent<UdedCore>();
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }
    }
}
