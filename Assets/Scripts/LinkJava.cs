﻿using System;
using UnityEngine;
using System.Collections;
using System.Diagnostics;

public class LinkJava : MonoBehaviour 
{
	public string externalPath;
	private string pathCPSeparator = ":";
	public string pathSeparator = "/";

	public enum SimulationTypes {ME, Play, PCG}
	public SimulationTypes simulationMode = SimulationTypes.ME;

	public string filename;
	public int budget = 600000;

	GameManager gameManager;

	public delegate void ME_Simulation(SimulationFeedback feedback);
	public static event ME_Simulation OnSimulationCompleted;
	public enum SimulationFeedback {none, success, failure}

    void Awake()
    {
    	checkEnvironment();
    	if(Application.isEditor){
        	externalPath = Application.dataPath + "/../PCGMC4PP/dist/".Replace("/",pathSeparator);
        } else {
        	externalPath = Application.dataPath + "/../../PCGMC4PP/dist/".Replace("/",pathSeparator);
        }
        filename = Application.dataPath + "Resources/Exports/levels/level-2-prototype.txt";
		gameManager = GameManager.Instance;
		budget = 600000;
    }

	private int checkEnvironment()
	{
		switch (Application.platform) 
		{
		case RuntimePlatform.WindowsPlayer:
		case RuntimePlatform.WindowsEditor:
			pathCPSeparator = ";";
			pathSeparator = "\\";
			break;
		case RuntimePlatform.OSXEditor:
		case RuntimePlatform.OSXPlayer:
			pathCPSeparator = ":";
			pathSeparator = "/";
			break;
		default:
			UnityEngine.Debug.Log ("Wrong environment");
			return 1;
		}
		return 0;
	}
	private int checkJava()
	{
		// ExitCode = 0: OK
		// ExitCode = 1: Not found
		// ExitCode = -1: Some exception
		int ExitCode = -1;
		try 
		{
			Process myProcess = new Process();
			myProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			myProcess.StartInfo.CreateNoWindow = true;
			myProcess.StartInfo.UseShellExecute = false;
			myProcess.StartInfo.FileName = "java";
			myProcess.StartInfo.Arguments = "-version";
			myProcess.EnableRaisingEvents = true;
			myProcess.Start();
			myProcess.WaitForExit();
			ExitCode = myProcess.ExitCode;
			//while ((line = myProcess.StandardOutput.ReadLine()) != null) { mpout += line + "\n";}
		} 
		catch (Exception e)
		{
			UnityEngine.Debug.Log(e);
		}
		return ExitCode;
	}

	private Process externalProcess = null;
	private string externalNonBlocking()
	{
		
		// prevent concurrent calls
		if (externalProcess != null) 
		{
			return "Process may not have finished";
		} 
		else 
		{
			if(simulationMode == SimulationTypes.ME){
				budget = 600000;
			} else {
				budget = -1;
			}
			externalProcess = new Process ();
			externalProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			externalProcess.StartInfo.CreateNoWindow = true;
			externalProcess.StartInfo.UseShellExecute = false;
			externalProcess.StartInfo.RedirectStandardOutput = true;
			externalProcess.StartInfo.FileName = "java";
	        externalProcess.StartInfo.Arguments = "-Xmx 2147483648 ";		
			externalProcess.StartInfo.Arguments = "-cp \"";
			externalProcess.StartInfo.Arguments +=externalPath+"PCGMC4PP.jar";
			externalProcess.StartInfo.Arguments +=pathCPSeparator+externalPath+"lib"+pathSeparator+"gson-2.6.2.jar";
			externalProcess.StartInfo.Arguments +=pathCPSeparator+externalPath+"lib"+pathSeparator+"jdom.jar";
			externalProcess.StartInfo.Arguments +=pathCPSeparator+externalPath+"lib"+pathSeparator+"prefuse.jar";
			externalProcess.StartInfo.Arguments +=pathCPSeparator+externalPath+"lib"+pathSeparator+"OGE.jar";
			externalProcess.StartInfo.Arguments +=pathCPSeparator+externalPath+"lib"+pathSeparator+"JGGS.jar";
			externalProcess.StartInfo.Arguments +="\" support." + simulationMode.ToString() + " \""+filename+"\" "+budget;
			externalProcess.EnableRaisingEvents = true;
			externalProcess.Start ();
			StartCoroutine (externalNonBlockingWait ());
			return "External Async";
		}
	}
	IEnumerator externalNonBlockingWait()
	{
        // ExitCode = 0: OK
        // ExitCode = 1: System Errors
        // ExitCode = 2: Errors parsing input, use support.validate to check input
        // ExitCode = 3: Search budget depleted, no results, try a bigger budget
        // ExitCode = 4: No results found, search space exhausted (other search errors)
        // ExitCode = 5: Wrong arguments
        // ExitCode = 6: Other exception within the ME Java code

		SimulationFeedback simulationFeedback = SimulationFeedback.none;

		if (externalProcess == null) 
		{
			UnityEngine.Debug.Log ("Process is null");
		} 
		else 
		{
			while (!externalProcess.HasExited) 
			{
				yield return null;
			}
			int ExitCode = externalProcess.ExitCode;
			string mpout = "";
			string line = null;
			string filename = "";
			while ((line = externalProcess.StandardOutput.ReadLine()) != null) 
			{
				filename = line;
				mpout += line + "\n";
			}
			mpout += "Exit code: "+ExitCode.ToString ();
			UnityEngine.Debug.Log ("Java finished here...");
			if (ExitCode == 0) 
			{
				UnityEngine.Debug.Log ("It's now time to load "+filename);
				GameManager.Instance.TriggerLoadLevel( filename );
				//in case it takes time to load larger levels
				while(GameManager.Instance.gamePhase != GameManager.GamePhases.PlayerInteraction) yield return new WaitForEndOfFrame();
				simulationFeedback = SimulationFeedback.success;
			} 
			else 
			{
				UnityEngine.Debug.LogError (mpout);	
				UnityEngine.Debug.LogError (externalProcess.StartInfo.Arguments);	
				simulationFeedback = SimulationFeedback.failure;
			}
				
			GameManager.Instance.tracker.CreateEventExt("SimulationFeedback", externalProcess.ExitCode.ToString());

			externalProcess = null;	
		}
		UnityEngine.Debug.Log ("Finished waiting.");

		if(OnSimulationCompleted!=null) { OnSimulationCompleted(simulationFeedback); }
	}

	public void SendToME () 
	{
		bool java_found = false;
		if (checkEnvironment () != 0) 
		{
			UnityEngine.Debug.Log ("Cannot work here, give some feedback to the user...");
		} 
		else 
		{
			java_found = (checkJava () == 0);
		}
		if (!java_found) 
		{
			UnityEngine.Debug.LogError ("Java is not found, give some feedback to the user...");
		}
		else {
		UnityEngine.Debug.Log ("Calling Java...");
		UnityEngine.Debug.Log(externalNonBlocking ());
		UnityEngine.Debug.Log ("Finished calling Java...");
		}
	}
}
