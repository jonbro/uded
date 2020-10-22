/*
 * todo
 * - faces inside faces
 * - edge splitting
 * - switch to indexes
 * - organize code to be usable
 */
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class Uded : MonoBehaviour
{
    public Material DefaultMat;
    [Serializable]
    public class Face
    {
        public List<int> Edges = new List<int>();
        public bool clockwise;
    }

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
            _value = v;
        }

        public Vertex(float x, float y)
        {
            _value = new Vector2(x, y);
        }
        public static implicit operator Vertex(Vector2 value)
        {
            return new Vertex(value);
        }

        public bool Equals(Vertex b)
        {
            return _value == b._value;
        }

        public float x
        {
            get => _value.x;
            set => _value.x = value;
        }

        public float y
        {
            get => _value.y;
            set => _value.y = value;
        }
    }

    public List<Vertex> Vertexes;
    public List<HalfEdge> Edges;
    public List<Face> Faces;

    [Serializable]
    public class HalfEdge
    {
        public Vertex origin;
        public HalfEdge twin;
        public HalfEdge next;
        public HalfEdge prev;
        public int face;
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
        Faces = new List<Face>();

        AddRect(new Vertex(-1, -1), new Vertex(3,-1), new Vertex(3,3), new Vertex(-1,3));
        AddRect(new Vertex(0,0), new Vertex(2,0), new Vertex(2,2), new Vertex(0,2));

        // add another face

        // find how many sectors we have
        HashSet<HalfEdge> unvisitedEdges = new HashSet<HalfEdge>(Edges);
        // just build a lookup table for edges
        var edgeLookup = new Dictionary<HalfEdge, int>();
        for (int i = 0; i < Edges.Count; i++)
        {
            edgeLookup[Edges[i]] = i;
        }
        int faceCount = 0;
        while (unvisitedEdges.Count > 0)
        {
            var nextEdge = unvisitedEdges.ElementAt(0);
            var firstEdge = nextEdge;
            unvisitedEdges.Remove(nextEdge);
            var face = new Face();
            Faces.Add(face);
            var sideSum = 0f;
            while (nextEdge != null)
            {
                face.Edges.Add(edgeLookup[nextEdge]);
                var last = nextEdge;
                nextEdge = nextEdge.next;
                if (nextEdge != null)
                    sideSum += (nextEdge.origin.x - last.origin.x) * (nextEdge.origin.y + last.origin.y);
                if (!unvisitedEdges.Contains(nextEdge))
                {
                    if (firstEdge == nextEdge)
                    {
                        sideSum += (firstEdge.origin.x - nextEdge.origin.x) * (firstEdge.origin.y + nextEdge.origin.y);
                        face.clockwise = sideSum > 0;
                        faceCount++;
                    }

                    nextEdge = null;
                    continue;
                }
                unvisitedEdges.Remove(nextEdge);
            }
        }

        // assign faces to edges
        for (int i = 0; i < Faces.Count; i++)
        {
            for (int j = 0; j < Faces[i].Edges.Count; j++)
            {
                Edges[Faces[i].Edges[j]].face = i;
            }            
        }
        
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

        for (int testingFaceIndex = 0; testingFaceIndex < Faces.Count; testingFaceIndex++)
        {
            var face = Faces[testingFaceIndex];
            // if this is an outward facing edge
            if (face.clockwise)
            {
                var containingFaces = new Dictionary<int, ValueTuple<int, int>>();
                // determine if this face is contained within another poly
                var testEdge = Edges[face.Edges[0]];
                Ray2D testRay = new Ray2D(testEdge.origin, Vector2.right);
                int facePriority = 0;
                int count = -1;
                for (int j = 0; j < Faces.Count; j++)
                {
                    var faceExterior = Faces[j];
                    count++;
                    if(face == faceExterior || faceExterior.clockwise)
                        continue;
                    for (int exteriorFaceEdgeIndex = 0; exteriorFaceEdgeIndex < faceExterior.Edges.Count; exteriorFaceEdgeIndex++)
                    {
                        var edge = Edges[exteriorFaceEdgeIndex];
                        if (edge.twin.face == testingFaceIndex)
                            break;
                        if (RayLineIntersection(testRay, edge.origin, edge.next.origin) != null)
                        {
                            if (!containingFaces.ContainsKey(edge.face))
                            {
                                containingFaces[edge.face] = new ValueTuple<int, int>(1, facePriority++);
                            }
                            else
                            {
                                var lastValue = containingFaces[edge.face];
                                containingFaces[edge.face] = new ValueTuple<int, int>(lastValue.Item1++, lastValue.Item2);
                            }
                        }
                    }
                }
                // get the containing face with an odd number of crossings and the lowest priority value
                var lowestValue = 0;
                int containingFace = -1;
                foreach (var potentialContainer in containingFaces)
                {
                    if (potentialContainer.Value.Item1 % 2 == 0)
                    {
                        continue;
                    }
                    if (containingFace < 0 || lowestValue > potentialContainer.Value.Item2)
                    {
                        containingFace = potentialContainer.Key;
                        lowestValue = potentialContainer.Value.Item2;
                    }
                }
                Debug.Log(testingFaceIndex + " is contained by : " + containingFace);
                continue;
            }
                
            var go = new GameObject();
            go.AddComponent<MeshFilter>().sharedMesh = PolyToMesh.GetMeshFromFace(face, Edges);
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] {DefaultMat, DefaultMat};
        }

    }

    private Vertex AddOrFindVert(Vertex newVert)
    {
        // detemine if these verts already exist
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

    public static float Vector2Cross(Vector2 a, Vector2 b)
    {
        return Vector3.Cross(new Vector3(a.x, a.y, 0), new Vector3(b.x, b.y, 0)).z;
    }
    public static float? RayLineIntersection(Ray2D ray, Vector2 a, Vector2 b)
    {
        var v1 = ray.origin - a;
        var v2 = b - a;
        var v3 = new Vector2(-ray.direction.y, ray.direction.x);


        var dot = Vector2.Dot(v2, v3);
        if (Mathf.Abs(dot) < 0.000001)
            return null;

        var t1 = Vector2Cross(v2, v1) / dot;
        var t2 = Vector2.Dot(v1, v3) / dot;

        if (t1 >= 0.0 && (t2 >= 0.0f && t2 <= 1.0f))
            return t1;

        return null;
    }
    private static float AngleBetweenTwoVectors(Vector2 mid, Vector2 left, Vector2 right)
    {
        left = (left - mid).normalized;
        right = (right - mid).normalized;
        var angle = Mathf.Atan2(left.y, left.x) - Mathf.Atan2(-right.y, -right.x);
        if (angle > Mathf.PI)
        {
            angle -= 2 * Mathf.PI;
        }
        else if (angle <= -Mathf.PI)
        {
            angle += 2 * Mathf.PI;
        }

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
                break;
            }

            id++;
        }

        id = 0;
        foreach (var edge in Edges)
        {
            if (edge != incoming && edge.origin.Equals(target))
            {
                float newAngle =
                    AngleBetweenTwoVectors((Vector2) target, (Vector2) incoming.origin, (Vector2) edge.next.origin);
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
            incoming.next = res;
            if (res.prev != null && res.prev != incoming)
            {
                var nextFixup = res.prev;
                res.prev = null;
                FixLink(res.origin, nextFixup);
            }

            res.prev = incoming;
            return true;
        }

        return false;
    }

    private void AddRect(Vertex a, Vertex b, Vertex c, Vertex d)
    {
        AddLine(a, b);
        AddLine(b, c);
        AddLine(c, d);
        AddLine(d, a);
    }

    private void AddLine(Vertex a, Vertex b)
    {
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
    private void AddLine(float ax, float ay, float bx, float by)
    {
        Vertex a = new Vector2(ax, ay);
        Vertex b = new Vector2(bx, by);
        AddLine(a,b);
    }
}
