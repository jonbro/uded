using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Uded
{
    [CustomEditor( typeof( UdedCore ) )]
    public class UdedEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var uded = (UdedCore)target;
            DrawDefaultInspector ();
            if (GUILayout.Button("Rebuild"))
            {
                uded.Rebuild();
                EditorUtility.SetDirty(uded);
            }
            if (GUILayout.Button("Set Ids"))
            {
                uded.SetIds();
                EditorUtility.SetDirty(uded);
            }
            if (GUILayout.Button("Clear"))
            {
                uded.Clear();
                EditorUtility.SetDirty(uded);
            }
            
        }

        void OnSceneGUI()
        {
            // get the chosen game object
            var uded = target as UdedCore;
            for (int i = 0; i < uded.Edges.Count; i+=2)
            {
                var edge = uded.Edges[i];
                var twin = uded.Edges[i + 1];
                Handles.DrawLine(uded.EdgeVertex(edge), uded.EdgeVertex(twin));
            }
            if (!uded.displayDebug)
                return;
            HashSet<HalfEdge> displayedEdges = new HashSet<HalfEdge>();
            // display all edges
            int count = 0;
            foreach (var edge in uded.Edges)
            {
                if(displayedEdges.Contains(edge))
                    continue;
                Handles.color = Color.black;
                // offset to the left
                var forward = (Vector3)uded.EdgeVertex(edge.nextId) - (Vector3)uded.EdgeVertex(edge);
                var forwardRot = Quaternion.LookRotation(forward);
                var arrowRot = forwardRot * Quaternion.AngleAxis(190, Vector3.up);
                Vector3 left = forwardRot * Vector3.left * 0.02f;
                Handles.DrawLine((Vector3)(Vector3)uded.EdgeVertex(edge)+left, (Vector3)uded.EdgeVertex(edge.nextId)+left);
                var center = Vector3.Lerp((Vector3) (Vector3)uded.EdgeVertex(edge)+left, (Vector3) uded.EdgeVertex(edge.nextId)+left, 0.5f);
                // arrow displaying orientation
                Handles.DrawLine((Vector3)uded.EdgeVertex(edge.nextId)+left, (Vector3)uded.EdgeVertex(edge.nextId)+left+arrowRot*Vector3.forward*0.1f);
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
