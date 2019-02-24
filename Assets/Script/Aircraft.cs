using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace Aircraft {
	[System.Serializable]
	public class Aircraft {

		[SerializeField]
		public string _id = "";

		[SerializeField]
		public string icao = "";

		[SerializeField]
		public string callsign = "";

		[SerializeField]
		public double latitude = 0.0;

		[SerializeField]
		public double longitude = 0.0;

		[SerializeField]
		public double altitude = 0.0;

		[SerializeField]
		public bool isOnGround = false;

		[SerializeField]
		public double originLatitude = 0.0;

		[SerializeField]
		public double originLongitude = 0.0;

		[SerializeField]
		public double originAltitude = 0.0;

		[SerializeField]
		public float world_x = 0.0f;

		[SerializeField]
		public float world_y = 0.0f;

		[SerializeField]
		public float world_alt = 0.0f;

		public double direction = 0.0f;
		public double distance = 0.0f;


		[SerializeField]
		public GameObject bodyObject { get; set; } = null;

		[SerializeField]
		public GameObject labelObject{ get; set; } = null;

		[SerializeField]
		public GameObject targetBox { get; set; } = null;


		public GameObject canvas { get; set; } = null;

		private void Awake() {
		}


		//setUser's position with latitude and longitude
		public void setOriginPointWithCoordinate(double latitude, double longitude, double altitude) {
			this.originLatitude = latitude;
			this.originLongitude = longitude;
			this.originAltitude = altitude;
		}


		//set position of Aircraft with latitude and longitude
		public void setPositionWithCoordinate(double latitude, double longitude, double altitude) {
			this.latitude = latitude;
			this.longitude = longitude;
			this.altitude = altitude;


	//		this.latitude = 35.7970407;
	//		this.longitude = 139.2967381;
	//		this.altitude = (172 + 333)/ 0.33;


			this.bodyObject.SetActive(true);
		}

		// set Unity's world position from latitude and longitude
		public void setWorldPosition() {
			this.direction = this.getDirection(this.latitude, this.longitude);
			this.distance = this.getDistance(this.latitude, this.longitude);

//			this.world_x = (float)(this.distance * rad2deg(System.Math.Sin(deg2rad(this.direction))));
//			this.world_y = (float)(this.distance * rad2deg(System.Math.Cos(deg2rad(this.direction))));


			this.world_x = (float)(this.distance * System.Math.Sin(deg2rad(this.direction)));
			this.world_y = (float)(this.distance * System.Math.Cos(deg2rad(this.direction)));
			this.world_alt = (float)((this.altitude * 0.33) - this.originAltitude);
			this.bodyObject.transform.position = new Vector3(this.world_x, this.world_alt, this.world_y);
			this.setLabelPosition();
		}

		public void setLabelPosition() {
			Vector3 camtr = Camera.main.transform.rotation.eulerAngles;
			Debug.Log(camtr);
			Debug.Log(this.direction);

			//機体への角度と、カメラの方向が±120を超える場合は描画しない
			//（Canvasの裏側からTextとLabelが描画される不具合の対策）
			var diff = camtr.y - this.direction;
			if (diff > 120 || diff < -120) {
				Debug.Log("SKIP");
				return;
			}

			RectTransform canvasRt = canvas.GetComponent<RectTransform>();

			Text text = this.labelObject.GetComponent<Text>();
			Image targetBox = this.targetBox.GetComponent<Image>();
			RectTransform textRt = text.GetComponent<RectTransform>();
			RectTransform targetBoxRt =  targetBox.GetComponent<RectTransform>();

			Vector2 screenPoint;
			Vector2 localPoint;

			screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main , this.bodyObject.transform.position);
			RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screenPoint, Camera.main, out localPoint);
			textRt.localPosition = localPoint;
			targetBoxRt.localPosition = localPoint;
		}

		//calculate distance from user's position to aircraft 
		public double getDistance(double latitude, double longitude) {
			var lat1 = deg2rad(this.originLatitude);
			var lng1 = deg2rad(this.originLongitude);
			var lat2 = deg2rad(latitude);
			var lng2 = deg2rad(longitude);
			var r = 6378137.0;
			var avelat = (lat1 - lat2) / 2;
			var avelng = (lng1 - lng2) / 2;
			return r * 2 * System.Math.Asin(System.Math.Sqrt(System.Math.Pow(System.Math.Sin(avelat), 2) + System.Math.Cos(lat1) * System.Math.Cos(lat2) * System.Math.Pow(System.Math.Sin(avelng), 2)));
		}

		public void setTextInfo() {
			var uiText = this.labelObject.GetComponent<Text>();
			uiText.text = $"{this.callsign}\n{this.altitude}\n{this.icao}";
		}


		//calculate direction from user's position to aircraft 
		public double getDirection(double latitude, double longitude) {
			var y1 = deg2rad(this.originLatitude);
			var x1 = deg2rad(this.originLongitude);
			var y2 = deg2rad(latitude);
			var x2 = deg2rad(longitude);
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
	}
}
