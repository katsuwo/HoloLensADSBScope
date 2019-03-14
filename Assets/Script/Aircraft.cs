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

		//補正角
		public double calibrationAngle = 0.0f;


		/// <summary>
		/// Distance:カメラからの距離
		/// direction:カメラからの方位
		/// </summary>
		public double direction = 0.0f;
		public double distance = 0.0f;

		/// <summary>
		/// bodyObject:機体オブジェクト
		/// labelObject:機体情報ラベルオブジェクト
		/// targetBox：機体位置ボックス
		/// </summary>
		public GameObject bodyObject { get; set; } = null;
		public GameObject labelObject { get; set; } = null;
		public GameObject targetBox { get; set; } = null;


		public GameObject canvas { get; set; } = null;

		private string text = "";
		private string oldText = "";

		private void Awake() {
		}


		/// <summary>
		/// set camera position with latitude and longitude
		/// </summary>
		/// <param name="latitude">Latitude of Camera</param>
		/// <param name="longitude">Longitude of Camera</param>
		/// <param name="altitude">Altitude of Camera</param>
		public void setOriginPointWithCoordinate(double latitude, double longitude, double altitude) {
			this.originLatitude = latitude;
			this.originLongitude = longitude;
			this.originAltitude = altitude;
		}

		/// <summary>
		/// set aircraft position  with latitude and longitude
		/// </summary>
		/// <param name="latitude">latitude of aircract</param>
		/// <param name="longitude">Longitude of aircraft</param>
		/// <param name="altitude">Altitude of aircraft</param>
		/// <returns>
		/// false: Invalid position or warped
		/// true: position is valid.
		/// </returns>
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

		/// <summary>
		/// set Unity's world position from latitude and longitude
		/// </summary>
		/// <returns>
		/// true: WorldPosition is set normaly
		/// false: WorldPosition isn't set.because aircraft is not moved.</returns>
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

		/// <summary>
		/// setup Label / target box position on cavas.
		/// </summary>
		public void setLabelPosition() {
			Vector3 camtr = Camera.main.transform.rotation.eulerAngles;
			//機体への角度と、カメラの方向が±120を超える場合は描画しない
			//（Canvasの裏側からTextとLabelが描画される不具合の対策）
			float diff = camtr.y - (float)this.direction;
			if (diff >= 180.0f) { diff = 360.0f - diff; }
			if (diff <= -180.0f) { diff = 360.0f + diff;  }
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

		/// <summary>
		/// calculate distance from camera position to aircraft
		/// </summary>
		/// <param name="latitude1">Latitude of camera position</param>
		/// <param name="longitude1">LOngitude of camera position</param>
		/// <param name="latitude2">Latitude of aircraft position</param>
		/// <param name="longitude2">Longitude of aircraft position</param>
		/// <returns>distance (meters)</returns>		
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

		/// <summary>
		/// setup Aircraft information text on the textobject.
		/// </summary>
		public void setTextInfo() {
			var uiText = this.labelObject.GetComponent<Text>();
			var distanceText = string.Format("{0:####.##}km", this.distance / 1000.0);
			var altText = string.Format("{0:#####.#}ft", this.altitude);
			var csText = this.callsign.Replace("_", "");
			var gndText = (this.isOnGround == true) ? "GND:" : ""; 
			this.text = $"{gndText}{csText}\n{altText}:{distanceText}\n{this.icao}";

			if (this.text != this.oldText) {
				uiText.text = this.text;
				this.oldText = this.text;
			}
		}

		/// <summary>
		/// calculate direction from user's position to aircraft 
		/// </summary>
		/// <param name="latitude">Latitude of aircraft</param>
		/// <param name="longitude">Longitude of aircraft</param>
		/// <returns>direction (degree)</returns>
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

		/// <summary>
		/// convert degrees to radians
		/// </summary>
		/// <param name="degree"></param>
		/// <returns>radians value</returns>
		public double deg2rad(double degree) {
			return degree * System.Math.PI / 180.0;
		}

		/// <summary>
		/// convert radians to degrees
		/// </summary>
		/// <param name="radian"></param>
		/// <returns>degree value</returns>
		public double rad2deg(double radian) {
			return 180 * radian / System.Math.PI;
		}
	}
}
