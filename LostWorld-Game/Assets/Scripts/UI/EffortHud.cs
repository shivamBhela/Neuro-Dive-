using UnityEngine;
using UnityEngine.UI;
namespace LostWorlds.Person2.UI
{
    public sealed class EffortHud : MonoBehaviour
    {
        [SerializeField] EffortModel model;
        [SerializeField] Text label;
        [SerializeField] Image bar;
        public string DisplayText => label != null ? label.text : "";
        public EffortModel Model => model;
        public void Bind(EffortModel source)
        {
            if(model!=null)model.Changed-=Refresh;
            model=source;
            if(isActiveAndEnabled && model!=null)model.Changed+=Refresh;
            Refresh();
        }
        void OnEnable() { if(model!=null)model.Changed+=Refresh; Refresh(); }
        void OnDisable() { if(model!=null)model.Changed-=Refresh; }
        void Refresh()
        {
            if(label==null)return;
            if(model==null) { label.text="Effort unavailable"; if(bar!=null)bar.rectTransform.anchorMax=new Vector2(0,1); return; }
            string state=model.State==EffortState.Normal?"Normal • Take your time":model.State==EffortState.NearLimit?"Near Limit • Go at your own pace":"Rest Needed • REST when you feel ready";
            label.text=$"EFFORT  {model.Value:0} / 100   |   Cap {model.Cap:0}\n{state}";
            if(bar!=null)
            {
                bar.rectTransform.anchorMax=new Vector2(model.Value/100,1);
                bar.color=model.State==EffortState.Normal?new Color(.48f,.78f,.69f):model.State==EffortState.NearLimit?new Color(.85f,.79f,.51f):new Color(.74f,.80f,.95f);
            }
        }
    }
}
