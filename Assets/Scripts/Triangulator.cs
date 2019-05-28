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

		private int[] _indices;
		private HashSet<int> _indicesSet;
	}

	public static List<Vector3> TriangulatePolygon(List<Vector3> vertices, Vector3 polygonNormal)
	{
		var tris = new List<Vector3>();

		var earIndices = new int[vertices.Count - 2];

		var reflexVertices = new VertexList(vertices.Count);
		var convexVertices = new VertexList(vertices.Count);

		SortVerticesByAngleType(vertices, polygonNormal, convexVertices, reflexVertices);

		return tris;
	}

	public static void SortVerticesByAngleType(List<Vector3> vertices, Vector3 polygonNormal, VertexList convex, VertexList reflex)
	{
		for (int i = 0; i < vertices.Count; ++i)
		{
			var centre = vertices[i];
			var previousIndex = i == 0 ? vertices.Count - 1 : i - 1;
			var nextIndex = i == vertices.Count - 1 ? 0 : i + 1;

			var v1 = centre - vertices[previousIndex];
			var v2 = vertices[nextIndex] - centre;

			var turnVector = Vector3.Cross(v1, v2);

			if (Vector3.Dot(turnVector, polygonNormal) > 0f)
			{
				convex.AddIndex(i);
			}
			else
			{
				reflex.AddIndex(i);
			}
		}
	}

	public static int TryFindEarindices(List<Vector3> vertices, int[] earIndices)
	{
		var earCount = 0;

		return earCount;
	}

}
