using System.Collections.Generic;
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

		public Vector3 Iab;
		public Vector3 Ibc;
		public Vector3 Ica;

		public CutType type;
		public Vector3 normal;
	}

	public static void LogIf(bool log, string msg)
	{
		if (log) { Debug.LogWarning(msg); }
	}

	public static Vector3 staticCutNormal;

	public static bool SliceSingleTriangleMesh(GameObject meshGO, Vector3 cutStartPos, Vector3 cutEndPos, Material material, bool log = true)
	{
		var cutNormal = CalculateCutNormal(cutStartPos, cutEndPos, log);
		LogIf(log, string.Format("Cut normal: {0}", cutNormal));
		staticCutNormal = cutNormal;

		var meshTransform = meshGO.transform;
		TransformCutToObjectSpace(ref cutStartPos, ref cutEndPos, ref cutNormal, meshTransform);
		LogIf(log, string.Format("Cut: {0} -> {1}; normal: {2}", cutStartPos, cutEndPos, cutNormal));

		var mesh = meshGO.GetComponent<MeshFilter>().sharedMesh;
		var intersections = CalculateIntersections(mesh, cutStartPos, cutEndPos, cutNormal, log);

		if (intersections.type != CutType.None)
		{
			var meshGOs = CreateMeshes(intersections, material, meshGO.transform);
			foreach (var go in meshGOs)
			{
				var collider = go.AddComponent<MeshCollider>();
				collider.convex = true;

				var rigidbody = go.AddComponent<Rigidbody>();
				rigidbody.useGravity = true;
				rigidbody.isKinematic = false;
			}

			return true;
		}

		return false;
	}

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


	private static TriIntersections CalculateIntersections(Mesh mesh, Vector3 cutStartObjectSpace, Vector3 cutEndObjectSpace, Vector3 cutNormal, bool log)
	{
		var result = new TriIntersections();

		var indexA = mesh.triangles[0];
		var indexB = mesh.triangles[1];
		var indexC = mesh.triangles[2];

		var pointA = mesh.vertices[indexA];
		var pointB = mesh.vertices[indexB];
		var pointC = mesh.vertices[indexC];

		// It doesn't really matter where the origin is, the point is that
		// it's a point on the plane. I think. So this could be an optimization.
		var cutPlaneOrigin = cutStartObjectSpace;

		var intersectsAB = TryIntersect(pointA, pointB, cutPlaneOrigin, cutNormal, log, ref result.Iab);
		var intersectsBC = TryIntersect(pointB, pointC, cutPlaneOrigin, cutNormal, log, ref result.Ibc);
		var intersectsCA = TryIntersect(pointC, pointA, cutPlaneOrigin, cutNormal, log, ref result.Ica);

		var intersectCount = 0;

		if (intersectsAB) { intersectCount++; LogIf(log, "Intersects AB"); }
		if (intersectsBC) { intersectCount++; LogIf(log, "Intersects BC"); }
		if (intersectsCA) { intersectCount++; LogIf(log, "Intersects CA"); }

		if (intersectCount == 2)
		{
			if (intersectsAB && intersectsBC) { result.type = CutType.ABBC; }
			else if (intersectsAB && intersectsCA) { result.type = CutType.ABCA; }
			else { result.type = CutType.BCCA; }

			result.A = pointA;
			result.B = pointB;
			result.C = pointC;

			result.normal = mesh.normals[0];
		}

		LogIf(log, string.Format("Intersection count: {0}", intersectCount));

		return result;
	}

	private static bool TryIntersect(Vector3 a, Vector3 b, Vector3 cutPlaneOrigin, Vector3 cutNormal, bool log, ref Vector3 intersectionPoint)
	{
		var doesIntersect = false;

		// If a and b are on the same side of the plane, then there's no intersection.

		var aCut = Vector3.Dot(a - cutPlaneOrigin, cutNormal);
		var bCut = Vector3.Dot(b - cutPlaneOrigin, cutNormal);

		LogIf(log, string.Format("TryIntersect: aCut: {0}, bCut: {1}", aCut, bCut));

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
				intersectionPoint = a + (b - a) * t;
				doesIntersect = true;
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
		var delta = cutEndWorldSpace - cutStartWorldSpace;
		LogIf(log, string.Format("Delta: {0}", delta));
		var rotation = Quaternion.Euler(0f, 0f, 90f);
		return (rotation * delta).normalized;
	}

	private static void TransformCutToObjectSpace(ref Vector3 cutStartWorldPos, ref Vector3 cutEndWorldPos, ref Vector3 cutNormal, 
												  Transform meshTransform)
	{
		var worldToLocal = meshTransform.worldToLocalMatrix;
		cutStartWorldPos = worldToLocal.MultiplyPoint3x4(cutStartWorldPos);
		cutEndWorldPos = worldToLocal.MultiplyPoint3x4(cutEndWorldPos);
		cutNormal = worldToLocal.MultiplyPoint3x4(cutNormal);
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
