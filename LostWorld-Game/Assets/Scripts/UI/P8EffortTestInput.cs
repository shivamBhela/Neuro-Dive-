using UnityEngine;
using UnityEngine.InputSystem;
namespace LostWorlds.Person2.UI
{
    /// <summary>Temporary local test input; no hardware simulation or final input manager.</summary>
    public sealed class P8EffortTestInput : MonoBehaviour
    {
        [SerializeField] EffortModel model;
        [SerializeField, Min(1)] float step=10;
        public void Bind(EffortModel source) => model=source;
        void Update() => PollKeyboard();
        public void PollKeyboard()
        {
            if(!isActiveAndEnabled || model==null || Keyboard.current==null)return;
            var k=Keyboard.current;
            if(k.backspaceKey.wasPressedThisFrame) { model.ResetEffort(); return; }
            bool up=k.pageUpKey.wasPressedThisFrame, down=k.pageDownKey.wasPressedThisFrame;
            if(up!=down)model.SetEffort(model.Value+(up?step:-step));
        }
    }
}
