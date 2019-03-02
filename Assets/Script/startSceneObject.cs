using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HoloToolkit.UI.Keyboard;
using HoloToolkit.Unity.InputModule;
using System;
using System.Net.Sockets;
using System.Net;
using UnityEngine.SceneManagement;

#if NETFX_CORE
using Windows.Networking;
using Windows.Networking.Connectivity;
#endif

public class startSceneObject : MonoBehaviour, IInputClickHandler {
	public TextMesh TargetTextMesh;
	UnityEngine.Ping ping;
	public static string serverIpAddress = "";

	void Start() {

		dispKeyboard(getServerAddressFromMyIp(GetMyIPAddress()));
	}

	private void dispKeyboard(string initial) {
		Keyboard.Instance.CloseOnInactivity = false;
		Keyboard.Instance.Close();
		Keyboard.Instance.PresentKeyboard();
		Keyboard.Instance.OnTextUpdated += KeyboardOnTextUpdated;
		Keyboard.Instance.OnTextSubmitted += KeyboardOnTextSubmitted;
		Keyboard.Instance.InputField.text = initial;
		TargetTextMesh.text = initial;
	}

	// Update is called once per frame
	void Update () {
	}

	private void KeyboardOnTextSubmitted(object sender, EventArgs eventArgs) { 
  		Debug.Log(TargetTextMesh.text);
		serverIpAddress = TargetTextMesh.text;


#if true
		GameObject cam = GameObject.Find("MixedRealityCameraStartScens");
		foreach (Transform n in cam.transform) {
			GameObject.Destroy(n.gameObject);
		}
		GameObject parent = GameObject.Find("MixedRealityCameraStartScens");
		Destroy(parent);

		SceneManager.LoadScene("CalibrationScene");
		SceneManager.UnloadScene("startScene");

#else
		ping = new UnityEngine.Ping(serverIpAddress);
		StartCoroutine(PingUpdate());
#endif
	}

	IEnumerator PingUpdate() {
		yield return new WaitForSeconds(1f);
		if (ping.isDone) {
			Debug.Log(ping.time);

			GameObject cam = GameObject.Find("MixedRealityCameraStartScens");
			foreach (Transform n in cam.transform) {
				GameObject.Destroy(n.gameObject);
			}
			GameObject parent = GameObject.Find("MixedRealityCameraStartScens");
			Destroy(parent);

			SceneManager.LoadScene("CalibrationScene");
			SceneManager.UnloadScene("startScene");
		}
		else {
			dispKeyboard(getServerAddressFromMyIp(GetMyIPAddress()));
		}

	}

	private void KeyboardOnTextUpdated(string s) {
		TargetTextMesh.text = s;
	}

	public void OnInputClicked(InputClickedEventData eventData) {
	}

	public string getServerAddressFromMyIp(string myip) {
		if (myip.Contains("172.20.10")) { return "172.20.10.6"; }
		if (myip.Contains("192.168.10.")) { return "192.168.10.155"; }
		return "0.0.0.0";

	}
	//現在のIPアドレスを求め、推測されるサーバーのIPを文字列で返す
	public static string GetMyIPAddress() {

#if !NETFX_CORE
		return UnityEngine.Network.player.ipAddress;
#else
       string ip = null;
        foreach (HostName localHostName in NetworkInformation.GetHostNames())
        {
            if (localHostName.IPInformation != null)
            {
                if (localHostName.Type == HostNameType.Ipv4)
                {
                    ip = localHostName.ToString();
                    break;
                }
            }
        }
        return ip;

#endif
	}

	public static string getServerAddress() {
		return serverIpAddress;
	}
}
