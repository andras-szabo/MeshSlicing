﻿using System.Collections.Generic;
using UnityEngine;

public static class MeshUtilities
{
	public enum CutType
	{
		None,
		ABCA,
		ABBC,
		BCCA
	}

	public struct TriIntersections
	{
		public Vector3 A;
		public Vector3 B;
		public Vector3 C;

		public Vector3 normalA;
		public Vector3 normalB;
		public Vector3 normalC;

		public Vector3 Iab;
		public Vector3 Ibc;
		public Vector3 Ica;

		public Vector3 normalIAB;
		public Vector3 normalIBC;
		public Vector3 normalICA;

		public CutType type;
	}

	public static void LogIf(bool log, string msg)
	{
		if (log) { Debug.LogWarning(msg); }
	}

	public static Vector3 staticCutNormal;

	public static bool IsPointInTriangle(Vector3 point, 
										 Vector3 a, Vector3 b, Vector3 c,
										 bool assumePointIsInSamePlane)
	{
		if (!assumePointIsInSamePlane && !IsPointInPlane(point, a, b, c))
		{
			return false;
		}

		// This is how this works: define vectors AB, BC, and CA, and
		// AP, BP, CP. Then P is inside the ABC triangle iff AB x AP,
		// BC x BP, and CA x CP all point in the same direction. So
		// what is AB x AP? A vector perpendicular to both AB and AP,
		// pointing into some random direction, indicating whether or not
		// we're taking a right turn or a left turn. We're then asking,
		// are we ALWAYS taking a right turn (or left turn)? This works,
		// because if the point is INSIDE the triangle, it will ALWAYS be
		// on the left (or right, depending on the CW or CCW order of the
		// coordinates) side.

		// Basically, think of the triangle edges as a fence. Say the
		// vertices are ordered clockwise. Then, as you follow the edges
		// of the triangle, whatever is inside it will always be on
		// your right side.

		// To tell if the cross products are roughly pointing in the same
		// direction, we can use the dot product.

		var abXap = Vector3.Cross(b - a, point - a);
		var bcXbp = Vector3.Cross(c - b, point - b);
		var caXcp = Vector3.Cross(a - c, point - c);

		var dot1 = Vector3.Dot(abXap, bcXbp);
		var dot2 = Vector3.Dot(abXap, caXcp);
		var dot3 = Vector3.Dot(bcXbp, caXcp);

		return Mathf.Sign(dot1) == Mathf.Sign(dot2) && Mathf.Sign(dot2) == Mathf.Sign(dot3);
	}

	public static bool IsPointInPlane(Vector3 point, Vector3 a, Vector3 b, Vector3 c)
	{
		var planeNormal = Vector3.Cross(a, b);
		return Mathf.Approximately(Vector3.Dot(planeNormal, point), 0f);
	}

	public static bool SliceMultiTriangleMesh(GameObject meshGO, Vector3 cutStartPos, Vector3 cutEndPos, Material material,
												bool makePiecesDrop = true, bool log = true)
	{
		// TODO: it might make sense to check in advance somehow if anything is going to be affected
		// by the cut, even if we don't actually do anything if it's not.

		var cutNormal = CalculateCutNormal(cutStartPos, cutEndPos, log);
		staticCutNormal = cutNormal;

		var meshTransform = meshGO.transform;
		TransformCutToObjectSpace(ref cutStartPos, ref cutEndPos, ref cutNormal, meshTransform);

		var mesh = meshGO.GetComponent<MeshFilter>().sharedMesh;

		var vertsAboveCut = new List<Vector3>();
		var normalsAboveCut = new List<Vector3>();

		var vertsBelowCut = new List<Vector3>();
		var normalsBelowCut = new List<Vector3>();

		var atLeastOneTriangleWasCut = false;

		var meshVertices = mesh.vertices;
		var meshTriangles = mesh.triangles;
		var meshNormals = mesh.normals;

		for (int triStartIndex = 0; triStartIndex < meshTriangles.Length; triStartIndex += 3)
		{
			var intersection = CalculateIntersections(meshVertices, meshTriangles, meshNormals, triStartIndex, 
													  cutStartPos, cutNormal, log);
			if (intersection.type == CutType.None)
			{
				if (IsTriAboveCut(meshVertices, meshTriangles, triStartIndex, cutStartPos, cutNormal, log))
				{
					CopyVertsAndNormals(meshVertices, meshTriangles, meshNormals, triStartIndex, vertsAboveCut, normalsAboveCut);
				}
				else
				{
					CopyVertsAndNormals(meshVertices, meshTriangles, meshNormals, triStartIndex, vertsBelowCut, normalsBelowCut);
				}
			}
			else
			{
				atLeastOneTriangleWasCut = true;
				CutTriangleAndCopyVertsAndNormals(mesh, intersection, cutStartPos, cutNormal,
												  vertsAboveCut, normalsAboveCut, 
												  vertsBelowCut, normalsBelowCut, log);
			}
		}

		if (atLeastOneTriangleWasCut)
		{
			var meshAbove = BuildMesh(vertsAboveCut, normalsAboveCut, log);
			var meshBelow = BuildMesh(vertsBelowCut, normalsBelowCut, log);

			var goAbove = BuildGO(meshAbove, string.Format("{0}_above", meshGO.name), meshTransform, material, cutNormal, true, makePiecesDrop, log);
			var goBelow = BuildGO(meshBelow, string.Format("{0}_below", meshGO.name), meshTransform, material, cutNormal, false, makePiecesDrop, log);
		}

		return atLeastOneTriangleWasCut;
	}

