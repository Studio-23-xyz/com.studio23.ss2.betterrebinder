
using System.Collections.Generic;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using System.IO;
using UnityEngine.UI;

namespace Studio23.SS2.BetterRebinder.Core
{
	public class Rebinder : MonoBehaviour
	{
        /// <summary>
        /// Reference to the action that is to be rebound.
        /// </summary>
        public InputActionReference actionReference
        {
            get => m_Action;
            set
            {
                m_Action = value;
                UpdateActionLabel();
                UpdateBindingDisplay();
            }
        }

        /// <summary>
        /// ID (in string form) of the binding that is to be rebound on the action.
        /// </summary>
        /// <seealso cref="InputBinding.id"/>
        public string bindingId
        {
            get => m_BindingId;
            set
            {
                m_BindingId = value;
                UpdateBindingDisplay();
            }
        }

        public bool IsBindingKeyBoard()
        {
            var bindingId = new Guid(m_BindingId);
            var bindingIndex = actionReference.action.bindings.IndexOf(x => x.id == bindingId);
            if (actionReference.action.bindings[bindingIndex].isComposite)
            {
                bindingIndex++;
            }
            return actionReference.action.bindings[bindingIndex].effectivePath.Contains("Keyboard");
        }

        public InputBinding.DisplayStringOptions displayStringOptions
        {
            get => m_DisplayStringOptions;
            set
            {
                m_DisplayStringOptions = value;
                UpdateBindingDisplay();
            }
        }

        /// <summary>
        /// Text component that receives the name of the action. Optional.
        /// </summary>
        public TextMeshProUGUI actionLabel
        {
            get => m_ActionLabel;
            set
            {
                m_ActionLabel = value;
                UpdateActionLabel();
            }
        }

        /// <summary>
        /// Text component that receives the display string of the binding. Can be <c>null</c> in which
        /// case the component entirely relies on <see cref="updateBindingUIEvent"/>.
        /// </summary>
        public TextMeshProUGUI bindingText
        {
            get => m_BindingText;
            set
            {
                m_BindingText = value;
                UpdateBindingDisplay();
            }
        }

        /// <summary>
        /// Optional text component that receives a text prompt when waiting for a control to be actuated.
        /// </summary>
        /// <seealso cref="startRebindEvent"/>
        /// <seealso cref="rebindOverlay"/>
        public TextMeshProUGUI rebindPrompt
        {
            get => m_RebindText;
            set => m_RebindText = value;
        }

        /// <summary>
        /// Optional UI that is activated when an interactive rebind is started and deactivated when the rebind
        /// is finished. This is normally used to display an overlay over the current UI while the system is
        /// waiting for a control to be actuated.
        /// </summary>
        /// <remarks>
        /// If neither <see cref="rebindPrompt"/> nor <c>rebindOverlay</c> is set, the component will temporarily
        /// replaced the <see cref="bindingText"/> (if not <c>null</c>) with <c>"Waiting..."</c>.
        /// </remarks>
        /// <seealso cref="startRebindEvent"/>
        /// <seealso cref="rebindPrompt"/>
        public GameObject rebindOverlay
        {
            get => m_RebindOverlay;
            set => m_RebindOverlay = value;
        }

        /// <summary>
        /// Event that is triggered every time the UI updates to reflect the current binding.
        /// This can be used to tie custom visualizations to bindings.
        /// </summary>
        public UpdateBindingUIEvent updateBindingUIEvent
        {
            get
            {
                if (m_UpdateBindingUIEvent == null)
                    m_UpdateBindingUIEvent = new UpdateBindingUIEvent();
                return m_UpdateBindingUIEvent;
            }
        }

        /// <summary>
        /// Event that is triggered when an interactive rebind is started on the action.
        /// </summary>
        public InteractiveRebindEvent startRebindEvent
        {
            get
            {
                if (m_RebindStartEvent == null)
                    m_RebindStartEvent = new InteractiveRebindEvent();
                return m_RebindStartEvent;
            }
        }

        /// <summary>
        /// Event that is triggered when an interactive rebind has been completed or canceled.
        /// </summary>
        public InteractiveRebindEvent stopRebindEvent
        {
            get
            {
                if (m_RebindStopEvent == null)
                    m_RebindStopEvent = new InteractiveRebindEvent();
                return m_RebindStopEvent;
            }
        }

        /// <summary>
        /// When an interactive rebind is in progress, this is the rebind operation controller.
        /// Otherwise, it is <c>null</c>.
        /// </summary>
        public InputActionRebindingExtensions.RebindingOperation ongoingRebind => m_RebindOperation;

