using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MiniJSON;
using Aircraft;
using System;



public class NetworkClient : MonoBehaviour {

	Dictionary<string, Aircraft.Aircraft> aircrafts = new Dictionary<string, Aircraft.Aircraft>();
	private double current_alt = 172;
	private double current_lat = 0.0;
	private double current_lng = 0.0;
	private double calibration_lat = 0.0;
	private double calibration_lng = 0.0;
	private GameObject myCanvas;
	private int readTime = 0;
	private float timeElapsed = 0.0f;
	private float calibrationAngle = 0f;

	private int deleteInterval = 120;

	private float drawPos = 0.0f;
	private float drawPos2 = -500.0f;
	private int openningCounter = 0;
	bool isOpening = true;

	// Use this for initialization
	void Start() {
		myCanvas = GameObject.Find("Canvas");
		StartCoroutine(getText(true));
	}

	// Update is called once per frame
	void Update() {

		if (isOpening == true) {
			openningEffect();
			return;
		}

		timeElapsed += Time.deltaTime;
		if (timeElapsed >= 0.5f) {
			StartCoroutine(getText(false));
			timeElapsed = 0.0f;
		}


		List<string> deleteList = new List<string>();
		foreach (KeyValuePair<string,Aircraft.Aircraft> kvp in aircrafts) {
			Aircraft.Aircraft ac = aircrafts[kvp.Key];
			if (this.readTime - ac.updateTimestamp > deleteInterval) {
				Destroy(ac.labelObject);
				Destroy(ac.bodyObject);
				Destroy(ac.targetBox);
				deleteList.Add(kvp.Key);
			}
			else {
				ac.calibrationAngle = this.calibrationAngle;
				ac.setWorldPosition();
			}
		}

		foreach(string ac in deleteList) {
			aircrafts.Remove(ac);
		}

		updateInformationLabel();
	}

	IEnumerator getText(bool dbClear) {
		WWW request;
		string url = "";

		if (dbClear == true) {
			url = "http://192.168.10.88:5000/clear";
		}
		else {
			if (this.readTime == 0) {
				url = "http://192.168.10.88:5000/all";
			}
			else {
				url = "http://192.168.10.88:5000/lastupdate/" + readTime;
			}
		}
		request = new WWW(url);
		yield return request;

		if (!string.IsNullOrEmpty(request.error)) {
			Debug.Log(request.error);
		}

		else {
			if (request.responseHeaders.ContainsKey("STATUS") && request.responseHeaders["STATUS"].Contains("200")) {
				string text = request.text;
				IDictionary js = (IDictionary)Json.Deserialize(text);

				var calibrations = (IDictionary)js["Calibration"];
				var currentPos = (IDictionary)calibrations["currentPosition"];
				var calibPos = (IDictionary)calibrations["calibratePosition"];
				this.readTime = int.Parse((string)js["ReadTime"]);
				current_lat = (double)currentPos["latitude"];
				current_lng = (double)currentPos["longitude"];
				calibration_lat = (double)calibPos["latitude"];
				calibration_lng = (double)calibPos["longitude"];
				this.calibrationAngle = (float)this.calibrateDirection();

				var items = (IDictionary)js["Items"];
				var keys = items.Keys;
				foreach (KeyValuePair<string, object> kvp in items as Dictionary<string, object>) {
					var tmpDic = (IDictionary)Json.Deserialize(kvp.Value as string);
					string icao = (string)tmpDic["icao"];
//					if (icao != "8511CA") continue;
					if (!this.aircrafts.ContainsKey(icao)) {
						var newac = new Aircraft.Aircraft();
						newac.canvas = myCanvas;
						newac.calibrationAngle = this.calibrationAngle;
						newac.updateTimestamp = (int)(double)tmpDic["update_time_stamp"];
						newac.icao = icao;
						newac.setOriginPointWithCoordinate(current_lat, current_lng,current_alt);
						newac.bodyObject = this.makeGameObject(icao);
						newac.labelObject = this.makeLabelObject(icao);
						newac.targetBox = this.makeTargetBox(icao);
						aircrafts.Add(icao, newac);
					}

					Aircraft.Aircraft ac = this.aircrafts[icao];
					try {
						var tmpLatitude = (double)tmpDic["latitude"];
						var tmpLongitude = (double)tmpDic["longitude"];
						var tmpAltitude = double.Parse((string)tmpDic["altitude"]);
						var tmpCallsign = (string)tmpDic["callsign"];
						ac.callsign = tmpCallsign;
						ac.setPositionWithCoordinate(tmpLatitude, tmpLongitude, tmpAltitude);
						ac.setTextInfo();
					}
					catch (System.InvalidCastException e) {
						ac.latitude = 0;
						ac.longitude = 0;
						ac.altitude = 0;
					}
				}
			}
		}
	}

	public GameObject makeGameObject(string icao) {
		GameObject obj = GameObject.Find("Cube_Original");
		GameObject newobj = Instantiate(obj, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity);
		newobj.name = icao;
		return newobj;
	}

