﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiniJSON;
using Aircraft;
using System;



public class NetworkClient : MonoBehaviour {

	Dictionary<string, Aircraft.Aircraft> aircrafts = new Dictionary<string, Aircraft.Aircraft>();
	double current_lng = 139.2976628;
	double current_lat = 35.7979133;

	// Use this for initialization
	void Start() {

	}

	// Update is called once per frame
	void Update() {
		StartCoroutine(getText());
	}


	IEnumerator getText() {
		WWW request = new WWW("http://192.168.10.88:5000/all");
		yield return request;

		if (!string.IsNullOrEmpty(request.error)) {
			Debug.Log(request.error);
		}

		else {
			if (request.responseHeaders.ContainsKey("STATUS") && request.responseHeaders["STATUS"].Contains("200")) {
				string text = request.text;
				IDictionary js = (IDictionary)Json.Deserialize(text);
				var items = (IDictionary)js["Items"];
				var keys = items.Keys;
				foreach (KeyValuePair<string, object> kvp in items as Dictionary<string, object>) {
					var tmpDic = (IDictionary)Json.Deserialize(kvp.Value as string);
					string icao = (string)tmpDic["icao"];

					if (!this.aircrafts.ContainsKey(icao)) {
						var newac = new Aircraft.Aircraft();
						newac.icao = icao;
						newac.setOriginPointWithCoordinate(current_lat, current_lng);
						newac.gameObj = this.makeGameObject();
						aircrafts.Add(icao, newac);
					}

					Aircraft.Aircraft ac = this.aircrafts[icao];
					ac.callsign = (string)tmpDic["callsign"];
					try {
						ac.setPositionWithCoordinate((double)tmpDic["latitude"] * 100, (double)tmpDic["longitude"] * 100);
						Debug.Log($"{ac.icao}");
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

	public GameObject makeGameObject() {
		GameObject obj = GameObject.Find("Cube_original");
		GameObject newobj = Instantiate(obj, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity);
		return newobj;
	}

}
