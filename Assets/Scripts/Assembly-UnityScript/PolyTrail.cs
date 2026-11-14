using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[RequireComponent(typeof(MeshFilter))]
public class PolyTrail : MonoBehaviour
{
	public float height;

	public float time;

	public bool alwaysUp;

	public float minDistance;

	public Color startColor;

	public Color endColor;

	public List<PolyTrailSection> sections;

	public PolyTrail()
	{
		height = 2f;
		time = 2f;
		minDistance = 0.1f;
		startColor = Color.white;
		endColor = new Color(1f, 1f, 1f, 0f);
		sections = new List<PolyTrailSection>();
	}

	public virtual void LateUpdate()
	{
		checked
		{
			while (sections.Count > 0 && Time.time > sections[sections.Count - 1].time + time)
			{
				sections.RemoveAt(sections.Count - 1);
			}
			if (sections.Count == 0 || !((sections[0].point - transform.position).sqrMagnitude <= minDistance * minDistance))
			{
				PolyTrailSection polyTrailSection = new PolyTrailSection();
				polyTrailSection.point = transform.position;
				if (alwaysUp)
				{
					polyTrailSection.upDir = Vector3.up;
				}
				else
				{
					polyTrailSection.upDir = transform.TransformDirection(Vector3.up);
				}
				polyTrailSection.time = Time.time;
				sections.Insert(0, polyTrailSection);
			}
			Mesh mesh = ((MeshFilter)GetComponent(typeof(MeshFilter))).mesh;
			mesh.Clear();
			if (sections.Count < 2)
			{
				return;
			}
			Vector3[] array = new Vector3[sections.Count * 2];
			Color[] array2 = new Color[sections.Count * 2];
			Vector2[] array3 = new Vector2[sections.Count * 2];
			PolyTrailSection polyTrailSection2 = sections[0];
			PolyTrailSection polyTrailSection3 = sections[0];
			Matrix4x4 worldToLocalMatrix = transform.worldToLocalMatrix;
			for (int i = 0; i < sections.Count; i++)
			{
				polyTrailSection2 = polyTrailSection3;
				polyTrailSection3 = sections[i];
				float num = 0f;
				if (i != 0)
				{
					num = Mathf.Clamp01((Time.time - polyTrailSection3.time) / time);
				}
				Vector3 upDir = polyTrailSection3.upDir;
				array[i * 2 + 0] = worldToLocalMatrix.MultiplyPoint(polyTrailSection3.point);
				array[i * 2 + 1] = worldToLocalMatrix.MultiplyPoint(polyTrailSection3.point + upDir * height);
				array3[i * 2 + 0] = new Vector2(num, 0f);
				array3[i * 2 + 1] = new Vector2(num, 1f);
				Color color = Color.Lerp(startColor, endColor, num);
				array2[i * 2 + 0] = color;
				array2[i * 2 + 1] = color;
			}
			int[] array4 = new int[(sections.Count - 1) * 2 * 3];
			for (int i = 0; i < unchecked(array4.Length / 6); i++)
			{
				array4[i * 6 + 0] = i * 2;
				array4[i * 6 + 1] = i * 2 + 1;
				array4[i * 6 + 2] = i * 2 + 2;
				array4[i * 6 + 3] = i * 2 + 2;
				array4[i * 6 + 4] = i * 2 + 1;
				array4[i * 6 + 5] = i * 2 + 3;
			}
			mesh.vertices = array;
			mesh.colors = array2;
			mesh.uv = array3;
			mesh.triangles = array4;
		}
	}

	public virtual void Main()
	{
	}
}
