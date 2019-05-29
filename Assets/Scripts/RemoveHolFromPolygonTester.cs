using System.Collections.Generic;
using UnityEngine;

public class RemoveHolFromPolygonTester : MonoBehaviour
{
	public bool showTris;

	private List<Vector3> _poly = new List<Vector3>();
	private List<Vector3> _hole = new List<Vector3>();
	private List<Vector3> _full;
	private List<Vector3> _tris;
	private bool _isDone;

	private void Start()
	{
		Debug.Log("Add vertices with left click IN CLOCKWISE ORDER for the outer ring, with right click in CCW for the hole. Hit 'Enter' to finish.");
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
				_hole.Add(GetMousePoint());
			}
		}

		if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
		{
			if (_poly.Count >= 3 && _hole.Count >= 3)
			{
				_isDone = true;
				_full = Triangulator.RemoveHoles(_poly, _hole);
				_tris = Triangulator.TriangulatePolygon(_full, new Vector3(0f, 0f, -1f));
			}
		}

		if (Input.GetKeyDown(KeyCode.T))
		{
			showTris = !showTris;
		}

		if (Input.GetKeyDown(KeyCode.C) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
		{
			_isDone = false;

			_poly.Clear();
			_hole.Clear();

			if (_tris != null)
			{
				_tris.Clear();
			}
		}
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
			DrawSpheresForVertices();
		}
	}

	private void DrawPolygon()
	{
		Gizmos.color = Color.green;

		if (showTris)
		{
			for (int i = 0; i < _tris.Count - 2; i += 3)
			{
				Gizmos.DrawLine(_tris[i], _tris[i + 1]);
				Gizmos.DrawLine(_tris[i], _tris[i + 2]);
				Gizmos.DrawLine(_tris[i + 1], _tris[i + 2]);
			}
		}
		else
		{
			Gizmos.color = Color.cyan;
			for (int i = 0; i < _full.Count; ++i)
			{
				var next = (i + 1) % _full.Count;
				Gizmos.DrawLine(_full[i], _full[next]);
			}
		}
	}

	private void DrawSpheresForVertices()
	{
		Gizmos.color = Color.blue;

		foreach (var v in _poly)
		{
			Gizmos.DrawSphere(v, 0.2f);
		}

		Gizmos.color = Color.red;

		foreach (var v in _hole)
		{
			Gizmos.DrawSphere(v, 0.2f);
		}
	}
}
