using System.Collections;
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

	// Use this for initialization
	void Start() {
		myCanvas = GameObject.Find("Canvas");
		StartCoroutine(getText());
	}

	// Update is called once per frame
	void Update() {
		foreach(KeyValuePair<string,Aircraft.Aircraft> kvp in aircrafts) {
			Aircraft.Aircraft ac = aircrafts[kvp.Key];
			ac.setWorldPosition();
		}


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
	//				if (icao != "8626CC") continue;
					if (!this.aircrafts.ContainsKey(icao)) {
						var newac = new Aircraft.Aircraft();
						newac.canvas = myCanvas;
						newac.icao = icao;
						newac.setOriginPointWithCoordinate(current_lat, current_lng,current_alt);
						newac.bodyObject = this.makeGameObject();
						newac.labelObject = this.makeLabelObject();
						newac.targetBox = this.makeTargetBox();
						aircrafts.Add(icao, newac);
					}

					Aircraft.Aircraft ac = this.aircrafts[icao];
					ac.callsign = (string)tmpDic["callsign"];
					try {
						var tmpLatitude = (double)tmpDic["latitude"];
						var tmpLongitude = (double)tmpDic["longitude"];
						var tmpAltitude = double.Parse((string)tmpDic["altitude"]);
						ac.setPositionWithCoordinate(tmpLatitude, tmpLongitude, tmpAltitude);
						Debug.Log($"POSITION SET:{ac.icao}");
						ac.setTextInfo();
					}
					catch (System.InvalidCastException e) {
						ac.latitude = 0;
						ac.longitude = 0;
						ac.altitude = 0;
						Debug.Log($"ERR:{ac.icao}");
					}
				}
			}
		}
	}

	public GameObject makeGameObject() {
		GameObject obj = GameObject.Find("Cube_Original");
		GameObject newobj = Instantiate(obj, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity);
		return newobj;
	}

	public GameObject makeLabelObject() {
		var canvas = GameObject.Find("Canvas");
		GameObject newobj = new GameObject("Text");
		newobj.transform.parent = canvas.transform;
		newobj.AddComponent<Text>();
		Text text = newobj.GetComponent<Text>();
		text.fontSize = 10;
		text.alignment = TextAnchor.UpperCenter;
		text.text = "TEST1 \nTEST2 \nTEST3";
		text.font = Resources.FindObjectsOfTypeAll<Font>()[0];
		return newobj;
	}

	public GameObject makeTargetBox() {
		var canvas = GameObject.Find("Canvas");
		GameObject newobj = new GameObject("Image");
		newobj.transform.parent = canvas.transform;
		newobj.AddComponent<Image>();
		Image img = newobj.GetComponent<Image>();
		Sprite sp = Resources.Load<Sprite>("GreenSquare");
		RectTransform rt = newobj.GetComponent<RectTransform>();
		rt.localScale = new Vector3(0.3f, 0.3f, 0.3f);
		img.sprite = sp;
		return newobj;
	}
}
