

using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;// Required when using Event data.

    public class BrushSizeTooltip : MonoBehaviour, ISelectHandler// required interface when using the OnSelect method.
    {
      

        public Text TootipText;
        public string TootipString;
        public PaintMenuController PaintMenuControllerObj;

      
        public void OnSelect(BaseEventData eventData)
        {
        
                TootipText.text = TootipString +" (actual: "+ PaintMenuControllerObj.getVolumeInputController().BrushSize+")";
        }


    }
