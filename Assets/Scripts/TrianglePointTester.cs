using UnityEngine;

public class TrianglePointTester : MonoBehaviour
{
	private Vector3[] _testTriangle = new Vector3[3] { Vector3.zero, Vector3.zero, Vector3.zero };
	private Vector3 _testPoint;
	private bool _isPointInside;

	private int _verticesSetupCount;
	private bool _isTestPointSetup;

	private void Start()
	{
		Debug.Log("Tap Shift + C to clear setup.");
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.C) && (Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift)))
		{
			_verticesSetupCount = 0;
			_isTestPointSetup = false;
		}

		if (Input.GetMouseButtonDown(0))
		{
			if (_verticesSetupCount < 3)
			{
				_testTriangle[_verticesSetupCount++] = GetMousePoint();
			}

		}

		if (Input.GetMouseButton(0) && _verticesSetupCount == 3)
		{
			_testPoint = GetMousePoint();
			_isTestPointSetup = true;
			_isPointInside = MeshUtilities.IsPointInTriangle(_testPoint, _testTriangle[0], _testTriangle[1], _testTriangle[2], true);
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
			Gizmos.color = Color.blue;

			if (_verticesSetupCount < 3)
			{
				for (int i = 0; i < _verticesSetupCount; ++i)
				{
					Gizmos.DrawSphere(_testTriangle[i], 0.2f);
				}
			}
			else
			{
				for (int i = 0; i < 2; ++i)
				{
					Gizmos.DrawLine(_testTriangle[i], _testTriangle[i + 1]);
				}

				Gizmos.DrawLine(_testTriangle[2], _testTriangle[0]);
			}

			if (_isTestPointSetup)
			{
				Gizmos.color = _isPointInside ? Color.green : Color.red;

				for (int i = 0; i < 3; ++i)
				{
					Gizmos.DrawLine(_testTriangle[i], _testPoint);
				}
			}
		}
	}
}
