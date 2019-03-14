﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MiniJSON;
using Aircraft;
using System;

public class NetworkClient : MonoBehaviour {

	Dictionary<string, Aircraft.Aircraft> aircrafts = new Dictionary<string, Aircraft.Aircraft>();
	private double current_alt = 0;
	private double current_lat = 0.0;
	private double current_lng = 0.0;
	private double calibration_lat = 0.0;
	private double calibration_lng = 0.0;
	private GameObject myCanvas;
	private double readTime = 0;
	private float timeElapsed = 0.0f;
	private float calibrationAngle = 0f;

	private int deleteInterval = 180;

	private float drawPos = 0.0f;
	private float drawPos2 = -500.0f;
	private int openningCounter = 0;
	bool isOpening = true;

	private float dumangle = 0.0f;
	public GameObject linedrawObj = null;
	private LineDraw ld = null;
	public string SERVERADDRESS = "192.168.10.88";
	private int framCounter = 0;
	GameObject N_Object;
	GameObject E_Object;
	GameObject S_Object;
	GameObject W_Object;

	private int LINERESERVETIME = 5 * 60 * 60;
	private bool dataRequest = true;

	void Start() {
		//Destroy camera of Calibrate Scene
		GameObject cam1 = GameObject.Find("MixedRealityCameraCalibrateScene");
		Destroy(cam1);
	
		myCanvas = GameObject.Find("InfoCanvas");
		myCanvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
		myCanvas.transform.localEulerAngles = new Vector3(0, 0, 0);
		StartCoroutine(RequestAircraftDatas(true));
		linedrawObj = GameObject.Find("LineDraw");
		ld = (LineDraw)linedrawObj.GetComponent<LineDraw>();
		ld.addStrokeSet("CAMERA");
		N_Object = GameObject.Find("N_Text");
		S_Object = GameObject.Find("S_Text");
		E_Object = GameObject.Find("E_Text");
		W_Object = GameObject.Find("W_Text");
	}

	// Update is called once per frame
	void Update() {
#if false
		if (isOpening == true) {
			openningEffect();
			return;
		}
#endif

		framCounter++;
		timeElapsed += Time.deltaTime;
#if true
		if (this.dataRequest == true) {
			Debug.Log("__REQ__");
			StartCoroutine(RequestAircraftDatas(false));
		}
#else
		if (timeElapsed >= 0.25f) {
			StartCoroutine(RequestAircraftDatas(false));
			timeElapsed = 0.0f;
			Vector3 camPosition = Camera.main.transform.position;
			ld.addStroke("CAMERA", new Vector3(camPosition.x, camPosition.y - 1, camPosition.z), new Color(0.5f, 0.5f, 0, 0.3f));
		}
#endif

		List<string> deleteList = new List<string>();
		foreach (KeyValuePair<string, Aircraft.Aircraft> kvp in aircrafts) {
			Aircraft.Aircraft ac = aircrafts[kvp.Key];
			
			//10秒に一度、消去が必要かチェック	
			if ((framCounter % 600 == 0) && (this.readTime - ac.updateTimestamp > deleteInterval)) {
				this.playDisapearSound();
				Destroy(ac.bodyObject);
				Destroy(ac.labelObject);
				Destroy(ac.targetBox);
				deleteList.Add(kvp.Key);
			}
			else {
				ac.calibrationAngle = this.calibrationAngle;
				ac.setLabelPosition();
			}
		}

		foreach (string ac in deleteList) {
			aircrafts.Remove(ac);
			ld.removeStrokeSet(ac);
		}

		UpdateInformationLabel();
		SetupNESWLabels();
	}

