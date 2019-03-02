using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using UnityEngine.SceneManagement;

public class CalibrationAirtapDetect : MonoBehaviour, IInputClickHandler {
	public Camera MixedRealityCamera;
	public static double camAngleAtStart = 0.0;

	public GameObject text1 = null;
	public GameObject text2 = null;
	public GameObject text3 = null;


	void Start() {
		InputManager.Instance.PushFallbackInputHandler(gameObject);
		text1 = GameObject.Find("Text1");
		text2 = GameObject.Find("Text2");
		text3 = GameObject.Find("Text3");
		text3.SetActive(false);
	}

	void Update() {
	}

	public void OnInputClicked(InputClickedEventData eventData) {
		Invoke("movetoMainScene", 1.5f);
		camAngleAtStart = MixedRealityCamera.transform.localEulerAngles.y;
		text1.SetActive(false);
		text2.SetActive(false);
		text3.SetActive(true);
	}

	private void movetoMainScene() {
		SceneManager.UnloadScene("CalibrationScene");
		Destroy(GameObject.Find("MixedRealityCameraParentCalibrateScene"));
		SceneManager.LoadScene("scene1");
	}
}