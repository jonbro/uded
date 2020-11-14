/*
 * todo
 * - nothing is serialized :/
 * - undo support
 * - grid snapping
 * - walls
 * - sector height control
 * - switch half edge to use indexes
 * - switch 
 * - faces are sectors (materials & heights)
 */
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphs;
using UnityEngine;

namespace Uded
{
    [ExecuteInEditMode]
    [Serializable]
    
    public class UdedCore : MonoBehaviour
    {
        public Material DefaultMat;
        public List<Vertex> Vertexes = new List<Vertex>();
        public List<HalfEdge> Edges = new List<HalfEdge>();
        public List<Face> Faces = new List<Face>();
        public bool displayDebug;
        public List<GameObject> childObjects = new List<GameObject>();

        public void OnEnable()
        {
            Rebuild();
        }

        public void Clear()
        {
            Vertexes = new List<Vertex>();
            Edges = new List<HalfEdge>();
            Faces = new List<Face>();
            foreach (var childObject in childObjects)
            {
                DestroyImmediate(childObject);
            }
        }

        public void SetIds()
        {
            foreach (var edge in Edges)
            {
            }
        }

        private HalfEdge GetTwin(int edgeIndex)
        {
            return Edges[GetTwinIndex(edgeIndex)];
        }

        private int GetTwinIndex(int edgeIndex)
        {
            return edgeIndex % 2 == 0 ? edgeIndex + 1 : edgeIndex - 1;
        }
        public void Rebuild()
        {
            foreach (var childObject in childObjects)
            {
                DestroyImmediate(childObject);
            }
            childObjects.Clear();
            Faces.Clear();
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
                    nextEdge = Edges[nextEdge.nextId];
                    var nextVert = EdgeVertex(nextEdge);
                    var lastVert = EdgeVertex(last);
                    var firstVert = EdgeVertex(firstEdge);
                    if (nextEdge != null)
                        sideSum += (nextVert.x - lastVert.x) * (nextVert.y + lastVert.y);
                    if (!unvisitedEdges.Contains(nextEdge))
                    {
                        if (firstEdge == nextEdge)
                        {
                            sideSum += (firstVert.x - nextVert.x) * (firstVert.y + nextVert.y);
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
            

            for (int testingFaceIndex = 0; testingFaceIndex < Faces.Count; testingFaceIndex++)
            {
                var face = Faces[testingFaceIndex];
                // if this is an outward facing edge
                if (face.clockwise)
                {
                    var containingFaces = new Dictionary<int, ValueTuple<int, int>>();
                    // determine if this face is contained within another poly
                    var testEdge = Edges[face.Edges[0]];
                    Ray2D testRay = new Ray2D(EdgeVertex(testEdge), Vector2.right);
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
                            if (GetTwin(exteriorFaceEdgeIndex).face == testingFaceIndex)
                                break;
                            if (RayLineIntersection(testRay, EdgeVertex(edge), EdgeVertex(edge.nextId)) != null)
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

                    if (containingFace >= 0)
                    {
                        Faces[containingFace].InteriorFaces.Add(testingFaceIndex);
                    }
                }
            }
            for (int i = 0; i < Faces.Count; i++)
            {
                if (Faces[i].clockwise || Faces[i].Edges.Count < 3)
                {
                    continue;
                }
                var go = new GameObject("face " + i);
                go.transform.SetParent(transform);
                go.AddComponent<MeshFilter>().sharedMesh = PolyToMesh.GetMeshFromFace(i, this, Edges, Faces);
                go.AddComponent<MeshRenderer>().sharedMaterials = new[] {DefaultMat, DefaultMat};
                childObjects.Add(go);
            }

        }

        private int AddOrFindVert(Vertex newVert)
        {
            // detemine if these verts already exist
            for (int i = 0; i < Vertexes.Count; i++)
            {
                var vert = Vertexes[i];
                if (vert.Equals(newVert))
                {
                    return i;
                }
            }

            Vertexes.Add(newVert);
            return Vertexes.Count-1;
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

        public static bool LineLineIntersection(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 intersectionPoint)
        {
            // init out var
            intersectionPoint = Vector2.zero;
            float p0_x = a1.x;
            float p0_y = a1.y;
            float p1_x = a2.x;
            float p1_y = a2.y;

            float p2_x = b1.x;
            float p2_y = b1.y;
            float p3_x = b2.x;
            float p3_y = b2.y;
            // ignore intersections at the endpoints
            if (
                a1.Equals(b1) ||
                a1.Equals(b2) ||
                a2.Equals(b1) ||
                a2.Equals(b2))
                return false;
            float s1_x, s1_y, s2_x, s2_y;
            s1_x = p1_x - p0_x;     s1_y = p1_y - p0_y;
            s2_x = p3_x - p2_x;     s2_y = p3_y - p2_y;

            float s, t;
            s = (-s1_y * (p0_x - p2_x) + s1_x * (p0_y - p2_y)) / (-s2_x * s1_y + s1_x * s2_y);
            t = ( s2_x * (p0_y - p2_y) - s2_y * (p0_x - p2_x)) / (-s2_x * s1_y + s1_x * s2_y);

            if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
            {
                // Collision detected
                intersectionPoint = Vector2.Lerp(a1, a2, t);
                return true;
            }
            return false;
        }
        private static float AngleBetweenTwoVectors(Vector2 mid, Vector2 left, Vector2 right)
        {
            left = (left - mid).normalized;
            right = (right - mid).normalized;
            if (left.Equals(right))
                return Mathf.PI;
            var angle = Mathf.Atan2(left.y, left.x) - Mathf.Atan2(-right.y, -right.x);
            if (angle > Mathf.PI)
            {
                angle -= 2 * Mathf.PI;
            }
            if (angle - Mathf.Epsilon*2.0f <= -Mathf.PI)
            {
                angle += 2 * Mathf.PI;
            }

            return angle;
        }

        private bool FixLink(int edgeIndex)
        {
            HalfEdge incoming = Edges[edgeIndex];
            int res = -1;
            HalfEdge twin = GetTwin(edgeIndex);
            // SetIds();
            float minimumAngle = 100000;
            for (int i = 0; i < Edges.Count; i++)
            {
                var edge = Edges[i];
                if (edge != incoming && edge.vertexIndex == twin.vertexIndex)
                {
                    float newAngle =
                        AngleBetweenTwoVectors((Vector2) EdgeVertex(twin), (Vector2) EdgeVertex(incoming), (Vector2) EdgeVertex(edge.nextId));
                    if (newAngle < minimumAngle)
                    {
                        minimumAngle = newAngle;
                        res = i;
                    }
                }
            }
            
            if (res >=0)
            {
                incoming.nextId = res;
                Edges[res].prevId = edgeIndex;
                return true;
            }

            return false;
        }

        public void AddRect(Vertex a, Vertex b, Vertex c, Vertex d)
        {
            AddLine(a, b);
            AddLine(b, c);
            AddLine(c, d);
            AddLine(d, a);
        }

        private void SplitEdge(int edgeIndex, Vertex splitPoint)
        {
            HalfEdge edge = Edges[edgeIndex];
            var c = AddOrFindVert(splitPoint);
            var ctoa = GetTwin(edgeIndex);
            var b = ctoa.vertexIndex;
            HalfEdge ctob = new HalfEdge
            {
                vertexIndex = c,
                prevId = edgeIndex,
                nextId = Edges.Count+1
            };
            edge.nextId = Edges.Count;
            var btoc = new HalfEdge
            {
                vertexIndex = b,
                nextId = GetTwinIndex(edgeIndex),
                prevId = ctoa.prevId
            };

            ctoa.prevId = Edges.Count+1;
            ctoa.vertexIndex = c;
            Edges.Add(ctob);
            Edges.Add(btoc);
        }

        public Vertex EdgeVertex(HalfEdge e)
        {
            return Vertexes[e.vertexIndex];
        }
        public Vertex EdgeVertex(int edgeId)
        {
            return Vertexes[Edges[edgeId].vertexIndex];
        }
        public void AddLine(Vertex a, Vertex b)
        {
            AddLineInternal(a, b);
            for (int i = 0; i < Edges.Count; i++)
            {
                FixLink(i);
            }
        }

        private void AddLineInternal(Vertex a, Vertex b, int edgeSearchOffset = 0)
        {
            if (a.Equals(b))
            {
                return;
            }
            // test to see if this line causes any existing lines to be split
            // only do half of the edges
            for (int i = edgeSearchOffset; i < Edges.Count; i+=2)
            {
                var edge = Edges[i];
                Vector2 intersectionPoint;
                if (LineLineIntersection(a, b, EdgeVertex(edge), EdgeVertex(edge.nextId), out intersectionPoint))
                {
                    SplitEdge(i, intersectionPoint);
                    AddLineInternal(a, intersectionPoint, edgeSearchOffset+2);
                    AddLineInternal(intersectionPoint, b, edgeSearchOffset+2);
                    return;
                }
            }
            var vertexIndexA = AddOrFindVert(a);
            var vertexIndexB = AddOrFindVert(b);
            HalfEdge atob = new HalfEdge
            {
                vertexIndex = vertexIndexA,
                nextId = Edges.Count+1,
                prevId = Edges.Count+1
            };
            var btoa = new HalfEdge()
            {
                vertexIndex = vertexIndexB,
                nextId = Edges.Count,
                prevId = Edges.Count
            };
            Edges.Add(atob);
            Edges.Add(btoa);
        }

        private void AddLine(float ax, float ay, float bx, float by)
        {
            Vertex a = new Vector2(ax, ay);
            Vertex b = new Vector2(bx, by);
            AddLine(a,b);
        }
    }
}