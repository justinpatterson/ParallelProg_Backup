using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour {
	public static GameManager Instance = null;

	public enum GamePhases {StartScreen, LoadScreen, GenerateTrack, PlayerInteraction, GradeSubmission, GradeReport, EndScreen, CloseGame}
	public GamePhases gamePhase = GamePhases.StartScreen;
	public GamePhaseBehavior startScreenBehavior, loadScreenBehavior, generateTrackBehavior, playerInteractionBehavior, gradeSubmissionBehavior, gradeReportBehavior, endScreenBehavior, exitGameBehavior;
	public bool hideTestsForBuild = false;
	GamePhaseBehavior currentPhase;

	public Tracks trackGrid;

	DataManager dataManager;
	GridManager gridManager;
	TutorialManager tutorialManager;
	LinkJava linkJava;
	public Tracker tracker;

	void Awake()
	{
		if(Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(this);
		}

		else
		{
			Destroy(this);
		}

		dataManager = GetComponent<DataManager>();
		gridManager = GetComponent<GridManager>();
		tutorialManager = GetComponent<TutorialManager>();
		linkJava = GetComponent<LinkJava>();
		tracker = GetComponent<Tracker>();

		//if( PlayerPrefs.HasKey("PlayerId") );
	}

	void Start()
	{
		SetGamePhase(GamePhases.StartScreen);
	}

	public void SetGamePhase(GamePhases inputPhase)
	{
		if(currentPhase!=null) { EndGamePhaseBehavior(); }

		gamePhase = inputPhase;
		switch(gamePhase)
		{
			case GamePhases.StartScreen:
				currentPhase = startScreenBehavior;
			break;
			case GamePhases.LoadScreen:
				currentPhase = loadScreenBehavior;
				tutorialManager.tutorialIndex = 0;
			break;
			case GamePhases.GenerateTrack:
				currentPhase = generateTrackBehavior;
			break;
			case GamePhases.PlayerInteraction:
				currentPhase = playerInteractionBehavior;
			break;
			case GamePhases.GradeSubmission:
				currentPhase = gradeSubmissionBehavior;
			break;
			case GamePhases.GradeReport:
				currentPhase = gradeReportBehavior;
			break;
			case GamePhases.EndScreen:
				currentPhase = endScreenBehavior;
			break;
            case GamePhases.CloseGame:
                currentPhase = exitGameBehavior;
                break;
        }

		BeginGamePhaseBehavior();
	}

	void BeginGamePhaseBehavior()
	{
		currentPhase.BeginPhase();
	}

	void EndGamePhaseBehavior()
	{
		switch( gamePhase )
		{
		case GamePhases.PlayerInteraction:
			gridManager.ClearGrid();
		break;
		}
		currentPhase.EndPhase();
	}

	void UpdateGamePhaseBehavior()
	{
		currentPhase.UpdatePhase();
	}

	void Update()
	{
		UpdateGamePhaseBehavior();
	}

	public void UpdatePlayerField(string inputPlayerId)
	{
		Debug.Log("Player ID is now:" + inputPlayerId);
		PlayerPrefs.SetString("PlayerId", inputPlayerId);
	}


	public void TriggerLoadLevel(string inputLevelName = "")
	{
		tracker.CreateEventExt("TriggerLoadLevel",inputLevelName);
		if(inputLevelName.Length == 0) inputLevelName = dataManager.levelname;
		dataManager.InitializeLoadLevel( inputLevelName );
		SetGamePhase(GameManager.GamePhases.GenerateTrack);
	}

	public void TriggerPCG(string inputLevelName = "")
	{
		tutorialManager.tutorialIndex = -1;
		string filename = Application.persistentDataPath + linkJava.pathSeparator + "currentParameters.txt";
		System.IO.File.WriteAllText(filename, "");
		tracker.CreateEventExt("TriggerPCG",filename);
		linkJava.filename = filename;
		//LinkJava.OnSimulationCompleted += TriggerLevelSimulation;
		linkJava.simulationMode = LinkJava.SimulationTypes.PCG;
		linkJava.SendToME();
		//SetGamePhase(GameManager.GamePhases.GenerateTrack);
	}

	public void InitiateTrackGeneration()
	{
		gridManager.GenerateGrid(/*dataManager.currentLevelData.layoutList,*/ dataManager.currentLevelData.tracks, dataManager.currentLevelData.components);
	}


	public void TriggerTrackUpdate()
	{
		//trackGrid.UpdateTracks();
	}

	public void TriggerLevelTutorial(int inputLevelId)
	{
		tutorialManager.PerformTutorialSeries( inputLevelId );
	}

	public void SubmitCurrentLevel(LinkJava.SimulationTypes inputSimulationType)
	{
		string levelToString = SerializeCurrentLevel();
		Debug.Log( levelToString );
		string filename = 
            Application.persistentDataPath 
            + linkJava.pathSeparator 
            + Constants.FilePrefixes.inputLevelFile + "_"  + inputSimulationType.ToString().ToUpper() + "_"
            + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";
        Debug.Log(filename);
        System.IO.File.WriteAllText(filename, levelToString);
		linkJava.filename = filename;
		linkJava.simulationMode = inputSimulationType;
		GameManager.Instance.tracker.CreateEventExt("SubmitCurrentLevel"+inputSimulationType.ToString(),filename);

		LinkJava.OnSimulationCompleted += TriggerLevelSimulation;
		linkJava.SendToME();
	}

	public void TriggerLevelSimulation(LinkJava.SimulationFeedback feedback)
	{
		LinkJava.OnSimulationCompleted -= TriggerLevelSimulation;

		//none indicates hitting the replay button
		if(feedback != LinkJava.SimulationFeedback.none) Debug.Log("Feedback from LinkJava to GameManager was " + feedback.ToString());

		PlayerInteraction_GamePhaseBehavior castBehavior = (PlayerInteraction_GamePhaseBehavior) playerInteractionBehavior;
		if(feedback == LinkJava.SimulationFeedback.success) castBehavior.StartSimulation();
		else 
		{
			castBehavior.playerInteraction_UI.simulationErrorOverlay.OpenPanel();
		}
	}

	public string SerializeCurrentLevel()
	{
		string levelJSON = "";
		//levelJSON = JsonUtility.ToJson(dataManager.currentLevelData, true);//dataManager.currentLevelData
		levelJSON = dataManager.GetLevelJson();
		return levelJSON;
	}
		
	public int GetLevelHeight() { return GetDataManager().currentLevelData.metadata.board_height; }
	public int GetLevelWidth() { return GetDataManager().currentLevelData.metadata.board_width; }
	public GridManager GetGridManager(){ return gridManager;}
	public DataManager GetDataManager(){ return dataManager;}

}