	private static GameObject BuildGO(Mesh mesh, string goName, Transform transformTemplate, Material material, 
									  Vector3 cutNormal, bool aboveCut, bool makeDrop, bool log)
	{
		var go = CreateMeshGameObject(mesh, goName, material);

		go.transform.SetPositionAndRotation(transformTemplate.position, transformTemplate.rotation);
		go.transform.localScale = transformTemplate.localScale;

		var collider = go.AddComponent<MeshCollider>();
		collider.convex = true;

		var rb = go.AddComponent<Rigidbody>();
		rb.useGravity = true;
		rb.isKinematic = !makeDrop;

		if (makeDrop)
		{
			rb.AddForce(cutNormal * 1.8f * (aboveCut ? 1f : -1f), ForceMode.VelocityChange);
		}

		go.layer = LayerMask.NameToLayer("Sliceable");

		return go;
	}

	private static Mesh BuildMesh(List<Vector3> verts, List<Vector3> norms, bool log)
	{
		var mesh = new Mesh();
		mesh.vertices = verts.ToArray();
		mesh.normals = norms.ToArray();
		
		var triangles = new int[verts.Count];
		for (int i = 0; i < triangles.Length; ++i)
		{
			triangles[i] = i;
		}

		mesh.triangles = triangles;
		mesh.Optimize();

		return mesh;
	}

	private static void CopyVertsAndNormals(Vector3[] meshVertices, int[] meshTriangles, Vector3[] meshNormals, 
											int triStartIndex, List<Vector3> vertsDst, List<Vector3> normsDst)
	{
		for (int i = 0; i < 3; ++i)
		{
			var index = meshTriangles[triStartIndex + i];

			vertsDst.Add(meshVertices[index]);
			normsDst.Add(meshNormals[index]);
		}
	}

	private static bool IsTriAboveCut(Vector3[] meshVertices, int[] meshTriangles, int triStartIndex, Vector3 cutStartPos, Vector3 cutNormal, bool log)
	{
		var pointInTri = meshVertices[meshTriangles[triStartIndex]];
		var dot = Vector3.Dot(cutNormal, pointInTri - cutStartPos);
		return dot > 0f;
	}

	//TODO REMOVE
	/*
	public static bool SliceSingleTriangleMesh(GameObject meshGO, Vector3 cutStartPos, Vector3 cutEndPos, Material material,
												bool makePiecesDrop = true, bool log = true)
	{
		var cutNormal = CalculateCutNormal(cutStartPos, cutEndPos, log);
		//LogIf(log, string.Format("Cut normal: {0}", cutNormal));
		staticCutNormal = cutNormal;

		var meshTransform = meshGO.transform;
		TransformCutToObjectSpace(ref cutStartPos, ref cutEndPos, ref cutNormal, meshTransform);
		//LogIf(log, string.Format("Cut: {0} -> {1}; normal: {2}", cutStartPos, cutEndPos, cutNormal));

		var mesh = meshGO.GetComponent<MeshFilter>().sharedMesh;
		var meshVertices = mesh.vertices;
		var meshTriangles = mesh.triangles;
		var meshNormals = mesh.normals;
		var intersections = CalculateIntersections(meshVertices, meshTriangles, meshNormals, 
												   0, cutStartPos, cutNormal, log);

		if (intersections.type != CutType.None)
		{
			var meshGOs = CreateMeshes(intersections, material, meshGO.transform);
			foreach (var go in meshGOs)
			{
				var collider = go.AddComponent<MeshCollider>();
				collider.convex = true;

				var rigidbody = go.AddComponent<Rigidbody>();
				rigidbody.useGravity = true;
				rigidbody.isKinematic = !makePiecesDrop;
			}

			return true;
		}

		return false;
	}
	*/

