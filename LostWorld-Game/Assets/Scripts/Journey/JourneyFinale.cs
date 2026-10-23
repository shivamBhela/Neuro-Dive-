using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using LostWorlds.Person2.Integration;
using LostWorlds.Person2.Polish;
namespace LostWorlds.Person2.Journey
{
    /// <summary>Presentation/reward observer of P11 completion, never a second progression system.</summary>
    public sealed class JourneyFinale : MonoBehaviour
    {
        [SerializeField] Sprite trophyIcon;
        [SerializeField] Sprite starIcon;
        Image[] stars;
        public int EarnedStars {get;private set;}
        public static int StarsForTokens(int tokens)=>tokens>=30?3:tokens>=18?2:1;
        [SerializeField] AudioClip celebration;
        IntegrationSession session;RealmPolish polish;GameObject screen;RectTransform trophy;
        AudioSource sound;Text heading,summary;Image[] motes;
        public int AwardCount {get;private set;}
        public int FinalTokenCount {get;private set;}
        public bool IsVisible=>screen!=null&&screen.activeSelf;
        public Button ReplayButton {get;private set;}
        public Button FinishButton {get;private set;}
        IEnumerator Start()
        {
            while(IntegrationSession.Instance==null)yield return null;
            session=IntegrationSession.Instance;polish=transform.root.GetComponentInChildren<RealmPolish>();
            session.Completed+=Show;session.SessionFinished+=Finished;
            if(session.IsComplete)Show();
        }
        void OnDestroy(){if(session!=null){session.Completed-=Show;session.SessionFinished-=Finished;session.AutoRunPause.ReleasePause(this);}}
        void Show()
        {
            if(session==null||!session.IsComplete||session.CurrentRealm==null||session.CurrentRealm.RealmIndex!=3||AwardCount!=0)return;
            AwardCount=1;FinalTokenCount=JourneyLedger.Ensure(session).Count;session.AutoRunPause.RequestPause(this);
            screen=new GameObject("JourneyComplete_FinalScreen",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));screen.transform.SetParent(transform,false);
            var canvas=screen.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=300;
            var scaler=screen.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var back=Element<Image>("Background",screen.transform,Vector2.zero,new Vector2(1920,1080));back.color=new Color(.035f,.07f,.10f,1);var br=back.rectTransform;br.anchorMin=Vector2.zero;br.anchorMax=Vector2.one;br.sizeDelta=Vector2.zero;
            heading=Label("Title","CONGRATULATIONS! YOU DID IT!",new Vector2(0,420),42,new Vector2(1680,80));
            Label("CompletionTitle","JOURNEY COMPLETE",new Vector2(0,345),32,new Vector2(1400,65));
            var icon=Element<Image>("GoldenTrophy",screen.transform,new Vector2(0,180),new Vector2(200,235));icon.sprite=trophyIcon;icon.preserveAspect=true;icon.raycastTarget=false;trophy=icon.rectTransform;
            Label("Encouragement","Every step is progress!",new Vector2(0,15),36,new Vector2(1400,65));
            Label("Realms","Realms completed: 4/4",new Vector2(0,-155),30,new Vector2(1000,55));
            summary=Label("Tokens","Journey Tokens collected: "+FinalTokenCount+"/36",new Vector2(0,-210),30,new Vector2(1000,55));
            EarnedStars=StarsForTokens(FinalTokenCount);stars=new Image[3];
            for(int i=0;i<3;i++)
            {
                stars[i]=Element<Image>("VictoryStar"+(i+1),screen.transform,new Vector2((i-1)*110,-75),new Vector2(76,76));stars[i].sprite=starIcon;stars[i].preserveAspect=true;stars[i].raycastTarget=false;
                stars[i].color=i<EarnedStars?new Color(1,.77f,.19f):new Color(.24f,.29f,.32f);
                if(i<EarnedStars){var glow=stars[i].gameObject.AddComponent<Outline>();glow.effectColor=new Color(1,.6f,.08f,.3f);glow.effectDistance=new Vector2(2,2);}
            }
            ReplayButton=Button("Replay Journey",new Vector2(-240,-320));FinishButton=Button("Finish Session",new Vector2(240,-320));
            ReplayButton.onClick.AddListener(()=>{if(session.ReplayJourney()){ReplayButton.interactable=false;FinishButton.interactable=false;}});FinishButton.onClick.AddListener(()=>session.FinishJourney());
            if(EventSystem.current==null){var es=new GameObject("FinaleEventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));es.transform.SetParent(screen.transform,false);es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
            EventSystem.current.SetSelectedGameObject(ReplayButton.gameObject);
            motes=new Image[8];for(int i=0;i<motes.Length;i++){motes[i]=Element<Image>("GentleSpark",screen.transform,new Vector2((i-3.5f)*65,160),new Vector2(5,5));motes[i].color=new Color(1,.8f,.35f,.35f);motes[i].raycastTarget=false;}
            sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;sound.volume=.06f;sound.clip=celebration;if(celebration!=null&&(polish==null||!polish.Muted))sound.Play();
        }
        void Finished(){if(!IsVisible)return;heading.text="SESSION FINISHED";summary.text="Your journey is complete. You can safely close the game.";FinishButton.interactable=false;if(sound!=null)sound.Stop();}
        void Update()
        {
            if(!IsVisible)return;bool calm=polish!=null&&polish.ReducedMotion;
            trophy.localScale=Vector3.one*(calm||session.IsSessionFinished?1:1+Mathf.Sin(Time.unscaledTime*.7f)*.015f);
            for(int i=0;i<stars.Length;i++)stars[i].rectTransform.localScale=Vector3.one*(calm||session.IsSessionFinished||i>=EarnedStars?1:1+Mathf.Sin(Time.unscaledTime*.7f+i*.5f)*.025f);
            for(int i=0;i<motes.Length;i++){motes[i].enabled=!calm&&!session.IsSessionFinished;motes[i].rectTransform.anchoredPosition=new Vector2((i-3.5f)*65,80+Mathf.Repeat(Time.unscaledTime*10+i*23,180));}
            if(sound!=null&&polish!=null)sound.mute=polish.Muted;
        }
        static T Element<T>(string name,Transform parent,Vector2 position,Vector2 size) where T:Component
        {var go=new GameObject(name,typeof(RectTransform),typeof(T));go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;return go.GetComponent<T>();}
        Text Label(string name,string text,Vector2 pos,int fontSize,Vector2 size)
        {var t=Element<Text>(name,screen.transform,pos,size);t.text=text;t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=fontSize;t.color=new Color(1,.91f,.68f);t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;}
        Button Button(string label,Vector2 pos)
        {var image=Element<Image>(label,screen.transform,pos,new Vector2(410,86));image.color=new Color(.21f,.31f,.36f);var b=image.gameObject.AddComponent<Button>();var t=Element<Text>("Label",image.transform,Vector2.zero,new Vector2(400,80));t.text=label;t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=30;t.alignment=TextAnchor.MiddleCenter;t.color=Color.white;t.raycastTarget=false;return b;}
    }
}
