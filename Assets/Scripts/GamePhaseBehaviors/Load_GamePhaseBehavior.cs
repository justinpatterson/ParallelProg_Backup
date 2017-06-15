using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using ParallelProg.UI;

public class Load_GamePhaseBehavior : GamePhaseBehavior {

	[System.Serializable]
	public class Load_UI
	{
		public GameObject loadPhaseUI;
		public RectTransform levelButtonContainer;
		public GameObject levelButtonPrefab;
        public Button exitLevelSelectionButton;
        [SerializeField] public UIOverlay levelLoadingOverlay;
	}
	public Load_UI loadUI;


	public override void BeginPhase()
	{
		foreach(Transform child in loadUI.levelButtonContainer)
		{
			GameObject.Destroy(child.gameObject);
		}


		foreach( string levelName in  GameManager.Instance.GetDataManager().allLevelNames ) 
		{
			if(GameManager.Instance.hideTestsForBuild && levelName.StartsWith("level-")){
				continue;
			}	
			string targetLevel = levelName;
			GameObject g = Instantiate(loadUI.levelButtonPrefab) as GameObject;
			Button gButton = g.GetComponent<Button>();
			g.transform.SetParent( loadUI.levelButtonContainer );
			g.transform.localScale = Vector3.one;
			Text gText = g.GetComponentInChildren<Text>();
			gText.text = levelName;
			gButton.onClick.AddListener( ()=> LoadButtonBehavior(targetLevel) );
		}
		if(!GameManager.Instance.hideTestsForBuild){
			AddPCGButton();
		}

        loadUI.levelLoadingOverlay.ClosePanel(true);

        loadUI.loadPhaseUI.SetActive(true);

        loadUI.exitLevelSelectionButton.onClick.AddListener(() => GameManager.Instance.SetGamePhase(GameManager.GamePhases.StartScreen));
	}
	public void AddPCGButton(){
			GameObject g = Instantiate(loadUI.levelButtonPrefab) as GameObject;
			Button gButton = g.GetComponent<Button>();
			g.transform.SetParent( loadUI.levelButtonContainer );
			g.transform.localScale = Vector3.one;
			Text gText = g.GetComponentInChildren<Text>();
			gText.text = "PCG";
			gButton.onClick.AddListener( ()=> LoadPCGBehavior() );
	}
	public override void UpdatePhase()
	{
	}

	public override void EndPhase()
	{
        if(loadUI.levelLoadingOverlay.isOpen) loadUI.levelLoadingOverlay.ClosePanel();
        loadUI.loadPhaseUI.SetActive(false);
	}

	public void LoadButtonBehavior( string levelName ) 
	{
        loadUI.levelLoadingOverlay.OpenPanel();
        GameManager.Instance.TriggerLoadLevel( levelName );
	}
    public void LoadPCGBehavior()
    {
        loadUI.levelLoadingOverlay.OpenPanel();
        GameManager.Instance.TriggerPCG();
    }
}
