using com.studio23.ss2.betterrebinder.Control;
using Studio23.SS2.BetterRebinder.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace com.studio23.ss2.betterrebinder.Core
{
    public class RebindHelper : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private string rebindingDisplayText = "...";
        private readonly string _rebindsPlayerPrefsKey = Rebinder.DefaultRebindsPlayerPrefsKey;

        private readonly Rebinder _rebinder = new Rebinder();
        private Button _waitingForRebindButton;
        private string _waitingForRebindButtonText;

        private void Awake()
        {
            playerInput ??= GetComponent<PlayerInput>();
            _rebinder.LoadBindingOverrides(playerInput, _rebindsPlayerPrefsKey);
        }

        private void OnEnable()
        {
            _rebinder.LoadBindingOverrides(playerInput, _rebindsPlayerPrefsKey);
            BindToVisualTree();
            RefreshFields();

            var document = ResolveDocument();
            document?.rootVisualElement?.schedule.Execute(() =>
            {
                if (!isActiveAndEnabled)
                    return;

                BindToVisualTree();
                RefreshFields();
            });
        }

        private void OnDisable()
        {
            ClearWaitingForRebind();
            UnbindFromVisualTree();
        }

        private void BindToVisualTree()
        {
            var document = ResolveDocument();
            if (document == null || document.rootVisualElement == null)
            {
                Debug.LogWarning("RebindHelper could not find a valid UIDocument root. Make sure it is on the same GameObject as the UI document or assign one manually.");
                return;
            }

            document.rootVisualElement.Query<InputActionField>().ForEach(AttachToField);
        }

        private void UnbindFromVisualTree()
        {
            var document = ResolveDocument();
            if (document == null || document.rootVisualElement == null)
                return;

            document.rootVisualElement.Query<InputActionField>().ForEach(DetachFromField);
        }

        private void RefreshFields()
        {
            var document = ResolveDocument();
            if (document == null || document.rootVisualElement == null)
                return;

            document.rootVisualElement.Query<InputActionField>().ForEach(field => field.Refresh());
        }

        private void AttachToField(InputActionField field)
        {
            field.BindingClicked -= HandleBindingClicked;
            field.BindingClicked += HandleBindingClicked;
        }

        private void DetachFromField(InputActionField field)
        {
            field.BindingClicked -= HandleBindingClicked;
        }

        private UIDocument ResolveDocument()
        {
            if (uiDocument != null)
                return uiDocument;

            return GetComponent<UIDocument>() ?? GetComponentInParent<UIDocument>();
        }

        private void HandleBindingClicked(BindingClickedEvent evt)
        {
            if (evt == null)
                return;

            var action = evt.ActionReference?.action;
            if (action == null)
            {
                Debug.LogWarning("Binding clicked without a valid InputActionReference.");
                return;
            }

            Debug.Log($"Starting rebind for action '{evt.ActionReference.name}' binding index {evt.BindingIndex}.");
            SetWaitingForRebind(evt.Button);
            _rebinder.StartInteractiveRebind(
                action,
                evt.BindingIndex,
                playerInput,
                _rebindsPlayerPrefsKey,
                () =>
                {
                    ClearWaitingForRebind();
                    evt.Button?.GetFirstAncestorOfType<InputActionField>()?.Refresh();
                },
                ClearWaitingForRebind);
        }

        private void SetWaitingForRebind(Button button)
        {
            ClearWaitingForRebind();
            _waitingForRebindButton = button;
            _waitingForRebindButtonText = button?.text;
            _waitingForRebindButton?.AddToClassList(InputActionField.WaitingForRebindUssClassName);
            if (_waitingForRebindButton != null)
                _waitingForRebindButton.text = rebindingDisplayText;
        }

        private void ClearWaitingForRebind()
        {
            if (_waitingForRebindButton != null)
            {
                _waitingForRebindButton.RemoveFromClassList(InputActionField.WaitingForRebindUssClassName);
                _waitingForRebindButton.text = _waitingForRebindButtonText;
            }

            _waitingForRebindButton = null;
            _waitingForRebindButtonText = null;
        }
    }
}
