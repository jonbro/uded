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

        void OnSceneGUI()
        {
            // get the chosen game object
            var uded = target as UdedCore;

            if (!uded.displayDebug)
                return;
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
                Vector3 left = forwardRot * Vector3.left * 0.02f;
                Handles.DrawLine((Vector3) (Vector3) uded.EdgeVertex(edge) + left,
                    (Vector3) uded.EdgeVertex(uded.GetTwin(i)) + left);
                var center = Vector3.Lerp((Vector3) (Vector3) uded.EdgeVertex(edge) + left,
                    (Vector3) uded.EdgeVertex(uded.GetTwin(i)) + left, 0.5f);
                // arrow displaying orientation
                Handles.DrawLine((Vector3) uded.EdgeVertex(uded.GetTwin(i)) + left,
                    (Vector3) uded.EdgeVertex(uded.GetTwin(i)) + left + arrowRot * Vector3.forward * 0.1f);
                Handles.Label(center, "" + count++);
                displayedEdges.Add(edge);
            }

            if (uded.Vertexes.Count > 0)
            {
                count = 0;
                // display all verts
                foreach (var vertex in uded.Vertexes)
                {
                    Handles.color = Color.white;
                    Handles.DrawSolidDisc((Vector3) vertex, Vector3.up, 0.01f);
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
