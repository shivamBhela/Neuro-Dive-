using UnityEngine;
using UnityEngine.UI;
using LostWorlds.Person2.Integration;
namespace LostWorlds.Person2.Journey
{
    public sealed class JourneyTokenHud : MonoBehaviour
    {
        JourneyLedger ledger;Text label;
        public string DisplayText=>label!=null?label.text:"";
        public static void Ensure(IntegrationSession session,JourneyLedger source)
        {
            if(session.Hud==null||session.Hud.GetComponentInChildren<JourneyTokenHud>(true)!=null)return;
            var panel=new GameObject("JourneyTokenPanel",typeof(RectTransform),typeof(Image));panel.transform.SetParent(session.Hud.transform,false);var rect=panel.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-127);rect.sizeDelta=new Vector2(340,36);var image=panel.GetComponent<Image>();image.color=new Color(.055f,.12f,.12f,.94f);image.raycastTarget=false;
            var go=new GameObject("JourneyTokenCounter",typeof(RectTransform),typeof(Text));go.transform.SetParent(panel.transform,false);
            var text=go.GetComponent<Text>();text.rectTransform.anchorMin=text.rectTransform.anchorMax=new Vector2(.5f,.5f);text.rectTransform.anchoredPosition=Vector2.zero;text.rectTransform.sizeDelta=new Vector2(328,30);text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=20;text.color=new Color(.96f,.92f,.73f);text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;
            go.AddComponent<JourneyTokenHud>().Bind(source);
        }
        public void Bind(JourneyLedger source){if(ledger!=null)ledger.CountChanged-=Refresh;ledger=source;label=GetComponent<Text>();if(isActiveAndEnabled&&ledger!=null)ledger.CountChanged+=Refresh;Refresh(ledger!=null?ledger.Count:0);}
        void OnEnable(){if(ledger!=null){ledger.CountChanged+=Refresh;Refresh(ledger.Count);}}
        void OnDisable(){if(ledger!=null)ledger.CountChanged-=Refresh;}
        void Refresh(int count){if(label!=null)label.text="Journey Tokens: "+count;}
    }
}
