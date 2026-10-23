using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace LostWorlds.Person2.Integration
{
    public sealed class MainMenu : MonoBehaviour
    {
        public const string ScenePath="Assets/Scenes/MainMenu.unity";
        public GameObject home, instructions;
        public Button startButton, howButton, backButton, exitButton;
        public Text status;
        public bool Starting {get;private set;}
        void Awake()
        {
            // A menu never runs the old session, even if opened from a running journey.
            var old=IntegrationSession.Instance;
            if(old!=null){old.gameObject.SetActive(false);Destroy(old.gameObject);}
            Time.timeScale=1;
            startButton.onClick.AddListener(StartJourney);howButton.onClick.AddListener(()=>ShowInstructions(true));
            backButton.onClick.AddListener(()=>ShowInstructions(false));exitButton.onClick.AddListener(ExitGame);
            ShowInstructions(false);
        }
        public void ShowInstructions(bool show)
        {if(Starting)return;home.SetActive(!show);instructions.SetActive(show);if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject((show?backButton:startButton).gameObject);}
        public void StartJourney()
        {
            if(Starting)return;
            if(!Application.CanStreamedLevelBeLoaded(IntegrationSession.RealmScenes[0])){status.text="Jungle is unavailable. Check the build scene configuration.";return;}
            Starting=true;startButton.interactable=howButton.interactable=exitButton.interactable=false;
            status.text="Preparing your journey…";StartCoroutine(Load());
        }
        IEnumerator Load()
        {
            var polish=GetComponent<MainMenuPolish>();
            if(polish!=null)yield return polish.FadeOut();
            yield return null; // Let any previous persistent session finish destruction.
            var operation=SceneManager.LoadSceneAsync(IntegrationSession.RealmScenes[0]);
            yield return operation; // Jungle creates its existing fresh session, ledger and effort model.
        }
        public void ExitGame()
        {
            if(Starting)return;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
    }
}
