using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Start_GamePhaseBehavior : GamePhaseBehavior {
	public GameObject startGameUI;

	public Button gameStart;
	public Button gameEnd;
	public InputField playerIdField;

	public override void BeginPhase()
	{
		startGameUI.SetActive(true);
		gameStart.onClick.AddListener( ()=> StartPlaying() );
		gameEnd.onClick.AddListener( ()=> /*Application.Quit()*/ GameManager.Instance.SetGamePhase(GameManager.GamePhases.CloseGame) );
		if(PlayerPrefs.HasKey("PlayerId")) playerIdField.text = PlayerPrefs.GetString("PlayerId");
		//gameEnd.interactable = false;
	}

	void StartPlaying(){
		GameManager.Instance.tracker.StartTracker();
		GameManager.Instance.SetGamePhase( GameManager.GamePhases.LoadScreen );
	}

	public override void UpdatePhase()
	{

	}

	public override void EndPhase()
	{
		startGameUI.SetActive(false);
	}
}
