﻿using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ParallelProg.UI;

#region UI Layout / UI Event Setup
[System.Serializable]
public class PlayerInteraction_UI
{
	[Header("UI Containers")]
	public RectTransform UIOverlayContainer;
	public RectTransform UICameraContainer;
	//public RectTransform UIOverlay_Pause_Container;
	[SerializeField] public Pause_UIOverlay pauseOverlay;
	//public RectTransform UIOverlay_Goal_Container;
	[SerializeField] public Goal_UIOverlay goalOverlay;
	public RectTransform UIOverlay_Hint_Container;
	public Transform hint_button_container;
	[SerializeField] public Hint_UIOverlay hintOverlay;
	[SerializeField] public Tooltip_UIOverlay tooltipOverlay;
    [SerializeField] public Lightbox_UIOverlay onHoverLightbox;
    [SerializeField] public UIOverlay loadingOverlay;
	[SerializeField] public UIOverlay simulationErrorOverlay;

	[Header("Banner Event Triggers")]
	public EventTrigger place_semaphore;
	public EventTrigger place_button;
	public EventTrigger trash;
	public EventTrigger preview;
	//public EventTrigger exit;
	public Button simulationButton;
	public Button stopSimulationButton;
	public Button submitButton;
	public Button revealHintsButton;
	public Button pauseButton;
	public EventTrigger[] rightPanelColors;
	public HintButton[] hintButtons;
	public TooltipEvent[] tooltipEvents;

	[Header("Updatable Elements")]
	public Text goalDescriptionText;
	public Text levelNameText;
	public Image draggableElement;
	public Text text_goalContainer;
	public Text text_hintPopUp;
	public Image image_hintPopUp;
	public Text levelText;
    public Image rightPanelColorLock;
	public Image topPanelConnectionLock;

	[Header("Prefabs")]
	public GameObject hint_button_prefab;

	public void OpenUI()
	{
		draggableElement.gameObject.SetActive(false);
		UICameraContainer.gameObject.SetActive(true);
		UIOverlayContainer.gameObject.SetActive(true);
		pauseOverlay.ClosePanel(true);
		goalOverlay.ClosePanel(true);
		hintOverlay.ClosePanel(true);
		tooltipOverlay.ClosePanel(true);
		levelText.text = GameManager.Instance.GetDataManager().currentLevelData.metadata.level_id.ToString();
	}

	public void CloseUI()
	{
		draggableElement.gameObject.SetActive(false);
		UICameraContainer.gameObject.SetActive(false);
		UIOverlayContainer.gameObject.SetActive(false);
		pauseOverlay.ClosePanel(true);
		goalOverlay.ClosePanel(true);
		hintOverlay.ClosePanel(true);
		tooltipOverlay.ClosePanel(true);
		UIOverlay_Hint_Container.gameObject.SetActive(false);
	}

	public void ClearButtonBehaviors()
	{
		place_semaphore.triggers.Clear();
		place_button.triggers.Clear();
		trash.triggers.Clear();
		preview.triggers.Clear();
		//exit.triggers.Clear();
	}

	public void SetDraggableElement ( Sprite inputTexture )
	{
		Debug.Log("Setting texture to " + inputTexture.name);
		draggableElement.sprite = inputTexture;
		draggableElement.gameObject.SetActive(true);
	}

	public void SetDraggableElementPosition(Vector2 inputPosition)
	{
		draggableElement.transform.position = Vector3.Lerp(draggableElement.transform.position, inputPosition, 1f);
	}

	public void ReleaseDraggableElement ()
	{
		draggableElement.gameObject.SetActive(false);
	}

	public void SetText(Level inputLevel)
	{
		levelNameText.text = inputLevel.metadata.level_title;
		goalDescriptionText.text = inputLevel.metadata.goal_string;
	}

	public IEnumerator TriggerGoalPopUp(string inputGoalText)
	{
		GameManager.Instance.tracker.CreateEventExt("TriggerGoalPopUp",inputGoalText);
		goalOverlay.SetFeedbackText( inputGoalText );
		goalOverlay.OpenPanel();
		while( goalOverlay.waitForUserInput ) { yield return new WaitForEndOfFrame(); }
		//goalOverlay.ClosePanel();
	}

