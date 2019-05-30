using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VertexChainTester : MonoBehaviour
{
	private List<MeshUtilities.Edge> _edges = new List<MeshUtilities.Edge>();
	private List<Vector3> _poly = new List<Vector3>();

	private bool _isDone;
	private Vector3 _currentEdgeStart;
	private Vector3 _currentEdgeEnd;

	private static Vector3 GetMousePoint()
	{
		var cam = Camera.main;
		var screenCoords = Input.mousePosition;
		screenCoords.z = -cam.transform.position.z;
		return cam.ScreenToWorldPoint(screenCoords);
	}

	private MeshUtilities.Edge GetLastEdge()
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

	private void EndNewEdge()
	{
		_edges.Add(new MeshUtilities.Edge(_currentEdgeStart, _currentEdgeEnd));
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

		if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
		{
			if (_edges.Count >= 2)
			{
				_edges.Add(new MeshUtilities.Edge(GetLastEdge().to, _edges[0].from));
				_currentEdgeStart = _currentEdgeEnd;

				ShuffleEdgesAndTurnIntoPolygon();
				_isDone = true;
			}
		}
	}

	private void ShuffleEdgesAndTurnIntoPolygon()
	{
		var shuffledIndices = new List<int>(capacity: _edges.Count);
		for (int i = 0; i < _edges.Count; ++i) { shuffledIndices.Add(i); }
		for (int i = 0; i < shuffledIndices.Count - 1; ++i)
		{
			var left = Random.Range(0, i + 1);
			var right = Random.Range(i + 1, shuffledIndices.Count);

			var tmp = shuffledIndices[left];
			shuffledIndices[left] = shuffledIndices[right];
			shuffledIndices[right] = tmp;
		}

		var shuffledEdges = new List<MeshUtilities.Edge>(capacity: _edges.Count);
		foreach (var index in shuffledIndices)
		{
			shuffledEdges.Add(_edges[index]);
		}

		_poly = MeshUtilities.ConnectEdges(shuffledEdges);
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
				DrawPoly();
			}
		}
	}

	private void DrawPoly()
	{
		Gizmos.color = Color.blue;
		for (int i = 0; i < _poly.Count; ++i)
		{
			var next = (i + 1) % _poly.Count;
			Gizmos.DrawLine(_poly[i], _poly[next]);
		}
	}

	private void DrawPendingEdge()
	{
		if (_currentEdgeEnd != _currentEdgeStart)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawLine(_currentEdgeStart, _currentEdgeEnd);
		}
	}

	private void DrawSetEdges()
	{
		Gizmos.color = Color.green;
		foreach (var edge in _edges)
		{
			Gizmos.DrawLine(edge.from, edge.to);
		}
	}

	// So the idea here is this:
	// - put in a point -> edge start
	// - put in another point -> edge end
	// - put in new point -> it will create a new, connecting edge:
	//		- create a separate vertex for the start (with the same coords as the previous end),
	//		  you're free to put the end down
	// - enter to finalize

}
