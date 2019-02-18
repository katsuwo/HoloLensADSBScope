using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
		public GameObject gameObj = null;

		private void Awake() {
		}

		public void setOriginPointWithCoordinate(double latitude, double longitude) {
			this.originLatitude = latitude;
			this.originLongitude = longitude;
		}

		public void setPositionWithCoordinate(double latitude, double longitude) {
			this.latitude = latitude;
			this.longitude = longitude;
			this.gameObj.transform.position = new Vector3((float)this.latitude, 0.0f, (float)this.longitude);
			this.gameObj.SetActive(true);
		}

		public void setWorldPosition() {
			var dir = this.getDirection(this.latitude, this.longitude);
			var distance = this.getDistance(this.latitude, this.longitude) / 1000.0;
		}

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
