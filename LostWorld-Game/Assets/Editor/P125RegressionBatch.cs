using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace LostWorlds.Person2.Editor
{
    // Batch dispatch waits for Unity startup to finish resolving existing scene references.
    public static class P125RegressionBatch
    {
        public static void Run()=>EditorApplication.delayCall+=Dispatch;
        static void Dispatch()
        {
            try {
                var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-journeySuite");
                string suite=index>=0&&index+1<args.Length?args[index+1]:"";
                string[] allowed={"P4InteractionValidation","P6RouteValidation","P7HudValidation","P8EffortValidation","P9SimulatorValidation","P10AdapterValidation","P11IntegrationValidation","P12PolishValidation"};
                if(!allowed.Contains(suite))throw new ArgumentException("Unknown regression suite");
                typeof(P125RegressionBatch).Assembly.GetType("LostWorlds.Person2.Editor."+suite).GetMethod("BuildAndValidateBatch").Invoke(null,null);
            }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
