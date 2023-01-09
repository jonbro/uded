using System.Linq;
using jbgeo;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using Random = UnityEngine.Random;

public class VillageGenerator : MonoBehaviour
{
    public HalfEdgeMesh _halfEdgeMesh;
    public int seed = 10;
    public int splitCount = 10;
    public bool showDebug;
    public float radius = 100;
    public void GenerateVillage()
    {
        Random.InitState(seed);
        _halfEdgeMesh = new HalfEdgeMesh();
        _halfEdgeMesh.AddRect(
            new Vertex(-100, -100),
            new Vertex(100, -100),
            new Vertex(100, 100),
            new Vertex(-100, 100));
        _halfEdgeMesh.Rebuild();

        // add main road
        var mainRoad = new Spline();

        var roadPointA = Random.insideUnitCircle.normalized * radius;
        var roadPointB = Random.insideUnitCircle.normalized * radius;
        mainRoad.Add(new BezierKnot(new Vector3(roadPointA.x, 0, roadPointA.y)));
        mainRoad.Add(new BezierKnot(new float3(0, 0, 0)), TangentMode.AutoSmooth, 0.3f);
        mainRoad.Add(new BezierKnot(new Vector3(roadPointB.x, 0, roadPointB.y)));
        // insert some wobblers
        for (int i = 0; i < 20; i++)
        {
            var startPosition = mainRoad.EvaluatePosition(Random.value);
        }

        int evalCount = 40;
        // lets just use the road as a line for now
        for (int i = 0; i < evalCount; i++)
        {
            var a3 = mainRoad.EvaluatePosition(i / (float)evalCount);
            var b3 = mainRoad.EvaluatePosition((i + 1) / (float)evalCount);
            _halfEdgeMesh.AddLine((Vector2)a3.xz, (Vector2)b3.xz);
        }
        _halfEdgeMesh.Rebuild();
        return;
        for (int i = 0; i < splitCount; i++)
        {
            // pick a face to get random edges from
            // do this by dropping a point within the bounds, and finding the face it belongs to (to bias towards larger faces)
            // maybe should do this with a face area sort eventually?
            var point = new Vertex(Random.value * 200 - 100, Random.value * 200 - 100);
            var face = _halfEdgeMesh.Faces[(int) (Random.value * _halfEdgeMesh.Faces.Count)];
            for (int j = 0; j < _halfEdgeMesh.Faces.Count; j++)
            {
                face = _halfEdgeMesh.Faces[j];
                if (_halfEdgeMesh.PointInFace(point, j) && !face.clockwise && face.Edges.Count() > 2)
                {
                    face = _halfEdgeMesh.Faces[j];
                    break;
                }
            }
            // pick a random face
            // while (face.clockwise || face.Edges.Count < 2)
            // {
            //     face = _halfEdgeMesh.Faces[(int) (Random.value * _halfEdgeMesh.Faces.Count)];
            // }
            if(face.Edges.Count() < 2)
                continue;
            // sort the edges by length, and split the two longest edges
            var sorted = face.Edges.ToList();
            sorted.Sort(delegate(int x, int y)
            {
                return -_halfEdgeMesh.EdgeLength(x).CompareTo(_halfEdgeMesh.EdgeLength(y));
            });
            // pick two random lines, then draw a line between them
            var edgeA = sorted[Mathf.FloorToInt(Random.value*3)];
            // pick points along these edges, then create a line that connects
            var pointA = _halfEdgeMesh.GetPointOnEdge(edgeA, .5f+Random.value*0.2f-0.1f);
            // point b is calculated by starting at pointA, then projecting out to the left from the edge A
            // then finding the nearest collision point
            var forward = (Vector3) _halfEdgeMesh.EdgeVertex(_halfEdgeMesh.GetTwin(edgeA)) - (Vector3) _halfEdgeMesh.EdgeVertex(edgeA);
            var forwardRot = Quaternion.LookRotation(forward.normalized);
            Vector3 left3 = forwardRot * Quaternion.Euler(0, Random.value * 10f-5f, 0) * Vector3.left;
            Vector2 left = new Vector2(left3.x, left3.z);

            int hitEdgeId;
            Vector2 pointB;
            // find intersection point
            if(_halfEdgeMesh.FindNearestRayIntersectionEdge(new Ray2D(pointA, left), out hitEdgeId, out pointB))
            {
                _halfEdgeMesh.AddLine(
                    pointA,
                    pointB
                );
            }
            _halfEdgeMesh.Rebuild();
        }
        // generate our various outputs - this is just a matter of populating some spline data in subcontainers
        // find the children that have spline containers
        var splineChildren = GetComponentsInChildren<SplineContainer>();
        foreach (var splineChild in splineChildren)
        {
            foreach (var spline in splineChild.Splines)
            {
                splineChild.RemoveSpline(spline);
            }
        }
        for (int i = 0; i < _halfEdgeMesh.Faces.Count; i++)
        {
            var face = _halfEdgeMesh.Faces[i];
            if(face.clockwise || face.Edges.Count < 3)
                continue;
            var targetSplineChild = splineChildren[i % splineChildren.Length];//Mathf.FloorToInt(Random.value * splineChildren.Length)];
            var faceSpline = targetSplineChild.AddSpline();
            faceSpline.Closed = true;
            foreach (var edge in face.Edges)
            {
                faceSpline.Add(new BezierKnot((Vector3)_halfEdgeMesh.EdgeVertex(edge)));
            }
        }
    }
}
