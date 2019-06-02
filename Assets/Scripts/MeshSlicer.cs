using System.Collections.Generic;
using UnityEngine;

public class MeshSlicer : MonoBehaviour
{
	public bool makePiecesDrop;

	private Vector3 _cutStartPoint;
	private Vector3 _cutEndPoint;

	private bool _hasStartedMarkingTheCut;
	private bool _shouldDrawCutNormal;

	public void CleanupCut()
	{
		_cutStartPoint = Vector3.zero;
		_cutEndPoint = Vector3.zero;
		_hasStartedMarkingTheCut = false;
		_shouldDrawCutNormal = false;
	}

	private void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			_cutStartPoint = GetMousePoint();
			_hasStartedMarkingTheCut = true;
			_shouldDrawCutNormal = false;
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

			if (_shouldDrawCutNormal)
			{
				Gizmos.color = Color.blue;
				Gizmos.DrawLine(_cutStartPoint, _cutStartPoint + MeshUtilities.staticCutNormal);
			}
		}
	}

	private List<GameObject> CollectSliceables()
	{
		var result = new List<GameObject>();
		var foundAlready = new HashSet<Collider>();

		//TODO - should depend on length
		var rayCastCount = 64;
		var cutVector = _cutEndPoint - _cutStartPoint;
		var rayOrigin = Camera.main.transform.position;

		//TODO
		int sliceableLayerMask = 1 << LayerMask.NameToLayer("Sliceable");

		for (int i = 0; i <= rayCastCount; ++i)
		{
			var rayTarget = _cutStartPoint + cutVector * ((float)i / rayCastCount);
			var ray = new Ray(rayOrigin, rayTarget - rayOrigin);
			var hits = Physics.RaycastAll(ray, maxDistance: 120f, layerMask: sliceableLayerMask);

			foreach (var hit in hits)
			{
				if (!foundAlready.Contains(hit.collider))
				{
					foundAlready.Add(hit.collider);
					result.Add(hit.collider.gameObject);
				}
			}
		}

		return result;
	}

	private void MakeTheCut()
	{
		_hasStartedMarkingTheCut = false;

		var gameObjectsToSlice = CollectSliceables();

		foreach (var go in gameObjectsToSlice)
		{
			var mMaterial = go.GetComponent<MeshRenderer>().sharedMaterial;

			var couldSlice = MeshUtilities.SliceMultiTriangleMesh(go, _cutStartPoint, _cutEndPoint, mMaterial, makePiecesDrop, false);
			if (couldSlice)
			{
				Destroy(go);
			}
			else
			{
				Debug.Log("Couldn't slice");
			}

			_shouldDrawCutNormal = true;
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
