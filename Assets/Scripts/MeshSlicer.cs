using UnityEngine;

public class MeshSlicer : MonoBehaviour
{
	public Vector3 pointA;
	public Vector3 pointB;
	public Vector3 pointC;
	public Vector3 normal = new Vector3(0, 0, -1f);

	public Material meshMaterial;

	private Vector3 _cutStartPoint;
	private Vector3 _cutEndPoint;

	private bool _hasStartedMarkingTheCut;

	public void CleanUpCut()
	{
		_cutStartPoint = Vector3.zero;
		_cutEndPoint = Vector3.zero;
		_hasStartedMarkingTheCut = false;
	}

	private void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			_cutStartPoint = GetMousePoint();
			_hasStartedMarkingTheCut = true;
		}

		if (_hasStartedMarkingTheCut && Input.GetMouseButton(0))
		{
			_cutEndPoint = GetMousePoint();
		}

		if (Input.GetMouseButtonUp(0))
		{
			_cutEndPoint = GetMousePoint();
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
	}

	private Vector3 GetMousePoint()
	{
		var screenCoords = Input.mousePosition;
		screenCoords.z = -Camera.main.transform.position.z;
		var worldPoint = Camera.main.ScreenToWorldPoint(screenCoords);
		Debug.Log(worldPoint);
		return worldPoint;
	}
}
