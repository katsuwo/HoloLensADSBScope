using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public  class LineDraw : MonoBehaviour {
	static Material lineMaterial;
	static public int STOROKEMAX = 1000;

	static Dictionary<string, List<Vector3>> strokeSet = new Dictionary<string, List<Vector3>>();
	static Dictionary<string, List<Color>> colorSet = new Dictionary<string, List<Color>>();

	//https://qiita.com/arcsin16/items/7249b272f19e5412ea94
	public void OnRenderObject() {
		// Materialの初期化と設定、詳しくは↑のUnityのScriptReferenceを参照。
		CreateLineMaterial();

		lineMaterial.SetPass(0);

		// GL.PushMatrix()～GL.PopMatrix()の間に行われた、行列マトリクスの変更が外に漏れないようにPush&Pop。おまじない、おまじない
		GL.PushMatrix();

		// 複数の線を描画する
		var keys = strokeSet.Keys;
		foreach (KeyValuePair<string, List<Vector3>> kvp in strokeSet as Dictionary<string, List<Vector3>>) {
			GL.Begin(GL.LINE_STRIP);
			List<Color> colorlist = colorSet[kvp.Key];
			int i = 0;
			foreach (var stroke in kvp.Value) {
				var col = colorlist[i++];
//				GL.Color(new Color( 1.0f, 0.0f ,0.0f,0.5f));
				GL.Color(col);
				GL.Vertex(stroke);
			}
			GL.End();
		}


		for( float r = 10.0f; r <= 1000.0f; r+= 25.0f) {
			GL.Begin(GL.LINE_STRIP);
			for (float l = 0; l <= 360.0; l += 3.0f) {
				var tmpx = (float)(r * System.Math.Sin(deg2rad(l)));
				var tmpy = (float)(r * System.Math.Cos(deg2rad(l)));
				GL.Color(new Color(0.0f, 0.5f, 0.5f, 1.0f));
				GL.Vertex(new Vector3(tmpx, -2.0f, tmpy));
			}
			GL.End();
		}


		for (float l = 0; l <= 360.0; l += 10.0f) {
			GL.Begin(GL.LINE_STRIP);
			var tmpx = (float)(1000.0f * System.Math.Sin(deg2rad(l)));
			var tmpy = (float)(1000.0f * System.Math.Cos(deg2rad(l)));
			GL.Color(new Color(0.0f, 0.5f, 0.5f, 1.0f));
			GL.Vertex(new Vector3(tmpx, -2.0f, tmpy));
			GL.Vertex(new Vector3(0, -2.0f, 0));
			GL.End();
		}


		GL.PopMatrix();
	}

	public void addStrokeSet(string icao) {
		List<Vector3> pointList = new List<Vector3>();
		List<Color> colorList = new List<Color>();
		strokeSet.Add(icao, pointList);
		colorSet.Add(icao, colorList);
	}

	public void addStroke(string icao, Vector3 point, Color color) {
		List<Vector3> pointList = strokeSet[icao];
		List<Color> colorList = colorSet[icao];

		if (pointList.Count >= STOROKEMAX) {
			pointList.RemoveAt(0);
			colorList.RemoveAt(0);
		}
		pointList.Add(point);
		colorList.Add(color);
	}


	public void removeStrokeSet(string icao) {
		strokeSet.Remove(icao);
	}

	static void CreateLineMaterial() {
		if (!lineMaterial) {
			// Unity has a built-in shader that is useful for drawing
			// simple colored things.
			Shader shader = Shader.Find("Hidden/Internal-Colored");
			lineMaterial = new Material(shader);
			lineMaterial.hideFlags = HideFlags.HideAndDontSave;
			// Turn on alpha blending
			lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			// Turn backface culling off
			lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
			// Turn off depth writes
			lineMaterial.SetInt("_ZWrite", 0);
		}
	}
	private double deg2rad(double degree) {
		return degree * System.Math.PI / 180.0;
	}
	private double rad2deg(double radian) {
		return 180 * radian / System.Math.PI;
	}
}
