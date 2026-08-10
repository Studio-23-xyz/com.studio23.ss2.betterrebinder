using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace com.studio23.ss2.betterrebinder.Core
{
    public class ResetClickedEvent : EventBase<ResetClickedEvent>
    {
        public InputActionReference ActionReference { get; private set; }
        public Button Button { get; private set; }

        public ResetClickedEvent Init(InputActionReference actionReference, Button button)
        {
            ActionReference = actionReference;
            Button = button;
            return this;
        }
    }
}
