using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;

namespace Uded
{
    [CustomEditor(typeof(UdedCore))]
    public class UdedEditor : Editor
    {
        private int currentFace = -1;
        private int debugVert = -1;
        private int fixLink = -1;

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
        enum WallSegment
        {
            lower,
            mid,
            upper
        }

        void OnSceneGUI()
        {
            // get the chosen game object
            var uded = target as UdedCore;

            if (uded.displayDebug)
                DisplayDebug(uded);
            // highlight the currently hovered wall
            Event e = Event.current;
            if (e.type == EventType.Repaint || e.type == EventType.Layout)
            {
                var nearest = GetNearestLevelElement(uded);
                if (nearest.t is ElementType.wall_lower or ElementType.wall_mid or ElementType.wall_upper)
                {
                    var face = uded.Faces[uded.Edges[nearest.index].face];
                    var backface = uded.Faces[uded.GetTwin(nearest.index).face];
                    var edgeIndex = nearest.index;
                    // outline the current edge
                    Handles.color = Color.green;
                    var floorPos = face.floorHeight*Vector3.up;
                    var ceilPos = face.ceilingHeight*Vector3.up;

                    switch (nearest.t)
                    {
                        case ElementType.wall_lower:
                            ceilPos = backface.floorHeight*Vector3.up;
                            break;
                        case ElementType.wall_upper:
                            floorPos = backface.ceilingHeight*Vector3.up;
                            break;
                    }
                    Handles.DrawLine(uded.EdgeVertex(edgeIndex)+floorPos,uded.EdgeVertex(uded.GetTwin(edgeIndex))+floorPos);
                    Handles.DrawLine(uded.EdgeVertex(edgeIndex)+ceilPos,uded.EdgeVertex(edgeIndex)+floorPos);
                    Handles.DrawLine(uded.EdgeVertex(uded.GetTwin(edgeIndex))+floorPos,uded.EdgeVertex(uded.GetTwin(edgeIndex))+ceilPos);
                    Handles.DrawLine(uded.EdgeVertex(edgeIndex)+ceilPos,uded.EdgeVertex(uded.GetTwin(edgeIndex))+ceilPos);
                    SceneView.RepaintAll();
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
            }


            if (DragAndDrop.objectReferences.Length == 1 &&
                DragAndDrop.objectReferences[0].GetType() == typeof(Material))
            {
                var mat = DragAndDrop.objectReferences[0] as Material;
                // determine if there is a wall under our cursor
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                DragAndDrop.AcceptDrag();
                // just use raycast checks against the mesh collider
                Ray mouseRay = HandleUtility.GUIPointToWorldRay (Event.current.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(mouseRay, out hit))
                {
                    uded.ApplyMaterialToFace(hit.collider.gameObject, mat);
                }
            }
        }

        public enum ElementType
        {
            wall_mid,
            wall_lower,
            wall_upper,
            floor,
            ceiling,
            none
        }
    
        public (ElementType t, int index) GetNearestLevelElement(UdedCore uded)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay (Event.current.mousePosition);
            var res = (t:ElementType.none, index:-1, distance:0.0);
            for (int i = 0; i < uded.Edges.Count; i++)
            {
                var edge = uded.Edges[i];
                var face = uded.Faces[edge.face];
                if(face.clockwise)
                    continue;
                var forward = (Vector3) uded.EdgeVertex(uded.GetTwin(i)) - (Vector3) uded.EdgeVertex(edge);
                var forwardRot = Quaternion.LookRotation(forward);
                var center = Vector3.Lerp(uded.EdgeVertex(edge), uded.EdgeVertex(edge.nextId), 0.5f);
                var left = forwardRot * Vector3.left;
                var wallPlane = new Plane(left, center);
                // var floorPlane = new Plane(Vector3.up, new Vector3(0,face.floorHeight, 0));
                if(Vector3.Dot(ray.direction, left)>0)
                    continue;
                var rayHit = wallPlane.Raycast(ray, out var enter);
                if(rayHit)
                {
                    var enterPoint = ray.origin+ray.direction*enter;
                    // flatten the ray to 2d
                    var r = new Ray2D(new Vector2(ray.origin.x, ray.origin.z),
                        new Vector2(ray.direction.x, ray.direction.z));
                    // if the back face is clockwise, then that means it is a center wall (i.e. the backface is an empty sector)
                    var backface = uded.Faces[uded.GetTwin(i).face];
                    var seg = ElementType.wall_mid;
                    if (enterPoint.y > face.ceilingHeight || enterPoint.y < face.floorHeight)
                    {
                        continue;
                    }

                    if (!backface.clockwise)
                    {
                        if (enterPoint.y < backface.floorHeight)
                        {
                            seg = ElementType.wall_lower;
                        }
                        else if (enterPoint.y > backface.ceilingHeight)
                        {
                            seg = ElementType.wall_upper;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    if (UdedCore.RayLineIntersection(r, uded.EdgeVertex(edge), uded.EdgeVertex(uded.GetTwin(i))) !=
                        null)
                    {
                        if (res.t == ElementType.none || enter < res.distance)
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
                var rayHit = floorPlane.Raycast(ray, out var enter);
                if(rayHit)
                {
                    var enterPoint = ray.origin+ray.direction*enter;
                    if (uded.PointInFace(new Vector2(enterPoint.x, enterPoint.z), i))
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
                rayHit = ceilingPlane.Raycast(ray, out enter);
                if(rayHit)
                {
                    var enterPoint = ray.origin+ray.direction*enter;
                    if (uded.PointInFace(new Vector2(enterPoint.x, enterPoint.z), i))
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
                    // Handles.DrawSolidDisc((Vector3) vertex, Vector3.up, 0.01f);
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
