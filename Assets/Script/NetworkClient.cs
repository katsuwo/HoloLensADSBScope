﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MiniJSON;
using Aircraft;
using System;



public class NetworkClient : MonoBehaviour {

	Dictionary<string, Aircraft.Aircraft> aircrafts = new Dictionary<string, Aircraft.Aircraft>();
	double current_lng = 139.2976628;
	double current_lat = 35.7979133;
	double current_alt = 172;
	private GameObject myCanvas;
	private string readTime = "";
	private float timeElapsed = 0.0f;
	private float calibrationAngle = 180.0f;

	// Use this for initialization
	void Start() {
		myCanvas = GameObject.Find("Canvas");
	}

	// Update is called once per frame
	void Update() {

		timeElapsed += Time.deltaTime;
		if (timeElapsed >= 0.5f) {
			StartCoroutine(getText());
			timeElapsed = 0.0f;
		}

		foreach (KeyValuePair<string,Aircraft.Aircraft> kvp in aircrafts) {
			Aircraft.Aircraft ac = aircrafts[kvp.Key];
			ac.setWorldPosition();
		}
		updateCamDirectionLabel();
	}

	IEnumerator getText() {
		WWW request;
		string url = "";

		if (this.readTime == null || this.readTime == "") {
			url = "http://192.168.10.88:5000/all";
		}
		else {
			url = "http://192.168.10.88:5000/lastupdate/" + readTime;
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
				var items = (IDictionary)js["Items"];
				this.readTime = (string)js["ReadTime"];

				var keys = items.Keys;
				foreach (KeyValuePair<string, object> kvp in items as Dictionary<string, object>) {
					var tmpDic = (IDictionary)Json.Deserialize(kvp.Value as string);
					string icao = (string)tmpDic["icao"];
//					if (icao != "8511CA") continue;
					if (!this.aircrafts.ContainsKey(icao)) {
						var newac = new Aircraft.Aircraft();
						newac.canvas = myCanvas;
						newac.calibrationAngle = this.calibrationAngle;
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
	public void updateCamDirectionLabel() {
		GameObject obj = GameObject.Find("camDirectionLabel");
		var txtCp = obj.GetComponent<Text>();
		Vector3 camtr = Camera.main.transform.rotation.eulerAngles;
		var angle = (camtr.y + this.calibrationAngle) % 360.0;
		if (angle < 0) angle += 360.0;
		String txt = String.Format("{0:##} : {1:##}", angle, camtr.x);
		txtCp.text = txt;

	}

	public void calibrateDirection() {
		GameObject obj = GameObject.Find("MixedRealityCameraParent");
		Vector3 calibAngle = new Vector3(0, this.calibrationAngle, 0);
		obj.transform.localEulerAngles = calibAngle;
	}
}