	private static void CutTriangleAndCopyVertsAndNormals(Mesh mesh, TriIntersections cut,
														  Vector3 cutStartPos, Vector3 cutNormal,
														  List<Vector3> vertsAbove, List<Vector3> normsAbove,
														  List<Vector3> vertsBelow, List<Vector3> normsBelow, bool log)
	{
		switch (cut.type)
		{
			case CutType.ABCA:
			{
				LogIf(log, "ABCA");
				var isSmallPieceAboveCut = Vector3.Dot(cutNormal, cut.A - cutStartPos) > 0f;

				List<Vector3> smallPieceVerts = isSmallPieceAboveCut ? vertsAbove : vertsBelow;
				List<Vector3> smallPieceNorms = isSmallPieceAboveCut ? normsAbove : normsBelow;

				List<Vector3> bigPieceVerts = isSmallPieceAboveCut ? vertsBelow : vertsAbove;
				List<Vector3> bigPieceNorms = isSmallPieceAboveCut ? normsBelow : normsAbove;

				smallPieceVerts.Add(cut.A); smallPieceVerts.Add(cut.Iab); smallPieceVerts.Add(cut.Ica);

				smallPieceNorms.Add(cut.normalA);
				smallPieceNorms.Add(cut.normalIAB);
				smallPieceNorms.Add(cut.normalICA);

				bigPieceVerts.Add(cut.C); bigPieceVerts.Add(cut.Ica); bigPieceVerts.Add(cut.Iab);
				bigPieceVerts.Add(cut.C); bigPieceVerts.Add(cut.Iab); bigPieceVerts.Add(cut.B);
				
				bigPieceNorms.Add(cut.normalC);
				bigPieceNorms.Add(cut.normalICA);
				bigPieceNorms.Add(cut.normalIAB);

				bigPieceNorms.Add(cut.normalC);
				bigPieceNorms.Add(cut.normalIAB);
				bigPieceNorms.Add(cut.normalB);

				break;
			}

			case CutType.ABBC:
			{
				var isSmallPieceAboveCut = Vector3.Dot(cutNormal, cut.B - cutStartPos) > 0f;

				List<Vector3> smallPieceVerts = isSmallPieceAboveCut ? vertsAbove : vertsBelow;
				List<Vector3> smallPieceNorms = isSmallPieceAboveCut ? normsAbove : normsBelow;

				List<Vector3> bigPieceVerts = isSmallPieceAboveCut ? vertsBelow : vertsAbove;
				List<Vector3> bigPieceNorms = isSmallPieceAboveCut ? normsBelow : normsAbove;

				smallPieceVerts.Add(cut.B); smallPieceVerts.Add(cut.Ibc); smallPieceVerts.Add(cut.Iab);

				smallPieceNorms.Add(cut.normalB);
				smallPieceNorms.Add(cut.normalIBC);
				smallPieceNorms.Add(cut.normalIAB);

				bigPieceVerts.Add(cut.A); bigPieceVerts.Add(cut.Iab); bigPieceVerts.Add(cut.Ibc);
				bigPieceVerts.Add(cut.A); bigPieceVerts.Add(cut.Ibc); bigPieceVerts.Add(cut.C);

				bigPieceNorms.Add(cut.normalA);
				bigPieceNorms.Add(cut.normalIAB);
				bigPieceNorms.Add(cut.normalIBC);

				bigPieceNorms.Add(cut.normalA);
				bigPieceNorms.Add(cut.normalIBC);
				bigPieceNorms.Add(cut.normalC);

				break;
			}

			case CutType.BCCA:
			{
				var isSmallPieceAboveCut = Vector3.Dot(cutNormal, cut.C - cutStartPos) > 0f;

				List<Vector3> smallPieceVerts = isSmallPieceAboveCut ? vertsAbove : vertsBelow;
				List<Vector3> smallPieceNorms = isSmallPieceAboveCut ? normsAbove : normsBelow;

				List<Vector3> bigPieceVerts = isSmallPieceAboveCut ? vertsBelow : vertsAbove;
				List<Vector3> bigPieceNorms = isSmallPieceAboveCut ? normsBelow : normsAbove;

				smallPieceVerts.Add(cut.C); smallPieceVerts.Add(cut.Ica); smallPieceVerts.Add(cut.Ibc);
				smallPieceNorms.Add(cut.normalC);
				smallPieceNorms.Add(cut.normalICA);
				smallPieceNorms.Add(cut.normalIBC);

				bigPieceVerts.Add(cut.B); bigPieceVerts.Add(cut.Ibc); bigPieceVerts.Add(cut.Ica);
				bigPieceVerts.Add(cut.B); bigPieceVerts.Add(cut.Ica); bigPieceVerts.Add(cut.A);

				bigPieceNorms.Add(cut.normalB);
				bigPieceNorms.Add(cut.normalIBC);
				bigPieceNorms.Add(cut.normalICA);

				bigPieceNorms.Add(cut.normalB);
				bigPieceNorms.Add(cut.normalICA);
				bigPieceNorms.Add(cut.normalA);

				break;
			}

			default:
				break;
		}
	}

