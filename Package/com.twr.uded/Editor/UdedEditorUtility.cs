using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Uded
{
    public struct PickingElement
    {
        public ElementType t;
        public int index;
        public Vector3 PickPoint;
        public float Distance;
    }
    public enum ElementType
    {
        none = 0,
        wall_mid,
        wall_lower,
        wall_upper,
        vertex,
        floor,
        ceiling,
    }

    public class UdedEditorUtility
    {
        public static bool PointOnRay(Ray r1, Ray r2, out float distanceOnRay1, out float distanceOnRay2)
        {
            distanceOnRay1 = distanceOnRay2 = 0;
            float a = Vector3.Dot(r1.direction, r1.direction);
            float b = Vector3.Dot(r1.direction.normalized, r2.direction.normalized);
            float e = Vector3.Dot(r2.direction, r2.direction);
 
            float d = a*e - b*b;
            // check for parallel
            if(d != 0.0f)
            {
                Vector3 r = r1.origin - r2.origin;
                float c = Vector3.Dot(r1.direction, r);
                float f = Vector3.Dot(r2.direction, r);
                distanceOnRay1 = (b*f - c*e) / d;
                distanceOnRay2 = (a*f - c*b) / d;
                return true;
            }

            return false;
        }

        public static List<(Vector3, Vector3)> GetWorldSpaceEdges(UdedCore uded)
        {
            var res = new List<(Vector3, Vector3)>();
            var interiors = new Dictionary<int, int>();
            for (int i = 0; i < uded.Faces.Count; i++)
            {
                var fExterior = uded.Faces[i];
                foreach (var interior in fExterior.InteriorFaces)
                {
                    interiors[interior] = i;
                }
            }
            for (int i = 0; i < uded.Edges.Count; i++)
            { 
                var edge = uded.Edges[i];
                var face = uded.Faces[edge.face];
                var backfaceIndex = uded.GetTwin(i).face;
                var backface = uded.Faces[backfaceIndex];
                if (interiors.ContainsKey(backfaceIndex))
                {
                    backface = uded.Faces[interiors[backfaceIndex]];
                }
                var a = uded.EdgeVertex(edge);
                var b = uded.EdgeVertex(uded.GetTwin(i));
                var floorPosition = Vector3.up * face.floorHeight;
                var ceilingPosition = Vector3.up * face.ceilingHeight;
                if (!backface.clockwise)
                {
                    floorPosition = Vector3.up * backface.floorHeight;
                    ceilingPosition = Vector3.up * backface.ceilingHeight;
                }
                res.Add((a+floorPosition, b+floorPosition));
                res.Add((a+ceilingPosition, b+ceilingPosition));
            }

            return res;
        }
        
        public static PickingElement GetNearestLevelElement(UdedCore uded)
        {
            var interiors = new Dictionary<int, int>();
            for (int i = 0; i < uded.Faces.Count; i++)
            {
                var fExterior = uded.Faces[i];
                foreach (var interior in fExterior.InteriorFaces)
                {
                    interiors[interior] = i;
                }
            }

            Ray ray = HandleUtility.GUIPointToWorldRay (Event.current.mousePosition);
            var res = new PickingElement{
                t=ElementType.none,
                index=-1,
                Distance=0.0f
            };
            var testedVert = new HashSet<int>();
            for (int i = 0; i < uded.Edges.Count; i++)
            {
                var edge = uded.Edges[i];
                if(testedVert.Contains(edge.vertexIndex))
                    continue;
                testedVert.Add(edge.vertexIndex);
                var vertPoint = new Ray(uded.EdgeVertex(i), Vector3.up);
                if (PointOnRay(ray, vertPoint, out var r1, out var r2))
                {
                    var pointOnEdge = vertPoint.origin + vertPoint.direction * r2;
                    var size = HandleUtility.GetHandleSize(pointOnEdge)*0.05f;
                    var screenToPickDistance = Vector3.Distance(pointOnEdge, ray.origin);
                    var screenRayNearestPoint = ray.origin + ray.direction * r1;
                    var distanceToVert = Vector3.Distance(screenRayNearestPoint, pointOnEdge);
                    if (distanceToVert < size && res.t == ElementType.none || screenToPickDistance < res.Distance)
                    {
                        res.t = ElementType.vertex;
                        res.index = i;
                        res.PickPoint = screenRayNearestPoint;
                        res.Distance = distanceToVert;
                    }
                }
            }
            // return early if we found a vert hit
            if (res.t == ElementType.vertex)
                return res;
            for (int i = 0; i < uded.Edges.Count; i++)
            {
                var edge = uded.Edges[i];
                var face = uded.Faces[edge.face];
                var backfaceIndex = uded.GetTwin(i).face;
                var backface = uded.Faces[backfaceIndex];
                if (interiors.ContainsKey(backfaceIndex))
                {
                    backface = uded.Faces[interiors[backfaceIndex]];
                }
                var forward = (Vector3) uded.EdgeVertex(uded.GetTwin(i)) - (Vector3) uded.EdgeVertex(edge);
                var forwardRot = Quaternion.LookRotation(forward);
                var center = Vector3.Lerp(uded.EdgeVertex(edge), uded.EdgeVertex(edge.nextId), 0.5f);
                var left = forwardRot * Vector3.left;
                var wallPlane = new Plane(left, center);
                if(Vector3.Dot(left, ray.direction)<0 && wallPlane.Raycast(ray, out var enter))
                {
                    var enterPoint = ray.origin+ray.direction*enter;
                    // flatten the ray to 2d
                    var r = new Ray2D(new Vector2(ray.origin.x, ray.origin.z),
                        new Vector2(ray.direction.x, ray.direction.z));
                    if (enterPoint.y > face.ceilingHeight || enterPoint.y < face.floorHeight)
                    {
                        continue;
                    }
                    var seg = ElementType.wall_mid;
                    if (!backface.clockwise)
                    {
                        if (backface.floorHeight > face.floorHeight && enterPoint.y < backface.floorHeight
                            || backface.floorHeight > face.floorHeight && enterPoint.y < backface.floorHeight)
                        {
                            seg = ElementType.wall_lower;
                        }
                        else if (backface.ceilingHeight < face.ceilingHeight && enterPoint.y > backface.ceilingHeight
                            || backface.ceilingHeight > face.ceilingHeight && enterPoint.y > backface.ceilingHeight)
                        {
                            seg = ElementType.wall_upper;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (UdedCore.RayLineIntersection(r, uded.EdgeVertex(edge), uded.EdgeVertex(uded.GetTwin(i)), out _))
                    {
                        res.PickPoint = enterPoint;
                        if (res.t == ElementType.none || enter < res.Distance)
                        {
                            res.t = seg;
                            res.index = i;
                            res.Distance = enter;
                        }
                    }
                }
            }
            // test the floors / ceilings
            if(GetNearestFace(uded, out var faceRes))
            {
                res = faceRes;
            }
            return res;
        }

        public static bool GetNearestFace(UdedCore uded, out PickingElement res)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay (Event.current.mousePosition);
            res = new PickingElement
            {
                t = ElementType.none
            };
            for (int i = 0; i < uded.Faces.Count; i++)
            {
                var face = uded.Faces[i];
                if (face.clockwise)
                    continue;
                // test floor
                var floorPlane = new Plane(Vector3.up, new Vector3(0, face.floorHeight, 0));
                if (Vector3.Dot(floorPlane.normal, ray.direction) < 0 && floorPlane.Raycast(ray, out var enter))
                {
                    var enterPoint = ray.origin + ray.direction * enter;
                    var enterPoint2d = new Vector2(enterPoint.x, enterPoint.z);
                    bool isInInterior = false;
                    foreach (var interiorFaceIndex in face.InteriorFaces)
                    {
                        // need to get the flipped face so the point in face works correctly
                        if (uded.PointInFace(enterPoint2d, interiorFaceIndex))
                        {
                            isInInterior = true;
                            break;
                        }
                    }

                    if (!isInInterior && uded.PointInFace(enterPoint2d, i))
                    {
                        if (enter < res.Distance || res.t == ElementType.none)
                        {
                            res.PickPoint = enterPoint;
                            res.t = ElementType.floor;
                            res.index = i;
                            res.Distance = enter;
                        }
                    }
                }

                // test ceiling
                var ceilingPlane = new Plane(-Vector3.up, new Vector3(0, face.ceilingHeight, 0));
                ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                if (Vector3.Dot(ceilingPlane.normal, ray.direction) < 0 && ceilingPlane.Raycast(ray, out enter))
                {
                    var enterPoint = ray.origin + ray.direction * enter;
                    var enterPoint2d = new Vector2(enterPoint.x, enterPoint.z);
                    bool isInInterior = false;
                    foreach (var interiorFaceIndex in face.InteriorFaces)
                    {
                        // need to get the flipped face so the point in face works correctly
                        if (uded.PointInFace(enterPoint2d, interiorFaceIndex))
                        {
                            isInInterior = true;
                            break;
                        }
                    }

                    if (!isInInterior && uded.PointInFace(enterPoint2d, i))
                    {
                        if (enter < res.Distance || res.t == ElementType.none)
                        {
                            res.PickPoint = enterPoint;
                            res.t = ElementType.ceiling;
                            res.index = i;
                            res.Distance = enter;
                        }
                    }
                }
            }

            return res.t != ElementType.none;
        }
    }
}