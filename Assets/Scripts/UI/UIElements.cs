using UnityEngine;
using System.Collections;

namespace ParallelProg.UI
{
    [System.Serializable]
    public class UIOverlay
    {
        public RectTransform panelContainer;
        public bool isOpen;

        public virtual void OpenPanel()
        {
            isOpen = true;
            panelContainer.gameObject.transform.localScale = Vector3.zero;
            panelContainer.gameObject.SetActive(true);
            iTween.ScaleTo(panelContainer.gameObject, iTween.Hash("scale", Vector3.one, "time", 0.5f));
        }

        public virtual void ClosePanel(bool forceClose = false)
        {
            isOpen = false;
            if (forceClose)
            {
                panelContainer.gameObject.SetActive(false);
            }
            else
            {
                iTween.ScaleTo(panelContainer.gameObject, iTween.Hash("scale", Vector3.zero, "time", 0.5f));
            }
        }
    }
}
