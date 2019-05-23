using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MeshSlicer))]
public class MeshSlicerEditor : Editor
{
	private static int _goCounter = 0;
	private static List<GameObject> _createdGameObjects = new List<GameObject>();
	private float _randomMin = -2.5f;
	private float _randomMax = 2.5f;

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		EditorGUILayout.MinMaxSlider(ref _randomMin, ref _randomMax, -5f, 5f);

		if (GUILayout.Button("Create default"))
		{
			var slicer = (MeshSlicer)target;
			Cleanup(slicer);
			ResetDefault(slicer);
			CreateSingleTriangleMesh(slicer);
		}

		if (GUILayout.Button("Create random"))
		{
			var slicer = (MeshSlicer)target;
			Cleanup(slicer);
			RandomizePoints(slicer, _randomMin, _randomMax);
			CreateSingleTriangleMesh(slicer);
		}

		EditorGUILayout.Separator();

		if (GUILayout.Button("Cleanup"))
		{
			var slicer = (MeshSlicer)target;
			Cleanup(slicer);
			slicer.meshGameObjectToSlice = null;
		}
	}

	private void ResetDefault(MeshSlicer slicer)
	{
		slicer.pointA = new Vector3(-1f, 1f, 0f);
		slicer.pointB = new Vector3(1f, 1f, 0f);
		slicer.pointC = new Vector3(-1f, -1f, 0f);
	}

	private void Cleanup(MeshSlicer slicer)
	{
		foreach (var go in _createdGameObjects)
		{
			UnityEngine.Object.DestroyImmediate(go);
		}

		_createdGameObjects.Clear();

		if (Application.isPlaying)
		{
			slicer.CleanupCut();
		}
	}

	private void RandomizePoints(MeshSlicer slicer, float rangeMin, float rangeMax)
	{
		slicer.pointA = GetRandomPoint(rangeMin, rangeMax, 0f);
		slicer.pointB = GetRandomPoint(rangeMin, rangeMax, 0f);
		slicer.pointC = GetRandomPoint(rangeMin, rangeMax, 0f);
	}

	private Vector3 GetRandomPoint(float min, float max, float z)
	{
		return new Vector3(Random.Range(min, max), Random.Range(min, max), z);
	}

	private void CreateSingleTriangleMesh(MeshSlicer slicer)
	{
		var points = new Vector3[] { slicer.pointA, slicer.pointB, slicer.pointC };
		var mesh = MeshUtilities.CreateSingleTriangleMesh(points, slicer.normal);

		var go = new GameObject(string.Format("GO_{0}", _goCounter++));
		var meshFilter = go.AddComponent<MeshFilter>();
		meshFilter.mesh = mesh;
		var renderer = go.AddComponent<MeshRenderer>();
		renderer.material = slicer.meshMaterial;

		_createdGameObjects.Add(go);
		slicer.meshGameObjectToSlice = go;
	}
}
