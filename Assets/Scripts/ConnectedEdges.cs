using System.Collections.Generic;
using UnityEngine;

public struct Edge
{
	public Edge(Vector3 f, Vector3 t)
	{
		from = f;
		to = t;
	}

	public Vector3 from;
	public Vector3 to;
}

public class ConnectedEdges
{
	public const int INVALID_INDEX = -1;
	public ConnectedEdges(List<Edge> allEdges, int startEdgeIndex)
	{
		_allEdges = allEdges;
		_connectedEdgeSet = new HashSet<int>();
		_nextEdge = new List<int>(capacity: allEdges.Count);

		//TODO this could be optimized away if we kept +1 indices, and treat
		//0 as "invalid"

		for (int i = 0; i < _allEdges.Count; ++i) { _nextEdge.Add(-1); }
		Start(startEdgeIndex);
	}

	public void Start(int startEdgeIndex)
	{
		_connectedEdgeSet.Add(startEdgeIndex);
		_firstConnectedEdgeIndex = startEdgeIndex;
		_lastConnectedEdgeIndex = startEdgeIndex;

		_start = _allEdges[startEdgeIndex].from;
		_end = _allEdges[startEdgeIndex].to;
	}

	public bool Contains(int index)
	{
		return _connectedEdgeSet.Contains(index);
	}

	public bool TryConnect(int index)
	{
		if (_allEdges[index].from == _end)
		{
			_nextEdge[_lastConnectedEdgeIndex] = index;
			_connectedEdgeSet.Add(index);
			_end = _allEdges[index].to;
			_lastConnectedEdgeIndex = index;
			return true;
		}

		if (_allEdges[index].to == _start)
		{
			_nextEdge[index] = _firstConnectedEdgeIndex;
			_connectedEdgeSet.Add(index);
			_start = _allEdges[index].from;
			_firstConnectedEdgeIndex = index;
			return true;
		}

		return false;
	}

	public List<Vector3> GetVerts()
	{
		var verts = new List<Vector3>(capacity: _connectedEdgeSet.Count);

		var currentEdgeIndex = _firstConnectedEdgeIndex;

		verts.Add(_allEdges[_firstConnectedEdgeIndex].from);

		do
		{
			var edge = _allEdges[currentEdgeIndex];
			verts.Add(edge.to);
			currentEdgeIndex = _nextEdge[currentEdgeIndex];
		} while (currentEdgeIndex != INVALID_INDEX);

		return verts;
	}

	private HashSet<int> _connectedEdgeSet;
	private List<int> _nextEdge;
	private List<Edge> _allEdges;

	private int _firstConnectedEdgeIndex;
	private int _lastConnectedEdgeIndex;

	private Vector3 _start;
	private Vector3 _end;
}
