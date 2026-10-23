using System;
using System.Linq;
using UnityEngine;
using LostWorlds.Person2.World;
namespace LostWorlds.Person2.Integration
{
    /// <summary>World-space path snapshots. Selecting a path never moves a player.</summary>
    public sealed class BranchPathBinding : MonoBehaviour
    {
        [SerializeField] RouteChoiceZone zone;
        [SerializeField] Transform[] left=new Transform[0],right=new Transform[0];
        public RouteChoiceZone Zone=>zone;
        public event Action<RouteChoice,Vector3[]> PathSelected;
        public Vector3[] GetPath(RouteChoice choice)=>(choice==RouteChoice.Left?left:choice==RouteChoice.Right?right:Array.Empty<Transform>()).Where(t=>t!=null).Select(t=>t.position).ToArray();
        void OnEnable(){if(zone!=null)zone.SelectionAccepted+=Accepted;}
        void OnDisable(){if(zone!=null)zone.SelectionAccepted-=Accepted;}
        void Accepted(RouteChoiceZone source,RouteChoice choice)
        {if(PathSelected!=null)foreach(Action<RouteChoice,Vector3[]> callback in PathSelected.GetInvocationList())try{callback(choice,GetPath(choice));}catch(Exception ex){Debug.LogException(ex,this);}}
    }
}
