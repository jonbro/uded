using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Uded : MonoBehaviour
{
    /// <summary>
    /// wrapper class for vector2 so we can store refs
    /// </summary>
    public class Vertex
    {
        private Vector2 _value;
        public static implicit operator Vector2(Vertex v) => v._value;
        public static implicit operator Vector3(Vertex v) => new Vector3(v._value.x, 0, v._value.y);
        public Vertex(Vector2 v)
        {
            this._value = v;
        }
        public static implicit operator Vertex(Vector2 value)
        {
            return new Vertex(value);
        }

        public bool Equals(Vertex b)
        {
            return this._value == b._value;
        }
    }
    public List<Vertex> Vertexes;
    public List<HalfEdge> Edges;

    public class HalfEdge
    {
        public Vertex origin;
        public HalfEdge twin;
        public HalfEdge next;
    }
    /// <summary>
    /// test function
    /// </summary>
    private void OnEnable()
    {
        Vertexes = new List<Vertex>();
        Edges = new List<HalfEdge>();
        AddLine (0, 0, 2, 0);
        AddLine (2, 0, 2, 2);
        AddLine (2, 2, 0, 2);
        AddLine (0, 2, 0, 0);
    }

    private Vertex AddOrFindVert(Vertex newVert)
    {
        // detemine if these verts already exist
        Vertex currentVert = null;
        for (int i = 0; i < Vertexes.Count; i++)
        {
            var vert = Vertexes[i];
            if (vert.Equals(newVert))
            {
                return vert;
            }
        }
        Vertexes.Add(newVert);
        return newVert;
    }

    private HalfEdge FindEdgeWithOrigin(Vertex origin)
    {
        foreach (var edge in Edges)
        {
            // will need to sort by winding when we have more than one edge per origin, but this will work for now
            if (edge.origin.Equals(origin))
                return edge;
        }
        return null;
    }
    private void AddLine(float ax, float ay, float bx, float by)
    {
        Vertex a = new Vector2(ax, ay);
        Vertex b = new Vector2(bx, by);
        if (a.Equals(b))
        {
            return;
        }
        a = AddOrFindVert(a);
        b = AddOrFindVert(b);
        // create halfedges
        var aOriginEdge = FindEdgeWithOrigin(a);
        var bOriginEdge = FindEdgeWithOrigin(b);
        HalfEdge btoa = new HalfEdge();
        HalfEdge atob = new HalfEdge
        {
            origin = a,
            next = bOriginEdge??btoa,
            twin = btoa
        };
        btoa.origin = b;
        btoa.twin = atob;
        btoa.next = aOriginEdge??atob;
        Edges.Add(atob);
        Edges.Add(btoa);
    }
}
