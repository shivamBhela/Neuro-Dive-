using System.Collections.Generic;
using UnityEngine;

namespace Realmwalker
{
    [DefaultExecutionOrder(200), DisallowMultipleComponent]
    public sealed class RealmwalkerFollowCamera : MonoBehaviour
    {
        [SerializeField] Vector3 offset = new Vector3(0, 3.6f, -6.5f);
        [SerializeField, Range(.03f, .5f)] float smoothing = .13f;
        [SerializeField, Range(40, 80)] float fieldOfView = 58;
        [SerializeField] LayerMask collisionMask = ~0;
        Camera view;
        Vector3 velocity, oldPosition;
        Quaternion oldRotation;
        float oldFov, oldNear;
        bool oldOrtho;
        readonly List<Camera> suspended = new List<Camera>();
        readonly RaycastHit[] hits = new RaycastHit[24];
        public Camera View => view;

        void Start()
        {
            // Reuse the realm's camera and its URP settings/UI references; no extra camera or listener.
            foreach (var camera in Camera.allCameras)
            {
                if (camera.gameObject.scene != gameObject.scene || camera.targetTexture != null) continue;
                if (view == null) view = camera;
                else { suspended.Add(camera); camera.enabled = false; }
            }
            if (view == null) return;
            oldPosition = view.transform.position; oldRotation = view.transform.rotation;
            oldFov = view.fieldOfView; oldNear = view.nearClipPlane; oldOrtho = view.orthographic;
            view.orthographic = false; view.fieldOfView = fieldOfView; view.nearClipPlane = .15f;
            view.transform.position = transform.position + offset;
            Aim();
        }

        void LateUpdate()
        {
            if (view == null) return;
            Vector3 focus = transform.position + Vector3.up * 1.25f;
            Vector3 desired = transform.position + offset;
            Vector3 ray = desired - focus;
            float distance = ray.magnitude;
            int count = Physics.SphereCastNonAlloc(focus, .18f, ray.normalized, hits, distance,
                collisionMask, QueryTriggerInteraction.Ignore);
            float clear = distance;
            for (int i = 0; i < count; i++)
                if (!hits[i].transform.IsChildOf(transform)) clear = Mathf.Min(clear, Mathf.Max(.6f, hits[i].distance - .2f));
            Vector3 safe = focus + ray.normalized * clear;
            Vector3 next = Vector3.SmoothDamp(view.transform.position, safe, ref velocity, smoothing);
            // Snap inward only when necessary to avoid interpolating through an occluder.
            view.transform.position = clear < distance && Vector3.Distance(next, focus) > clear ? safe : next;
            Aim();
        }

        void Aim() => view.transform.rotation = Quaternion.LookRotation(
            transform.position + new Vector3(0, 1.15f, 3.2f) - view.transform.position, Vector3.up);

        void OnDisable()
        {
            if (view != null)
            {
                view.transform.SetPositionAndRotation(oldPosition, oldRotation);
                view.fieldOfView = oldFov; view.nearClipPlane = oldNear; view.orthographic = oldOrtho;
            }
            foreach (var camera in suspended) if (camera != null) camera.enabled = true;
        }
    }
}
