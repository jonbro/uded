﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TriangleNet.Geometry;
using UnityEditor.Graphs;
using UnityEngine.Analytics;

// alternately could use this
// https://github.com/speps/LibTessDotNet
namespace Uded
{
	public class PolyToMesh
	{
		private float multiplier = 10000;
		private static List<List<int>> wallIndexes = new();
		private static List<Material> resMats = new();

		private static Vector3 Vector2To3(Vector2 v)
		{
			return new(v.x, 0, v.y);
		}

		private static void DebugPoint(Vector2 v, Color c)
		{
			Debug.DrawRay(Vector2To3(v)+Random.onUnitSphere*0.1f, Vector3.up, c, 0.01f);
		}
		public static (Mesh mesh, Material[] wallMaterials) GetMeshFromFace(int faceIndex, UdedCore uded, List<Uded.HalfEdge> edges, List<Uded.Face> faces)
		{
			resMats.Clear();
			wallIndexes.Clear();
			var face = faces[faceIndex];
			TriangleNet.Mesh tmesh = new TriangleNet.Mesh();

			InputGeometry input = new InputGeometry();

			// generate the floor / wall geo from the sector linedefs
			int startIndexMark = input.Count;
			int indexMarker = startIndexMark;
			int EdgeMarker = indexMarker;
			if (face.Edges.Count == 0)
				return (new Mesh(), resMats.ToArray());
			for (int edgeIndex = 0; edgeIndex < face.Edges.Count; edgeIndex++)
			{
				var edge = edges[face.Edges[edgeIndex]];
				Vector2 nextPoint = uded.EdgeVertex(edge);
				input.AddPoint(nextPoint.x, nextPoint.y, EdgeMarker);
				input.AddSegment(indexMarker, ((indexMarker - startIndexMark + 1) % face.Edges.Count + startIndexMark),
					EdgeMarker);
				indexMarker++;
			}
			for (int i = 0; i < face.InteriorFaces.Count; i++)
			{
				var interiorFace = faces[face.InteriorFaces[i]];
				startIndexMark = input.Count;
				EdgeMarker++;
				for (int edgeIndex = 0; edgeIndex < interiorFace.Edges.Count; edgeIndex++)
				{
					var interiorEdge = edges[interiorFace.Edges[edgeIndex]];
					// add segments to the input (defining the interior shape)
					Vector2 nextPoint = uded.EdgeVertex(interiorEdge);
					input.AddPoint(nextPoint.x, nextPoint.y, EdgeMarker);
					var p1 = ((indexMarker - startIndexMark + 1) % interiorFace.Edges.Count + startIndexMark);
					input.AddSegment(indexMarker, p1,
						EdgeMarker);
					indexMarker++;
				}
				// look through all the pairs of edges until we find a point that is within the shape
				// there must be at least one acute angle in the shape, and the midpoint between its verts will be within the shape
				for (int edgeIndex = 0; edgeIndex < interiorFace.Edges.Count; edgeIndex++)
				{
					var interiorEdge = edges[interiorFace.Edges[edgeIndex]];
					var pointA = uded.EdgeVertex(interiorEdge);
					var pointB = uded.EdgeVertex(uded.GetTwin(interiorFace.Edges[edgeIndex]));
					var pointC = uded.EdgeVertex(interiorEdge.prevId);
					// DebugPoint(pointA, Color.magenta);
					// DebugPoint(pointB, Color.cyan);
					// DebugPoint(pointC, Color.blue);
					var midpoint = (pointA._value+pointB._value+pointC._value)/3.0f;
					if (uded.PointInFace(midpoint, face.InteriorFaces[i]))
					{
						// DebugPoint(midpoint, Color.yellow);
						input.AddHole(midpoint.x, midpoint.y);
						break;
					}
				}
			}


			tmesh.Triangulate(input);

			// convert the mesh to a unity mesh
			Color32[] cs;
			List<Vector2> uv;
			List<Vector3> vertices;
			List<Vector3> normals;

			Mesh mesh = new Mesh();
			mesh.name = "sector mesh";
			mesh.hideFlags = HideFlags.HideAndDontSave;
			mesh.subMeshCount = 3;
			vertices = new List<Vector3>();
			normals = new List<Vector3>();
			uv = new List<Vector2>();

			TriangleNet.Data.Vertex[] tVertices = tmesh.Vertices.ToArray();
			int[] floorTri = new int[tmesh.Triangles.Count() * 3];
			int[] ceilingTri = new int[tmesh.Triangles.Count() * 3];
			List<int> wallTri = new List<int>();

			// add the floor vertices
			for (int i = 0; i < tVertices.Length; i++)
			{
				vertices.Add(new Vector3((float) tVertices[i].X, face.floorHeight, (float) tVertices[i].Y));
				uv.Add(new Vector2((float) tVertices[i].X, (float) tVertices[i].Y));
				normals.Add(Vector3.up.normalized);
			}

			// add the ceiling vertices
			for (int i = 0; i < tVertices.Length; i++)
			{
				vertices.Add(new Vector3((float) tVertices[i].X, face.ceilingHeight, (float) tVertices[i].Y));
				uv.Add(new Vector2((float) tVertices[i].X, (float) tVertices[i].Y));
				normals.Add(Vector3.down.normalized);
			}
			// TODO: needed for the picking, will get back to this
			// if (face.floorTriangles == null)
			// 	face.floorTriangles = new List<int>();
			// if (face.ceilingTriangles == null)
			// 	face.ceilingTriangles = new List<int>();
			// face.floorTriangles.Clear();
			// face.ceilingTriangles.Clear();
			// // setup the triangle indexes for the ceiling / floor
			for (int i = 0; i < tmesh.Triangles.Count(); i++)
			{
				floorTri[0 + i * 3] = tmesh.Triangles.ElementAt(i).P0;
				floorTri[2 + i * 3] = tmesh.Triangles.ElementAt(i).P1;
				floorTri[1 + i * 3] = tmesh.Triangles.ElementAt(i).P2;
			}
			//
			for (int i = 0; i < tmesh.Triangles.Count(); i++)
			{
				ceilingTri[0 + i * 3] = tmesh.Triangles.ElementAt(i).P0 + vertices.Count() / 2;
				ceilingTri[1 + i * 3] = tmesh.Triangles.ElementAt(i).P1 + vertices.Count() / 2;
				ceilingTri[2 + i * 3] = tmesh.Triangles.ElementAt(i).P2 + vertices.Count() / 2;
			}
			// build interiors map
			// this could be memoized elsewhere
			var interiors = new Dictionary<int, int>();
			for (int i = 0; i < faces.Count; i++)
			{
				var fExterior = faces[i];
				foreach (var interior in fExterior.InteriorFaces)
				{
					interiors[interior] = i;
				}
			}
			// add the walls!
			for (int edgeIndex = 0; edgeIndex < face.Edges.Count; edgeIndex++)
			{
				var edge = edges[face.Edges[edgeIndex]];
				Vector2 pointA = uded.EdgeVertex(edge);
				Vector2 pointB = uded.EdgeVertex(uded.GetTwin(face.Edges[edgeIndex]));
				// if the backface is shared with another counterclockwise face, then skip
				// after drawing ledges
				var otherFaceIndex = uded.GetTwin(face.Edges[edgeIndex]).face;
				var otherFace = faces[otherFaceIndex];
				if (interiors.ContainsKey(otherFaceIndex))
					otherFace = faces[interiors[otherFaceIndex]];
				if (!otherFace.clockwise)
				{
					if (otherFace.floorHeight > face.floorHeight)
					{
						resMats.Add(edge.lowerMat);
						AddWall(pointA, pointB, face.floorHeight, otherFace.floorHeight, vertices, uv, normals);
					}
					else
					{
						resMats.Add(uded.GetTwin(face.Edges[edgeIndex]).lowerMat);
						AddWall(pointB, pointA, otherFace.floorHeight, face.floorHeight, vertices, uv, normals);
					}

					if (otherFace.ceilingHeight < face.ceilingHeight)
					{
						resMats.Add(edge.upperMat);
						AddWall(pointA, pointB, otherFace.ceilingHeight, face.ceilingHeight, vertices, uv, normals);
					}
					else
					{
						resMats.Add(uded.GetTwin(face.Edges[edgeIndex]).upperMat);
						AddWall(pointB, pointA, face.ceilingHeight, otherFace.ceilingHeight, vertices, uv, normals);
					}
					continue;
				}
				resMats.Add(edge.midMat);
				AddWall(pointA, pointB, face.floorHeight, face.ceilingHeight, vertices, uv, normals);
			}
			//
			// // TODO: needded for the picking, get back to this
			// /**/
			// foreach (int i in ceilingTri)
			// {
			// 	face.ceilingTriangles.Add(i);
			// }
			//
			// foreach (int i in floorTri)
			// {
			// 	face.floorTriangles.Add(i);
			// }
			//

			mesh.vertices = vertices.ToArray();
			mesh.normals = normals.ToArray();

			mesh.subMeshCount = 2 + wallIndexes.Count;
			mesh.SetIndices(floorTri, MeshTopology.Triangles, 0);
			mesh.SetIndices(ceilingTri, MeshTopology.Triangles, 1);
			var c = 2;
			// todo: collapse these meshes? we basically have 1 mesh per wall, which is horrible
			foreach (var wallIndexGroup in wallIndexes)
			{
				mesh.SetIndices(wallIndexGroup, MeshTopology.Triangles, c++);	
			}
			// mesh.SetIndices(wallTri, MeshTopology.Triangles, 2);
			mesh.RecalculateBounds();
			
			mesh.RecalculateNormals();
			mesh.uv = uv.ToArray();
			mesh.uv2 = uv.ToArray();
			TangentSolver.Solve(mesh);
			
			// if you want to support lightmapping
			// TODO: make this optional, because it is somewhat slow
			// Unwrapping.GenerateSecondaryUVSet (mesh);

			return (mesh, resMats.ToArray());
		}

