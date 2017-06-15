using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class TutorialManager : MonoBehaviour {
	[System.Serializable]
	public class TutorialScreen 
	{
		public int tutorialLevelId = 0;
		public Sprite tutorialLevelImage;
		public MovieTexture tutorialLevelMovie;
	}
	[SerializeField]
	public TutorialScreen[] tutorialScreens;

	public EventTrigger tutorialImageTriggerObject;
	public EventTrigger tutorialMovieTriggerObject;
	public EventTrigger imageBackTrigger;
	public EventTrigger imageForwardTrigger;
	public EventTrigger movieBackTrigger;
	public EventTrigger movieForwardTrigger;

	public TutorialScreen[] tutorialDisplays;
	public int tutorialIndex = 0;

	public void PerformTutorialSeries(int inputLevelId)
	{
		List<TutorialScreen> returnDisplays = RetrieveTutorialDisplays( inputLevelId );
		//tutorialIndex = 0;
		if(returnDisplays.Count >= 0)
		{
			tutorialDisplays = returnDisplays.ToArray();
			InitializeTutorial();
		}
		else 
		{
			tutorialImageTriggerObject.gameObject.SetActive(false);
			tutorialMovieTriggerObject.gameObject.SetActive(false);
		}
	}

	List<TutorialScreen> RetrieveTutorialDisplays(int inputLevelId)
	{
		List<TutorialScreen> returnDisplays = new List<TutorialScreen>();
		foreach(TutorialScreen t in tutorialScreens)
		{
			if(t.tutorialLevelId == inputLevelId) returnDisplays.Add( t );
		}
		return returnDisplays;
	}

	void InitializeTutorial()
	{
		if(tutorialIndex >= tutorialDisplays.Length || tutorialIndex < 0) 
		{
			tutorialImageTriggerObject.gameObject.SetActive(false);
			tutorialMovieTriggerObject.gameObject.SetActive(false);
		}
		else 
		{
			if(tutorialDisplays[tutorialIndex].tutorialLevelMovie != null) {
				tutorialMovieTriggerObject.GetComponent<RawImage>().texture = tutorialDisplays[ tutorialIndex ].tutorialLevelMovie;
				tutorialMovieTriggerObject.triggers.Clear();
				movieBackTrigger.triggers.Clear();
				movieForwardTrigger.triggers.Clear();
                tutorialDisplays[tutorialIndex].tutorialLevelMovie.Play();

				EventTrigger.Entry tap_tutorial = new EventTrigger.Entry();
				tap_tutorial.eventID = EventTriggerType.PointerUp;
				tap_tutorial.callback.AddListener( (eventData) => { NextTutorial(); } );
				tutorialMovieTriggerObject.triggers.Add(tap_tutorial);
				tutorialMovieTriggerObject.gameObject.SetActive(true);

				EventTrigger.Entry next_tutorial = new EventTrigger.Entry();
				next_tutorial.eventID = EventTriggerType.PointerUp;
				next_tutorial.callback.AddListener( (eventData) => { NextTutorial(); } );

				movieForwardTrigger.triggers.Add(next_tutorial);
				movieForwardTrigger.gameObject.SetActive(true);
				movieBackTrigger.triggers.Add(next_tutorial);
				movieBackTrigger.gameObject.SetActive(true);
			} else {
				tutorialImageTriggerObject.GetComponent<Image>().sprite = tutorialDisplays[ tutorialIndex ].tutorialLevelImage;
				tutorialImageTriggerObject.triggers.Clear();
				imageBackTrigger.triggers.Clear();
				imageForwardTrigger.triggers.Clear();

				EventTrigger.Entry tap_tutorial = new EventTrigger.Entry();
				tap_tutorial.eventID = EventTriggerType.PointerUp;
				tap_tutorial.callback.AddListener( (eventData) => { NextTutorial(); } );
				tutorialImageTriggerObject.triggers.Add(tap_tutorial);
				tutorialImageTriggerObject.gameObject.SetActive(true);

				EventTrigger.Entry next_tutorial = new EventTrigger.Entry();
				next_tutorial.eventID = EventTriggerType.PointerUp;
				next_tutorial.callback.AddListener( (eventData) => { NextTutorial(); } );

				imageForwardTrigger.triggers.Add(next_tutorial);
				imageForwardTrigger.gameObject.SetActive(true);
				imageBackTrigger.triggers.Add(next_tutorial);
				imageBackTrigger.gameObject.SetActive(true);
			}
		}
	}

	public void NextTutorial()
	{
		if(tutorialDisplays[tutorialIndex].tutorialLevelMovie != null) {
			tutorialDisplays[tutorialIndex].tutorialLevelMovie.Stop();
			tutorialMovieTriggerObject.gameObject.SetActive(false);
			movieForwardTrigger.gameObject.SetActive(false);
			movieBackTrigger.gameObject.SetActive(false);
		} else {
			tutorialImageTriggerObject.gameObject.SetActive(false);
			imageForwardTrigger.gameObject.SetActive(false);
			imageBackTrigger.gameObject.SetActive(false);
		}
		tutorialIndex++;
		if(tutorialIndex >= tutorialDisplays.Length)
		{
			GameManager.Instance.tracker.CreateEventExt("endTutorial",tutorialIndex.ToString());
			tutorialImageTriggerObject.gameObject.SetActive(false);
			tutorialMovieTriggerObject.gameObject.SetActive(false);
		}
		else 
		{ 
			if(tutorialIndex>=1)
			{
				InitializeTutorial();
				imageBackTrigger.triggers.Clear();
				movieBackTrigger.triggers.Clear();
				EventTrigger.Entry last_tutorial = new EventTrigger.Entry();
				last_tutorial.eventID = EventTriggerType.PointerUp;
				last_tutorial.callback.AddListener( (eventData) => { PreviousTutorial(); } );
				if(tutorialDisplays[tutorialIndex].tutorialLevelMovie != null) {
					movieBackTrigger.triggers.Add( last_tutorial );
				} else {
					imageBackTrigger.triggers.Add( last_tutorial );
				}
			}
			GameManager.Instance.tracker.CreateEventExt("nextTutorial",tutorialIndex.ToString());
		}
	}

	public void PreviousTutorial()
	{
		if(tutorialDisplays[tutorialIndex].tutorialLevelMovie != null) {
			tutorialDisplays[tutorialIndex].tutorialLevelMovie.Stop();
			tutorialMovieTriggerObject.gameObject.SetActive(false);
		}
		tutorialIndex--;
		if(tutorialIndex < 0)
		{
			tutorialImageTriggerObject.gameObject.SetActive(false);
			tutorialMovieTriggerObject.gameObject.SetActive(false);
		}
		else {
			InitializeTutorial();
			imageBackTrigger.triggers.Clear();
			movieBackTrigger.triggers.Clear();
			EventTrigger.Entry last_tutorial = new EventTrigger.Entry();
			last_tutorial.eventID = EventTriggerType.PointerUp;
			last_tutorial.callback.AddListener( (eventData) => { PreviousTutorial(); } );
			if(tutorialDisplays[tutorialIndex].tutorialLevelMovie != null) {
				movieBackTrigger.triggers.Add( last_tutorial );
			} else {
				imageBackTrigger.triggers.Add( last_tutorial );
			}
			/*if(tutorialDisplays[tutorialIndex].tutorialLevelMovie != null) {
				tutorialMovieTriggerObject.GetComponent<RawImage>().texture = tutorialDisplays[ tutorialIndex ].tutorialLevelMovie;
			} else {
				tutorialImageTriggerObject.GetComponent<Image>().sprite = tutorialDisplays[ tutorialIndex ].tutorialLevelImage;
			}*/
			GameManager.Instance.tracker.CreateEventExt("previousTutorial",tutorialIndex.ToString());
		}
	}
}