	public bool IsSubPanelOpen()
	{
		return (pauseOverlay.isOpen || goalOverlay.isOpen);
	}
    
	[System.Serializable]
	public class Goal_UIOverlay : UIOverlay
	{
		public Text feedbackText;
		public Button retry, replay, levels, levelsConfirm, levelsDeny, exit, exitConfirm, exitDeny;

        [SerializeField] public UIOverlay rootOverlay, confirmExitOverlay, confirmLevelsOverlay;

        public bool waitForUserInput = false;
		public enum UserInputs {none, retry, replay, levels, exit}
		public UserInputs userInput = UserInputs.none;

		public override void OpenPanel()
		{
			waitForUserInput = true;
			base.OpenPanel();
            rootOverlay.OpenPanel();
            confirmExitOverlay.ClosePanel(true);
            confirmLevelsOverlay.ClosePanel(true);
			EnableButtonBehaviors();
		}

		public override void ClosePanel(bool forceClose = false) 
		{
			waitForUserInput = false;
			base.ClosePanel(forceClose);
		}

        public void OpenRootScreen()
        {
            rootOverlay.OpenPanel();
            confirmExitOverlay.ClosePanel();
            confirmLevelsOverlay.ClosePanel();
        }

        public void OpenExitConfirmationScreen()
        {
            rootOverlay.ClosePanel();
            confirmExitOverlay.OpenPanel();
            confirmLevelsOverlay.ClosePanel();
        }
        public void OpenLevelsConfirmationScreen()
        {
            rootOverlay.ClosePanel();
            confirmExitOverlay.ClosePanel();
            confirmLevelsOverlay.OpenPanel();
        }

		public void SetFeedbackText(string inFeedback)
		{
			feedbackText.text = inFeedback;
		}

		public void EnableButtonBehaviors()
		{
			retry.onClick.RemoveAllListeners();
			replay.onClick.RemoveAllListeners();

            levels.onClick.RemoveAllListeners();
            levelsConfirm.onClick.RemoveAllListeners();
            levelsDeny.onClick.RemoveAllListeners();

            exit.onClick.RemoveAllListeners();
            exitConfirm.onClick.RemoveAllListeners();
            exitDeny.onClick.RemoveAllListeners();

			retry.onClick.AddListener(()=> {userInput = UserInputs.retry; ClosePanel();} );
			replay.onClick.AddListener(()=> {userInput = UserInputs.replay; ClosePanel();} );
			levels.onClick.AddListener(()=> {userInput = UserInputs.levels; OpenLevelsConfirmationScreen(); } );
            levelsConfirm.onClick.AddListener(() => { ClosePanel(); } );
            levelsDeny.onClick.AddListener(() => OpenRootScreen());
			exit.onClick.AddListener(()=> {userInput = UserInputs.exit; OpenExitConfirmationScreen(); } );
            exitConfirm.onClick.AddListener(() => { GameManager.Instance.SetGamePhase(GameManager.GamePhases.CloseGame);/* Application.Quit(); /*ClosePanel();*/ });
            exitDeny.onClick.AddListener(() => OpenRootScreen());
        }
	}

	[System.Serializable]
	public class Hint_UIOverlay : UIOverlay
	{
		public Text descriptionTitle;
		public Text descriptionText;
		public Image descriptionImage;

		public Button exit;

		public override void OpenPanel()
		{
			base.OpenPanel();
			EnableButtonBehaviors();
		}

		public override void ClosePanel(bool forceClose = false) 
		{
			base.ClosePanel(forceClose);
		}

		public void SetHint(string inTitle, string inDescription, Sprite inImage)
		{
			descriptionTitle.text = inTitle;
			descriptionText.text = inDescription;
			descriptionImage.sprite = inImage;
		}

		public void EnableButtonBehaviors()
		{
			exit.onClick.RemoveAllListeners();
			exit.onClick.AddListener( ()=> ClosePanel() );
		}
	}

