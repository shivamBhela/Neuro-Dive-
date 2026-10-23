using UnityEngine;
using LostWorlds.Person2.Polish;

namespace LostWorlds.Person2.Journey
{
    // Presentation only: no commands, colliders or transition references.
    public sealed class PortalAtmosphere : MonoBehaviour
    {
        [SerializeField] Renderer energySurface;
        [SerializeField] ParticleSystem motes;
        RealmPolish polish;
        MaterialPropertyBlock properties;
        float phase;
        float appliedTime = float.NaN;
        void EnsureRuntimeState()
        {
            // Nonserialized state can be lost during a Play-mode script reload without another Awake.
            if (polish == null) polish = transform.root.GetComponentInChildren<RealmPolish>();
            if (energySurface != null && properties == null)
            {
                properties = new MaterialPropertyBlock();
                appliedTime = float.NaN;
            }
        }
        void Awake() => EnsureRuntimeState();
        void OnEnable() => EnsureRuntimeState();
        void Update()
        {
            // Optional visuals may be omitted or destroyed; neither is required by the portal trigger.
            if (energySurface == null && motes == null) return;
            EnsureRuntimeState();
            bool calm = polish != null && (!polish.isActiveAndEnabled || polish.ReducedMotion);
            if (!calm) phase += Time.deltaTime;
            float motionTime = calm ? 0 : phase;
            if (energySurface != null && appliedTime != motionTime)
            {
                properties.SetFloat("_MotionTime", motionTime);
                energySurface.SetPropertyBlock(properties);
                appliedTime = motionTime;
            }
            if (motes == null) return;
            if (calm && motes.isPlaying) motes.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            else if (!calm && !motes.isPlaying) motes.Play();
        }
        void OnDisable()
        {
            if (motes != null) motes.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
