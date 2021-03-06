﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class PlayerInteraction_GamePhaseBehavior : GamePhaseBehavior {
	public enum InteractionPhases { ingame_default, ingame_dragging, ingame_connecting, simulation }
	public InteractionPhases interactionPhase = InteractionPhases.simulation;

	public enum InGamePhases { none, optionClicked, movingObject, placingObject }
	public enum MenuOptions { pause, semaphore, button, trash, simulate }

	bool flowVisibility = false;
	public bool trashHover = false;
	bool connectVisibility;
    public bool connectVisibilityLock = false;
	public bool colorFlowVisibilityLock = false;

	public PlayerInteraction_UI playerInteraction_UI;

	GridObjectBehavior currentGridObject;

	float simulationTime = 0f;
	float stationaryTime = 0f;
	Dictionary<int, List<StepData>> simulationDStepDictionary = new Dictionary<int, List<StepData>>();

	Vector3 stationaryMousePosition;
	GridObjectBehavior hoverObject;
	GridObjectBehavior hoverObject_;

	public LayerMask defaultMask, draggingMask;

	public override void BeginPhase()
	{
		playerInteraction_UI.SetText ( GameManager.Instance.GetDataManager().currentLevelData );
		playerInteraction_UI.OpenUI();
		GameManager.Instance.TriggerLevelTutorial( GameManager.Instance.GetDataManager().currentLevelData.metadata.level_id );
		DefineButtonBehaviors();
    }

	public override void UpdatePhase()
	{
		if(interactionPhase == InteractionPhases.simulation)
		{
			GameManager.Instance.TriggerTrackUpdate();
		}
		else 
		{
			PlayerInteractionListener();
		}
	}

	public override void EndPhase()
	{
		playerInteraction_UI.CloseUI();
        LockFlowVisibility(-1);
    }


	public void DefineButtonBehaviors()
	{
		playerInteraction_UI.ClearButtonBehaviors();
		foreach(Transform t in playerInteraction_UI.hint_button_container) { Destroy(t.gameObject); }

		/* semaphore placement events */
			EventTrigger.Entry beginDrag_semaphore = new EventTrigger.Entry();
			beginDrag_semaphore.eventID = EventTriggerType.BeginDrag;
			beginDrag_semaphore.callback.AddListener( (eventData) => { BeginDrag(MenuOptions.semaphore); } );
			playerInteraction_UI.place_semaphore.triggers.Add(beginDrag_semaphore);

			EventTrigger.Entry continueDrag_semaphore = new EventTrigger.Entry();
			continueDrag_semaphore.eventID = EventTriggerType.Drag;
			continueDrag_semaphore.callback.AddListener( (eventData) => { ContinueDrag(MenuOptions.semaphore); } );
			playerInteraction_UI.place_semaphore.triggers.Add(continueDrag_semaphore);

			EventTrigger.Entry endDrag_semaphore = new EventTrigger.Entry();
			endDrag_semaphore.eventID = EventTriggerType.EndDrag;
			endDrag_semaphore.callback.AddListener( (eventData) => { EndDrag(MenuOptions.semaphore); } );
			playerInteraction_UI.place_semaphore.triggers.Add(endDrag_semaphore);

		/* signal placement events */
			EventTrigger.Entry beginDrag_button = new EventTrigger.Entry();
			beginDrag_button.eventID = EventTriggerType.BeginDrag;
			beginDrag_button.callback.AddListener( (eventData) => { BeginDrag(MenuOptions.button); } );
			playerInteraction_UI.place_button.triggers.Add(beginDrag_button);
				
			EventTrigger.Entry continueDrag_button = new EventTrigger.Entry();
			continueDrag_button.eventID = EventTriggerType.Drag;
			continueDrag_button.callback.AddListener( (eventData) => { ContinueDrag(MenuOptions.button); } );
			playerInteraction_UI.place_button.triggers.Add(continueDrag_button);

			EventTrigger.Entry endDrag_button = new EventTrigger.Entry();
			endDrag_button.eventID = EventTriggerType.EndDrag;
			endDrag_button.callback.AddListener( (eventData) => { EndDrag(MenuOptions.button); } );
			playerInteraction_UI.place_button.triggers.Add(endDrag_button);

		/* trash events */
			EventTrigger.Entry hover_trash = new EventTrigger.Entry();
			hover_trash.eventID = EventTriggerType.PointerEnter;
			hover_trash.callback.AddListener( (eventData) => { BeginHover(MenuOptions.trash); } );
			playerInteraction_UI.trash.triggers.Add(hover_trash);

			EventTrigger.Entry endHover_trash = new EventTrigger.Entry();
			endHover_trash.eventID = EventTriggerType.PointerExit;
			endHover_trash.callback.AddListener( (eventData) => { EndHover(MenuOptions.trash); } );
			playerInteraction_UI.trash.triggers.Add(endHover_trash);
		
        /* Bezier Visibility */
			/*Button flowButton = playerInteraction_UI.preview.GetComponent<Button>();
			flowButton.onClick.RemoveAllListeners();
			flowButton.onClick.AddListener( ()=> ToggleConnectionVisibility() );
            */
            EventTrigger.Entry hover_bezier = new EventTrigger.Entry();
            hover_bezier.eventID = EventTriggerType.PointerEnter;
            hover_bezier.callback.AddListener((eventData) => { connectVisibility = false; ToggleConnectionVisibility(); });
            playerInteraction_UI.preview.triggers.Add(hover_bezier);

			EventTrigger.Entry click_bezier = new EventTrigger.Entry();
			click_bezier.eventID = EventTriggerType.PointerDown;
			click_bezier.callback.AddListener((eventData) => { LockConnectionVisibility(); });
			playerInteraction_UI.preview.triggers.Add(click_bezier);

            EventTrigger.Entry endHover_bezier = new EventTrigger.Entry();
            endHover_trash.eventID = EventTriggerType.PointerExit;
            endHover_trash.callback.AddListener((eventData) => { connectVisibility = true; ToggleConnectionVisibility(); });
            playerInteraction_UI.preview.triggers.Add(endHover_trash);

        /* Exit */
            //Note: Exit Button behavior is handled in playerInteraction_UI.pauseOverlay


		LinkJava.SimulationTypes playSimulation = LinkJava.SimulationTypes.Play;
		playerInteraction_UI.simulationButton.onClick.RemoveAllListeners();
        playerInteraction_UI.simulationButton.onClick.AddListener(() => TriggerSimulation(playSimulation)/*GameManager.Instance.SubmitCurrentLevel(playSimulation)*/ );
		playerInteraction_UI.simulationButton.onClick.AddListener( ()=> playerInteraction_UI.simulationButton.interactable = false );
		playerInteraction_UI.simulationButton.interactable = true;

		playerInteraction_UI.stopSimulationButton.onClick.RemoveAllListeners();
		playerInteraction_UI.stopSimulationButton.onClick.AddListener( ()=> EndSimulation() );
		playerInteraction_UI.stopSimulationButton.interactable = false;
		playerInteraction_UI.stopSimulationButton.gameObject.SetActive(false);

		LinkJava.SimulationTypes fullSimulation = LinkJava.SimulationTypes.ME;
		playerInteraction_UI.submitButton.onClick.RemoveAllListeners();
		playerInteraction_UI.submitButton.onClick.AddListener( ()=> TriggerSimulation(fullSimulation)/*GameManager.Instance.SubmitCurrentLevel(fullSimulation)*/ );
		playerInteraction_UI.submitButton.onClick.AddListener( ()=> playerInteraction_UI.submitButton.interactable = false );
		playerInteraction_UI.submitButton.interactable = true;

		playerInteraction_UI.revealHintsButton.onClick.RemoveAllListeners();
		playerInteraction_UI.revealHintsButton.onClick.AddListener( ()=> ToggleHintsVisibility() );

/* Track Color Hover Setup */
		for(int triggerIndex = 0; triggerIndex < playerInteraction_UI.rightPanelColors.Length; triggerIndex++)
		{
            playerInteraction_UI.rightPanelColors[triggerIndex].triggers.Clear();
            if ( GameManager.Instance.GetGridManager().IsCurrentThreadColor( triggerIndex ) )
			{
				playerInteraction_UI.rightPanelColors[triggerIndex].gameObject.SetActive(true);
				int loadColors = triggerIndex;// + 1;

                EventTrigger.Entry beginHoverColor = new EventTrigger.Entry();
				beginHoverColor.eventID = EventTriggerType.PointerEnter;
				beginHoverColor.callback.AddListener( (eventData) => {
					if ( !/*connectVisibilityLock*/colorFlowVisibilityLock )  GameManager.Instance.GetGridManager().RevealGridColorMask(loadColors);
                    ToggleFlowVisibility(true);
                } );
				playerInteraction_UI.rightPanelColors[triggerIndex].triggers.Add(beginHoverColor);

                EventTrigger.Entry lockHoverColor = new EventTrigger.Entry();
                lockHoverColor.eventID = EventTriggerType.PointerDown;
                int lockIndex = triggerIndex;
                lockHoverColor.callback.AddListener((eventData) => { LockFlowVisibility(lockIndex); });
                playerInteraction_UI.rightPanelColors[triggerIndex].triggers.Add(lockHoverColor);


                EventTrigger.Entry endHoverColor = new EventTrigger.Entry();
				endHoverColor.eventID = EventTriggerType.PointerExit;
				endHoverColor.callback.AddListener( (eventData) => {
					if ( !/*connectVisibilityLock*/colorFlowVisibilityLock ) GameManager.Instance.GetGridManager().HideGridColorMask();
                    ToggleFlowVisibility(false);
                } );
				playerInteraction_UI.rightPanelColors[triggerIndex].triggers.Add(endHoverColor);
			}
			else 
			{
//				Debug.Log(playerInteraction_UI.rightPanelColors[triggerIndex].gameObject.name);
				playerInteraction_UI.rightPanelColors[triggerIndex].gameObject.SetActive(false);
			}
		}

/* Question Mark Hint Setup */
		for(int hintIndex = 0; hintIndex < playerInteraction_UI.hintButtons.Length; hintIndex++)
		{
			HintConstructor h = playerInteraction_UI.hintButtons[hintIndex].hint;
			if(playerInteraction_UI.hintButtons[hintIndex].levelIds.Count == 0 || playerInteraction_UI.hintButtons[hintIndex].levelIds.Contains( GameManager.Instance.GetDataManager().currentLevelData.metadata.level_id )) 
			{
				GameObject hintInstance = Instantiate( playerInteraction_UI.hint_button_prefab, playerInteraction_UI.hint_button_container ) as GameObject;
				hintInstance.transform.localScale = Vector3.one;
				Button instanceButton = hintInstance.GetComponent<Button>();
				Image instanceImage = hintInstance.GetComponent<Image>();
				instanceButton.onClick.AddListener( ()=> TriggerHint( h.hintTitle, h.hintDescription, h.hintImage ) );

				if(playerInteraction_UI.hintButtons[hintIndex].hint.hintButtonImage!=null)
				{
					//	playerInteraction_UI.hintButtons[hintIndex].hintButtonTrigger.GetComponent<Image>().sprite = playerInteraction_UI.hintButtons[hintIndex].hint.hintButtonImage;
					instanceImage.sprite =  playerInteraction_UI.hintButtons[hintIndex].hint.hintButtonImage;
				}
				else if(playerInteraction_UI.hintButtons[hintIndex].hint.hintImage!=null)
				{
					//playerInteraction_UI.hintButtons[hintIndex].hintButtonTrigger.GetComponent<Image>().sprite = playerInteraction_UI.hintButtons[hintIndex].hint.hintImage;
					instanceImage.sprite =  playerInteraction_UI.hintButtons[hintIndex].hint.hintImage;
				}
				//playerInteraction_UI.hintButtons[hintIndex].hintButtonTrigger.onClick.AddListener( ()=> TriggerHint( h.hintTitle, h.hintDescription, h.hintImage ) );	
			}
		}
/* Toolbar Tooltips */
		foreach(TooltipEvent t in playerInteraction_UI.tooltipEvents)
		{
			EventTrigger.Entry beginHover_event = new EventTrigger.Entry();
			string tooltipText = t.tooltipContent.tooltipText;
			string tooltipName = t.tooltipUiElement.name;
			beginHover_event.eventID = EventTriggerType.PointerEnter;
			Debug.Log("Adding listener for " + t.tooltipUiElement.name);
			beginHover_event.callback.AddListener( (eventData) => { if(interactionPhase == InteractionPhases.ingame_default) { playerInteraction_UI.tooltipOverlay.OpenPanel(); playerInteraction_UI.tooltipOverlay.SetTooltip(tooltipText, Input.mousePosition); GameManager.Instance.tracker.CreateEventExt("tooltip",tooltipName);} } );
			t.tooltipUiElement.triggers.Add(beginHover_event);
		}
	}

	public void TriggerHint(string title, string description, Sprite texture)
	{
		GameManager.Instance.tracker.CreateEventExt("hint",title);
		playerInteraction_UI.hintOverlay.OpenPanel();
		playerInteraction_UI.hintOverlay.SetHint(title,description,texture);
	}

	public void BeginDrag(MenuOptions selectedOption)
	{
		if(interactionPhase != InteractionPhases.ingame_default) return;

		Sprite[] spriteSheet = Resources.LoadAll<Sprite>("Sprites/gridsprites_v3") as Sprite[];
		GameManager.Instance.tracker.CreateEventExt("startDrag",selectedOption.ToString());
		switch(selectedOption)
		{
			case MenuOptions.semaphore:
				foreach(Sprite s in spriteSheet)
				{
					if(s.name == "semaphore")
					{
						playerInteraction_UI.SetDraggableElement( s );	
					}
				}
			break;
			case MenuOptions.button:
				foreach(Sprite s in spriteSheet)
				{
					if(s.name == "button_up")
					{
						playerInteraction_UI.SetDraggableElement( s );	
					}
				}
			break;
		}
        Vector3 draggableItemScale = Vector3.one;
        draggableItemScale *= 5f /* default ortho size */ / (GameManager.Instance.GetGridManager().worldCamera.orthographicSize) /* current ortho size */;
        playerInteraction_UI.draggableElement.transform.localScale = draggableItemScale;
    }
	public void ContinueDrag(MenuOptions selectedOption)
	{
		playerInteraction_UI.SetDraggableElementPosition(Input.mousePosition);
	}

	public void EndDrag(MenuOptions selectedOption)
	{
		if(interactionPhase != InteractionPhases.ingame_default) return;
		playerInteraction_UI.ReleaseDraggableElement();
		GameManager.Instance.tracker.CreateEventExt("startDrag",selectedOption.ToString());
		if( GameManager.Instance.GetGridManager().IsValidLocation(Input.mousePosition) && !GameManager.Instance.GetGridManager().IsOccupied(Input.mousePosition) ) 
		{ 
			GameManager.Instance.GetGridManager().PlaceGridElementAtLocation( Input.mousePosition, selectedOption ); 
		}
	}

	public void BeginHover(MenuOptions selectedOption)
	{
		GameManager.Instance.tracker.CreateEventExt("beginHover",selectedOption.ToString());
		switch(selectedOption)
		{
			case MenuOptions.trash:
			trashHover = true;
			break;
		}
	}
	public void EndHover(MenuOptions selectedOption)
	{
		GameManager.Instance.tracker.CreateEventExt("endHover",selectedOption.ToString());
		switch(selectedOption)
		{
			case MenuOptions.trash:
			trashHover = false;
			break;
		}
	}



	void ResetStartValues()
	{
		List<GridObjectBehavior> resetObjects = GameManager.Instance.GetGridManager().GetGridComponentsOfType(new List<string>(){"thread","delivery","pickup","exchange","semaphore"});
		foreach(GridObjectBehavior resetObject in resetObjects)
		{
				resetObject.ResetPosition();
		}

		flowVisibility = false;
    }

    void TriggerSimulation(LinkJava.SimulationTypes simulationType)
    {
        playerInteraction_UI.loadingOverlay.OpenPanel();
        GameManager.Instance.SubmitCurrentLevel(simulationType);
    }

	public void StartSimulation()
	{
		if(interactionPhase != InteractionPhases.ingame_default) return;
        playerInteraction_UI.loadingOverlay.ClosePanel();
        interactionPhase = InteractionPhases.simulation;

		GridObjectBehavior[] gridObjs = GameManager.Instance.GetGridManager().RetrieveComponentsOfType("thread");
		foreach(GridObjectBehavior g in gridObjs) g.GetComponent<SpriteRenderer>().sortingOrder = Constants.ComponentSortingOrder.thread_simulation;

		playerInteraction_UI.simulationButton.interactable = false;
		playerInteraction_UI.simulationButton.gameObject.SetActive(false);
		playerInteraction_UI.submitButton.interactable = false;
		//playerInteraction_UI.submitButton.gameObject.SetActive(false);
		playerInteraction_UI.stopSimulationButton.interactable = true;
		playerInteraction_UI.stopSimulationButton.gameObject.SetActive( true );

		simulationTime = 0f;
		playerInteraction_UI.goalOverlay.userInput = PlayerInteraction_UI.Goal_UIOverlay.UserInputs.none;
		StartCoroutine( SimulationBehavior() );
	}

	void EndSimulation()
	{
		StopCoroutine( SimulationBehavior() );

		interactionPhase = InteractionPhases.ingame_default;

		ResetStartValues();

		playerInteraction_UI.simulationButton.interactable = true;
		playerInteraction_UI.simulationButton.gameObject.SetActive(true);
		playerInteraction_UI.submitButton.interactable = true;
		playerInteraction_UI.stopSimulationButton.interactable = false;
		playerInteraction_UI.simulationButton.gameObject.SetActive(true);

	}
		
	public void PlayerInteractionListener()
	{
		switch(interactionPhase)
		{
			case InteractionPhases.ingame_default:
				if(playerInteraction_UI.IsSubPanelOpen()) return;
                /*
                 * if player LEFT clicks during basic play, they can 
                 * (1) Click and drag movable elements
                */
				if(Input.GetKeyDown(KeyCode.Mouse0))
				{
					if( GameManager.Instance.GetGridManager().IsEditableElement( Input.mousePosition ) )
					{
						currentGridObject = GameManager.Instance.GetGridManager().RetrieveEditableGridObject( Input.mousePosition );
						currentGridObject.BeginDrag();
						interactionPhase = InteractionPhases.ingame_dragging;
						GameManager.Instance.tracker.CreateEventExt("BeginReposition",currentGridObject.component.type);
                        
                        if(currentGridObject.component.type == "signal" && connectVisibilityLock)
                        {
                            Signal_GridObjectBehavior s = (Signal_GridObjectBehavior)currentGridObject;
                            s.SetHighlight(false);
                        }
                    }
					if(hoverObject) 
					{
						if(!connectVisibility) hoverObject.EndHoverBehavior();
						hoverObject = null;
					}
				}
                /*
                * if player RIGHT clicks during basic play, they can:
                * (1) link connectable elements through Signals
                * (2) Open/Close Semaphores
               */
                else if (Input.GetKeyDown(KeyCode.Mouse1))
				{
					if( GameManager.Instance.GetGridManager().IsObjectOfType( Input.mousePosition, "signal" ) /*&& GameManager.Instance.GetGridManager().IsEditableElement( Input.mousePosition )*/ )
					{
						currentGridObject = GameManager.Instance.GetGridManager().RetrieveGridObjectOfType( Input.mousePosition, "signal" );
						currentGridObject.EnableGridObjectEventBehaviors(GridObjectBehavior.InteractTypes.rightClick);
						interactionPhase = InteractionPhases.ingame_connecting;
						currentGridObject.BeginInteraction();
						
						List<GridObjectBehavior> otherSignals = GameManager.Instance.GetGridManager().GetGridComponentsOfType(new List<string>(){"signal"});
						foreach(GridObjectBehavior otherSignal in otherSignals)
						{
						if(currentGridObject != otherSignal ) { otherSignal.GetComponent<SpriteRenderer>().sortingOrder = Constants.ComponentSortingOrder.connectionOverlay-1;}
						}

						GameManager.Instance.tracker.CreateEventExt("BeginLink",currentGridObject.component.type);

						playerInteraction_UI.onHoverLightbox.OpenPanel();

					}
					else if( GameManager.Instance.GetGridManager().IsObjectOfType( Input.mousePosition, "semaphore" ) && GameManager.Instance.GetGridManager().IsEditableElement( Input.mousePosition ) )
					{
						currentGridObject = GameManager.Instance.GetGridManager().RetrieveGridObjectOfType( Input.mousePosition, "semaphore" );
						currentGridObject.EnableGridObjectEventBehaviors(GridObjectBehavior.InteractTypes.rightClick);
						currentGridObject.BeginInteraction();
						GameManager.Instance.tracker.CreateEventExt("BeginLink",currentGridObject.component.type);
					}

					if(hoverObject /*&& !connectVisibilityLock*/) 
					{
						hoverObject.EndHoverBehavior();
						hoverObject = null;
					}
				}
                /*
                 * if a player isn't clicking the mouse, we should check for hover behaviors
                */
				else
				{
					if( Input.mousePosition == stationaryMousePosition) //if mouse is stationary
                    {
						if(hoverObject==null)
						{
							stationaryTime+=Time.deltaTime;
							if( stationaryTime >= 0.2f ) 
							{
							if ( 
									GameManager.Instance.GetGridManager().IsObjectOfType( Input.mousePosition, "signal" ) 
									|| GameManager.Instance.GetGridManager().IsObjectOfType( Input.mousePosition, "diverter" ) 
									|| GameManager.Instance.GetGridManager().IsObjectOfType( Input.mousePosition, "exchange" )
									|| GameManager.Instance.GetGridManager().IsObjectOfType( Input.mousePosition, "delivery" )
									|| GameManager.Instance.GetGridManager().IsObjectOfType( Input.mousePosition, "pickup" ) 
									|| GameManager.Instance.GetGridManager().IsObjectOfType( Input.mousePosition, "conditional") 
									|| GameManager.Instance.GetGridManager().IsObjectOfType( Input.mousePosition, "semaphore") 
								)
								{
									hoverObject  = GameManager.Instance.GetGridManager().GetGridObjectByMousePosition( Input.mousePosition );
									hoverObject.OnHoverBehavior();
									GameManager.Instance.tracker.CreateEventExt("OnHoverBehavior",hoverObject.component.type);
								}
							}
						}
					}
					else //if mouse has moved since last frame 
					{
						stationaryMousePosition = Input.mousePosition;
						if (hoverObject)
						{
							if (GameManager.Instance.GetGridManager().IsOccupied(Input.mousePosition))
							{
								if (hoverObject != GameManager.Instance.GetGridManager().GetGridObjectByMousePosition(Input.mousePosition))
								{ EndHoverEvent(); }
							}
							else { EndHoverEvent(); }
						}
						else
						{
							stationaryTime = 0f;
						}

                        //pop up tooltip close check
						if(playerInteraction_UI.tooltipOverlay.tooltipActive && Time.time - playerInteraction_UI.tooltipOverlay.openTime > 0.5f)
                        { playerInteraction_UI.tooltipOverlay.ClosePanel();}	
					}
					stationaryMousePosition = Input.mousePosition;
					GridObjectBehavior hoverObject__  = GameManager.Instance.GetGridManager().GetGridObjectByMousePosition( Input.mousePosition );
					if(hoverObject__!=null && hoverObject__!=hoverObject_){
						hoverObject_ = hoverObject__;
						if(hoverObject_.component!=null){
							GameManager.Instance.tracker.CreateEventExt("OnMouseComponent",hoverObject_.component.type.ToString()+"/"+hoverObject_.component.id.ToString());	
						}
					}
					if(hoverObject__==null && hoverObject_!=null){
						GameManager.Instance.tracker.CreateEventExt("OutMouseComponent",hoverObject_.component.type.ToString()+"/"+hoverObject_.component.id.ToString());	
						hoverObject_= null;
					}

				}
			break;
		case InteractionPhases.ingame_dragging:
			if(Input.GetKey(KeyCode.Mouse0))
			{
				if( currentGridObject != null )
				{
					currentGridObject.ContinueDrag();
					if(trashHover) 
					{
					}
					else 
					{
					}
				}
				else 
				{
					interactionPhase = InteractionPhases.ingame_default;
				}
			}
			else 
			{
				if(trashHover)
				{
					GameManager.Instance.GetGridManager().ForgetGridElement( currentGridObject );
					if( currentGridObject.component.configuration.link != null && currentGridObject.component.configuration.link > 0 )
					{
					//	GridObjectBehavior g = GameManager.Instance.GetGridManager().GetGridObjectByID( currentGridObject.component.configuration.link );
					//	g.component.configuration.link = 0;
					}
					GameManager.Instance.tracker.CreateEventExt("Destroying",currentGridObject.component.type);	
					Destroy( currentGridObject.gameObject );
					currentGridObject = null;
					interactionPhase = InteractionPhases.ingame_default;
					
				}
				else 
				{
					GameManager.Instance.tracker.CreateEventExt("EndReposition",currentGridObject.component.type);	
					currentGridObject.EndDrag();

                    if (currentGridObject.component.type == "signal" && connectVisibilityLock)
                    {
                        Signal_GridObjectBehavior s = (Signal_GridObjectBehavior)currentGridObject;
                        s.SetHighlight(true);
                    }

                    currentGridObject = null;
					interactionPhase = InteractionPhases.ingame_default;
                }


			}
		break;
		case InteractionPhases.ingame_connecting:

			if(Input.GetKeyDown(KeyCode.Mouse1))
			{
				currentGridObject.EndInteraction();
				if( GameManager.Instance.GetGridManager().IsObjectOfType(Input.mousePosition, "semaphore") ) 
				{
					GridObjectBehavior g = GameManager.Instance.GetGridManager().GetGridObjectByMousePosition(Input.mousePosition);
					currentGridObject.LinkTo( g );
					GameManager.Instance.tracker.CreateEventExt("LinkTo",currentGridObject.component.type);
				}

				else if( GameManager.Instance.GetGridManager().IsObjectOfType(Input.mousePosition, "conditional") ) 
				{
					GridObjectBehavior g = GameManager.Instance.GetGridManager().GetGridObjectByMousePosition(Input.mousePosition);
					currentGridObject.LinkTo( g );
					GameManager.Instance.tracker.CreateEventExt("LinkTo",currentGridObject.component.type);
				}

				playerInteraction_UI.onHoverLightbox.ClosePanel();

				List<GridObjectBehavior> otherSignals = GameManager.Instance.GetGridManager().GetGridComponentsOfType(new List<string>(){"signal"});
				foreach(GridObjectBehavior otherSignal in otherSignals)
				{
					if(currentGridObject != otherSignal ) { otherSignal.GetComponent<SpriteRenderer>().sortingOrder = Constants.ComponentSortingOrder.connectionComponents;}
					if(connectVisibilityLock) otherSignal.SetHighlight( true );
				}


				interactionPhase = InteractionPhases.ingame_default;
			}
			else 
			{
				currentGridObject.ContinueInteraction();
			}
		break;
		case InteractionPhases.simulation:
			simulationTime+=Time.deltaTime;
		break;
		}

	}

	IEnumerator SimulationBehavior()
	{
		Level lvl = GameManager.Instance.GetDataManager().currentLevelData;
		int currentStep = 0;
		int maxStep = 0;
		int maxGoalsCompleted = 0;
		Dictionary<int,List<StepData>> stepDictionary = new Dictionary<int, List<StepData>>();
		Dictionary<int,List<int>> componentStepsDictionary = new Dictionary<int,List<int>>();

		for( int i = 0; i < lvl.execution.Count; i++ ) 
		{
			StepData step = lvl.execution[i];

			if(step.timeStep > maxStep)
			{
				maxStep = step.timeStep;
			}

			if( step.eventType == "M") 
			{
				if( !componentStepsDictionary.ContainsKey( step.componentID ) ) { componentStepsDictionary.Add(step.componentID, new List<int>()); componentStepsDictionary[step.componentID].Add(i); }
				else { componentStepsDictionary[step.componentID].Add(i); }
			}

			if( stepDictionary.ContainsKey( step.timeStep ) )
			{
				if(step.eventType=="D")
				{
					stepDictionary[ step.timeStep ].Insert( 0, step );
				}
				else
				{
					stepDictionary[ step.timeStep ].Add( step );	
				}
			}
			else 
			{
				stepDictionary[ step.timeStep ] = new List<StepData>();
				stepDictionary[ step.timeStep ].Add( step );
			}
		}

		foreach( int componentId in componentStepsDictionary.Keys )
		{
			//componentStepsDictionary[componentId].Sort();
			for(int listIndex = 0; listIndex < componentStepsDictionary[componentId].Count-1; listIndex++)
			{
				int executionIndex = componentStepsDictionary[componentId][listIndex];
				int nextExecutionIndex = componentStepsDictionary[componentId][listIndex+1];
				lvl.execution[executionIndex].SetNextStep(nextExecutionIndex);
			}
		}
			
		while(interactionPhase == InteractionPhases.simulation && currentStep <= maxStep)
		{
			if( stepDictionary.ContainsKey(currentStep) ) 
			{
				foreach( StepData step in stepDictionary[currentStep] )
				{
					if(step.componentID==0)
					{
						if(step.componentStatus == null) continue;
						if(step.componentStatus.goals_completed != null)
						{
							if(maxGoalsCompleted < step.componentStatus.goals_completed) { maxGoalsCompleted = step.componentStatus.goals_completed; }
						}
						if(step.componentStatus.final_condition != null && step.componentStatus.final_condition != -1)
						{
							string goalString = "";
							switch(step.componentStatus.final_condition)
							{
							case 2:
							case 8:
							case 10:
								goalString = "Successfully completed the level!";
								break;
							default:
								goalString = "";
								if ((step.componentStatus.final_condition & 1)!=0) {
									goalString += "You hit a dead end. ";
								}
								if ((step.componentStatus.final_condition & 16)!=0) {
									goalString += "You missed some deliveries. ";
								}
								if ((step.componentStatus.final_condition & 32)!=0) {
									goalString += "You " +
										"have starvation. ";
								}
								goalString += "Sorry, try again!";
								break;
							}

							yield return StartCoroutine( playerInteraction_UI.TriggerGoalPopUp(goalString) );
						}
					}
					else
					{
						GridObjectBehavior g = GameManager.Instance.GetGridManager().GetGridObjectByID( step.componentID );
						if(g!=null) 
						{
							g.DoStep( step );
						}
						else { Debug.Log("Could not find " + step.componentID); }
					}
				}
				yield return new WaitForSeconds(0.5f);
			}
			currentStep ++;
			Debug.Log(currentStep);
		}


		//yield return new WaitForSeconds(1f);
		//todo: switch statement of the selected goal option

		ResetStartValues();

		switch( playerInteraction_UI.goalOverlay.userInput )
		{
			case PlayerInteraction_UI.Goal_UIOverlay.UserInputs.exit:
			case PlayerInteraction_UI.Goal_UIOverlay.UserInputs.levels:
				TriggerPlayPhaseEnd();
				EndSimulation();
			break;
			case PlayerInteraction_UI.Goal_UIOverlay.UserInputs.replay:
				
				interactionPhase = InteractionPhases.ingame_default;
				GameManager.Instance.TriggerLevelSimulation( LinkJava.SimulationFeedback.none );

			break;
			default:
				GameManager.Instance.TriggerLoadLevel();
				EndSimulation();
			break;
		}
	}



	public void ToggleFlowVisibility()
	{
		flowVisibility = !flowVisibility;
		GameManager.Instance.tracker.CreateEventExt("ToggleFlowVisibility",flowVisibility.ToString());

		GridObjectBehavior[] tracks = GameManager.Instance.GetGridManager().RetrieveTracks();//.RetrieveComponentsOfType("intersection");
		foreach(GridObjectBehavior track in tracks)
		{
			if(flowVisibility) { track.BeginInteraction(); }
			else { track.EndInteraction(); }
		}
	}
	public void ToggleFlowVisibility(bool setVisibility)
	{
		if(setVisibility == flowVisibility) return;
		if (setVisibility == false && /*connectVisibilityLock*/colorFlowVisibilityLock) return;
		flowVisibility = setVisibility;
		GameManager.Instance.tracker.CreateEventExt("ToggleFlowVisibility",flowVisibility.ToString());

		GridObjectBehavior[] tracks = GameManager.Instance.GetGridManager().RetrieveTracks();//.RetrieveComponentsOfType("intersection");
		foreach(GridObjectBehavior track in tracks)
		{
			if(flowVisibility) { track.BeginInteraction(); }
			else { track.EndInteraction(); }
		}
	}

    void LockFlowVisibility(int lockTarget)
    {
        if (lockTarget == -1) //force quit
        {
            playerInteraction_UI.rightPanelColorLock.enabled = false;
			colorFlowVisibilityLock = false;
            return;
        }
		colorFlowVisibilityLock = !colorFlowVisibilityLock;
		if (/*connectVisibilityLock*/colorFlowVisibilityLock)
        {
            playerInteraction_UI.rightPanelColorLock.rectTransform.position = playerInteraction_UI.rightPanelColors[lockTarget].transform.position;
            playerInteraction_UI.rightPanelColorLock.enabled = true;
        }
        else
        {
            playerInteraction_UI.rightPanelColorLock.enabled = false;
            GameManager.Instance.GetGridManager().RevealGridColorMask(lockTarget);
        }
    }


    public void ToggleConnectionVisibility()
	{
		connectVisibility = !connectVisibility;

		if(connectVisibilityLock && !connectVisibility) return;

		GridObjectBehavior[] gridObjects = GameManager.Instance.GetGridManager().RetrieveComponentsOfType("signal");
		foreach(GridObjectBehavior g in gridObjects)
		{
			Signal_GridObjectBehavior s = (Signal_GridObjectBehavior) g;
			s.SetHighlight(connectVisibility);
		}
	}
	void LockConnectionVisibility()
	{
		connectVisibilityLock = !connectVisibilityLock;
		playerInteraction_UI.topPanelConnectionLock.enabled =  connectVisibilityLock;
	}

	public void TogglePauseMenu()
	{
		if(playerInteraction_UI.pauseOverlay.isPaused)
		{
			playerInteraction_UI.pauseOverlay.ClosePanel();	
			GameManager.Instance.tracker.CreateEventExt("ClosePausePanel","");
		}
		else 
		{
			playerInteraction_UI.pauseOverlay.OpenPanel();
			GameManager.Instance.tracker.CreateEventExt("OpenPausePanel","");
		}
	}

	void ToggleHintsVisibility()
	{
		GameManager.Instance.tracker.CreateEventExt("ToggleHintsVisibility",(!playerInteraction_UI.UIOverlay_Hint_Container.gameObject.activeSelf).ToString());
		if(playerInteraction_UI.UIOverlay_Hint_Container.gameObject.activeSelf) { playerInteraction_UI.hintOverlay.ClosePanel(); }
		playerInteraction_UI.UIOverlay_Hint_Container.gameObject.SetActive( !playerInteraction_UI.UIOverlay_Hint_Container.gameObject.activeSelf );

	}

    void EndHoverEvent()
    {
        if ( connectVisibilityLock || hoverObject==null ) return;
        stationaryTime = 0f;
        hoverObject.EndHoverBehavior();
        GameManager.Instance.tracker.CreateEventExt("EndHoverBehavior", hoverObject.component.type);
        hoverObject = null;
    }

	public void TriggerPlayPhaseEnd()
	{
		if(interactionPhase == InteractionPhases.simulation) 
		{
			StopAllCoroutines();
			EndSimulation();
		}

		GameManager.Instance.SetGamePhase( GameManager.GamePhases.LoadScreen );
	}
}