	public GameObject makeLabelObject(string icao) {
		var canvas = GameObject.Find("Canvas");
		GameObject newobj = new GameObject("Text");
		newobj.transform.parent = canvas.transform;
		newobj.AddComponent<Text>();
		newobj.name = "TEXT_" + icao;
		Text text = newobj.GetComponent<Text>();
		text.fontSize = 10;
		text.alignment = TextAnchor.UpperCenter;
		text.text = "";
		text.font = Resources.FindObjectsOfTypeAll<Font>()[0];
		text.color = new Color(1, 0, 0);
		return newobj;
	}

	public GameObject makeTargetBox(string icao) {
		var canvas = GameObject.Find("Canvas");
		GameObject newobj = new GameObject("Image");
		newobj.transform.parent = canvas.transform;
		newobj.AddComponent<Image>();
		newobj.name = "IMAGE_" + icao;
		Image img = newobj.GetComponent<Image>();
		Sprite sp = Resources.Load<Sprite>("GreenSquare");
		RectTransform rt = newobj.GetComponent<RectTransform>();
		rt.localScale = new Vector3(0.3f, 0.3f, 0.3f);
		img.sprite = sp;
		return newobj;
	}

	public void updateInformationLabel() {
		GameObject obj = GameObject.Find("camDirectionLabel");
		var txtCp = obj.GetComponent<Text>();
		Vector3 camtr = Camera.main.transform.rotation.eulerAngles;
		var angle_h = (camtr.y + this.calibrationAngle) % 360.0;
		if (angle_h < 0) angle_h += 360.0;
		var angle_v = camtr.x - 360.0f;
		String txt = String.Format("H:{0:##} / V:{1:##}", angle_h, angle_v);
		txtCp.text = txt;

		GameObject obj2 = GameObject.Find("numberOfAircraftLabel");
		var txtCp2 = obj2.GetComponent<Text>();
		String txt2 = String.Format("TRK:{0:####}", this.aircrafts.Count);
		txtCp2.text = txt2;
	}

	public double calibrateDirection() {
		var y2 = deg2rad(this.calibration_lat);
		var x2 = deg2rad(this.calibration_lng);
		var y1 = deg2rad(this.current_lat);
		var x1 = deg2rad(this.current_lng);
		var Y = System.Math.Cos(y2) * System.Math.Sin(x2 - x1);
		var X = System.Math.Cos(y1) * System.Math.Sin(y2) - System.Math.Sin(y1) * System.Math.Cos(y2) * System.Math.Cos(x2 - x1);
		var ret = rad2deg(System.Math.Atan2(Y, X)) % 360.0;
		if (ret < 0) { ret += 360.0; }
		return ret;
	}
	public double deg2rad(double degree) {
		return degree * System.Math.PI / 180.0;
	}
	public double rad2deg(double radian) {
		return 180 * radian / System.Math.PI;
	}


	public void openningEffect() {
		if (openningCounter < 50.0) {
			GameObject obj = GameObject.Find("openning");
			GameObject newobj = Instantiate(obj, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity);
			openning op = newobj.GetComponent<openning>();
			newobj.name = "lineObject_upper1_" + openningCounter.ToString();
			op.pos = drawPos;
			op.hight = 20.0f;
			op.DrawLine();

			GameObject newobj2 = Instantiate(obj, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity);
			openning op2 = newobj2.GetComponent<openning>();
			newobj2.name = "lineObject_lower1_" + openningCounter.ToString();
			op2.pos = drawPos;
			op2.hight = -20.0f;
			op2.DrawLine();

			GameObject newobj3 = Instantiate(obj, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity);
			openning op3 = newobj3.GetComponent<openning>();
			newobj3.name = "lineObject_upper2_" + openningCounter.ToString();
			op3.pos = drawPos2;
			op3.direction = true;
			op3.hight = 20.0f;
			op3.DrawLine();

			GameObject newobj4 = Instantiate(obj, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity);
			openning op4 = newobj4.GetComponent<openning>();
			newobj4.name = "lineObject_lower2_" + openningCounter.ToString();
			op4.direction = true;
			op4.pos = drawPos2;
			op4.hight = -20.0f;
			op4.DrawLine();

			drawPos = drawPos + 30.0f;
			drawPos2 = drawPos2 + 30.0f;
		}
		else if (openningCounter < 150.0) {
		}
		else if (openningCounter < 200.0) {
			GameObject obj = GameObject.Find("lineObject_upper1_" + (openningCounter - 150).ToString());
			GameObject obj2 = GameObject.Find("lineObject_lower1_" + (openningCounter - 150).ToString());
			GameObject obj3 = GameObject.Find("lineObject_upper2_" + (openningCounter - 150).ToString());
			GameObject obj4 = GameObject.Find("lineObject_lower2_" + (openningCounter - 150).ToString());
			Destroy(obj);
			Destroy(obj2);
			Destroy(obj3);
			Destroy(obj4);
		}


		if (openningCounter++ > 400.0) isOpening = false;
	}
}
