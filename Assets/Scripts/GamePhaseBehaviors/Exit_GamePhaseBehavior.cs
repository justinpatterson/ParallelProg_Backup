using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Exit_GamePhaseBehavior : GamePhaseBehavior {
	public override void BeginPhase()
	{
        Debug.Log("Exit.");
		Application.Quit();
	}

	public override void UpdatePhase()
	{

	}

	public override void EndPhase()
	{
		
	}
}
