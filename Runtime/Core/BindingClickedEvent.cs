using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace com.studio23.ss2.betterrebinder.Core
{
    public class BindingClickedEvent : EventBase<BindingClickedEvent>
    {
        public InputActionReference ActionReference { get; private set; }
        public int BindingIndex { get; private set; }
        public string BindingPath { get; private set; }
        public Button Button { get; private set; }

        public BindingClickedEvent Init(InputActionReference actionReference, int bindingIndex, string bindingPath, Button button)
        {
            ActionReference = actionReference;
            BindingIndex = bindingIndex;
            BindingPath = bindingPath;
            Button = button;
            return this;
        }
    }
}
