using System;
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
        public enum ElementType
        {
            wall_mid,
            wall_lower,
            wall_upper,
            vertex,
            floor,
            ceiling,
            none
        }

        private (ElementType t, Material mat, int index) lastMaterialRollback = new()
        {
            t = ElementType.none
        };
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

        private Vector3 startPosition;
        private bool dragging;
        private int edgeDragIndex;
        private Vector2 startVertPosition;
        void OnSceneGUI()
        {
            // get the chosen game object
            var uded = target as UdedCore;
            if (uded == null)
                return;
            if (uded.displayDebug)
                DisplayDebug(uded);
            // highlight the currently hovered wall
            var nearest = GetNearestLevelElement(uded);
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
            if (dragging)
            {
                if (e.type == EventType.MouseUp && e.button == 0)
                {
                    dragging = false;
                }
                var controlId = GUIUtility.GetControlID(FocusType.Passive);
                GUIUtility.hotControl = controlId;
                if(e.type == EventType.MouseDrag)
                {
                    // new mouse point
                    Ray ray = HandleUtility.GUIPointToWorldRay (Event.current.mousePosition);
                    new Plane(Vector3.up, startPosition).Raycast(ray, out float enter);
                    var offset = (ray.origin+ray.direction*enter) - startPosition;
                    uded.Vertexes[uded.Edges[edgeDragIndex].vertexIndex]._value =
                        startVertPosition + new Vector2(offset.x, offset.z);
                    e.Use();
                    uded.Rebuild();
                }
            }

            // direct manipulation of vertexes
            if (nearest.t == ElementType.vertex)
            {
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    edgeDragIndex = nearest.index;
                    dragging = true;
                    startVertPosition = uded.Vertexes[uded.Edges[edgeDragIndex].vertexIndex]._value;
                    e.Use();
                }
            }
            if (e.type == EventType.DragExited)
            {
                lastMaterialRollback.t = ElementType.none;
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
                    lastMaterialRollback.t = ElementType.none;
                    uded.Rebuild();
                }
                if (nearest.t == ElementType.ceiling)
                {
                    // run rollback on the last change
                    RollbackMaterialChange(uded);
                    lastMaterialRollback.index = nearest.index;
                    lastMaterialRollback.t = ElementType.ceiling;
                    lastMaterialRollback.mat = uded.Faces[nearest.index].ceilingMat;
                    uded.Faces[nearest.index].ceilingMat = mat;
                    // todo: add a rebuild that only reconstructs the material array
                    uded.Rebuild();
                }
                if (nearest.t == ElementType.floor)
                {
                    RollbackMaterialChange(uded);
                    lastMaterialRollback.index = nearest.index;
                    lastMaterialRollback.t = ElementType.floor;
                    lastMaterialRollback.mat = uded.Faces[nearest.index].floorMat;
                    uded.Faces[nearest.index].floorMat = mat;
                    // todo: add a rebuild that only reconstructs the material array
                    uded.Rebuild();
                }
                if (nearest.t == ElementType.wall_mid)
                {
                    RollbackMaterialChange(uded);
                    lastMaterialRollback.index = nearest.index;
                    lastMaterialRollback.t = ElementType.wall_mid;
                    lastMaterialRollback.mat = uded.Edges[nearest.index].midMat;
                    uded.Edges[nearest.index].midMat = mat;
                    // todo: add a rebuild that only reconstructs the material array
                    uded.Rebuild();
                }
                if (nearest.t == ElementType.wall_upper)
                {
                    RollbackMaterialChange(uded);
                    lastMaterialRollback.index = nearest.index;
                    lastMaterialRollback.t = ElementType.wall_upper;
                    lastMaterialRollback.mat = uded.Edges[nearest.index].upperMat;
                    uded.Edges[nearest.index].upperMat = mat;
                    // todo: add a rebuild that only reconstructs the material array
                    uded.Rebuild();
                }
                if (nearest.t == ElementType.wall_lower)
                {
                    RollbackMaterialChange(uded);
                    lastMaterialRollback.index = nearest.index;
                    lastMaterialRollback.t = ElementType.wall_lower;
                    lastMaterialRollback.mat = uded.Edges[nearest.index].lowerMat;
                    uded.Edges[nearest.index].lowerMat = mat;
                    // todo: add a rebuild that only reconstructs the material array
                    uded.Rebuild();
                }
            }
        }

        private static void DrawWall(UdedCore uded, (ElementType t, int index) nearest)
        {
            var face = uded.Faces[uded.Edges[nearest.index].face];
            var backface = uded.Faces[uded.GetTwin(nearest.index).face];
            var edgeIndex = nearest.index;
            // outline the current edge
            var floorPos = face.floorHeight * Vector3.up;
            var ceilPos = face.ceilingHeight * Vector3.up;
            switch (nearest.t)
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
            switch (lastMaterialRollback.t)
            {
                case ElementType.ceiling:
                    uded.Faces[lastMaterialRollback.index].ceilingMat = lastMaterialRollback.mat;
                    break;
                case ElementType.floor:
                    uded.Faces[lastMaterialRollback.index].floorMat = lastMaterialRollback.mat;
                    break;
                case ElementType.wall_mid:
                    uded.Edges[lastMaterialRollback.index].midMat = lastMaterialRollback.mat;
                    break;
                case ElementType.wall_upper:
                    uded.Edges[lastMaterialRollback.index].upperMat = lastMaterialRollback.mat;
                    break;
                case ElementType.wall_lower:
                    uded.Edges[lastMaterialRollback.index].lowerMat = lastMaterialRollback.mat;
                    break;
            }
        }
        public (ElementType t, int index) GetNearestLevelElement(UdedCore uded)
        {
            var interiors = new Dictionary<int, int>();
            for (int i = 0; i < uded.Faces.Count; i++)
            {
                var fExterior = uded.Faces[i];
                foreach (var interior in fExterior.InteriorFaces)
                {
                    interiors[interior] = i;
                }
            }

            Ray ray = HandleUtility.GUIPointToWorldRay (Event.current.mousePosition);
            var res = (t:ElementType.none, index:-1, distance:0.0);
            for (int i = 0; i < uded.Edges.Count; i++)
            {
                var edge = uded.Edges[i];
                var face = uded.Faces[edge.face];
                var backfaceIndex = uded.GetTwin(i).face;
                var backface = uded.Faces[backfaceIndex];
                if (interiors.ContainsKey(backfaceIndex))
                {
                    backface = uded.Faces[interiors[backfaceIndex]];
                }
                var forward = (Vector3) uded.EdgeVertex(uded.GetTwin(i)) - (Vector3) uded.EdgeVertex(edge);
                var forwardRot = Quaternion.LookRotation(forward);
                var center = Vector3.Lerp(uded.EdgeVertex(edge), uded.EdgeVertex(edge.nextId), 0.5f);
                var left = forwardRot * Vector3.left;
                var wallPlane = new Plane(left, center);
                // var floorPlane = new Plane(Vector3.up, new Vector3(0,face.floorHeight, 0));
                if(Vector3.Dot(left, ray.direction)<0 && wallPlane.Raycast(ray, out var enter))
                {
                    var enterPoint = ray.origin+ray.direction*enter;
                    // flatten the ray to 2d
                    var r = new Ray2D(new Vector2(ray.origin.x, ray.origin.z),
                        new Vector2(ray.direction.x, ray.direction.z));
                    if (enterPoint.y > face.ceilingHeight || enterPoint.y < face.floorHeight)
                    {
                        continue;
                    }
                    var seg = ElementType.wall_mid;
                    if (!backface.clockwise)
                    {
                        if (backface.floorHeight > face.floorHeight && enterPoint.y < backface.floorHeight
                            || backface.floorHeight > face.floorHeight && enterPoint.y < backface.floorHeight)
                        {
                            seg = ElementType.wall_lower;
                        }
                        else if (backface.ceilingHeight < face.ceilingHeight && enterPoint.y > backface.ceilingHeight
                            || backface.ceilingHeight > face.ceilingHeight && enterPoint.y > backface.ceilingHeight)
                        {
                            seg = ElementType.wall_upper;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    var intersectionDistance = UdedCore.RayLineIntersection(r, uded.EdgeVertex(edge), uded.EdgeVertex(uded.GetTwin(i)));
                    if (intersectionDistance != null)
                    {
                        // bias towards grabbing verts
                        var vertGrabDisance = HandleUtility.GetHandleSize(enterPoint)*0.12f;
                        var vertADistance = (uded.EdgeVertex(edge) - (r.origin + r.direction * intersectionDistance)).Value.magnitude;
                        if (vertADistance < vertGrabDisance && res.t == ElementType.none || vertADistance < res.distance)
                        {
                            startPosition = enterPoint;
                            res.t = ElementType.vertex;
                            res.index = i;
                            res.distance = vertADistance;
                        }
                        else if (res.t == ElementType.none || enter < res.distance)
                        {
                            res.t = seg;
                            res.index = i;
                            res.distance = enter;
                        }
                    }
                }
            }
            // test the floors / ceilings
            for (int i = 0; i < uded.Faces.Count; i++)
            {
                var face = uded.Faces[i];
                if(face.clockwise)
                    continue;
                // test floor
                var floorPlane = new Plane(Vector3.up, new Vector3(0,face.floorHeight, 0));
                if(Vector3.Dot(floorPlane.normal, ray.direction) < 0 && floorPlane.Raycast(ray, out var enter))
                {
                    var enterPoint = ray.origin+ray.direction*enter;
                    var enterPoint2d = new Vector2(enterPoint.x, enterPoint.z);
                    bool isInInterior = false;
                    foreach (var interiorFaceIndex in face.InteriorFaces)
                    {
                        // need to get the flipped face so the point in face works correctly
                        if (uded.PointInFace(enterPoint2d, interiorFaceIndex))
                        {
                            isInInterior = true;
                            break;
                        }
                    }
                    if (!isInInterior && uded.PointInFace(enterPoint2d, i))
                    {
                        if(enter < res.distance || res.t == ElementType.none)
                        {
                            res.t = ElementType.floor;
                            res.index = i;
                            res.distance = enter;
                        }
                    }
                }
                // test ceiling
                var ceilingPlane = new Plane(-Vector3.up, new Vector3(0,face.ceilingHeight, 0));
                ray = HandleUtility.GUIPointToWorldRay (Event.current.mousePosition);
                if(Vector3.Dot(ceilingPlane.normal, ray.direction) < 0 && ceilingPlane.Raycast(ray, out enter))
                {
                    var enterPoint = ray.origin+ray.direction*enter;
                    var enterPoint2d = new Vector2(enterPoint.x, enterPoint.z);
                    bool isInInterior = false;
                    foreach (var interiorFaceIndex in face.InteriorFaces)
                    {
                        // need to get the flipped face so the point in face works correctly
                        if (uded.PointInFace(enterPoint2d, interiorFaceIndex))
                        {
                            isInInterior = true;
                            break;
                        }
                    }
                    if (!isInInterior && uded.PointInFace(enterPoint2d, i))
                    {
                        if(enter < res.distance || res.t == ElementType.none)
                        {
                            res.t = ElementType.ceiling;
                            res.index = i;
                            res.distance = enter;
                        }
                    }
                }
            }
            return (res.t, res.index);
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
