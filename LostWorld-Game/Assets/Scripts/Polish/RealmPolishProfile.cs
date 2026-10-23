using UnityEngine;
namespace LostWorlds.Person2.Polish
{
    [CreateAssetMenu(menuName="Lost Worlds/Person 2/Realm Polish Profile")]
    public sealed class RealmPolishProfile : ScriptableObject
    {
        [SerializeField] string realmName;
        [Tooltip("Optional licensed looping ambience. Null means silence.")]
        [SerializeField] AudioClip ambience;
        [Tooltip("Optional gentle success sound. Never required for gameplay.")]
        [SerializeField] AudioClip successSound;
        [SerializeField,Range(0,.18f)] float ambienceVolume=.10f;
        [SerializeField,Range(0,.12f)] float successVolume=.07f;
        public string RealmName=>realmName;
        public AudioClip Ambience=>ambience;
        public AudioClip SuccessSound=>successSound;
        public float AmbienceVolume=>Mathf.Clamp(ambienceVolume,0,.18f);
        public float SuccessVolume=>Mathf.Clamp(successVolume,0,.12f);
    }
}
