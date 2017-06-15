using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System.Text;
//using SimpleJSON; // http://wiki.unity3d.com/index.php/SimpleJSON
#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
using System.IO;
using System.Net.NetworkInformation;
#endif

public class Tracker : MonoBehaviour {
	public string DebugInfoLabel = "NOT TRACKING";
	public bool ShowDebugInfoLabel = true;
	private bool allowQuitting = false;
	private bool allowQuittingRequested = false;
	public string url = "";
	private string tracking_session_id = "NA";
	private string tracking_session_user = "NONE";
	private string tracking_session_version = "Alpha";
	public bool connected = false;
	public bool ready = false;

	public bool tracking_local_enabled = true;
	public bool tracking_message_collection = true;
	public bool register_remote_enabled = true;
	
	#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
	StreamWriter log = null;
	#endif
	private StringBuilder log2 = null;

	private static int[] keys_of_interest = new int[] {
		(int)KeyCode.Return, (int)KeyCode.Escape, (int)KeyCode.Space,
		(int)KeyCode.Keypad8, (int)KeyCode.Keypad2, (int)KeyCode.Keypad4, (int)KeyCode.Keypad6,
		(int)KeyCode.UpArrow, (int)KeyCode.DownArrow, (int)KeyCode.LeftArrow, (int)KeyCode.RightArrow,
		(int)KeyCode.W, (int)KeyCode.S, (int)KeyCode.A, (int)KeyCode.D
	};
	
	private static Vector2 mouse_position_last = new Vector2(0,0);
	private static Vector2 mouse_vector_last = new Vector2(0,0);
	private static float mouse_position_delta;
	private static float mouse_vector_delta;
	private static float mouse_probe_dist_sensitivity = 100.0f;
	private static float mouse_probe_vect_sensitivity = 0.5f;
	private static float mouse_probe_vect_sensitivity_max = 0.75f;
	private static float mouse_probe_time = 0.0f;
	private static float mouse_probe_freq = 0.1f;
	private static bool mouse_moving = false;

	void Start () {
		DebugInfoLabel = "NOT TRACKING";
		allowQuitting = false;
		allowQuittingRequested = false;
		connected = false;
		ready = false;
		url = "https://tkv2t9v8ad.execute-api.us-east-1.amazonaws.com/prod";
	}
	public void StartTracker(){
		if (ready) return;
		tracking_session_user = PlayerPrefs.GetString("PlayerId","NONE");
		if (register_remote_enabled){
			StartCoroutine("FetchConfig");
		} else {
			SetupTracking();
		}

	}
	IEnumerator FetchConfig(){
		Debug.Log("FetchConfig");
		DebugInfoLabel = "FetchConfig from " + url;
		string ourPostData = "{\"user\":\""+tracking_session_user+"\",\"version\":\""+tracking_session_version+"\"}";
        Dictionary<string,string> headers = new Dictionary<string, string>();
        headers.Add("Content-Type", "application/json");
        byte[] pData = System.Text.Encoding.ASCII.GetBytes(ourPostData.ToCharArray());
        WWW www = new WWW(url, pData, headers);
        yield return www;
		if (www.error==null){
			try{
			Debug.Log(www.text);
			JSONObject ob = new JSONObject(www.text.Trim());
	        tracking_session_id = ob.GetField("id").ToString();
	    	} catch {
	    		
	    	}
		} else {
			Debug.Log(www.error);
			tracking_session_id = "NA";
		}
        //tracking_session_id = {"id": 11, "user": "hello"}
		SetupTracking();
		yield return null; 
	}
	void SetupTracking(){
		#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
		string fileName = Application.persistentDataPath + "/Session-" + tracking_session_id.ToString() + "-" + System.DateTime.Now.ToString("MM-dd-yy-HH-mm-ss") + ".log";
		log = File.CreateText(fileName);
		if(log==null){
			Debug.Log("Error opening local log " + fileName);
		}{
			Debug.Log("Logging locally to " + fileName);
		}
		#endif
		log2 = new StringBuilder();
		CreateEvent("SessionID",tracking_session_id);
		CreateEvent("SessionUser",tracking_session_user);
		CreateEvent("SessionVersion",tracking_session_version);
		CreateEvent("Calibration",Screen.width.ToString()+"x"+Screen.height.ToString());
		ready = true;
		DebugInfoLabel = "TRACKING "+tracking_session_id;
	}
	
	public struct TrackedEvent{
		public string e;
		public float time;
		public float mouse_x;
		public float mouse_y;
		public string data;
	}
	
