using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TriangleNet.Geometry;
// alternately could use this
// https://github.com/speps/LibTessDotNet
namespace Uded
{
	public class PolyToMesh
	{
		private float multiplier = 10000;

		public static Mesh GetMeshFromFace(int faceIndex, UdedCore uded, List<Uded.HalfEdge> edges, List<Uded.Face> faces)
		{
			var face = faces[faceIndex];
			TriangleNet.Mesh tmesh = new TriangleNet.Mesh();

			InputGeometry input = new InputGeometry();

			// generate the floor / wall geo from the sector linedefs
			int startIndexMark = input.Count;
			int indexMarker = startIndexMark;
			int EdgeMarker = indexMarker;
			if (face.Edges.Count == 0)
				return new Mesh();
			for (int edgeIndex = 0; edgeIndex < face.Edges.Count; edgeIndex++)
			{
				var edge = edges[face.Edges[edgeIndex]];
				Vector2 nextPoint = uded.EdgeVertex(edge);
				input.AddPoint(nextPoint.x, nextPoint.y, EdgeMarker);
				input.AddSegment(indexMarker, ((indexMarker - startIndexMark + 1) % face.Edges.Count + startIndexMark),
					EdgeMarker);
				indexMarker++;
			}

			//for (int i = face.InteriorFaces.Count-1; i >= 0; i--)
			for (int i = 0; i < face.InteriorFaces.Count; i++)
			{
				var interiorFace = faces[face.InteriorFaces[i]];
				startIndexMark = input.Count;
				float avgX = 0;
				float avgY = 0;
				EdgeMarker++;
				for (int edgeIndex = 0; edgeIndex < interiorFace.Edges.Count; edgeIndex++)
				{
					var interiorEdge = edges[interiorFace.Edges[edgeIndex]];
					Vector2 nextPoint = uded.EdgeVertex(interiorEdge);
					input.AddPoint(nextPoint.x, nextPoint.y, EdgeMarker);
					avgX += nextPoint.x;
					avgY += nextPoint.y;
					var p1 = ((indexMarker - startIndexMark + 1) % interiorFace.Edges.Count + startIndexMark);
					input.AddSegment(indexMarker, p1,
						EdgeMarker);
					indexMarker++;
				}
				avgX = avgX / interiorFace.Edges.Count;
				avgY = avgY / interiorFace.Edges.Count;
				// TODO: should use the first triangle within the interior face to define the hole position
				// this can miss for shapes with concavities 
				input.AddHole(avgX, avgY);
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
			mesh.subMeshCount = 2;
			vertices = new List<Vector3>();
			normals = new List<Vector3>();
			uv = new List<Vector2>();

			TriangleNet.Data.Vertex[] tVertices = tmesh.Vertices.ToArray();
			int[] floorTri = new int[tmesh.Triangles.Count() * 3];
			int[] ceilingTri = new int[tmesh.Triangles.Count() * 3];

			// add the floor vertices
			for (int i = 0; i < tVertices.Length; i++)
			{
				vertices.Add(new Vector3((float) tVertices[i].X, 0, (float) tVertices[i].Y));
				uv.Add(new Vector2((float) tVertices[i].X, (float) tVertices[i].Y));
				normals.Add(Vector3.up.normalized);
			}

			// add the ceiling vertices
			for (int i = 0; i < tVertices.Length; i++)
			{
				vertices.Add(new Vector3((float) tVertices[i].X, 6f, (float) tVertices[i].Y));
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
			// List<List<int>> wallIndexes = new List<List<int>>();

			mesh.vertices = vertices.ToArray();
			mesh.normals = normals.ToArray();

			mesh.SetIndices(floorTri, MeshTopology.Triangles, 0);
			mesh.SetIndices(ceilingTri, MeshTopology.Triangles, 1);
			mesh.subMeshCount = 2;
			mesh.RecalculateBounds();
			// TODO: support walls
			// mesh.subMeshCount = 2 + wallIndexes.Count();
			// /**/
			// int wallCount = 2;
			// foreach (List<int> wallTris in wallIndexes)
			// {
			// 	mesh.SetIndices(wallTris.ToArray(), MeshTopology.Triangles, wallCount);
			//
			// 	wallCount++;
			// }

			mesh.RecalculateNormals();
			mesh.uv = uv.ToArray();
			mesh.uv2 = uv.ToArray();
			TangentSolver.Solve(mesh);
			// if you want to support lightmapping
	//		TODO: make this optional, because it is slow as fuck
	//		Unwrapping.GenerateSecondaryUVSet (mesh);

			return mesh;

		}
	}	
}
