using System.Collections.Generic;
using UnityEngine;

public class VertexChainTester : MonoBehaviour
{
	private List<Edge> _edges = new List<Edge>();
	private List<Edge> _holeEdges = new List<Edge>();

	private List<Vector3> _polyA = new List<Vector3>();
	private List<Vector3> _polyB = new List<Vector3>();
	private bool _aIsHole = false;
	private bool _bIsHole = false;

	private bool _isDone;
	private Vector3 _currentEdgeStart;
	private Vector3 _currentEdgeEnd;

	private Vector3 _currHoleEdgeStart;
	private Vector3 _currHoleEdgeEnd;

	private static Vector3 GetMousePoint()
	{
		var cam = Camera.main;
		var screenCoords = Input.mousePosition;
		screenCoords.z = -cam.transform.position.z;
		return cam.ScreenToWorldPoint(screenCoords);
	}

	private Edge GetLastEdge()
	{
		return _edges[_edges.Count - 1];
	}

	private void StartNewEdge(Vector3 mousePoint)
	{
		if (_edges.Count == 0)
		{
			_currentEdgeStart = mousePoint;
		}
		else
		{
			_currentEdgeStart = GetLastEdge().to;
			_currentEdgeEnd = mousePoint;
		}
	}

	private void StartNewHoleEdge(Vector3 mousePoint)
	{
		if (_holeEdges.Count == 0)
		{
			_currHoleEdgeStart = mousePoint;
		}
		else
		{
			_currHoleEdgeStart = _holeEdges[_holeEdges.Count - 1].to;
			_currHoleEdgeEnd = mousePoint;
		}
	}

	private void EndNewHoleEdge()
	{
		_holeEdges.Add(new Edge(_currHoleEdgeStart, _currHoleEdgeEnd));
	}

	private void EndNewEdge()
	{
		_edges.Add(new Edge(_currentEdgeStart, _currentEdgeEnd));
	}

	private void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			StartNewEdge(GetMousePoint());
		}
		else
		{
			if (Input.GetMouseButton(0))
			{
				_currentEdgeEnd = GetMousePoint();
			}
		}

		if (Input.GetMouseButtonUp(0))
		{
			EndNewEdge();
		}

		if (Input.GetMouseButtonDown(1)) { StartNewHoleEdge(GetMousePoint()); }
		else if (Input.GetMouseButton(1)) { _currHoleEdgeEnd = GetMousePoint(); }
		if (Input.GetMouseButtonUp(1)) { EndNewHoleEdge(); }

		if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
		{
			if (_edges.Count >= 2)
			{
				_edges.Add(new Edge(GetLastEdge().to, _edges[0].from));
				_currentEdgeStart = _currentEdgeEnd;

				if (_holeEdges.Count >= 2)
				{
					_holeEdges.Add(new Edge(_holeEdges[_holeEdges.Count - 1].to, _holeEdges[0].from));
					_currHoleEdgeEnd = _currHoleEdgeStart;
				}

				ShuffleEdgesAndTurnIntoPolygon();
				_isDone = true;
			}
		}

		if (Input.GetKeyDown(KeyCode.C) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
		{
			_isDone = false;

			_edges.Clear();
			_holeEdges.Clear();
			_currentEdgeEnd = _currentEdgeStart;
			_currHoleEdgeEnd = _currHoleEdgeStart;
		}
	}

	private void ShuffleEdgesAndTurnIntoPolygon()
	{
		var totalEdgeCount = _edges.Count + _holeEdges.Count;
		var shuffledIndices = new List<int>(capacity: totalEdgeCount);

		for (int i = 0; i < totalEdgeCount; ++i) { shuffledIndices.Add(i); }
		for (int i = 0; i < shuffledIndices.Count - 1; ++i)
		{
			var left = Random.Range(0, i + 1);
			var right = Random.Range(i + 1, shuffledIndices.Count);

			var tmp = shuffledIndices[left];
			shuffledIndices[left] = shuffledIndices[right];
			shuffledIndices[right] = tmp;
		}

		var shuffledEdges = new List<Edge>(capacity: totalEdgeCount);

		foreach (var index in shuffledIndices)
		{
			if (index < _edges.Count)
			{
				shuffledEdges.Add(_edges[index]);
			}
			else
			{
				shuffledEdges.Add(_holeEdges[index - _edges.Count]);
			}
		}

		var polys = MeshUtilities.ConnectEdges(shuffledEdges);

		_polyA = polys[0];
		_aIsHole = false;

		if (polys.Count > 1)
		{
			_polyB = polys[1];

			_aIsHole = MeshUtilities.IsSecondHoleInFirst(_polyB, _polyA);
			_bIsHole = MeshUtilities.IsSecondHoleInFirst(_polyA, _polyB);
		}
	}

	private void OnDrawGizmos()
	{
		if (Application.isPlaying)
		{
			if (!_isDone)
			{
				DrawSetEdges();
				DrawPendingEdge();
			}
			else
			{
				DrawPoly(_polyA, _aIsHole);
				DrawPoly(_polyB, _bIsHole);
			}
		}
	}

	private void DrawPoly(List<Vector3> poly, bool isHole)
	{
		Gizmos.color = isHole ? Color.blue : Color.green;

		for (int i = 0; i < poly.Count; ++i)
		{
			var next = (i + 1) % poly.Count;
			Gizmos.DrawLine(poly[i], poly[next]);
		}
	}

	private void DrawPendingEdge()
	{
		if (_currentEdgeEnd != _currentEdgeStart)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawLine(_currentEdgeStart, _currentEdgeEnd);
		}

		if (_currHoleEdgeEnd != _currHoleEdgeStart)
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawLine(_currHoleEdgeStart, _currHoleEdgeEnd);
		}
	}

	private void DrawSetEdges()
	{
		Gizmos.color = Color.green;
		foreach (var edge in _edges)
		{
			Gizmos.DrawLine(edge.from, edge.to);
		}

		Gizmos.color = Color.cyan;
		foreach (var edge in _holeEdges)
		{
			Gizmos.DrawLine(edge.from, edge.to);
		}
	}
}

