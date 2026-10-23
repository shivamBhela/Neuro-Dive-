using UnityEngine;
using LostWorlds.Person2.Integration;

namespace Realmwalker
{
    /// <summary>Authored gap dimensions and one calm acknowledgement per successful crossing.</summary>
    public sealed class RealmJumpChallenge : MonoBehaviour
    {
        public float near,far,width;
        bool approached,airborne,acknowledged;
        void Update()
        {
            var s=IntegrationSession.Instance;
            if(acknowledged||s==null||!s.CommandsReady||s.CurrentRealm.gameObject.scene!=gameObject.scene||s.PlayerReference==null)return;
            var r=s.PlayerReference.GetComponent<RealmwalkerRunner>();if(r==null)return;
            var p=r.transform.position;
            if(Mathf.Abs(p.x)>width*.5f)return;
            if(p.z>=near-3&&p.z<near&&r.Grounded)approached=true;
            if(approached&&!r.Grounded&&r.VerticalSpeed>0&&p.z<far)airborne=true;
            if(airborne&&r.Grounded&&p.z>far&&p.z<far+3){acknowledged=true;s.Hud?.ShowFeedback("Nice jump. Keep going at your pace.");}
        }
    }
}
