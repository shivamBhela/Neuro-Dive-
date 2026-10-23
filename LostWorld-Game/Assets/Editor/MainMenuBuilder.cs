using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using LostWorlds.Person2.Integration;

public static class MainMenuBuilder
{
    static Font font; static readonly Color gold=new Color(.94f,.75f,.38f);
    static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size)
    {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;return r;}
    static Text Label(string name,Transform parent,string text,Vector2 pos,Vector2 size,int points,Color color)
    {var t=Rect(name,parent,pos,size).gameObject.AddComponent<Text>();t.font=font;t.text=text;t.fontSize=points;t.color=color;t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;return t;}
    static Button Button(Transform parent,string text,float y)
    {var r=Rect(text,parent,new Vector2(0,y),new Vector2(510,70));var image=r.gameObject.AddComponent<Image>();image.color=new Color(.11f,.18f,.19f,.96f);var outline=r.gameObject.AddComponent<Outline>();outline.effectColor=new Color(gold.r,gold.g,gold.b,.5f);outline.effectDistance=new Vector2(1,-1);var b=r.gameObject.AddComponent<Button>();var colors=b.colors;colors.highlightedColor=new Color(1,.85f,.55f);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.65f,.6f,.4f);b.colors=colors;var t=Label("Label",r,text,Vector2.zero,new Vector2(460,60),24,gold);t.alignment=TextAnchor.MiddleCenter;return b;}
    [MenuItem("Tools/Lost Worlds/Main Menu/Create or Open")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        if(System.IO.File.Exists(MainMenu.ScenePath)){EditorSceneManager.OpenScene(MainMenu.ScenePath);return;}
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var camera=new GameObject("MenuCamera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();camera.transform.position=new Vector3(0,4,-14);camera.transform.LookAt(new Vector3(2,3,8));camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.045f,.10f,.13f);camera.fieldOfView=48;
        var light=new GameObject("Sunset",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.color=new Color(1,.77f,.45f);light.intensity=1.6f;light.transform.rotation=Quaternion.Euler(35,-30,0);light.shadows=LightShadows.Soft;
        RenderSettings.ambientLight=new Color(.32f,.39f,.42f);RenderSettings.fog=true;RenderSettings.fogColor=camera.backgroundColor;RenderSettings.fogMode=FogMode.Exponential;RenderSettings.fogDensity=.027f;
        System.IO.Directory.CreateDirectory("Assets/Art/Materials/MainMenu");
        var stone=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="MenuSandstone"};stone.color=new Color(.50f,.40f,.27f);AssetDatabase.CreateAsset(stone,"Assets/Art/Materials/MainMenu/MenuSandstone.mat");
        var dark=new Material(stone){name="MenuTealStone",color=new Color(.12f,.23f,.24f)};AssetDatabase.CreateAsset(dark,"Assets/Art/Materials/MainMenu/MenuTealStone.mat");
        System.Action<string,Vector3,Vector3,Material> block=(name,pos,scale,mat)=>{var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.position=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(g.GetComponent<Collider>());};
        block("Distant plaza",new Vector3(6,-.4f,12),new Vector3(40,.6f,50),dark);
        for(int i=0;i<5;i++){float z=8+i*6;float h=5+i*.5f;foreach(float x in new[]{3f,10f}){block("Ancient pillar",new Vector3(x,h/2,z),new Vector3(1.2f,h,1.2f),stone);block("Capital",new Vector3(x,h,z),new Vector3(1.8f,.4f,1.8f),stone);}block("Lintel",new Vector3(6.5f,h+.5f,z),new Vector3(8.5f,.7f,1.3f),stone);}
        for(int i=0;i<4;i++)block("Temple terrace",new Vector3(7,i*.7f,38),new Vector3(18-i*3,.7f,10-i),stone);
        var canvas=new GameObject("MainMenuCanvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
        var shade=Rect("Readability",canvas.transform,Vector2.zero,Vector2.zero);shade.anchorMin=Vector2.zero;shade.anchorMax=Vector2.one;shade.offsetMin=shade.offsetMax=Vector2.zero;shade.gameObject.AddComponent<Image>().color=new Color(.015f,.035f,.045f,.35f);
        var panel=Rect("Menu panel",canvas.transform,new Vector2(-330,0),new Vector2(650,800));panel.gameObject.AddComponent<Image>().color=new Color(.02f,.055f,.065f,.88f);
        var menu=canvas.gameObject.AddComponent<MainMenu>();menu.home=Rect("Home",panel,Vector2.zero,new Vector2(510,740)).gameObject;menu.instructions=Rect("How to play",panel,Vector2.zero,new Vector2(510,740)).gameObject;
        Label("Eyebrow",menu.home.transform,"A LOST WORLD AWAITS",new Vector2(0,285),new Vector2(510,40),18,gold);
        Label("Title",menu.home.transform,"LOST\nWORLDS",new Vector2(0,175),new Vector2(510,180),72,new Color(1,.92f,.73f));
        Label("Subtitle",menu.home.transform,"Journey to the Hidden City",new Vector2(0,55),new Vector2(510,45),25,Color.white);
        menu.startButton=Button(menu.home.transform,"START JOURNEY",-40);menu.howButton=Button(menu.home.transform,"HOW TO PLAY",-130);menu.exitButton=Button(menu.home.transform,"EXIT GAME",-220);
        menu.status=Label("Status",panel,"Your journey begins when you're ready.",new Vector2(0,-345),new Vector2(550,65),20,new Color(.8f,.85f,.81f));menu.status.alignment=TextAnchor.MiddleCenter;
        Label("Instructions title",menu.instructions.transform,"HOW TO PLAY",new Vector2(0,270),new Vector2(510,65),36,gold);
        Label("Instructions",menu.instructions.transform,"Hold W to move; release W to stop.\nA / D or arrow keys  •  LEFT / RIGHT\nSpace  •  JUMP\nE  •  ACTIVATE nearby gateways\n\nTouch Journey Tokens to collect them.\nTokens are optional—take your time.\nWatch the effort meter. At the cap, REST\nis recommended. There are no penalties.\n\nHardware: LEFT, RIGHT, JUMP, GRAB,\nACTIVATE; processed effort 0–100.\nGRAB is reserved; no grab action is required.",new Vector2(0,10),new Vector2(510,430),22,Color.white);
        menu.backButton=Button(menu.instructions.transform,"BACK",-270);menu.instructions.SetActive(false);
        new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
        EditorSceneManager.SaveScene(scene,MainMenu.ScenePath);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(MainMenu.ScenePath,true)}.Concat(IntegrationSession.RealmScenes.Select(p=>new EditorBuildSettingsScene(p,true))).Concat(EditorBuildSettings.scenes.Where(s=>s.path!=MainMenu.ScenePath&&!IntegrationSession.RealmScenes.Contains(s.path))).ToArray();AssetDatabase.SaveAssets();
    }
    public static void Batch(){try{Build();EditorApplication.Exit(0);}catch(System.Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    [MenuItem("Tools/Lost Worlds/Main Menu/Apply Cinematic Polish")]
    public static void Polish()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var scene=EditorSceneManager.OpenScene(MainMenu.ScenePath);var menu=Object.FindAnyObjectByType<MainMenu>();
        if(menu.GetComponent<MainMenuPolish>()==null)menu.gameObject.AddComponent<MainMenuPolish>();
        EditorSceneManager.SaveScene(scene);
    }
    public static void PolishBatch(){try{Polish();EditorApplication.Exit(0);}catch(System.Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    public static void CapturePolishedMenu()
    {
        var camera=Object.FindAnyObjectByType<Camera>();var canvas=Object.FindAnyObjectByType<MainMenu>().GetComponent<Canvas>();
        var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;float plane=canvas.planeDistance;var previous=camera.targetTexture;var active=RenderTexture.active;
        try
        {
            foreach(int width in new[]{1280,1600,1920})
            {
                int height=width*9/16;var target=new RenderTexture(width,height,24);var image=new Texture2D(width,height,TextureFormat.RGB24,false);
                try
                {
                    canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;camera.targetTexture=target;Canvas.ForceUpdateCanvases();
                    foreach(var text in canvas.GetComponentsInChildren<Text>())
                        if(text.preferredHeight>text.rectTransform.rect.height+2)throw new System.Exception("Menu text overflow: "+text.name);
                    camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
                    System.IO.File.WriteAllBytes("Assets/Realmwalker/Validation/MenuPolish"+width+".png",image.EncodeToPNG());
                    Debug.Log("MENU PASS: readable layout at "+width+"x"+height);
                }
                finally{camera.targetTexture=previous;RenderTexture.active=active;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);}
            }
        }
        finally{canvas.renderMode=mode;canvas.worldCamera=oldCamera;canvas.planeDistance=plane;}
    }
    public static void PreviewBatch()
    {
        try
        {
            EditorSceneManager.OpenScene(MainMenu.ScenePath);
            var camera=Object.FindAnyObjectByType<Camera>();var canvas=Object.FindAnyObjectByType<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            var target=new RenderTexture(1600,900,24);camera.targetTexture=target;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
            var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
            System.IO.File.WriteAllBytes("Assets/Realmwalker/Validation/MainMenuPreview.png",image.EncodeToPNG());
            EditorApplication.Exit(0);
        }
        catch(System.Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
