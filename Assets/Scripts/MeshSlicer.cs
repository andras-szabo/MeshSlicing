using UnityEngine;

public class MeshSlicer : MonoBehaviour
{
	public Vector3 pointA;
	public Vector3 pointB;
	public Vector3 pointC;
	public Vector3 normal = new Vector3(0, 0, -1f);

	public Material meshMaterial;

	public GameObject meshGameObjectToSlice;

	private Vector3 _cutStartPoint;
	private Vector3 _cutEndPoint;

	private bool _hasStartedMarkingTheCut;

	public void CleanupCut()
	{
		_cutStartPoint = Vector3.zero;
		_cutEndPoint = Vector3.zero;
		_hasStartedMarkingTheCut = false;
	}

	private void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			_cutStartPoint = GetMousePoint(log: true);
			_hasStartedMarkingTheCut = true;
		}

		if (_hasStartedMarkingTheCut && Input.GetMouseButton(0))
		{
			_cutEndPoint = GetMousePoint();
		}

		if (Input.GetMouseButtonUp(0))
		{
			_cutEndPoint = GetMousePoint(log: true);
			MakeTheCut();
		}
	}

	private void OnDrawGizmos()
	{
		if (Application.isPlaying && _cutStartPoint != _cutEndPoint)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawLine(_cutStartPoint, _cutEndPoint);
		}
	}

	private void MakeTheCut()
	{
		_hasStartedMarkingTheCut = false;

		if (meshGameObjectToSlice != null)
		{
			MeshUtilities.SliceSingleTriangleMesh(meshGameObjectToSlice, _cutStartPoint, _cutEndPoint);
		}
		else
		{
			Debug.LogWarning("No mesh gameObject to slice.");
		}
	}

	private Vector3 GetMousePoint(bool log = false)
	{
		var screenCoords = Input.mousePosition;
		screenCoords.z = -Camera.main.transform.position.z;
		var worldPoint = Camera.main.ScreenToWorldPoint(screenCoords);
		if (log)
		{
			Debug.Log(worldPoint);
		}
		return worldPoint;
	}
}
