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

		[SerializeField]
		public int updateTimestamp = 0;


		public double direction = 0.0f;
		public double calibrationAngle = 0.0f;
		public double distance = 0.0f;


		[SerializeField]
		public GameObject bodyObject { get; set; } = null;

		[SerializeField]
		public GameObject labelObject { get; set; } = null;

		[SerializeField]
		public GameObject targetBox { get; set; } = null;


		public GameObject canvas { get; set; } = null;

		private string text = "";
		private string oldText = "";

		private void Awake() {
		}


		//setUser's position with latitude and longitude
		public void setOriginPointWithCoordinate(double latitude, double longitude, double altitude) {
			this.originLatitude = latitude;
			this.originLongitude = longitude;
			this.originAltitude = altitude;
		}

		//set position of Aircraft with latitude and longitude
		public bool setPositionWithCoordinate(double latitude, double longitude, double altitude) {
			if (this.latitude == latitude && this.longitude == longitude && this.altitude == altitude) return false;

			//warp check
			if ((getDistance(this.latitude, this.longitude, latitude, longitude) > 10.0 * 1000.0) && (this.latitude != 0.0 && this.longitude != 0.0)) {
				Debug.Log("WARP");
				return false;
			}	
			this.latitude = latitude;
			this.longitude = longitude;
			this.altitude = altitude;

			 return this.setWorldPosition();
		}

		// set Unity's world position from latitude and longitude
		public bool setWorldPosition() {
			var tmpDir = (this.getDirection(this.latitude, this.longitude) - this.calibrationAngle);
			if (tmpDir < 0) tmpDir += 360.0;
			this.direction = tmpDir;
			this.distance = this.getDistance(this.originLatitude, originLongitude,  this.latitude, this.longitude);
			var tmpx = (float)(this.distance * System.Math.Sin(deg2rad(this.direction)));
			var tmpy = (float)(this.distance * System.Math.Cos(deg2rad(this.direction)));
			var tmpalt = (float)((this.altitude * 0.33) - this.originAltitude);
			if (this.world_x != tmpx || this.world_y != tmpy || this.world_alt != tmpalt) {
				this.world_x = tmpx;
				this.world_y = tmpy;
				this.world_alt = tmpalt;
				this.bodyObject.transform.position = new Vector3(this.world_x, this.world_alt, this.world_y);
				this.setLabelPosition();
				return true;
			}
			return false;
		}

		public void setLabelPosition() {
			Vector3 camtr = Camera.main.transform.rotation.eulerAngles;
			//機体への角度と、カメラの方向が±120を超える場合は描画しない
			//（Canvasの裏側からTextとLabelが描画される不具合の対策）
			var diff = camtr.y - this.direction;
//			var diff = ((camtr.y + calibrationAngle) - this.direction) % 360.0;
			if (diff > 120 || diff < -120) {
				this.labelObject.SetActive(false);
				this.targetBox.SetActive(false);
				return;
			}
			this.labelObject.SetActive(true);
			this.targetBox.SetActive(true);

			RectTransform canvasRt = canvas.GetComponent<RectTransform>();

			Text text = this.labelObject.GetComponent<Text>();
			Image targetBox = this.targetBox.GetComponent<Image>();
			RectTransform textRt = text.GetComponent<RectTransform>();
			RectTransform targetBoxRt = targetBox.GetComponent<RectTransform>();
			textRt.transform.eulerAngles = new Vector3(0.0f, camtr.y, 0.0f);
			targetBoxRt.transform.eulerAngles = new Vector3(0.0f, camtr.y, 0.0f);

			Vector2 screenPoint;
			Vector2 localPoint;

			screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, this.bodyObject.transform.position);
			RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screenPoint, Camera.main, out localPoint);
			textRt.localPosition = localPoint;
			targetBoxRt.localPosition = localPoint;
		}

		//calculate distance from camera position to aircraft 
		public double getDistance(double latitude1, double longitude1, double latitude2, double longitude2) {
			var lat1 = deg2rad(latitude1);
			var lng1 = deg2rad(longitude1);
			var lat2 = deg2rad(latitude2);
			var lng2 = deg2rad(longitude2);
			var r = 6378137.0;
			var avelat = (lat1 - lat2) / 2;
			var avelng = (lng1 - lng2) / 2;
			return r * 2 * System.Math.Asin(System.Math.Sqrt(System.Math.Pow(System.Math.Sin(avelat), 2) + System.Math.Cos(lat1) * System.Math.Cos(lat2) * System.Math.Pow(System.Math.Sin(avelng), 2)));
		}

		public void setTextInfo() {
			var uiText = this.labelObject.GetComponent<Text>();
			var distanceText = string.Format("{0:####.##}km", this.distance / 1000.0);
			var altText = string.Format("{0:#####.#}ft", this.altitude);
			var csText = this.callsign.Replace("_", "");
			this.text = $"{csText}\n{altText}:{distanceText}\n{this.icao}";

			if (this.text != this.oldText) {
				uiText.text = this.text;
				this.oldText = this.text;
			}
		}

		//calculate direction from user's position to aircraft 
		public double getDirection(double latitude, double longitude) {
			var y1 = deg2rad(this.originLatitude);
			var x1 = deg2rad(this.originLongitude);
			var y2 = deg2rad(latitude);
			var x2 = deg2rad(longitude);
			var Y = System.Math.Cos(y2) * System.Math.Sin(x2 - x1);
			var X = System.Math.Cos(y1) * System.Math.Sin(y2) - System.Math.Sin(y1) * System.Math.Cos(y2) * System.Math.Cos(x2 - x1);
//			var ret = (rad2deg(System.Math.Atan2(Y, X)) + calibrationAngle) % 360.0;
			var ret = rad2deg(System.Math.Atan2(Y, X))  % 360.0;
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
