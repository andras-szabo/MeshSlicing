﻿using System.Collections.Generic;
using UnityEngine;

public static class MeshUtilities
{
	public static void LogIf(bool log, string msg)
	{
		if (log) { Debug.LogWarning(msg); }
	}

	public static void SliceSingleTriangleMesh(GameObject meshGO, Vector3 cutStartWorldPos, Vector3 cutEndWorldPos, bool log = true)
	{
		var meshTransform = meshGO.transform;
		TransformCutToObjectSpace(ref cutStartWorldPos, ref cutEndWorldPos, meshTransform);
		LogIf(log, string.Format("Cut: {0} -> {1}", cutStartWorldPos, cutEndWorldPos));

		var cutNormal = CalculateCutNormal(cutStartWorldPos, cutEndWorldPos, meshTransform.forward, log);
		LogIf(log, string.Format("Cut normal: {0}", cutNormal));
	}

	private static Vector3 CalculateCutNormal(Vector3 cutStartObjectSpace, Vector3 cutEndObjectSpace, Vector3 localForward, bool log)
	{
		var delta = cutEndObjectSpace - cutStartObjectSpace;
		LogIf(log, string.Format("Delta: {0}", delta));
		var rotation = Quaternion.AngleAxis(90f, localForward);
		return rotation * delta;
	}

	private static void TransformCutToObjectSpace(ref Vector3 cutStartWorldPos, ref Vector3 cutEndWorldPos, Transform meshTransform)
	{
		var worldToLocal = meshTransform.worldToLocalMatrix;
		cutStartWorldPos = worldToLocal.MultiplyPoint3x4(cutStartWorldPos);
		cutEndWorldPos = worldToLocal.MultiplyPoint3x4(cutEndWorldPos);
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

		mesh.RecalculateNormals();

		return mesh;
	}
}
