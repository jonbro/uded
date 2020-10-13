﻿using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor( typeof( Uded ) )]
public class UdedEditor : Editor
{
    void OnSceneGUI()
    {
        // get the chosen game object
        var uded = target as Uded;
        HashSet<Uded.HalfEdge> displayedEdges = new HashSet<Uded.HalfEdge>();
        // display all edges
        foreach (var edge in uded.Edges)
        {
            if(displayedEdges.Contains(edge))
                continue;
            Handles.color = Color.black;
            // offset to the left
            var forward = (Vector3) edge.next.origin - (Vector3) edge.origin;
            var forwardRot = Quaternion.LookRotation(forward);
            var arrowRot = forwardRot * Quaternion.AngleAxis(190, Vector3.up);
            Vector3 left = forwardRot * Vector3.left * 0.02f;
            Handles.DrawLine((Vector3)edge.origin+left, (Vector3)edge.next.origin+left);
            // arrow displaying orientation
            Handles.DrawLine((Vector3)edge.next.origin+left, (Vector3)edge.next.origin+left+arrowRot*Vector3.forward*0.1f);

            displayedEdges.Add(edge);
        }
        // display all verts
        foreach (var vertex in uded.Vertexes)
        {
            Handles.color = Color.white;
            Handles.DrawSolidDisc((Vector3)vertex, Vector3.up, 0.01f);
            Handles.color = Color.black;
            Handles.DrawWireDisc((Vector3)vertex, Vector3.up, 0.01f);
        }  
        Handles.Label((Vector3)uded.Vertexes[0], "edges: " + uded.Edges.Count);
    }
}