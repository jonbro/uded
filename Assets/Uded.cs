using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class Uded : MonoBehaviour
{
    [Serializable]
    /// <summary>
    /// wrapper class for vector2 so we can store refs
    /// </summary>
    public class Vertex
    {
        public Vector2 _value;
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
    
    [Serializable]
    public class HalfEdge
    {
        public Vertex origin;
        public HalfEdge twin;
        public HalfEdge next;
        public HalfEdge prev;
        public int twinId;
        public int nextId;
        public int prevId;
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
        // find how many sectors we have
        HashSet<HalfEdge> unvisitedEdges = new HashSet<HalfEdge>(Edges);
        int faceCount = 0;
        int breakcount = 100;
        while (unvisitedEdges.Count > 0 && breakcount > 0)
        {
            breakcount--;
            var nextEdge = unvisitedEdges.ElementAt(0);
            var firstEdge = nextEdge;
            unvisitedEdges.Remove(nextEdge);
            while (nextEdge != null)
            {
                nextEdge = nextEdge.next;
                if (!unvisitedEdges.Contains(nextEdge))
                {
                    Debug.Log("used up edges");
                    if (firstEdge == nextEdge)
                        faceCount++;
                    nextEdge = null;
                    continue;
                }
                unvisitedEdges.Remove(nextEdge);
            }
        }
        //angle = Mathf.Atan2(vector2.y, vector2.x) - atan2(vector1.y, vector1.x);
        Debug.Log("facecount: " + faceCount + " edgecount: " + Edges.Count);
        foreach (var edge in Edges)
        {
            int i = 0;
            foreach (var edge2 in Edges)
            {
                if (edge.next == edge2)
                    edge.nextId = i;
                if (edge.prev == edge2)
                    edge.prevId = i;
                if (edge.twin == edge2)
                    edge.twinId = i;
                i++;
            }
        }
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

    private float AngleBetweenTwoVectors(Vector2 mid, Vector2 left, Vector2 right)
    {
        Debug.Log(mid + " " + left + " " + right);
        left = (left - mid).normalized;
        right = (right - mid).normalized;
        var angle = Mathf.Atan2(left.y, left.x) - Mathf.Atan2(-right.y, -right.x);
        // if (angle < 0) { angle += 2 * Mathf.PI; }
        if (angle > Mathf.PI)        { angle -= 2 * Mathf.PI; }
        else if (angle <= -Mathf.PI) { angle += 2 * Mathf.PI; }
        return angle;
    }
    private bool FixLink(Vertex target, HalfEdge incoming)
    {
        HalfEdge res = null;
        float minimumAngle = 100000;
        int id = 0;
        int foundId = 0;
        int incomingId = 0;
        foreach (var edge in Edges)
        {
            if (edge == incoming)
            {
                incomingId = id;
                Debug.Log("found incoming");
                break;
            }
            id++;
        }
        Debug.Log("searching from: " + incomingId);
        id = 0;
        foreach (var edge in Edges)
        {
            if (edge != incoming && edge.origin.Equals(target))
            {
                Debug.Log("checking: " + id + " " + incomingId);
                float newAngle =
                    AngleBetweenTwoVectors((Vector2) target, (Vector2)incoming.origin, (Vector2)edge.next.origin);
                if (newAngle < minimumAngle)
                {
                    minimumAngle = newAngle;
                    res = edge;
                    foundId = id;
                }
            }
            id++;
        }

        if (res != null)
        {
            Debug.Log("foundConnectionFrom " + incomingId + " to " + foundId + " minimumAngle " + minimumAngle);
            incoming.next = res;
            if (res.prev != null && res.prev != incoming)
            {
                var nextFixup = res.prev;
                res.prev = null;
                Debug.Log("fixing up internal");
                FixLink(res.origin, nextFixup);
            }
            res.prev = incoming;
            return true;
        }
        Debug.Log("connection not found");
        return false;
    }
    private void AddLine(float ax, float ay, float bx, float by)
    {
        Debug.Log("adding line");
        Vertex a = new Vector2(ax, ay);
        Vertex b = new Vector2(bx, by);
        if (a.Equals(b))
        {
            return;
        }
        a = AddOrFindVert(a);
        b = AddOrFindVert(b);
        HalfEdge atob = new HalfEdge
        {
            origin = a,
        };
        atob.prev = atob.next = atob.twin = new HalfEdge()
        {
            origin = b
        };
        atob.twin.next = atob;
        atob.twin.prev = atob;
        atob.twin.twin = atob;
        Edges.Add(atob);
        Edges.Add(atob.twin);
        FixLink(b, atob);
        FixLink(a, atob.twin);
    }
}
