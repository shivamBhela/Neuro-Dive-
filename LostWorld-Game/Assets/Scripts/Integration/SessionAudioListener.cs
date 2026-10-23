using UnityEngine;
using UnityEngine.SceneManagement;

namespace LostWorlds.Person2.Integration
{
    // One session-owned listener survives Single-mode scene unloading; no extra camera.
    [DisallowMultipleComponent]
    public sealed class SessionAudioListener : MonoBehaviour
    {
        AudioListener listener;
        Camera view;
        void Awake()
        {
            var child = new GameObject("SessionAudioListener");
            child.transform.SetParent(transform, false);
            listener = child.AddComponent<AudioListener>();
            Refresh();
        }
        void OnEnable() { SceneManager.sceneLoaded += Loaded; if(listener!=null)listener.enabled=true; }
        void OnDisable() { SceneManager.sceneLoaded -= Loaded; if(listener!=null)listener.enabled=false; }
        void Loaded(Scene scene, LoadSceneMode mode) => Refresh();
        void Refresh()
        {
            foreach(var other in Object.FindObjectsByType<AudioListener>())
                if(other!=listener)other.enabled=false;
            view=null;
            foreach(var camera in Camera.allCameras)
                if(camera.targetTexture==null && camera.gameObject.scene==SceneManager.GetActiveScene()) { view=camera; break; }
        }
        void LateUpdate()
        {
            if(listener==null)return;
            if(view!=null && view.isActiveAndEnabled)listener.transform.SetPositionAndRotation(view.transform.position,view.transform.rotation);
            else if(IntegrationSession.Instance!=null && IntegrationSession.Instance.PlayerReference!=null)
            {
                var player=IntegrationSession.Instance.PlayerReference;
                listener.transform.SetPositionAndRotation(player.position+Vector3.up*1.5f,player.rotation);
            }
        }
    }
}
