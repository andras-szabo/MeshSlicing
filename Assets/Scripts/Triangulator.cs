using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Triangulator
{
	public class VertexList
	{
		public int Count { get { return _indicesSet.Count; } }

		public VertexList(int capacity)
		{
			_indices = new int[capacity];
			_indicesSet = new HashSet<int>();
		}

		public void AddIndex(int index)
		{
			_indices[Count] = index;
			_indicesSet.Add(index);
		}

		public bool Contains(int index)
		{
			return _indicesSet.Contains(index);
		}

		public void Clear(int newCapacity)
		{
			if (_indices.Length < newCapacity)
			{
				_indices = new int[newCapacity];
			}

			_indicesSet.Clear();
		}

		public int GetFirst()
		{
			return _indices[0];
		}

		public void RemoveIndex(int indexToRemove)
		{
			_indicesSet.RemoveWhere(i => i == indexToRemove);
			_indicesSet.CopyTo(_indices, 0);
		}

		private int[] _indices;
		private HashSet<int> _indicesSet;
	}

	public class PolyVertex
	{
		public bool isConvex;

		public int prevIndex;
		public int currIndex;
		public int nextIndex;

		public bool wasRemoved;
	}

	public static List<Vector3> TriangulatePolygon(List<Vector3> vertices, Vector3 polygonNormal)
	{
		var tris = new List<Vector3>();

		if (vertices.Count < 3)
		{
			tris.AddRange(vertices);
			return tris;
		}

		var earIndices = new VertexList(vertices.Count);
		var reflexVertices = new VertexList(vertices.Count);
		var convexVertices = new VertexList(vertices.Count);

		var polyVerts = new List<PolyVertex>(vertices.Count);

		SortVerticesByAngleType(vertices, polygonNormal, convexVertices, reflexVertices);
		SetupPolyVerts(vertices, polyVerts, convexVertices);
		TryFindEarindices(vertices, polyVerts, earIndices);

		// Start chopping off ears.

		var untouchedPolyVertCount = polyVerts.Count;

		while (untouchedPolyVertCount > 3)
		{
			var earTipPolyVertIndex = earIndices.GetFirst();

			var earTip = polyVerts[earTipPolyVertIndex];
			var prev = polyVerts[earTip.prevIndex];
			var next = polyVerts[earTip.nextIndex];

			tris.Add(vertices[prev.currIndex]);
			tris.Add(vertices[earTip.currIndex]);
			tris.Add(vertices[next.currIndex]);

			earIndices.RemoveIndex(earTipPolyVertIndex);
			earTip.wasRemoved = true;

			prev.nextIndex = earTip.nextIndex;
			next.prevIndex = earTip.prevIndex;

			CheckAfterEarCutoff(vertices, polyVerts, prev, polygonNormal, earIndices);
			CheckAfterEarCutoff(vertices, polyVerts, next, polygonNormal, earIndices);

			untouchedPolyVertCount--;
		}

		var arTipPolyVertIndex = earIndices.GetFirst();

		var arTip = polyVerts[arTipPolyVertIndex];
		var rev = polyVerts[arTip.prevIndex];
		var ext = polyVerts[arTip.nextIndex];

		tris.Add(vertices[rev.currIndex]);
		tris.Add(vertices[arTip.currIndex]);
		tris.Add(vertices[ext.currIndex]);

		earIndices.RemoveIndex(arTipPolyVertIndex);

		return tris;
	}

	public static void CheckAfterEarCutoff(List<Vector3> vertices, List<PolyVertex> pvs, PolyVertex pv, Vector3 polygonNormal,
										   VertexList ears)
	{
		// 1.) if prev was convex, do nothing.
		// 2.) if prev was reflex, re-check, and modify convex / reflex lists.

		if (!pv.isConvex)
		{
			var prev = vertices[pv.prevIndex];
			var tip = vertices[pv.currIndex];
			var next = vertices[pv.nextIndex];

			pv.isConvex = IsFacingSameAsPolyNormal(prev, tip, next, polygonNormal);

			// If we kept separate lists of conv and refl, they should get modified here.
			// TODO: clean those up - we don't really need them actually.
		}

		if (pv.isConvex)
		{
			var isEar = IsEarTip(vertices, pvs, pv);
			var weKnowItsAnEar = ears.Contains(pv.currIndex);

			if (isEar != weKnowItsAnEar)
			{
				if (isEar) { ears.AddIndex(pv.currIndex); }
				else { ears.RemoveIndex(pv.currIndex); }
			}
		}

		// 3.) if (re-checked) prev is convex, then check if it's an ear.
		//		- if it's an ear, and it's not on the list, add it to the ear list.
		//		- it it's not an ear, but it's on the list, remove it from the ear list, tho can this happen?

	}

	public static void SetupPolyVerts(List<Vector3> vertices, List<PolyVertex> polyVerts, VertexList convex)
	{
		for (int i = 0; i < vertices.Count; ++i)
		{
			var curr = new PolyVertex();

			curr.currIndex = i;
			curr.prevIndex = i == 0 ? vertices.Count - 1 : i - 1;
			curr.nextIndex = (i + 1) % vertices.Count;
			curr.isConvex = convex.Contains(i);

			polyVerts.Add(curr);
		}
	}

	public static void SortVerticesByAngleType(List<Vector3> vertices, Vector3 polygonNormal, VertexList convex, VertexList reflex)
	{
		for (int i = 0; i < vertices.Count; ++i)
		{
			var centre = vertices[i];
			var previousIndex = i == 0 ? vertices.Count - 1 : i - 1;
			var nextIndex = i == vertices.Count - 1 ? 0 : i + 1;

			if (IsFacingSameAsPolyNormal(vertices[previousIndex], centre, vertices[nextIndex], polygonNormal))
			{
				convex.AddIndex(i);
			}
			else
			{
				reflex.AddIndex(i);
			}
		}
	}

	public static bool IsFacingSameAsPolyNormal(Vector3 prev, Vector3 tip, Vector3 next, Vector3 polygonNormal)
	{
		var v1 = tip - prev;
		var v2 = next - tip;

		var turnVector = Vector3.Cross(v1, v2);
		return Vector3.Dot(turnVector, polygonNormal) > 0f;
	}

	public static int TryFindEarindices(List<Vector3> vertices, List<PolyVertex> polyVertices, VertexList earIndices)
	{
		foreach (var v in polyVertices)
		{
			if (IsEarTip(vertices, polyVertices, v))
			{
				earIndices.AddIndex(v.currIndex);
			}
		}

		return earIndices.Count;
	}

	public static bool IsEarTip(List<Vector3> vertices, List<PolyVertex> pvs, PolyVertex pv)
	{
		if (pv.wasRemoved || !pv.isConvex)
		{
			return false;
		}

		var prev = pv.prevIndex;
		var curr = pv.currIndex;
		var next = pv.nextIndex;

		var prevVert = vertices[prev];
		var currVert = vertices[curr];
		var nextVert = vertices[next];

		var isEar = true;

		foreach (var p in pvs)
		{
			if (!p.wasRemoved && !p.isConvex && p.currIndex != prev && p.currIndex != curr && p.currIndex != next)
			{
				if (MeshUtilities.IsPointInTriangle(vertices[p.currIndex], prevVert, currVert, nextVert, true))
				{
					isEar = false;
					break;
				}
			}
		}

		return isEar;
	}
}
