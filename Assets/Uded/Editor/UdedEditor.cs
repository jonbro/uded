using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;

namespace Uded
{
    [CustomEditor( typeof( UdedCore ) )]
    public class UdedEditor : Editor
    {
        private int currentFace = -1;
        private int debugVert = -1;
        private int fixLink = -1;
        public override void OnInspectorGUI()
        {
            var uded = (UdedCore)target;
            DrawDefaultInspector ();
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

        static readonly int kFloorHandle		= "FloorHandle".GetHashCode();
        private SectorHandle _sectorHandle = new SectorHandle();
        void OnSceneGUI()
        {
            // get the chosen game object
            var uded = target as UdedCore;
            var handleMoved = false;
            if (currentFace >= 0 && currentFace < uded.Faces.Count)
            {
                var selectedFaceCenter = uded.GetFaceCenter(currentFace);
                var originalPosition = new Vector3(selectedFaceCenter.x, uded.Faces[currentFace].floorHeight, selectedFaceCenter.y);
                Vector3 newTargetPosition = _sectorHandle.DrawHandle(originalPosition, 1.0f);
                if (originalPosition.y != newTargetPosition.y)
                {
                    uded.Faces[currentFace].floorHeight = newTargetPosition.y;
                    uded.BuildFaceMeshes();
                }
                Handles.Label(originalPosition, "face: " + currentFace);
            }

            var e = Event.current;
            if (GUIUtility.hotControl != _sectorHandle.controlId && e.type == EventType.MouseDown && e.button == 0)
            {
                var newFace = -1;
                for (int i = 0; i < uded.Faces.Count; i++)
                {
                    var face = uded.Faces[i];
                    if(face.clockwise)
                        continue;
                    var floorPlane = new Plane(Vector3.up, new Vector3(0,face.floorHeight, 0));
                    Ray ray = HandleUtility.GUIPointToWorldRay (Event.current.mousePosition);
                    var rayHit = floorPlane.Raycast(ray, out var enter);
                    if(rayHit)
                    {
                        var enterPoint = ray.origin+ray.direction*enter;
                        if (uded.PointInFace(new Vector2(enterPoint.x, enterPoint.z), i))
                        {
                            newFace = i;
                        }
                    }
                }
                if (newFace != currentFace)
                {    
                    currentFace = newFace;
                    SceneView.RepaintAll();
                    GUIUtility.hotControl = GUIUtility.GetControlID(kFloorHandle, FocusType.Passive);
                    e.Use();
                }
            }
            
            if (uded.displayEdges)
            {
                for (int i = 0; i < uded.Faces.Count; i++)
                {
                    var face = uded.Faces[i];
                    if (face.clockwise)
                        continue;
                    for (int j = 0; j < face.Edges.Count; j++)
                    {
                        var edgeIndex = face.Edges[j];
                        var edge = uded.Edges[edgeIndex];
                        if (edge.face != currentFace)
                            continue;
                        var twin = uded.GetTwin(edgeIndex);
                        Handles.color = Color.yellow;
                        var thisVert = uded.EdgeVertex(edge);
                        var nextVert = uded.EdgeVertex(twin);
                        Handles.DrawLine(new Vector3(thisVert.x, uded.Faces[edge.face].floorHeight, thisVert.y), new Vector3(nextVert.x, uded.Faces[edge.face].floorHeight, nextVert.y));
                    }
                }
            }
            
            if (!uded.displayDebug)
                return;
            HashSet<HalfEdge> displayedEdges = new HashSet<HalfEdge>();
            // display all edges
            int count = 0;
            for (int i = 0; i < uded.Edges.Count; i++)
            {
                var edge = uded.Edges[i];
                if(displayedEdges.Contains(edge))
                    continue;
                Handles.color = Color.black;
                // offset to the left
                var forward = (Vector3)uded.EdgeVertex(uded.GetTwin(i)) - (Vector3)uded.EdgeVertex(edge);
                var forwardRot = Quaternion.LookRotation(forward);
                var arrowRot = forwardRot * Quaternion.AngleAxis(190, Vector3.up);
                Vector3 left = forwardRot * Vector3.left * 0.02f;
                Handles.DrawLine((Vector3)(Vector3)uded.EdgeVertex(edge)+left,(Vector3)uded.EdgeVertex(uded.GetTwin(i))+left);
                var center = Vector3.Lerp((Vector3) (Vector3)uded.EdgeVertex(edge)+left, (Vector3)uded.EdgeVertex(uded.GetTwin(i))+left, 0.5f);
                // arrow displaying orientation
                Handles.DrawLine((Vector3)uded.EdgeVertex(uded.GetTwin(i))+left, (Vector3)uded.EdgeVertex(uded.GetTwin(i))+left+arrowRot*Vector3.forward*0.1f);
                Handles.Label(center, ""+count++);
                displayedEdges.Add(edge);
            }

            if (uded.Vertexes.Count > 0)
            {
                count = 0;
                // display all verts
                foreach (var vertex in uded.Vertexes)
                {
                    Handles.color = Color.white;
                    Handles.DrawSolidDisc((Vector3)vertex, Vector3.up, 0.01f);
                    Handles.color = Color.black;
                    Handles.DrawWireDisc((Vector3)vertex, Vector3.up, 0.01f);
                    Handles.Label((Vector3)vertex, ""+count++);
                }
                Handles.Label((Vector3)uded.Vertexes[0], "edges: " + uded.Edges.Count);
            }    
        }
    }
    
}
