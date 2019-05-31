using System.Collections.Generic;
using UnityEngine;

public static class Triangulator
{
	//TODO - this could be optimized- it creates a lot of garbage
	//		 with the HashSet.
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
			_indicesSet.Remove(indexToRemove);
			_indicesSet.CopyTo(_indices, 0);
		}

		public void Print(string prefix)
		{
			var sb = new System.Text.StringBuilder();
			sb.Append(prefix);
			for (int i = 0; i < _indicesSet.Count; ++i)
			{
				sb.AppendFormat(" {0};", _indices[i]);
			}
			Debug.LogWarning(sb.ToString());
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

	public static List<Vector3> RemoveHoles(List<Vector3> outerVerticesCW, List<Vector3> innerVerticesCCW)
	{
		var verts = new List<Vector3>(capacity: outerVerticesCW.Count + 1 + innerVerticesCCW.Count);

		var solutionFound = false;
		var foundOuterIndex = -1;
		var foundInnerIndex = -1;
		var outerVertexCount = outerVerticesCW.Count;
		var innerVertexCount = innerVerticesCCW.Count;

		for (int outerIndex = 0; !solutionFound && outerIndex < outerVertexCount; ++outerIndex)
		{
			var rayStart = outerVerticesCW[outerIndex];

			for (int innerIndex = 0; !solutionFound && innerIndex < innerVertexCount; ++innerIndex)
			{
				if (DoesIntersect(rayStart, innerVerticesCCW[innerIndex], innerVerticesCCW) == 0 &&
					DoesIntersect(rayStart, innerVerticesCCW[innerIndex], outerVerticesCW) == 0)
				{
					solutionFound = true;
					foundOuterIndex = outerIndex;
					foundInnerIndex = innerIndex;
				}
			}
		}

		if (solutionFound)
		{
			for (int i = 0; i < outerVerticesCW.Count; ++i)
			{
				verts.Add(outerVerticesCW[i]);

				if (i == foundOuterIndex)
				{
					for (int j = 0; j < innerVerticesCCW.Count; ++j)
					{
						var nextInnerIndexToCopy = (foundInnerIndex + j) % innerVerticesCCW.Count;
						verts.Add(innerVerticesCCW[nextInnerIndexToCopy]);
					}

					verts.Add(innerVerticesCCW[foundInnerIndex]);
					verts.Add(outerVerticesCW[i]);
				}
			}
		}
		else
		{
			Debug.LogWarning("No solution found");
		}

		return verts;
	}

	// Does the line segment from rayStart to rayEnd intersect any of the line segments
	// in polygonSegments? (where polygonSegments is expected to be a p0->p1->p2->p3->...->p0 chain)
	public static int DoesIntersect(Vector3 rayStart, Vector3 rayEnd, List<Vector3> polygonSegments, 
									bool countAllIntersections = false,
									bool treatRayAsUnbounded = false)
	{
		// Parberry-Dunn, pp722-723
		var p1 = rayStart;
		var d1 = rayEnd - rayStart;
		var polyVertexCount = polygonSegments.Count;

		var intersectionCount = 0;

		for (int i = 0; i < polyVertexCount; ++i)
		{
			var nextIndex = (i + 1) % polyVertexCount;
			var p2 = polygonSegments[i];
			var d2 = polygonSegments[nextIndex] - p2;
			var d1xd2 = Vector3.Cross(d1, d2);
			var denom = Vector3.Dot(d1xd2, d1xd2);
			if (!Mathf.Approximately(denom, 0f))
			{
				var t1 = Vector3.Dot(Vector3.Cross(p2 - p1, d2), d1xd2) / denom;
				if (0f < t1 && (treatRayAsUnbounded || t1 < 1f))
				{
					var t2 = Vector3.Dot(Vector3.Cross(p2 - p1, d1), d1xd2) / denom;
					if (0f < t2 && t2 < 1f)
					{
						intersectionCount++;
						if (!countAllIntersections)
						{
							return intersectionCount;
						}
					}
				}
			}
		}

		return intersectionCount;
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
		var polyVerts = new List<PolyVertex>(vertices.Count);

		SetupPolyVerts(vertices, polyVerts, polygonNormal);
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

		return tris;
	}

	public static void CheckAfterEarCutoff(List<Vector3> vertices, List<PolyVertex> pvs, PolyVertex pv, Vector3 polygonNormal,
										   VertexList ears)
	{
		// 1.) if prev was convex, do nothing.
		// 2.) if prev was reflex, re-check, and modify convex / reflex lists.
		// 3.) if convex, update its ear status.

		if (!pv.isConvex)
		{
			var prev = vertices[pv.prevIndex];
			var tip = vertices[pv.currIndex];
			var next = vertices[pv.nextIndex];

			pv.isConvex = IsFacingSameAsPolyNormal(prev, tip, next, polygonNormal);
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
	}

	public static void SetupPolyVerts(List<Vector3> vertices, List<PolyVertex> polyVerts, Vector3 polygonNormal)
	{
		for (int i = 0; i < vertices.Count; ++i)
		{
			var curr = new PolyVertex();

			curr.currIndex = i;
			curr.prevIndex = i == 0 ? vertices.Count - 1 : i - 1;
			curr.nextIndex = (i + 1) % vertices.Count;

			curr.isConvex = IsFacingSameAsPolyNormal(vertices[curr.prevIndex], vertices[curr.currIndex], vertices[curr.nextIndex], polygonNormal);

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
		return Vector3.Dot(turnVector, polygonNormal) >= 0f;
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
				if (MeshUtilities.IsPointInTriangle(vertices[p.currIndex], prevVert, currVert, nextVert, true, true))
				{
					isEar = false;
					break;
				}
			}
		}

		return isEar;
	}
}
