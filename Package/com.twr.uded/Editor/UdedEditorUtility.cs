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
                // var floorPlane = new Plane(Vector3.up, new Vector3(0,face.floorHeight, 0));
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

                    var intersectionDistance = UdedCore.RayLineIntersection(r, uded.EdgeVertex(edge), uded.EdgeVertex(uded.GetTwin(i)));
                    if (intersectionDistance != null)
                    {
                        // bias towards grabbing verts
                        var vertGrabDisance = HandleUtility.GetHandleSize(enterPoint)*0.12f;
                        var vertADistance = (uded.EdgeVertex(edge) - (r.origin + r.direction * intersectionDistance)).Value.magnitude;
                        if (vertADistance < vertGrabDisance && res.t == ElementType.none || vertADistance < res.Distance)
                        {
                            res.PickPoint = enterPoint;
                            res.t = ElementType.vertex;
                            res.index = i;
                            res.Distance = vertADistance;
                        }
                        else if (res.t == ElementType.none || enter < res.Distance)
                        {
                            res.t = seg;
                            res.index = i;
                            res.Distance = enter;
                        }
                    }
                }
            }
            // test the floors / ceilings
            for (int i = 0; i < uded.Faces.Count; i++)
            {
                var face = uded.Faces[i];
                if(face.clockwise)
                    continue;
                // test floor
                var floorPlane = new Plane(Vector3.up, new Vector3(0,face.floorHeight, 0));
                if(Vector3.Dot(floorPlane.normal, ray.direction) < 0 && floorPlane.Raycast(ray, out var enter))
                {
                    var enterPoint = ray.origin+ray.direction*enter;
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
                        if(enter < res.Distance || res.t == ElementType.none)
                        {
                            res.t = ElementType.floor;
                            res.index = i;
                            res.Distance = enter;
                        }
                    }
                }
                // test ceiling
                var ceilingPlane = new Plane(-Vector3.up, new Vector3(0,face.ceilingHeight, 0));
                ray = HandleUtility.GUIPointToWorldRay (Event.current.mousePosition);
                if(Vector3.Dot(ceilingPlane.normal, ray.direction) < 0 && ceilingPlane.Raycast(ray, out enter))
                {
                    var enterPoint = ray.origin+ray.direction*enter;
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
                        if(enter < res.Distance || res.t == ElementType.none)
                        {
                            res.t = ElementType.ceiling;
                            res.index = i;
                            res.Distance = enter;
                        }
                    }
                }
            }

            return res;
        }
    }
}