	//TODO remove
	/*
	private static List<GameObject> CreateMeshes(TriIntersections cut, Material material, Transform originalTransform)
	{
		Mesh mesh1 = null;
		Mesh mesh2 = null;

		switch (cut.type)
		{
				case CutType.ABCA:
				{
					mesh1 = CreateSingleTriangleMesh(new Vector3[] { cut.A, cut.Iab, cut.Ica }, cut.normal);
					mesh2 = CreateMultiTriangleMesh(new Vector3[]
							{ cut.C, cut.Ica, cut.Iab,
						cut.C, cut.Iab, cut.B }, cut.normal);
					break;
				}

				case CutType.ABBC:
				{
					mesh1 = CreateSingleTriangleMesh(new Vector3[] { cut.B, cut.Ibc, cut.Iab }, cut.normal);
					mesh2 = CreateMultiTriangleMesh(new Vector3[]
							{ cut.A, cut.Iab, cut.Ibc,
						cut.A, cut.Ibc, cut.C }, cut.normal);
					break;
				}

				case CutType.BCCA:
				{
					mesh1 = CreateSingleTriangleMesh(new Vector3[] { cut.C, cut.Ica, cut.Ibc }, cut.normal);
					mesh2 = CreateMultiTriangleMesh(new Vector3[]
							{ cut.B, cut.Ibc, cut.Ica,
						cut.B, cut.Ica, cut.A }, cut.normal);
					break;
				}

				default:
					break;
		}

		if (mesh1 != null && mesh2 != null)
		{
			var go1 = CreateMeshGameObject(mesh1, "Small bit", material);
			var go2 = CreateMeshGameObject(mesh2, "Larger bit", material);

			go1.transform.rotation = originalTransform.rotation;
			go1.transform.localScale = originalTransform.localScale;

			go2.transform.rotation = originalTransform.rotation;
			go2.transform.localScale = originalTransform.localScale;

			var result = new List<GameObject> { go1, go2 };
			return result;
		}

		return null;
	}
	*/

	private static TriIntersections CalculateIntersections(Vector3[] meshVertices, int[] meshTriangles, Vector3[] meshNormals,
														   int triStartIndex,
														   Vector3 cutStartObjectSpace,
														   Vector3 cutNormal, bool log)
	{
		var result = new TriIntersections();

		var indexA = meshTriangles[triStartIndex];
		var indexB = meshTriangles[triStartIndex + 1];
		var indexC = meshTriangles[triStartIndex + 2];

		var pointA = meshVertices[indexA];
		var pointB = meshVertices[indexB];
		var pointC = meshVertices[indexC];

		// It doesn't really matter where the origin is, the point is that
		// it's a point on the plane. I think. So this could be an optimization.
		var cutPlaneOrigin = cutStartObjectSpace;

		var intersectsAB = TryIntersect(pointA, pointB, cutPlaneOrigin, cutNormal, log, ref result.Iab);
		var intersectsBC = TryIntersect(pointB, pointC, cutPlaneOrigin, cutNormal, log, ref result.Ibc);
		var intersectsCA = TryIntersect(pointC, pointA, cutPlaneOrigin, cutNormal, log, ref result.Ica);

		var intersectCount = 0;

		if (intersectsAB) { intersectCount++; }
		if (intersectsBC) { intersectCount++; }
		if (intersectsCA) { intersectCount++; }

		if (intersectCount == 2)
		{
			if (intersectsAB && intersectsBC) { result.type = CutType.ABBC; }
			else if (intersectsAB && intersectsCA) { result.type = CutType.ABCA; }
			else { result.type = CutType.BCCA; }

			result.A = pointA;
			result.B = pointB;
			result.C = pointC;

			result.normalA = meshNormals[indexA];
			result.normalB = meshNormals[indexB];
			result.normalC = meshNormals[indexC];

			//TODO: actually weigh-blend the normals depending on where the cut is

			result.normalIAB = (result.normalA + result.normalB) / 2f;
			result.normalIBC = (result.normalB + result.normalC) / 2f;
			result.normalICA = (result.normalC + result.normalA) / 2f;
		}

		return result;
	}