        /// <summary>
        /// Return the action and binding index for the binding that is targeted by the component
        /// according to
        /// </summary>
        /// <param name="action"></param>
        /// <param name="bindingIndex"></param>
        /// <returns></returns>
        public bool ResolveActionAndBinding(out InputAction action, out int bindingIndex)
        {
            bindingIndex = -1;

            action = m_Action?.action;
            if (action == null)
                return false;

            if (string.IsNullOrEmpty(m_BindingId))
                return false;

            // Look up binding index.
            var bindingId = new Guid(m_BindingId);
            bindingIndex = action.bindings.IndexOf(x => x.id == bindingId);
            if (bindingIndex == -1)
            {
                Debug.LogError($"Cannot find binding with ID '{bindingId}' on '{action}'", this);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Trigger a refresh of the currently displayed binding.
        /// </summary>
        public void UpdateBindingDisplay()
        {
            var displayString = string.Empty;
            var deviceLayoutName = default(string);
            var controlPath = default(string);

            // Get display string from action.
            var action = m_Action?.action;
            if (action != null)
            {
                var bindingIndex = action.bindings.IndexOf(x => x.id.ToString() == m_BindingId);
                if (bindingIndex != -1)
                    displayString = action.GetBindingDisplayString(bindingIndex, out deviceLayoutName, out controlPath, displayStringOptions);
            }

            // Set on label (if any).
            if (m_BindingText != null)
                m_BindingText.text = displayString;

            // Give listeners a chance to configure UI in response.
            m_UpdateBindingUIEvent?.Invoke(this, displayString, deviceLayoutName, controlPath);
        }

        /// <summary>
        /// Remove currently applied binding overrides.
        /// </summary>
        public void ResetToDefault()
        {
            if (!ResolveActionAndBinding(out var action, out var bindingIndex))
                return;

            if (action.bindings[bindingIndex].isComposite)
            {
                // It's a composite. Remove overrides from part bindings.
                for (var i = bindingIndex + 1; i < action.bindings.Count && action.bindings[i].isPartOfComposite; ++i)
                    action.RemoveBindingOverride(i);
            }
            else
            {
                action.RemoveBindingOverride(bindingIndex);
            }
            UpdateBindingDisplay();
        }

        /// <summary>
        /// Initiate an interactive rebind that lets the player actuate a control to choose a new binding
        /// for the action.
        /// </summary>
        public void StartInteractiveRebind()
        {
            if (!ResolveActionAndBinding(out var action, out var bindingIndex))
                return;

            // If the binding is a composite, we need to rebind each part in turn.
            if (action.bindings[bindingIndex].isComposite)
            {
                var firstPartIndex = bindingIndex + 1;
                if (firstPartIndex < action.bindings.Count && action.bindings[firstPartIndex].isPartOfComposite)
                    PerformInteractiveRebind(action, firstPartIndex, allCompositeParts: true);
            }
            else
            {
                PerformInteractiveRebind(action, bindingIndex);
            }
        }

        private void PerformInteractiveRebind(InputAction action, int bindingIndex, bool allCompositeParts = false)
        {
            m_RebindOperation?.Cancel(); // Will null out m_RebindOperation.

            void CleanUp()
            {
                m_RebindOperation?.Dispose();
                m_RebindOperation = null;
            }

            action.Disable();
            var previousPath = action.bindings[bindingIndex].effectivePath;

            // Configure the rebind.
            m_RebindOperation = action.PerformInteractiveRebinding(bindingIndex)
                .OnCancel(
                    operation =>
                    {
                        action.Enable();
                        m_RebindStopEvent?.Invoke(this, operation);
                        //m_RebindOverlay?.SetActive(false);
                        UpdateBindingDisplay();
                        CleanUp();
                    })
                .OnComplete(
                    operation =>
                    {
                        //m_RebindOverlay?.SetActive(false);
                        m_RebindStopEvent?.Invoke(this, operation);

                        CheckAndSwapDuplicates(action, bindingIndex, previousPath);
                        CheckAndApplyRebindInOtherActionMaps(action, bindingIndex, previousPath);
                        UpdateBindingDisplay();
                        CleanUp();

                        // If there's more composite parts we should bind, initiate a rebind
                        // for the next part.
                        if (allCompositeParts)
                        {
                            var nextBindingIndex = bindingIndex + 1;
                            if (nextBindingIndex < action.bindings.Count && action.bindings[nextBindingIndex].isPartOfComposite)
                                PerformInteractiveRebind(action, nextBindingIndex, true);
                        }
                        action.Enable();
                    });

            UpdateRebindingControlsExcluding();
            UpdateRebindingPotentialMatch();


            m_bindingIcon.gameObject.SetActive(false);
            m_BindingText.gameObject.SetActive(true);
            m_BindingText.text = $"[Waiting for Input]";

            // Give listeners a chance to act on the rebind starting.
            m_RebindStartEvent?.Invoke(this, m_RebindOperation);

            m_RebindOperation.Start();
        }


        private void CheckAndSwapDuplicates(InputAction action, int bindingIndex, string pathBeforeBinding)
        {
            string currentPath = action.bindings[bindingIndex].effectivePath;

            foreach (var otherAction in action.actionMap)
            {
                if (otherAction != action)
                {
                    for (int i = 0; i < otherAction.bindings.Count; i++)
                    {
                        if (otherAction.bindings[i].effectivePath.Equals(currentPath))
                        {
                            otherAction.ApplyBindingOverride(i,pathBeforeBinding);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < otherAction.bindings.Count; i++)
                    {

                        if(i == bindingIndex) continue;
                        
                        if (otherAction.bindings[i].isPartOfComposite && otherAction.bindings[i].effectivePath.Equals(currentPath))
                        {
                            otherAction.ApplyBindingOverride(i, pathBeforeBinding);
                        }
                    }
                }
            }
        }


        private void CheckAndApplyRebindInOtherActionMaps(InputAction action, int bindingIndex, string pathBeforeBinding)
        {
            string currentPath = action.bindings[bindingIndex].effectivePath;
            var inputActionAsset = action.actionMap.asset;

            foreach (var otherActionMap in inputActionAsset.actionMaps)
            {
                if(otherActionMap == action.actionMap) continue;

                var matchFound = otherActionMap.actions.FirstOrDefault(x => x.name.Replace(otherActionMap.name,"").Equals(action.name.Replace(action.actionMap.name,"")));
                if(matchFound == null) continue;

                foreach (var otherAction in otherActionMap.actions)
                {
                    if (otherAction == matchFound)
                    {
                        if (otherAction.bindings[0].isComposite)
                        {
                            for (int i = 0; i < otherAction.bindings.Count; i++)
                            {
                                if (i == bindingIndex)
                                {
                                    otherAction.ApplyBindingOverride(i, currentPath);
                                }
                                else if (otherAction.bindings[i].isPartOfComposite && otherAction.bindings[i].effectivePath.Equals(currentPath))
                                {
                                    otherAction.ApplyBindingOverride(i, pathBeforeBinding);
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < otherAction.bindings.Count; i++)
                            {
                                if (otherAction.bindings[i].effectivePath.Equals(pathBeforeBinding))
                                {
                                    otherAction.ApplyBindingOverride(i, currentPath);
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < otherAction.bindings.Count; i++)
                        {
                            if (otherAction.bindings[i].effectivePath.Equals(currentPath))
                            {
                                otherAction.ApplyBindingOverride(i, pathBeforeBinding);
                            }
                        }
                    }
                }
            }
        }


        private void UpdateRebindingControlsExcluding()
        {
            if(_controlsExcludePath.FirstOrDefault() == null) return;
            foreach (var excludePath in _controlsExcludePath)
            {
                m_RebindOperation.WithControlsExcluding(excludePath);
            }
        }

        public void PopulateControlsExcludingList(string path)
        {
            _controlsExcludePath.Add(path);
        }

        public void PopulateControlsExcludingList(List<InputBinding> inputBinding)
        {
            foreach (var binding in inputBinding)
            {
                _controlsExcludePath.Add(binding.effectivePath);
            }
            
        }

        private void UpdateRebindingCancel()
        {
            if (_rebindingCancelPath.FirstOrDefault() == null) return;
            foreach (var cancelPath in _rebindingCancelPath)
            {
                m_RebindOperation.WithCancelingThrough(cancelPath);
            }
        }

        public void PopulateRebindingCancelList(string path)
        {
            _rebindingCancelPath.Add(path);
        }

        public void PopulateRebindingCancelList(InputBinding inputBinding)
        {
            _rebindingCancelPath.Add(inputBinding.effectivePath);
        }

        public void PopulateRebindingCancelList(List<InputControl> controls)
        {
            foreach (var control in controls)
            {
                _rebindingCancelPath.Add(control.path);
            }
        }

        public void UpdateRebindingPotentialMatch()
        {
            m_RebindOperation.OnPotentialMatch(operation =>
            {
                foreach (var cancelPath in _rebindingCancelPath)
                {
                    var trimmedPath = cancelPath.Replace("<", "/").Replace(">", "");
                    if (operation.selectedControl.path.Equals(trimmedPath))
                    {
                        m_RebindOperation?.Cancel();
                        break;
                    }
                }
            });
        }

        protected void OnEnable()
        {
            if (s_RebindActionUIs == null)
                s_RebindActionUIs = new List<Rebinder>();
            s_RebindActionUIs.Add(this);
            if (s_RebindActionUIs.Count == 1)
                InputSystem.onActionChange += OnActionChange;
        }

        protected void OnDisable()
        {
            m_RebindOperation?.Dispose();
            m_RebindOperation = null;

            s_RebindActionUIs.Remove(this);
            if (s_RebindActionUIs.Count == 0)
            {
                s_RebindActionUIs = null;
                InputSystem.onActionChange -= OnActionChange;
            }
        }

        void Start()
        {
            UpdateActionLabel();
            UpdateBindingDisplay();
        }

        // When the action system re-resolves bindings, we want to update our UI in response. While this will
        // also trigger from changes we made ourselves, it ensures that we react to changes made elsewhere. If
        // the user changes keyboard layout, for example, we will get a BoundControlsChanged notification and
        // will update our UI to reflect the current keyboard layout.
        private static void OnActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.BoundControlsChanged)
                return;

            var action = obj as InputAction;
            var actionMap = action?.actionMap ?? obj as InputActionMap;
            var actionAsset = actionMap?.asset ?? obj as InputActionAsset;

            for (var i = 0; i < s_RebindActionUIs.Count; ++i)
            {
                var component = s_RebindActionUIs[i];
                var referencedAction = component.actionReference?.action;
                if (referencedAction == null)
                    continue;

                if (referencedAction == action ||
                    referencedAction.actionMap == actionMap ||
                    referencedAction.actionMap?.asset == actionAsset)
                    component.UpdateBindingDisplay();
            }
        }

        [Tooltip("Reference to action that is to be rebound from the UI.")]
        [SerializeField]
        private InputActionReference m_Action;



        [SerializeField]
        private string m_BindingId;

        [SerializeField]
        private InputBinding.DisplayStringOptions m_DisplayStringOptions;

        [Tooltip("Text label that will receive the name of the action. Optional. Set to None to have the "
            + "rebind UI not show a label for the action.")]
        [SerializeField]
        private TextMeshProUGUI m_ActionLabel;

        [SerializeField]
        private bool _overrideActionLabel;

        [SerializeField] 
        private string _overrideActionLabelText;

        [SerializeField]
        private Image m_bindingIcon;

        [Tooltip("Text label that will receive the current, formatted binding string.")]
        [SerializeField]
        private TextMeshProUGUI m_BindingText;

        [Tooltip("Optional UI that will be shown while a rebind is in progress.")]
        [SerializeField]
        private GameObject m_RebindOverlay;

        [Tooltip("Optional text label that will be updated with prompt for user input.")]
        [SerializeField]
        private TextMeshProUGUI m_RebindText;

        [Tooltip("Event that is triggered when the way the binding is display should be updated. This allows displaying "
            + "bindings in custom ways, e.g. using images instead of text.")]
        [SerializeField]
        private UpdateBindingUIEvent m_UpdateBindingUIEvent;

        [Tooltip("Event that is triggered when an interactive rebind is being initiated. This can be used, for example, "
            + "to implement custom UI behavior while a rebind is in progress. It can also be used to further "
            + "customize the rebind.")]
        [SerializeField]
        private InteractiveRebindEvent m_RebindStartEvent;

        [Tooltip("Event that is triggered when an interactive rebind is complete or has been aborted.")]
        [SerializeField]
        private InteractiveRebindEvent m_RebindStopEvent;

        private InputActionRebindingExtensions.RebindingOperation m_RebindOperation;

        private static List<Rebinder> s_RebindActionUIs;
        private List<string> _controlsExcludePath = new List<string>();
        private List<string> _rebindingCancelPath = new List<string>();
        

        // We want the label for the action name to update in edit mode, too, so
        // we kick that off from here.
#if UNITY_EDITOR
        protected void OnValidate()
        {
            UpdateActionLabel();
            UpdateBindingDisplay();
        }

#endif

        private void UpdateActionLabel()
        {
            if (m_ActionLabel != null)
            {
                var action = m_Action?.action;
                if (_overrideActionLabel)
                {
                    m_ActionLabel.text = _overrideActionLabelText;
                }
                else
                {
                    m_ActionLabel.text = action != null ? action.name : string.Empty;
                }
            }
        }

        [Serializable]
        public class UpdateBindingUIEvent : UnityEvent<Rebinder, string, string, string>
        {
        }

        [Serializable]
        public class InteractiveRebindEvent : UnityEvent<Rebinder, InputActionRebindingExtensions.RebindingOperation>
        {
        }
    }
}