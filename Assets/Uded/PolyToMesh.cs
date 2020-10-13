using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TriangleNet.Geometry;
// alternately could use this
// https://github.com/speps/LibTessDotNet

public class PolyToMesh
{
	private float multiplier = 10000;

	public static Mesh GetMeshFromFace(Uded.Face face)
	{
		TriangleNet.Mesh tmesh = new TriangleNet.Mesh();

		InputGeometry input = new InputGeometry();

		// generate the floor / wall geo from the sector linedefs
		int startIndexMark = input.Count;
		int indexMarker = startIndexMark;
		int EdgeMarker = indexMarker;
		List<Uded.HalfEdge> edges = face.Edges;
		if (edges.Count == 0)
			return new Mesh();
		if (edges.Count > 0)
		{
			float avgX = 0;
			float avgY = 0;
			foreach (Uded.HalfEdge edge in edges)
			{
				Vector2 nextPoint = edge.origin;
				input.AddPoint(nextPoint.x, nextPoint.y, EdgeMarker);
				input.AddSegment(indexMarker, ((indexMarker - startIndexMark + 1) % edges.Count + startIndexMark),
					EdgeMarker);
				indexMarker++;
			}
		}

		/**/
		// List<Sector.SideGroup> innerGroups = sector.GetInnerGroups();
		// if (innerGroups.Count > 0)
		// {
		// 	// cut the holes based on the subsectors
		// 	foreach (Sector.SideGroup innerGroup in innerGroups)
		// 	{
		// 		EdgeMarker++;
		// 		startIndexMark = input.Count;
		// 		float avgX = 0;
		// 		float avgY = 0;
		// 		foreach (SideDef sd in innerGroup.sides)
		// 		{
		// 			int nextMarker = ((indexMarker - startIndexMark + 1) % innerGroup.sides.Count + startIndexMark);
		// 			Vector2 nextPoint = sd.line.start.Vector;
		// 			// flip this based on line sidedness
		// 			if (sd.line.front != sd)
		// 			{
		// 				nextPoint = sd.line.end.Vector;
		// 			}
		//
		// 			input.AddPoint(nextPoint.x, nextPoint.y, EdgeMarker);
		// 			input.AddSegment(indexMarker,
		// 				((indexMarker - startIndexMark + 1) % innerGroup.sides.Count + startIndexMark), EdgeMarker);
		// 			avgX += sd.line.start.Vector.x;
		// 			avgY += sd.line.start.Vector.y;
		// 			indexMarker++;
		// 		}
		//
		// 		avgX = avgX / innerGroup.sides.Count;
		// 		avgY = avgY / innerGroup.sides.Count;
		// 		input.AddHole(avgX, avgY);
		// 	}
		// }

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