	IEnumerator RequestAircraftDatas(bool dbClear) {
		WWW request;
		string url = "";
		this.dataRequest = false;

		if (dbClear == true) {
			url = $"http://{SERVERADDRESS}:5000/clear";
		}
		else {
			if (this.aircrafts.Count == 0) {
				url = $"http://{SERVERADDRESS}:5000/all";
			}
			else {
			//	url = $"http://{SERVERADDRESS}:5000/lastupdate/" + readTime;
				url = $"http://{SERVERADDRESS}:5000/all";
			}
		}
		request = new WWW(url);
		yield return request;

		if (!string.IsNullOrEmpty(request.error)) {
			Debug.Log(request.error);
		}

		else {
			if (request.responseHeaders.ContainsKey("STATUS") && request.responseHeaders["STATUS"].Contains("200")) {
				Debug.Log("__RCV__");
				string text = request.text;
				IDictionary js = (IDictionary)Json.Deserialize(text);

				//Calibrate my position and direction
				var calibrations = (IDictionary)js["Calibration"];
				var currentPos = (IDictionary)calibrations["currentPosition"];
				var calibPos = (IDictionary)calibrations["calibratePosition"];
				this.readTime = double.Parse((string)js["ReadTime"]);
				current_lat = (double)currentPos["latitude"];
				current_lng = (double)currentPos["longitude"];
				current_alt = (double)currentPos["altitude"];
				current_alt = 0.0;
				calibration_lat = (double)calibPos["latitude"];
				calibration_lng = (double)calibPos["longitude"];
				this.calibrationAngle = (float)this.calibrateDirection();
//				this.setCalibration();

				var items = (IDictionary)js["Items"];
				var keys = items.Keys;
				foreach (KeyValuePair<string, object> kvp in items as Dictionary<string, object>) {

					try {
						var tmpDic = (IDictionary)Json.Deserialize(kvp.Value as string);
						string icao = (string)tmpDic["icao"];
						var tmpLatitude = (double)tmpDic["latitude"];
						var tmpLongitude = (double)tmpDic["longitude"];
						var tmpAltitude = double.Parse((string)tmpDic["altitude"]);
						var tmpCallsign = (string)tmpDic["callsign"];
						var tmpOnGround = (bool)tmpDic["onGround"];
						if (tmpLatitude == 0.0 && tmpLongitude == 0.0 && tmpAltitude == 0.0) { continue; }
						if (!this.aircrafts.ContainsKey(icao)) {
							var newac = new Aircraft.Aircraft();
							newac.canvas = myCanvas;
							newac.icao = icao;
							newac.bodyObject = this.BuildBodyObject(icao);
							newac.labelObject = this.BuildLabelObject(icao);
							newac.targetBox = this.BuildTargetBox(icao);
							aircrafts.Add(icao, newac);
							ld.addStrokeSet(icao);
						}
						Aircraft.Aircraft ac = this.aircrafts[icao];
						ac.calibrationAngle = this.calibrationAngle;
						ac.setOriginPointWithCoordinate(current_lat, current_lng, current_alt);
						ac.updateTimestamp = (int)(double)tmpDic["update_time_stamp"];
						ac.callsign = tmpCallsign;
						ac.isOnGround = tmpOnGround;
						bool isMoved = ac.setPositionWithCoordinate(tmpLatitude, tmpLongitude, tmpAltitude);
						ac.setTextInfo();
						if (isMoved == true) {
							ld.addStroke(icao, ac.bodyObject.transform.position, this.ConvertAltitude2Color(tmpAltitude));
						}
					}
					catch (System.InvalidCastException e) {
						continue;
					}
				}
			}
		}
		this.dataRequest = true;
	}


	private void SetupNESWLabels() {
		this.SetupDirectionLabelPosition(N_Object, 0.0f - calibrationAngle, 1000, -20);
		this.SetupDirectionLabelPosition(E_Object, 90.0f - calibrationAngle, 1000, -20);
		this.SetupDirectionLabelPosition(S_Object, 180.0f - calibrationAngle, 1000, -20);
		this.SetupDirectionLabelPosition(W_Object, 270.0f - calibrationAngle, 1000, -20);
		N_Object.transform.LookAt(Camera.main.transform);
		W_Object.transform.LookAt(Camera.main.transform);
		E_Object.transform.LookAt(Camera.main.transform);
		S_Object.transform.LookAt(Camera.main.transform);
		N_Object.transform.Rotate(new Vector3(0, -180.0f, 0));
		W_Object.transform.Rotate(new Vector3(0, -180.0f, 0));
		E_Object.transform.Rotate(new Vector3(0, -180.0f, 0));
		S_Object.transform.Rotate(new Vector3(0, -180.0f, 0));
	}

