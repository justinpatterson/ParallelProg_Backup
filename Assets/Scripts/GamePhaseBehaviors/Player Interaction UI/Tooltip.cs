using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

[System.Serializable]
public class Tooltip
{
	public string tooltipText;
}

[System.Serializable]
public class TooltipEvent 
{
	public EventTrigger tooltipUiElement;
	[SerializeField] public Tooltip tooltipContent;
}