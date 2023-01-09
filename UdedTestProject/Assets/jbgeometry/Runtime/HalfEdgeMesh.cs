using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace jbgeo
{
    [Serializable]
    public class HalfEdgeMesh
    {
        public List<Vertex> Vertexes = new();
        public List<HalfEdge> Edges = new();
        public List<Face> Faces = new();
        public void Clear()
        {
            Vertexes = new List<Vertex>();
            Edges = new List<HalfEdge>();
            Faces = new List<Face>();
        }

        public HalfEdge GetTwin(int edgeIndex)
        {
            return Edges[GetTwinIndex(edgeIndex)];
        }
        // output all the edge indexes that have their origin at a vert
        public void DebugExitsForVert(int vert)
        {
            List<int> edges = new List<int>();
            for (int i = 0; i < Edges.Count; i++)
            {
                if (Edges[i].vertexIndex == vert)
                {
                    edges.Add(i);
                }
            }
            Debug.Log(string.Join(",", edges));
        }
        public int GetTwinIndex(int edgeIndex)
        {
            return edgeIndex % 2 == 0 ? edgeIndex + 1 : edgeIndex - 1;
        }

        public float EdgeLength(int edgeIndex)
        {
            return Vector2.Distance(Vertexes[Edges[edgeIndex].vertexIndex],
                Vertexes[Edges[GetTwinIndex(edgeIndex)].vertexIndex]);
        }
        public void DrawEdge(int edgeIndex, Color color, float duration = 0)
        {
            Debug.DrawLine(Vertexes[Edges[edgeIndex].vertexIndex], Vertexes[GetTwin(edgeIndex).vertexIndex], color, duration);
        }

        public void Rebuild()
        {
            var newFaces = new List<Face>();
            // find how many sectors we have
            HashSet<HalfEdge> visitedEdges = new HashSet<HalfEdge>();
            // just build a lookup table for edges
            var edgeLookup = new Dictionary<HalfEdge, int>();
            for (int i = 0; i < Edges.Count; i++)
            {
                edgeLookup[Edges[i]] = i;
            }

            int faceCount = 0;
            bool borrowedFaceValues = false;
            for (int i = 0; i < Edges.Count; i++)
            {
                var nextEdge = Edges[i];
                if (visitedEdges.Contains(nextEdge) || nextEdge.face >= 0)
                {
                    continue;
                }

                visitedEdges.Add(nextEdge);
                var firstEdge = nextEdge;
                var face = new Face();
                newFaces.Add(face);
                var sideSum = 0f;
                while (nextEdge != null)
                {
                    face.Edges.Add(edgeLookup[nextEdge]);
                    var last = nextEdge;
                    int currentIndex = nextEdge.nextId;
                    if(currentIndex < 0 || currentIndex >= Edges.Count)
                        Debug.Log("what" + currentIndex + " " + edgeLookup[nextEdge]);
                    nextEdge = Edges[currentIndex];
                    if (nextEdge.face != -1)
                    {
                        // clean up the faces whose edges we are stealing
                        Faces[nextEdge.face].Edges.Remove(currentIndex);
                        if (!borrowedFaceValues)
                        {
                            //TODO: this should do a better job of borrowing values
                            // face.CopyFaceValues(Faces[nextEdge.face]);
                            borrowedFaceValues = true;
                        }
                    }
                    var nextVert = EdgeVertex(nextEdge);
                    var lastVert = EdgeVertex(last);
                    var firstVert = EdgeVertex(firstEdge);
                    sideSum += (nextVert.x - lastVert.x) * (nextVert.y + lastVert.y);
                    if (visitedEdges.Contains(nextEdge))
                    {
                        if (firstEdge == nextEdge)
                        {
                            sideSum += (firstVert.x - nextVert.x) * (firstVert.y + nextVert.y);
                            face.clockwise = sideSum > 0;
                            faceCount++;
                            // if we made it to the end of face without borrowing anything, check to see if any of the faces
                            // have a backface we can borrow from
                            if (!borrowedFaceValues)
                            {
                                foreach (var faceEdge in face.Edges)
                                {
                                    if (GetTwin(faceEdge).face != -1)
                                    {
                                        // face.CopyFaceValues(Faces[GetTwin(faceEdge).face]);
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // this seems like it would cause issues? Like whatever face this is being produced by this
                            // edge group isn't a full face
                            foreach (var edgeIndex in face.Edges)
                            {
                                DrawEdge(edgeIndex, Color.red, 2.0f);
                            }
                            throw new System.InvalidOperationException("Invalid face found: " + faceCount);
                        }
                        nextEdge = null;
                        continue;
                    }
                    visitedEdges.Add(nextEdge);
                }
            }

            // Faces = newFaces;
            // assign faces to edges
            for (int i = 0; i < newFaces.Count; i++)
            {
                for (int j = 0; j < newFaces[i].Edges.Count; j++)
                {
                    Edges[newFaces[i].Edges[j]].face = i+Faces.Count;
                }
            }

            int newFaceStart = Faces.Count;
            // append the new faces to the existing faces
            foreach (var face in newFaces)
            {
                Faces.Add(face);
            }

            var facesToTestIfInteriors = new HashSet<int>();
            for (int testingFaceIndex = newFaceStart; testingFaceIndex < Faces.Count; testingFaceIndex++)
            {
                facesToTestIfInteriors.Add(testingFaceIndex);
            }

            // if any faces lost all their edges, but have interiors, add their former interiors to the test list
            // these faces should be cleaned up and everything should be reindexed... but thats more work. lets waste memory.
            foreach (var face in Faces)
            {
                if (face.Edges.Count == 0)
                {
                    foreach (var interiorFace in face.InteriorFaces)
                    {
                        if(Faces[interiorFace].Edges.Count != 0)
                            facesToTestIfInteriors.Add(interiorFace);
                    }
                    face.InteriorFaces.Clear();
                }
            }

            foreach (var testingFaceIndex in facesToTestIfInteriors)
            {
                var face = Faces[testingFaceIndex];
                // if this is an outward facing edge
                if (face.clockwise)
                {
                    var containingFaces = new Dictionary<int, ValueTuple<int, float>>();
                    // determine if this face is contained within another poly
                    var testEdge = Edges[face.Edges[0]];
                    Ray2D testRay = new Ray2D(EdgeVertex(testEdge), Vector2.right);
                    int facePriority = 0;
                    int count = -1;
                    for (int j = 0; j < Faces.Count; j++)
                    {
                        var faceExterior = Faces[j];
                        count++;
                        if (testingFaceIndex == j || faceExterior.clockwise)
                            continue;
                        for (int exteriorFaceEdgeIndex = 0;
                            exteriorFaceEdgeIndex < faceExterior.Edges.Count;
                            exteriorFaceEdgeIndex++)
                        {
                            var edge = Edges[faceExterior.Edges[exteriorFaceEdgeIndex]];
                            if (GetTwin(faceExterior.Edges[exteriorFaceEdgeIndex]).face == testingFaceIndex)
                                break;
                            if (RayLineIntersection(testRay, EdgeVertex(edge), EdgeVertex(edge.nextId), out var distance))
                            {
                                if (!containingFaces.ContainsKey(edge.face))
                                {
                                    containingFaces[edge.face] = new ValueTuple<int, float>(1, distance);
                                }
                                else
                                {
                                    var lastValue = containingFaces[edge.face];
                                    containingFaces[edge.face] =
                                        new ValueTuple<int, float>(++lastValue.Item1, distance<lastValue.Item2?distance:lastValue.Item2);
                                }
                            }
                        }
                    }

                    // get the containing face with an odd number of crossings and the lowest priority value
                    float lowestValue = 0;
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
        }

        private Dictionary<GameObject, int> GOtoFace = new();

        public Vector2 GetFaceCenter(int faceIndex)
        {
            Vector2 center = EdgeVertex(Edges[Faces[faceIndex].Edges[0]]);
            var edgeCount = Faces[faceIndex].Edges.Count;
            if(edgeCount == 0)
                return Vector2.zero;
            for (int i = 1; i < edgeCount; i++)
            {
                var edgeIndex = Faces[faceIndex].Edges[i];
                var edge = Edges[edgeIndex];
                center += EdgeVertex(edgeIndex);
            }
            return center * (1.0f / edgeCount);
        }

        public bool PointInFace(Vector2 point, int faceIndex)
        {
            Ray2D testRay = new Ray2D(point, Vector2.right);
            int cross = 0;
            for (int i = 0; i < Faces[faceIndex].Edges.Count; i++)
            {
                var edgeIndex = Faces[faceIndex].Edges[i];
                var edge = Edges[edgeIndex];
                if (RayLineIntersection(testRay, EdgeVertex(edge), EdgeVertex(GetTwin(edgeIndex)), out _))
                {
                    cross++;
                }
            }
            var pointInFace = cross % 2 == 1;
            return pointInFace;
        }

        private int AddOrFindVert(Vertex newVert)
        {
            // determine if these verts already exist
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

        public bool FindNearestRayIntersectionEdge(Ray2D ray, out int edgeId, out Vector2 hitPoint)
        {
            bool hit = false;
            float distance = float.PositiveInfinity;
            edgeId = -1;
            hitPoint = Vector2.zero;
            for (int i = 0;i<Edges.Count;i++)
            {
                var edge = Edges[i];
                if(Faces[edge.face].clockwise)
                    continue;
                if (RayLineIntersection(ray, EdgeVertex(edge), EdgeVertex(edge.nextId), out var hitDistance))
                {
                    if (hitDistance > float.Epsilon && hitDistance < distance)
                    {
                        hitPoint = ray.origin + ray.direction * hitDistance;
                        hit = true;
                        distance = hitDistance;
                        edgeId = i;
                    }
                }
            }
            return hit;
        }
        public static float Vector2Cross(Vector2 a, Vector2 b)
        {
            return Vector3.Cross(new Vector3(a.x, a.y, 0), new Vector3(b.x, b.y, 0)).z;
        }
        public static bool RayLineIntersection(Ray2D ray, Vector2 a, Vector2 b, out float distance)
        {
            distance = 0;
            var v1 = ray.origin - a;
            var v2 = b - a;
            var v3 = new Vector2(-ray.direction.y, ray.direction.x);


            var dot = Vector2.Dot(v2, v3);
            if (Mathf.Abs(dot) < 0.000001)
                return false;

            var t1 = Vector2Cross(v2, v1) / dot;
            var t2 = Vector2.Dot(v1, v3) / dot;

            if (t1 >= 0.0 && (t2 >= 0.0f && t2 <= 1.0f))
            {
                distance = t1;
                return true;
            };

            return false;
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

        public bool FixLink(int edgeIndex, int depth = 0)
        {
            if (depth > 1000)
            {
                Debug.Log("fixing link: " +edgeIndex);
                Debug.Log("seems likely something has gone wrong");
                return false;
            }

            // do not use nextId while in this function, because it might be temporarily invalid
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
                        AngleBetweenTwoVectors((Vector2) EdgeVertex(twin), (Vector2) EdgeVertex(incoming), (Vector2) EdgeVertex(GetTwin(i)));
                    if (newAngle < minimumAngle)
                    {
                        minimumAngle = newAngle;
                        res = i;
                    }
                }
            }

            int initialPrev = Edges[res].prevId;
            // set this to invalid so we don't use it by mistake
            Edges[initialPrev].nextId = -1;

            if (res >=0)
            {
                incoming.nextId = res;
                Edges[res].prevId = edgeIndex;
                // if this fixup caused a previous link to break, go fix that one
                if (edgeIndex != initialPrev)
                {
                    FixLink(initialPrev, depth +1);
                }
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

        public void SplitEdge(int edgeIndex, Vertex splitPoint)
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
            // for (int i = 0; i < Edges.Count; i++)
            // {
            //     if (Edges[i].nextId == -1)
            //     {
            //         for (int j = 0; j < Faces[Edges[i].face].Edges.Count; j++)
            //         {
            //             var siblingEdge = Faces[Edges[i].face].Edges[j];
            //             Debug.Log("sibling edges: " + siblingEdge + " p: " + Edges[siblingEdge].prevId + " n: " + Edges[siblingEdge].nextId);
            //         }
            //     }
            // }
        }

        private void AddLineInternal(Vertex a, Vertex b, int edgeSearchOffset = 0)
        {
            // if it is a zero length line
            if (a.Equals(b))
            {
                return;
            }
            // test to see if this line already exists
            for (int i = edgeSearchOffset; i < Edges.Count; i += 2)
            {
                var existingA = Vertexes[Edges[i].vertexIndex];
                var existingB = Vertexes[Edges[i+1].vertexIndex];
                if (a.Equals(existingA) && b.Equals(existingB) || b.Equals(existingA) && a.Equals(existingB))
                {
                    return;
                }
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

        public Vector2 GetPointOnEdge(int edge, float percentage)
        {
            var vA = Vertexes[Edges[edge].vertexIndex];
            var vB = Vertexes[GetTwin(edge).vertexIndex];
            return Vector2.Lerp(vA, vB, percentage);
        }

        private void AddLine(float ax, float ay, float bx, float by)
        {
            Vertex a = new Vector2(ax, ay);
            Vertex b = new Vector2(bx, by);
            AddLine(a,b);
        }
    }
}
