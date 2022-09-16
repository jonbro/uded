using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Uded
{
    [CustomEditor(typeof(UdedCore))]
    public class UdedEditor : Editor
    {
        private (PickingElement element, Material mat) _lastMaterialRollback;
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
        }

        private PickingElement dragElement;
        private bool dragging;
        void OnSceneGUI()
        {
            // Handles.DrawLine(Vector3.zero, Vector3.up*10);
            // var r1 = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            // UdedEditorUtility.PointOnRay(r1, new Ray(Vector3.zero, Vector3.up), out var distance);
            // Handles.DrawSolidDisc(Vector3.up*distance, -r1.origin, 0.1f);
            // HandleUtility.Repaint();
            // return;

            // get the chosen game object
            var uded = target as UdedCore;
            if (uded == null)
                return;
            if (uded.displayDebug)
                DisplayDebug(uded);
            // bail out early if we are using one of the other tools
            if (ToolManager.activeToolType == typeof(LineTool))
            {
                return;
            }
            // highlight the currently hovered wall
            var nearest = UdedEditorUtility.GetNearestLevelElement(uded);
            Event evt = Event.current;
            if (nearest.t is ElementType.vertex)
            {
                Handles.color = Color.green;
                Handles.DrawLine(nearest.worldSpaceA, nearest.worldSpaceB);
                SceneView.RepaintAll();
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
                if (evt.type == EventType.MouseUp && evt.button == 0)
                {
                    DirectManipulation.EndDrag();
                    evt.Use();
                    GUIUtility.hotControl = 0;
                }
                if(evt.type == EventType.MouseDrag)
                {
                    // new mouse point
                    DirectManipulation.UpdateDrag();
                    evt.Use();
                    uded.Rebuild();
                }
            }

            if (ToolManager.activeToolType != typeof(LineTool) && evt.type == EventType.MouseDown && evt.button == 0)
            {
                Undo.RegisterCompleteObjectUndo(uded, "Move " + nearest.t);
                DirectManipulation.StartDrag(uded, nearest);
                evt.Use();
            }
            if (evt.type == EventType.DragExited)
            {
                _lastMaterialRollback.element = default;
            }

            if (DragAndDrop.objectReferences.Length == 1)
            {
                Debug.Log("drag drop event");
                ApplyDragDropMaterial(nearest, uded);
            }
        }

        private void ApplyDragDropMaterial(PickingElement nearest, UdedCore uded)
        {
            Material mat;
            if (DragAndDrop.objectReferences[0] is Material m)
            {
                mat = m;
            }
            else if (DragAndDrop.objectReferences[0] is Texture t)
            {
                if (uded.TextureMats.ContainsKey(t))
                {
                    mat = uded.TextureMats[t];
                }
                else
                {
                    mat = new Material(Shader.Find("Standard"));
                    mat.mainTexture = t;
                    uded.TextureMats[t] = mat;
                }
            }
            else
            {
                return;
            }

            // determine if there is a wall under our cursor
            Vector3 mousePosition = Event.current.mousePosition;
            
            if (!SceneView.currentDrawingSceneView.camera.pixelRect.Contains(HandleUtility.GUIPointToScreenPixelCoordinate(new Vector2(mousePosition.x, mousePosition.y))) || nearest.t == ElementType.none)
            {
                RollbackMaterialChange(uded);
                _lastMaterialRollback.element = new PickingElement();
                uded.Rebuild();
                return;
            }
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            DragAndDrop.AcceptDrag();

            if (nearest.t == ElementType.ceiling)
            {
                // run rollback on the last change
                RollbackMaterialChange(uded);
                _lastMaterialRollback.element = nearest;
                _lastMaterialRollback.mat = uded.Faces[nearest.index].ceilingMat;
                uded.Faces[nearest.index].ceilingMat = mat;
                uded.Rebuild();
            }

            if (nearest.t == ElementType.floor)
            {
                RollbackMaterialChange(uded);
                _lastMaterialRollback.element = nearest;
                _lastMaterialRollback.mat = uded.Faces[nearest.index].floorMat;
                uded.Faces[nearest.index].floorMat = mat;
                uded.Rebuild();
            }

            if (nearest.t == ElementType.wall_mid)
            {
                RollbackMaterialChange(uded);
                _lastMaterialRollback.element = nearest;
                _lastMaterialRollback.mat = uded.Edges[nearest.index].midMat;
                uded.Edges[nearest.index].midMat = mat;
                // todo: add a rebuild that only reconstructs the material array
                uded.Rebuild();
            }

            if (nearest.t == ElementType.wall_upper)
            {
                RollbackMaterialChange(uded);
                _lastMaterialRollback.element = nearest;
                _lastMaterialRollback.mat = uded.Edges[nearest.index].upperMat;
                uded.Edges[nearest.index].upperMat = mat;
                // todo: add a rebuild that only reconstructs the material array
                uded.Rebuild();
            }

            if (nearest.t == ElementType.wall_lower)
            {
                RollbackMaterialChange(uded);
                _lastMaterialRollback.element = nearest;
                _lastMaterialRollback.mat = uded.Edges[nearest.index].lowerMat;
                uded.Edges[nearest.index].lowerMat = mat;
                // todo: add a rebuild that only reconstructs the material array
                uded.Rebuild();
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
            switch (_lastMaterialRollback.element.t)
            {
                case ElementType.ceiling:
                    uded.Faces[_lastMaterialRollback.element.index].ceilingMat = _lastMaterialRollback.mat;
                    break;
                case ElementType.floor:
                    uded.Faces[_lastMaterialRollback.element.index].floorMat = _lastMaterialRollback.mat;
                    break;
                case ElementType.wall_mid:
                    uded.Edges[_lastMaterialRollback.element.index].midMat = _lastMaterialRollback.mat;
                    break;
                case ElementType.wall_upper:
                    uded.Edges[_lastMaterialRollback.element.index].upperMat = _lastMaterialRollback.mat;
                    break;
                case ElementType.wall_lower:
                    uded.Edges[_lastMaterialRollback.element.index].lowerMat = _lastMaterialRollback.mat;
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
