using System.Collections.Generic;
using jbgeo;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(VillageGenerator))]
public class VillageGeneratorEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        VisualElement myInspector = new VisualElement();
        var village = target as VillageGenerator;
        if (village == null)
            return myInspector;
        var seedField = new PropertyField(serializedObject.FindProperty("seed"));
        myInspector.Add(seedField);
        seedField.RegisterValueChangeCallback(evt =>
        {
            village.GenerateVillage();
        });
        myInspector.Add(new PropertyField(serializedObject.FindProperty("splitCount")));
        myInspector.Add(new Button(() =>
        {
            Undo.RecordObject(village, "Generate Village");
            village.GenerateVillage();
            EditorUtility.SetDirty(village);
        })
        {
            text = "Generate"
        });
        myInspector.Add(new PropertyField(serializedObject.FindProperty("showDebug")));
        myInspector.Add(new PropertyField(serializedObject.FindProperty("_halfEdgeMesh")));
        return myInspector;
    }
    void OnSceneGUI()
    {
        // get the chosen game object
        var village = target as VillageGenerator;
        if (village == null || village._halfEdgeMesh == null)
            return;
        if (village.showDebug)
            DisplayDebug(village._halfEdgeMesh);
        else
            SimpleDisplay(village._halfEdgeMesh);
    }
    private static void SimpleDisplay(HalfEdgeMesh halfEdgeMesh)
    {
        HashSet<HalfEdge> displayedEdges = new HashSet<HalfEdge>();
        // display all edges
        int count = 0;
        for (int i = 0; i < halfEdgeMesh.Edges.Count; i++)
        {
            var edge = halfEdgeMesh.Edges[i];
            if (displayedEdges.Contains(edge))
                continue;
            Handles.color = Color.black;
            Handles.DrawLine((Vector3) (Vector3) halfEdgeMesh.EdgeVertex(edge),
                (Vector3) halfEdgeMesh.EdgeVertex(halfEdgeMesh.GetTwin(i)));
            displayedEdges.Add(edge);
            displayedEdges.Add(halfEdgeMesh.GetTwin(i));
        }
    }

    private static void DisplayDebug(HalfEdgeMesh halfEdgeMesh)
    {
        HashSet<HalfEdge> displayedEdges = new HashSet<HalfEdge>();
        // display all edges
        int count = 0;
        for (int i = 0; i < halfEdgeMesh.Edges.Count; i++)
        {
            var edge = halfEdgeMesh.Edges[i];
            if (displayedEdges.Contains(edge))
                continue;
            Handles.color = Color.black;
            // offset to the left
            var forward = (Vector3) halfEdgeMesh.EdgeVertex(halfEdgeMesh.GetTwin(i)) - (Vector3) halfEdgeMesh.EdgeVertex(edge);
            var forwardRot = Quaternion.LookRotation(forward);
            var arrowRot = forwardRot * Quaternion.AngleAxis(190, Vector3.up);
            var center = Vector3.Lerp((Vector3) (Vector3) halfEdgeMesh.EdgeVertex(edge),
                (Vector3) halfEdgeMesh.EdgeVertex(halfEdgeMesh.GetTwin(i)), 0.5f);
            var size = HandleUtility.GetHandleSize(center) * 0.05f;
            Vector3 left = forwardRot * Vector3.left * size;
            Handles.DrawLine((Vector3) (Vector3) halfEdgeMesh.EdgeVertex(edge) + left,
                (Vector3) halfEdgeMesh.EdgeVertex(halfEdgeMesh.GetTwin(i)) + left);
            // arrow displaying orientation
            Handles.DrawLine((Vector3) halfEdgeMesh.EdgeVertex(halfEdgeMesh.GetTwin(i)) + left,
                (Vector3) halfEdgeMesh.EdgeVertex(halfEdgeMesh.GetTwin(i)) + left + arrowRot * Vector3.forward * 0.1f);
            Handles.Label(center + left * 3, "" + count++);
            displayedEdges.Add(edge);
        }
    }

}