	private static bool TryIntersect(Vector3 a, Vector3 b, Vector3 cutPlaneOrigin, Vector3 cutNormal, bool log, ref Vector3 intersectionPoint)
	{
		var doesIntersect = false;

		// If a and b are on the same side of the plane, then there's no intersection.

		var aCut = Vector3.Dot(a - cutPlaneOrigin, cutNormal);
		var bCut = Vector3.Dot(b - cutPlaneOrigin, cutNormal);

		if (Mathf.Sign(aCut) != Mathf.Sign(bCut))
		{
			// "ab" is a ray, such that: some point p = a + (b - a) * t.
			// we're looking for such a t, that:
			// (p - cutPlaneOrigin) . cutNormal == 0

			// If (b-a) . cutNormal == 0, that means that the AB edge is right in the plane of the cut.
			// If that is true, then the sign of aCut and bCut should be the same, so there should be
			// no division by zero. But just for safety:

			var denominator = Vector3.Dot(b - a, cutNormal);

			if (denominator != 0f)
			{
				var t = Vector3.Dot(-a + cutPlaneOrigin, cutNormal) / denominator;

				if (t > 0f && t < 1f)
				{
					intersectionPoint = a + (b - a) * t;
					doesIntersect = true;
				}
			}
			else
			{
				LogIf(log, "Denominator zero");
			}
		}

		return doesIntersect;
	}

	private static Vector3 CalculateCutNormal(Vector3 cutStartWorldSpace, Vector3 cutEndWorldSpace, bool log)
	{
		var delta = (cutEndWorldSpace - cutStartWorldSpace).normalized;
		var toCam = (Camera.main.transform.position - cutStartWorldSpace).normalized;

		return Vector3.Cross(delta, toCam);

		//var rotation = Quaternion.Euler(0f, 0f, 90f);
		//return (rotation * delta).normalized;
	}

	private static void TransformCutToObjectSpace(ref Vector3 cutStartPos, ref Vector3 cutEndPos, ref Vector3 cutNormal,
												  Transform meshTransform)
	{
		var worldToLocal = meshTransform.worldToLocalMatrix;

		cutStartPos = worldToLocal.MultiplyPoint3x4(cutStartPos);
		cutEndPos = worldToLocal.MultiplyPoint3x4(cutEndPos);
		cutNormal = worldToLocal.MultiplyVector(cutNormal);
	}

	private static Mesh CreateMultiTriangleMesh(Vector3[] vertices, Vector3 normal)
	{
		var normals = new Vector3[vertices.Length];
		var triangles = new int[vertices.Length];

		for (int i = 0; i < normals.Length; ++i)
		{
			normals[i] = normal;

			// So we're duplicating vertices here, yes.

			triangles[i] = i;
		}

		var mesh = new Mesh();

		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.normals = normals;

		return mesh;
	}

	public static GameObject CreateMeshGameObject(Mesh mesh, string goName, Material material)
	{
		var go = new GameObject(goName);
		var meshFilter = go.AddComponent<MeshFilter>();
		meshFilter.mesh = mesh;
		var renderer = go.AddComponent<MeshRenderer>();
		renderer.material = material;
		return go;
	}

	//TODO cleanup
	public static Mesh CreateSingleTriangleMesh(IEnumerable<Vector3> points, Vector3 normal)
	{
		var vertices = new Vector3[3];
		var normals = new Vector3[3];
		var triangles = new int[] { 0, 1, 2 };

		var i = 0;

		foreach (var point in points)
		{
			vertices[i] = point;
			normals[i] = normal;

			if (++i >= 3)
			{
				break;
			}
		}

		if (i != 3)
		{
			return null;
		}

		var mesh = new Mesh();

		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.normals = normals;

		mesh.RecalculateNormals();

		return mesh;
	}
}
