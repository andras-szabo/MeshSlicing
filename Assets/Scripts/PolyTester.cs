using System.Collections.Generic;
using UnityEngine;

public class PolyTester : MonoBehaviour
{
	private List<Vector3> _poly = new List<Vector3>();
	private bool _isDone;

	private Triangulator.VertexList _convex;
	private Triangulator.VertexList _reflex;

	private void Start()
	{
		Debug.Log("Add vertices with left click, toggle finish by right click. Backspace for clear.");
	}

	private void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			if (!_isDone)
			{
				_poly.Add(GetMousePoint());
			}
		}

		if (Input.GetMouseButtonDown(1))
		{
			if (!_isDone)
			{
				if (_poly.Count >= 3) 
				{ 
					_isDone = true;
					Triangulate();
				}
			}
			else
			{
				_isDone = false;
			}
		}

		if (Input.GetKeyDown(KeyCode.Backspace))
		{
			if (!_isDone && _poly.Count > 0)
			{
				_poly.RemoveAt(_poly.Count - 1);
			}
		}
	}

	private void Triangulate()
	{
		// For now, just indicate convex and reflex vertices

		if (_convex == null || _reflex == null)
		{
			_convex = new Triangulator.VertexList(_poly.Count);
			_reflex = new Triangulator.VertexList(_poly.Count);
		}
		else
		{
			_convex.Clear(_poly.Count);
			_reflex.Clear(_poly.Count);
		}

		var polygonNormal = new Vector3(0f, 0f, -1f);

		Triangulator.SortVerticesByAngleType(_poly, polygonNormal, _convex, _reflex);
	}

	private Vector3 GetMousePoint(bool log = false)
	{
		var screenCoords = Input.mousePosition;
		screenCoords.z = -Camera.main.transform.position.z;
		var worldPoint = Camera.main.ScreenToWorldPoint(screenCoords);
		if (log) { Debug.Log(worldPoint); }
		return worldPoint;
	}

	private void OnDrawGizmos()
	{
		if (Application.isPlaying)
		{
			if (_isDone) { DrawPolygon(); }
			DrawSpheresForVertices(convexColor: Color.green, reflexColor: Color.red);
		}
	}

	private void DrawPolygon()
	{
		Gizmos.color = Color.blue;
		for (int i = 0; i < _poly.Count; ++i)
		{
			Gizmos.DrawLine(_poly[i], _poly[(i + 1) % _poly.Count]);
		}
	}

	private void DrawSpheresForVertices(Color convexColor, Color reflexColor)
	{
		if (!_isDone)
		{
			Gizmos.color = Color.blue;
		}

		for (int i = 0; i < _poly.Count; ++i)
		{
			var point = _poly[i];

			if (_isDone)
			{
				Gizmos.color = _convex.Contains(i) ? convexColor : reflexColor;
			}

			Gizmos.DrawSphere(point, 0.2f);
		}
	}
}