	public void MessageEvent(string event_)    {
    	if(tracking_message_collection)
			CreateEvent(event_,"");
	}
	public void MessageEventData(ArrayList event_data)    {
    	if(tracking_message_collection)
			CreateEvent((string)event_data[0],(string)event_data[1]);
	}
	public TrackedEvent CreateEventExt(string tracked_event, string data){
		DebugInfoLabel = tracked_event + " " + data;
		return CreateEvent(tracked_event, data);
	}
	public TrackedEvent CreateEvent(string tracked_event, string data){
		TrackedEvent e;
		e.e = tracked_event;
		e.data = data;
		e.time = Time.time;
		e.mouse_x = Input.mousePosition.x;
		e.mouse_y = Input.mousePosition.y;

		string line = System.DateTime.Now.ToString("MM-dd-yy-HH-mm-ss") + "\t" 
			+ e.e + "\t" + e.data + "\t" + e.time + "\t" 
			+ e.mouse_x + "\t" + e.mouse_y;

		#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
		if(tracking_local_enabled && log!=null){
			log.WriteLine(line);
		}
		#endif
		if(log2!=null){
			log2.Append(line+"\n");
		} else {
			Debug.Log("Cannot log line: "+line);
		}
		return e;
	}
	void Update(){
		if(ready) EventCollection();
	}
	void EventCollection(){
		// Mouse clicks
		for(int i=0;i<=2;i++){
			if(Input.GetMouseButtonDown(i)){
				CreateEvent("MouseDown",i.ToString());

			}
			if(Input.GetMouseButtonUp(i)){
				CreateEvent("MouseUp",i.ToString());
			}
		}
		// Key presses
		foreach(KeyCode i in keys_of_interest){
			if(Input.GetKeyDown(i)){
				CreateEvent("KeyDown",i.ToString());
			}
			if(Input.GetKeyUp(i)){
				CreateEvent("KeyUp",i.ToString());
			}

		}
		mouse_probe_time+=1*Time.deltaTime;
		if(mouse_probe_time>mouse_probe_freq){
			mouse_probe_time=0.0f;
			// Mouse Movement (Distance)
			mouse_position_delta = Vector2.Distance((Vector2)Input.mousePosition, mouse_position_last);
			Vector2 vector = new Vector2(0.0f,0.0f);
			if(mouse_position_delta>mouse_probe_dist_sensitivity){
				CreateEvent("MouseMoveD",mouse_position_delta.ToString());
				mouse_moving = true;
			}
			// Mouse Movement (Direction)
			if(mouse_position_delta>0.0f){
				vector = (Vector2)Input.mousePosition - mouse_position_last;
				vector.Normalize();
				mouse_vector_delta = Vector2.Dot(mouse_vector_last,vector);
				mouse_vector_last = vector;
				if(mouse_vector_delta<mouse_probe_vect_sensitivity){
					CreateEvent("MouseMoveV",mouse_vector_delta.ToString());
					mouse_probe_vect_sensitivity -= 0.2f;
					if (mouse_probe_vect_sensitivity<-1.0f) mouse_probe_vect_sensitivity=-1.0f;
				}
				mouse_moving = true;
			}else{
				mouse_vector_delta = 0.0f;
			}
			// Mouse Movement (Stop)
			if(mouse_position_delta==0.0f && mouse_moving){
				mouse_moving = false;
				CreateEvent("MouseMoveS",mouse_position_delta.ToString());
			}
			// Book keeping
			mouse_probe_vect_sensitivity += 0.02f;
			if (mouse_probe_vect_sensitivity>mouse_probe_vect_sensitivity_max) mouse_probe_vect_sensitivity=mouse_probe_vect_sensitivity_max;
			mouse_position_last = Input.mousePosition;
		}
	}
	void OnGUI() {
		if(ShowDebugInfoLabel){
			GUI.Label(new Rect(2,2,3000,500),DebugInfoLabel);
		}
	}

	public void OnApplicationQuit () {
		if(allowQuitting){
			Application.Quit();
		} else {
			Application.CancelQuit();
			allowQuittingRequested = true;
			CreateEvent("End","Quit");
			#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
			if(log!=null) log.Close();
			log = null;
			#endif
			UploadLogs();
		}
	}
	public void UploadLogs(){
		CreateEvent("UploadRequest","1");
		StartCoroutine("UploadLogsWWW");
	}
	IEnumerator UploadLogsWWW(){
		//Debug.Log("Uploading logs form to: "+url);
		/*WWWForm form = new WWWForm();
		form.AddField("log",log2.ToString());
		form.AddField("version",tracking_session_version);
		form.AddField("session",tracking_session);
		form.AddField("session_id",tracking_session_id);
		form.AddField("session_pc",tracking_session_pc);
		WWW www = new WWW(url+url_logs,form);
		yield return www;
		Debug.Log(www.text);
		*/
		yield return null;
		if(allowQuittingRequested){
			allowQuitting = true;
			Application.Quit();
		}
	}
}
