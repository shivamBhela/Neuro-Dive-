using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace LostWorlds.Person2.Integration
{
    [DisallowMultipleComponent]
    public sealed class MainMenuPolish : MonoBehaviour
    {
        [SerializeField] bool reducedMotion;
        [SerializeField,Range(.2f,1)] float fadeSeconds=.6f;
        [SerializeField] AudioClip optionalAmbience;
        [SerializeField,Range(0,.15f)] float ambienceVolume=.06f;
        MainMenu menu; Button[] buttons; bool[] hovered;
        RectTransform[] motes; Vector2[] origins; Image[] glints;
        Outline glow; CanvasGroup curtain; AudioSource audioSource;
        public float FadeAlpha=>curtain==null?0:curtain.alpha;
        void Start()
        {
            menu=GetComponent<MainMenu>();buttons=new[]{menu.startButton,menu.howButton,menu.exitButton,menu.backButton};hovered=new bool[4];
            for(int i=0;i<buttons.Length;i++)
            {
                int index=i;var colors=buttons[i].colors;colors.fadeDuration=.22f;buttons[i].colors=colors;
                var events=buttons[i].gameObject.AddComponent<EventTrigger>();
                var enter=new EventTrigger.Entry{eventID=EventTriggerType.PointerEnter};enter.callback.AddListener(_=>hovered[index]=true);events.triggers.Add(enter);
                var exit=new EventTrigger.Entry{eventID=EventTriggerType.PointerExit};exit.callback.AddListener(_=>hovered[index]=false);events.triggers.Add(exit);
            }
            var title=menu.home.transform.Find("Title").GetComponent<Text>();
            glow=title.gameObject.AddComponent<Outline>();glow.effectDistance=new Vector2(2,-2);glow.effectColor=new Color(1,.65f,.19f,.16f);
            var layer=new GameObject("Quiet golden atmosphere",typeof(RectTransform)).GetComponent<RectTransform>();layer.SetParent(transform,false);layer.SetAsFirstSibling();layer.anchorMin=Vector2.zero;layer.anchorMax=Vector2.one;layer.offsetMin=layer.offsetMax=Vector2.zero;
            motes=new RectTransform[18];origins=new Vector2[18];glints=new Image[18];
            for(int i=0;i<motes.Length;i++)
            {
                var image=new GameObject("Golden mote",typeof(RectTransform),typeof(Image)).GetComponent<Image>();image.transform.SetParent(layer,false);image.raycastTarget=false;image.color=new Color(1,.77f,.36f,.3f);glints[i]=image;
                var r=image.rectTransform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.sizeDelta=Vector2.one*(2+i%3);r.localRotation=Quaternion.Euler(0,0,45);origins[i]=new Vector2(-740+(i*137)%1480,-360+(i*97)%720);r.anchoredPosition=origins[i];motes[i]=r;
            }
            var cover=new GameObject("Journey fade",typeof(RectTransform),typeof(Image),typeof(CanvasGroup));cover.transform.SetParent(transform,false);var rect=cover.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;cover.GetComponent<Image>().color=new Color(.015f,.025f,.03f);curtain=cover.GetComponent<CanvasGroup>();curtain.alpha=0;curtain.blocksRaycasts=false;
            if(optionalAmbience!=null){audioSource=gameObject.AddComponent<AudioSource>();audioSource.clip=optionalAmbience;audioSource.loop=true;audioSource.spatialBlend=0;audioSource.volume=ambienceVolume;audioSource.Play();}
        }
        void Update()
        {
            if(buttons==null)return;
            float t=Time.unscaledTime,blend=1-Mathf.Exp(-12*Time.unscaledDeltaTime);
            for(int i=0;i<buttons.Length;i++)
            {
                bool selected=EventSystem.current!=null&&EventSystem.current.currentSelectedGameObject==buttons[i].gameObject;
                float scale=!reducedMotion&&buttons[i].interactable&&(hovered[i]||selected)?1.025f:1;
                buttons[i].transform.localScale=Vector3.Lerp(buttons[i].transform.localScale,Vector3.one*scale,blend);
                if(!buttons[i].gameObject.activeInHierarchy)hovered[i]=false;
            }
            glow.effectColor=new Color(1,.65f,.19f,reducedMotion?.16f:.16f+.035f*Mathf.Sin(t*.65f));
            for(int i=0;i<motes.Length;i++)
            {
                motes[i].anchoredPosition=origins[i]+(reducedMotion?Vector2.zero:new Vector2(Mathf.Sin(t*.12f+i)*12,Mathf.Sin(t*.09f+i*.7f)*24));
            }
        }
        public IEnumerator FadeOut()
        {
            while(curtain==null)yield return null;
            curtain.blocksRaycasts=true;
            for(float elapsed=0;elapsed<fadeSeconds;elapsed+=Time.unscaledDeltaTime)
            {curtain.alpha=Mathf.SmoothStep(0,1,elapsed/fadeSeconds);if(audioSource!=null)audioSource.volume=ambienceVolume*(1-curtain.alpha);yield return null;}
            curtain.alpha=1;
        }
    }
}