	private void SetupDirectionLabelPosition(GameObject target, float direction, float distance, float alt) {
		var tmpx = (float)(distance * System.Math.Sin(deg2rad(direction)));
		var tmpy = (float)(distance * System.Math.Cos(deg2rad(direction)));
		target.transform.position = new Vector3(tmpx, alt, tmpy);
	}

	private void SetCalibrationAnble() {
		GameObject obj = GameObject.Find("MixedRealityCameraParent");
		Vector3 newAngle = new Vector3(0.0f, this.dumangle, 0.0f);
		obj.transform.localEulerAngles = newAngle;
		Debug.Log(obj.transform.localEulerAngles);
		dumangle += 1.0f;
	}

	private GameObject BuildBodyObject(string icao) {
		GameObject obj = GameObject.Find("Cube_Original");
		GameObject newobj = Instantiate(obj, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity);
		newobj.name = icao;
		return newobj;
	}

	private Color ConvertAltitude2Color(double alt) {
		var tmpAlt = alt / 100.0;
		if (tmpAlt > 300.0) { tmpAlt = 300.0; }
		float tmpColor1 = (float)( tmpAlt/ 300.0);
		float tmpColor2 = (float)(1.0 - tmpColor1);
		return new Color(0.0f, tmpColor1, tmpColor2);
	}


	private GameObject BuildLabelObject(string icao) {
		var canvas = GameObject.Find("InfoCanvas");
		GameObject newobj = new GameObject("Text");
		newobj.transform.parent = canvas.transform;
		newobj.AddComponent<Text>();
		newobj.name = "TEXT_" + icao;
		RectTransform rt = newobj.GetComponent<RectTransform>();
		var tmpVect = rt.transform.eulerAngles;
		rt.transform.Rotate(new Vector3(0.0f, 0.0f, 0.0f));
		rt.anchoredPosition = new Vector2(110.0f, 40.0f); //効いてなさそう
		Text text = newobj.GetComponent<Text>();
		text.fontSize = 16;
		text.fontStyle = FontStyle.Bold;
		text.alignment = TextAnchor.UpperCenter;
		text.text = "";
		text.font = Resources.FindObjectsOfTypeAll<Font>()[0];
		text.color = new Color(1, 0, 0);
		text.rectTransform.sizeDelta = new Vector2(150, 150);
		return newobj;
	}

	private GameObject BuildTargetBox(string icao) {
		var canvas = GameObject.Find("InfoCanvas");
		GameObject newobj = new GameObject("Image");
		newobj.transform.parent = canvas.transform;
		newobj.AddComponent<Image>();
		newobj.name = "IMAGE_" + icao;
		Image img = newobj.GetComponent<Image>();
		Sprite sp = Resources.Load<Sprite>("GreenSquare");
		RectTransform rt = newobj.GetComponent<RectTransform>();
		rt.localScale = new Vector3(0.3f, 0.3f, 0.3f);
		var tmpVect = rt.transform.eulerAngles;
		rt.transform.Rotate(new Vector3(0.0f, 0.0f, 0.0f));
		img.sprite = sp;
		return newobj;
	}

	private void UpdateInformationLabel() {
		GameObject obj = GameObject.Find("camDirectionLabel");
		var txtCp = obj.GetComponent<Text>();
		Vector3 camtr = Camera.main.transform.eulerAngles;
		var angle_h =(camtr.y + this.calibrationAngle) % 360.0;
		if (angle_h < 0) angle_h += 360.0;
		var angle_v = camtr.x - 360.0f;
		String txt = String.Format("H:{0:##} / V:{1:##}", angle_h, angle_v);
		txtCp.text = txt;

		GameObject obj2 = GameObject.Find("numberOfAircraftLabel");
		var txtCp2 = obj2.GetComponent<Text>();
		String txt2 = String.Format("TRK:{0:####}", this.aircrafts.Count);
		txtCp2.text = txt2;
	}

	private double calibrateDirection() {
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
	private double deg2rad(double degree) {
		return degree * System.Math.PI / 180.0;
	}
	private double rad2deg(double radian) {
		return 180 * radian / System.Math.PI;
	}

	private void playDisapearSound() {
		GameObject cube_org = GameObject.Find("Cube_Original");
		AudioSource sound = cube_org.GetComponents<AudioSource>()[1];
		sound.Play();
	}

	private void openningEffect() {
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