	[System.Serializable]
	public class Pause_UIOverlay : UIOverlay
	{
		public Button resume, exit, exitConfirmed, exitDenied;
		public bool isPaused = false;

        [SerializeField] public UIOverlay rootMenu, exitConfirmationMenu;

		public override void OpenPanel()
		{
			isPaused = true;
			base.OpenPanel();
			EnableButtonBehaviors();

            rootMenu.OpenPanel();
            exitConfirmationMenu.ClosePanel(true);
		}

		public override void ClosePanel(bool forceClose = false)
		{
			isPaused = false;
			base.ClosePanel(forceClose);
		}
		public void EnableButtonBehaviors()
		{
			resume.onClick.RemoveAllListeners();
			resume.onClick.AddListener( ()=> ClosePanel() );

            exit.onClick.RemoveAllListeners();
            exit.onClick.AddListener(() => { OpenConfirmationScreen(); });

            exitDenied.onClick.RemoveAllListeners();
            exitDenied.onClick.AddListener(() => CloseConfirmationScreen());

            exitConfirmed.onClick.RemoveAllListeners();
            exitConfirmed.onClick.AddListener( () => { OnExitConfrimed(); } );
			//exit uses a PlayerInteraction_GamePhaseBehavior method so I define it there
		}

        void OpenConfirmationScreen()
        {
            rootMenu.ClosePanel();
            exitConfirmationMenu.OpenPanel();
        }

        void CloseConfirmationScreen()
        {
            rootMenu.OpenPanel();
            exitConfirmationMenu.ClosePanel();
        }

        void OnExitConfrimed()
        {
            PlayerInteraction_GamePhaseBehavior playPhase = (PlayerInteraction_GamePhaseBehavior)GameManager.Instance.playerInteractionBehavior;
            playPhase.TriggerPlayPhaseEnd();
        }
	}

	[System.Serializable]
	public class Tooltip_UIOverlay : UIOverlay 
	{
		public Text tooltipText;
		public bool tooltipActive = false;
		public float openTime = 0f;

		public override void OpenPanel()
		{
			base.OpenPanel();
			tooltipActive = true;
			openTime = Time.time;
		}

		public override void ClosePanel(bool forceClose = false) 
		{
			base.ClosePanel(forceClose);
			tooltipActive = false;
		}

		public void SetTooltip(string inDescription, Vector3 mousePosition)
		{
			mousePosition.y = Screen.height * 0.9f;
			panelContainer.position = mousePosition;
			tooltipText.text = inDescription;
		}
	}

    [System.Serializable]
    public class Lightbox_UIOverlay : UIOverlay
    {
        public Transform lightboxElement;
        public override void OpenPanel()
        {
            isOpen = true;
            lightboxElement.gameObject.transform.localScale = Vector3.zero;

            lightboxElement.gameObject.SetActive(true); SpriteRenderer sr = lightboxElement.GetComponent<SpriteRenderer>();
            float width = sr.sprite.bounds.size.x;
            float height = sr.sprite.bounds.size.y;

            GameObject worldCamera = GameObject.Find("UICamera");
            if (worldCamera == null) worldCamera = Camera.main.gameObject;
            double worldScreenHeight = worldCamera.GetComponent<Camera>().orthographicSize * 2.0;
            double worldScreenWidth = worldScreenHeight / Screen.height * Screen.width;
            Vector3 targetScale = new Vector3((float)worldScreenWidth / width, (float)worldScreenHeight / height, 1f);

            iTween.ScaleTo(lightboxElement.gameObject, iTween.Hash("scale", targetScale, "time", 0.5f));
        }

        public override void ClosePanel(bool forceClose = false)
        {
            isOpen = false;
            if (forceClose)
            {
                lightboxElement.gameObject.SetActive(false);
            }
            else
            {
                iTween.ScaleTo(lightboxElement.gameObject, iTween.Hash("scale", Vector3.zero, "time", 0.5f));
            }
        }
    }
}
#endregion