		private static void AddWall(Vector2 pointA, Vector2 pointB, float floorHeight, float ceilingHeight, List<Vector3> vertices, List<Vector2> uv,
			List<Vector3> normals)
		{
			var size = (pointA - pointB).magnitude;
			var height = ceilingHeight - floorHeight;
			int startIndex = vertices.Count;
			vertices.Add(new Vector3(pointA.x, floorHeight, pointA.y));
			vertices.Add(new Vector3(pointB.x, floorHeight, pointB.y));
			vertices.Add(new Vector3(pointA.x, ceilingHeight, pointA.y));
			vertices.Add(new Vector3(pointB.x, ceilingHeight, pointB.y));

			// bull shit for uvs
			uv.Add(new Vector2(0, 0));
			uv.Add(new Vector2(size, 0));
			uv.Add(new Vector2(0, height));
			uv.Add(new Vector2(size, height));
			//
			var wallTri = new List<int>();
			wallTri.Add(startIndex);
			wallTri.Add(startIndex + 1);
			wallTri.Add(startIndex + 2);
			wallTri.Add(startIndex + 3);
			wallTri.Add(startIndex + 2);
			wallTri.Add(startIndex + 1);
			wallIndexes.Add(wallTri);
			normals.Add(Vector3.up.normalized);
			normals.Add(Vector3.up.normalized);
			normals.Add(Vector3.up.normalized);
			normals.Add(Vector3.up.normalized);
		}
	}	
}
