using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using LostWorlds.Person2.Interaction;
namespace LostWorlds.Person2.Polish
{
    /// <summary>Presentation only. Observes accepted interactions; never sends gameplay commands.</summary>
    public sealed class RealmPolish : MonoBehaviour
    {
        [SerializeField] RealmPolishProfile profile;
        [SerializeField] ParticleSystem[] atmosphere=new ParticleSystem[0];
        [SerializeField] ParticleSystem successParticles;
        [SerializeField] AudioSource ambienceSource,successSource;
        [SerializeField] bool reducedMotion,muted;
        readonly Dictionary<InteractableBase,UnityAction> listeners=new Dictionary<InteractableBase,UnityAction>();
        bool appliedMotion,appliedMute;
        float lastSound=float.NegativeInfinity;
        public RealmPolishProfile Profile=>profile;
        public int FeedbackCount {get;private set;}
        public bool ReducedMotion {get=>reducedMotion;set{reducedMotion=value;ApplyMotion();}}
        public bool Muted {get=>muted;set{muted=value;ApplyAudio();}}
        public ParticleSystem SuccessParticles=>successParticles;
        void OnEnable()
        {
            foreach(var root in gameObject.scene.GetRootGameObjects())
                foreach(var target in root.GetComponentsInChildren<InteractableBase>(true))
                {
                    var captured=target;UnityAction listener=()=>Succeeded(captured);
                    listeners.Add(target,listener);target.OnSucceeded.AddListener(listener);
                }
            if(ambienceSource!=null)
            {
                ambienceSource.playOnAwake=false;ambienceSource.loop=true;ambienceSource.volume=0;
                ambienceSource.clip=profile!=null?profile.Ambience:null;
                if(ambienceSource.clip!=null)ambienceSource.Play();
            }
            ApplyAudio();ApplyMotion();
        }
        void OnDisable()
        {
            foreach(var pair in listeners)if(pair.Key!=null)pair.Key.OnSucceeded.RemoveListener(pair.Value);
            listeners.Clear();
            if(ambienceSource!=null)ambienceSource.Stop();if(successSource!=null)successSource.Stop();
            foreach(var system in atmosphere)if(system!=null)system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            if(successParticles!=null)successParticles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        void Update()
        {
            if(appliedMotion!=reducedMotion)ApplyMotion();if(appliedMute!=muted)ApplyAudio();
            if(ambienceSource!=null)ambienceSource.volume=Mathf.MoveTowards(ambienceSource.volume,muted||profile==null?0:profile.AmbienceVolume,Time.unscaledDeltaTime*.06f);
        }
        void ApplyAudio() { appliedMute=muted;
            if(ambienceSource!=null){ambienceSource.mute=muted;if(muted)ambienceSource.volume=0;}
            if(successSource!=null){successSource.mute=muted;successSource.volume=profile!=null?profile.SuccessVolume:0;}
        }
        void ApplyMotion() { appliedMotion=reducedMotion;
            foreach(var system in atmosphere)
                if(system!=null){if(reducedMotion)system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);else if(isActiveAndEnabled&&!system.isPlaying)system.Play();}
            if(successParticles!=null){if(reducedMotion)successParticles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);else if(isActiveAndEnabled&&!successParticles.isPlaying)successParticles.Play();}
        }
        void Succeeded(InteractableBase target)
        {
            FeedbackCount++;
            if(!reducedMotion&&successParticles!=null)
            {
                for(int i=0;i<5;i++){float angle=i*Mathf.PI*2/5;var emit=new ParticleSystem.EmitParams {position=target.transform.position+Vector3.up*.65f+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*.3f,velocity=Vector3.up*.12f};successParticles.Emit(emit,1);}
            }
            if(!muted&&profile!=null&&profile.SuccessSound!=null&&successSource!=null&&Time.unscaledTime-lastSound>=.5f)
            {lastSound=Time.unscaledTime;successSource.PlayOneShot(profile.SuccessSound,1);}
        }
    }
}
