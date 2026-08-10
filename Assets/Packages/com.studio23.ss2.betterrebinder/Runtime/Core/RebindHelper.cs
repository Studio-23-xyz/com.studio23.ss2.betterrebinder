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
        
        [Header("Button Name")]
        [SerializeField] private string saveButtonName;
        [SerializeField] private string resetButtonName;
        [SerializeField] private long saveButtonHoldTimeMs;
        [SerializeField] private long resetButtonHoldTimeMs;

        private IVisualElementScheduledItem _holdTimer;

        private readonly string _rebindsPlayerPrefsKey = Rebinder.DefaultRebindsPlayerPrefsKey;

        private readonly Rebinder _rebinder = new Rebinder();
        private Button _waitingForRebindButton;
        private string _waitingForRebindButtonText;
        private Button _saveButton;
        private Button _resetButton;

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

            var root = document.rootVisualElement;
            root.Query<InputActionField>().ForEach(AttachToField);
            BindActionButtons(root);
        }

        private void UnbindFromVisualTree()
        {
            UnbindActionButtons();

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
            field.ResetClicked -= HandleResetClicked;
            field.ResetClicked += HandleResetClicked;
        }

        private void DetachFromField(InputActionField field)
        {
            field.BindingClicked -= HandleBindingClicked;
            field.ResetClicked -= HandleResetClicked;
        }

        private void BindActionButtons(VisualElement root)
        {
            UnbindActionButtons();

            if (!string.IsNullOrWhiteSpace(saveButtonName))
            {
                _saveButton = root.Q<Button>(saveButtonName);
                if (_saveButton == null)
                    Debug.LogWarning($"RebindHelper could not find a save button named '{saveButtonName}'.");
                else
                {
                    _saveButton.RegisterCallback<PointerDownEvent>(_ =>
                    {
                        _holdTimer = _saveButton.schedule.Execute(HandleSaveClicked);
                        _holdTimer.ExecuteLater(saveButtonHoldTimeMs);
                    }, TrickleDown.TrickleDown);
                    
                    _saveButton.RegisterCallback<PointerUpEvent>(_ => CancelPointerHold(), TrickleDown.TrickleDown);
                    _saveButton.RegisterCallback<PointerLeaveEvent>(_ => CancelPointerHold(), TrickleDown.TrickleDown);
                }
            }

            if (!string.IsNullOrWhiteSpace(resetButtonName))
            {
                _resetButton = root.Q<Button>(resetButtonName);
                if (_resetButton == null)
                    Debug.LogWarning($"RebindHelper could not find a reset button named '{resetButtonName}'.");
                else
                {
                    _resetButton.RegisterCallback<PointerDownEvent>(_ =>
                    {
                        _holdTimer = _resetButton.schedule.Execute(HandleResetAllClicked);
                        _holdTimer.ExecuteLater(resetButtonHoldTimeMs);
                    }, TrickleDown.TrickleDown);
                    
                    _resetButton.RegisterCallback<PointerUpEvent>(_ => CancelPointerHold(), TrickleDown.TrickleDown);
                    _resetButton.RegisterCallback<PointerLeaveEvent>(_ => CancelPointerHold(), TrickleDown.TrickleDown);
                }
            }
        }

        private void CancelPointerHold()
        {
            _holdTimer?.Pause();
        }

        private void UnbindActionButtons()
        {
            if (_saveButton != null)
                _saveButton.clicked -= HandleSaveClicked;

            if (_resetButton != null)
                _resetButton.clicked -= HandleResetAllClicked;

            _saveButton = null;
            _resetButton = null;
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

        private void HandleResetClicked(ResetClickedEvent evt)
        {
            if (evt == null)
                return;

            var action = evt.ActionReference?.action;
            if (action == null)
            {
                Debug.LogWarning("Reset clicked without a valid InputActionReference.");
                return;
            }

            _rebinder.ResetToDefault(action);
            RefreshFields();
        }

        private void HandleSaveClicked()
        {
            _rebinder.SaveBindingOverrides(playerInput, _rebindsPlayerPrefsKey);
        }

        private void HandleResetAllClicked()
        {
            _rebinder.ResetAll(playerInput);
            _rebinder.SaveBindingOverrides(playerInput, _rebindsPlayerPrefsKey);
            RefreshFields();
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
