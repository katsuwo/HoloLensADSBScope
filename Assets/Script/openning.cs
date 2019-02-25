using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class openning : MonoBehaviour {

	[SerializeField]
	public float pos = 0.0f;
	[SerializeField]
	public float hight = 0.0f;
	public bool direction = false;
	// Use this for initialization
	void Start () {
	}

	// Update is called once per frame
	void Update () {
	}

	public void DrawLine() {
		LineRenderer lRend = gameObject.AddComponent<LineRenderer>();
//		lRend.useWorldSpace = true;
		lRend.positionCount = 2;
		lRend.startWidth = 0.2f;
		lRend.endWidth = 0.2f;
		if (direction == false) {
			Vector3 sttVec = new Vector3(-10000, hight, pos);
			Vector3 endVec = new Vector3(10000, hight, pos);
			lRend.SetPosition(0, sttVec);
			lRend.SetPosition(1, endVec);
		}
		else {
			Vector3 sttVec = new Vector3(pos, hight, -10000);
			Vector3 endVec = new Vector3(pos, hight, 10000);
			lRend.SetPosition(0, sttVec);
			lRend.SetPosition(1, endVec);
		}

	}
}
