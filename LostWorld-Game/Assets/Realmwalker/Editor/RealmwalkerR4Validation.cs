using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LostWorlds.Person2.Integration;
using LostWorlds.Person2.Journey;
using LostWorlds.Person2.Hardware;

namespace Realmwalker.Authoring
{
    public static class RealmwalkerR4Validation
    {
        [MenuItem("Tools/Lost Worlds/R4/Validate Jungle Gameplay")]
        public static void Validate() { SessionState.SetBool("R4.pending", true); RealmwalkerR2.Tests(); }
        public static void Batch() => EditorApplication.delayCall += Validate;
        public static void BatchHardware() => EditorApplication.delayCall += () => { SessionState.SetBool("R4.hardware", true); RealmwalkerR2.Tests(); };
        static void Check(bool pass, string message) { if (!pass) throw new Exception("R4 FAIL: " + message); Debug.Log("R4 PASS: " + message); }

        public static IEnumerator Hardware(RealmwalkerRunner runner, IntegrationSession session, PortalUnlockMechanism lever)
        {
            Check(!lever.CanExit && !lever.Portal.gameObject.activeSelf, "Fresh portal starts locked");
            runner.ResetMotion(new Vector3(2, lever.GetComponent<BoxCollider>().bounds.center.y - 1, lever.GetComponent<BoxCollider>().bounds.center.z)); Physics.SyncTransforms();
            lever.GetComponent<PortalAutoRunPause>().SampleNow();
            Check(runner.Paused && lever.CanInteract(session.Interactor), "Hardware test pauses within reachable lever range");
            Check(session.GetComponent<ProcessedHardwareAdapter>().ReceiveMessage("{\"command\":\"ACTIVATE\"}") && lever.CanExit, "P10 ACTIVATE unlocks at actual lever");
            Check(!lever.TryInteract(session.Interactor) && !lever.TestActivate(), "Repeated activation rejected");
            float until = Time.time + 1.1f; while (Time.time < until) yield return null;
            Check(lever.RevealComplete && !runner.Paused, "Hardware activation reveals portal and releases pause");
            until = Time.time + 6; while (session.CurrentRealm.RealmIndex == 0 && Time.time < until) yield return null;
            Check(session.CurrentRealm.RealmIndex == 1, "Hardware path physically enters portal and reaches Ruins");
        }

        public static IEnumerator Run(RealmwalkerRunner runner, IntegrationSession session, PortalUnlockMechanism lever)
        {
            var world = session.CurrentRealm.GetComponentInChildren<JourneyRealm>();
            if (world == null) world = session.CurrentRealm.transform.root.GetComponentInChildren<JourneyRealm>();
            var tokens = world.Tokens.OrderBy(t => t.Center.z).ToArray();
            Check(tokens.Length == 10 && tokens.Select(t => t.TokenId).Distinct().Count() == 10, "Ten unique Jungle IDs preserved");
            Check(tokens.All(t => Mathf.Abs(t.Radius - .45f) < .001f), "Small 0.45 m pickup volumes");
            runner.SetAutoRunPaused(true);
            int initial = world.Ledger.Count;
            var first = tokens[0];
            float adjacentX = Mathf.Abs(first.Center.x) > 1 ? 0 : 2;
            runner.ResetMotion(new Vector3(adjacentX, first.Center.y - .925f, first.Center.z));
            Physics.SyncTransforms();
            Check(!first.TryCollect(runner.transform), "Adjacent running position cannot collect token");
            float until = Time.time + .1f; while (Time.time < until) yield return null;
            Check(!first.IsCollected && world.Ledger.Count == initial, "Automatic update and triggers also reject neighboring position");
            runner.ResetMotion(first.Center - Vector3.up * .925f); Physics.SyncTransforms();
            until = Time.time + .1f; while (Time.time < until) yield return null;
            Check(first.IsCollected && world.Ledger.Count == initial + 1, "Automatic collider contact collects exactly once");
            Check(!first.TryCollect(runner.transform) && !first.TestCollect() && world.Ledger.Count == initial + 1, "Duplicate contact/test calls do not increment count");

            var second = tokens[1];
            runner.ResetMotion(second.Center - Vector3.up * 3.0f); Physics.SyncTransforms();
            Check(!second.TryCollect(runner.transform), "Token above capsule cannot collect from below");
            runner.ResetMotion(second.Center + Vector3.up); Physics.SyncTransforms();
            Check(!second.TryCollect(runner.transform), "Passing above token without vertical overlap cannot collect");
            runner.ResetMotion(second.Center - Vector3.up * .6f); Physics.SyncTransforms();
            Check(!runner.Grounded && second.TryCollect(runner.transform), "Airborne capsule overlap collects");
            Check(!second.TryCollect(runner.transform), "Airborne duplicate blocked");

            var adapter = session.GetComponent<ProcessedHardwareAdapter>();
            foreach (var position in new[] { new Vector3(0, 4, 103), new Vector3(-2, 4, 103), new Vector3(0, 4, 108), new Vector3(2, 3, 97) })
            {
                runner.ResetMotion(position); Physics.SyncTransforms();
                Check(!lever.CanInteract(session.Interactor), "No prompt/eligibility outside actual lever range: " + position);
                Check(!lever.TryInteract(session.Interactor), "Direct interaction rejected outside radius");
                adapter.ReceiveMessage("{\"command\":\"ACTIVATE\"}");
                Check(!lever.CanExit, "Hardware ACTIVATE cannot unlock remotely");
                lever.GetComponent<PortalAutoRunPause>().SampleNow();
                Check(!lever.GetComponent<PortalAutoRunPause>().Waiting, "No unreachable pause outside lever range");
            }
            runner.ResetMotion(new Vector3(2, lever.GetComponent<BoxCollider>().bounds.center.y - 1, lever.GetComponent<BoxCollider>().bounds.center.z)); Physics.SyncTransforms();
            Check(lever.CanInteract(session.Interactor) && session.Interactor.IsInRange(lever), "Right position can reach actual lever");
            // Leave unlocking to the existing E/pause/transition regression. No state is reset or bypassed.
            runner.ResetMotion(new Vector3(0, .08f, 2)); runner.SetAutoRunPaused(false);
            until = Time.time + .3f; while (Time.time < until) yield return null;
        }
    }
}

