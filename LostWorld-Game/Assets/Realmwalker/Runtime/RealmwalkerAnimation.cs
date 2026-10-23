using UnityEngine;

namespace Realmwalker
{
    // Presentation only: reads R2's final movement result; never moves the root or bones directly.
    [DefaultExecutionOrder(100), DisallowMultipleComponent]
    public sealed class RealmwalkerAnimation : MonoBehaviour
    {
        RealmwalkerRunner runner;
        Animator animator;
        bool wasGrounded;
        int jumps;
        float landingUntil, takeoffUntil;
        public string CurrentState { get; private set; }

        void Awake()
        {
            runner = GetComponent<RealmwalkerRunner>();
            animator = GetComponent<Animator>();
            animator.applyRootMotion = false;
        }

        void Update()
        {
            if (runner == null || animator == null || !animator.enabled) return;
            if (runner.AcceptedJumps != jumps)
            {
                jumps = runner.AcceptedJumps;
                takeoffUntil = Time.time + .12f;
                landingUntil = 0;
            }
            if (runner.Grounded && !wasGrounded && runner.AcceptedJumps > 0)
                landingUntil = Time.time + .18f;
            wasGrounded = runner.Grounded;
            float speed = animator.GetFloat("ForwardSpeed");
            string next = !runner.Grounded
                ? (Time.time < takeoffUntil ? "Takeoff" : runner.VerticalSpeed > -.5f ? "Airborne" : "Fall")
                : Time.time < landingUntil ? "Land"
                : runner.Paused || speed < .15f ? "Idle" : "Run";
            animator.SetFloat("StrideRate", Mathf.Clamp(speed / 2.8f, .65f, 1.4f));
            if (next == CurrentState) return;
            CurrentState = next;
            animator.CrossFadeInFixedTime(next, next == "Takeoff" ? .055f : .12f);
        }
    }
}
