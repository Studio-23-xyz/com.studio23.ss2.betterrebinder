using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Studio23.SS2.BetterRebinder.Core
{
    public class Rebinder
    {
        public const string DefaultRebindsPlayerPrefsKey = "rebinds";

        public void LoadBindingOverrides(PlayerInput playerInput, string playerPrefsKey = DefaultRebindsPlayerPrefsKey)
        {
            if (playerInput?.actions == null || !PlayerPrefs.HasKey(playerPrefsKey))
                return;

            var rebinds = PlayerPrefs.GetString(playerPrefsKey);
            if (!string.IsNullOrEmpty(rebinds))
                playerInput.actions.LoadBindingOverridesFromJson(rebinds);
        }

        public void SaveBindingOverrides(PlayerInput playerInput, string playerPrefsKey = DefaultRebindsPlayerPrefsKey)
        {
            if (playerInput?.actions == null)
                return;

            PlayerPrefs.SetString(playerPrefsKey, playerInput.actions.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
        }

        public bool ResolveActionAndBinding(InputActionReference actionReference, string bindingId, out InputAction action, out int bindingIndex)
        {
            bindingIndex = -1;
            action = actionReference?.action;

            if (action == null)
                return false;

            return ResolveActionAndBinding(action, bindingId, out bindingIndex);
        }

        public bool ResolveActionAndBinding(InputAction action, string bindingId, out int bindingIndex)
        {
            bindingIndex = -1;

            if (action == null)
                return false;

            if (string.IsNullOrEmpty(bindingId))
                return false;

            if (!Guid.TryParse(bindingId, out var parsedBindingId))
            {
                Debug.LogWarning($"Invalid binding ID '{bindingId}'.");
                return false;
            }

            bindingIndex = action.bindings.IndexOf(x => x.id == parsedBindingId);

            if (bindingIndex == -1)
            {
                Debug.LogError($"Cannot find binding with ID '{parsedBindingId}' on '{action}'");
                return false;
            }

            return true;
        }

        public bool IsBindingKeyboard(InputActionReference actionReference, string bindingId)
        {
            if (!ResolveActionAndBinding(actionReference, bindingId, out var action, out var bindingIndex))
                return false;

            return IsBindingKeyboard(action, bindingIndex);
        }

        public bool IsBindingKeyboard(InputAction action, int bindingIndex)
        {
            if (action == null)
                return false;

            if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
                return false;

            var effectiveIndex = bindingIndex;
            if (action.bindings[effectiveIndex].isComposite)
            {
                effectiveIndex++;
                if (effectiveIndex >= action.bindings.Count || !action.bindings[effectiveIndex].isPartOfComposite)
                    return false;
            }

            var effectivePath = action.bindings[effectiveIndex].effectivePath;
            return !string.IsNullOrEmpty(effectivePath) && effectivePath.Contains("Keyboard");
        }

        public void ResetToDefault(InputActionReference actionReference, string bindingId)
        {
            if (!ResolveActionAndBinding(actionReference, bindingId, out var action, out var bindingIndex))
                return;

            ResetToDefault(action, bindingIndex);
        }

        public void ResetToDefault(InputAction action, int bindingIndex)
        {
            if (action == null)
                return;

            if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
                return;

            if (action.bindings[bindingIndex].isComposite)
            {
                for (var i = bindingIndex + 1; i < action.bindings.Count && action.bindings[i].isPartOfComposite; ++i)
                    action.RemoveBindingOverride(i);
            }
            else
            {
                action.RemoveBindingOverride(bindingIndex);
            }
        }

        public void StartInteractiveRebind(InputActionReference actionReference, string bindingId)
        {
            if (!ResolveActionAndBinding(actionReference, bindingId, out var action, out var bindingIndex))
                return;

            StartInteractiveRebind(action, bindingIndex);
        }

        public void StartInteractiveRebind(InputAction action, int bindingIndex, Action onComplete = null, Action onCancel = null)
        {
            StartInteractiveRebind(action, bindingIndex, null, DefaultRebindsPlayerPrefsKey, onComplete, onCancel);
        }

        public void StartInteractiveRebind(InputAction action, int bindingIndex, PlayerInput playerInput, string playerPrefsKey = DefaultRebindsPlayerPrefsKey, Action onComplete = null, Action onCancel = null)
        {
            if (action == null)
                return;

            if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
                return;

            var wasEnabled = action.enabled;
            if (wasEnabled)
                action.Disable();

            if (action.bindings[bindingIndex].isComposite)
            {
                var firstPartIndex = bindingIndex + 1;
                if (firstPartIndex < action.bindings.Count && action.bindings[firstPartIndex].isPartOfComposite)
                {
                    PerformInteractiveRebind(action, firstPartIndex, allCompositeParts: true, wasEnabled, playerInput, playerPrefsKey, onComplete, onCancel);
                    return;
                }
            }
            else
            {
                PerformInteractiveRebind(action, bindingIndex, allCompositeParts: false, wasEnabled, playerInput, playerPrefsKey, onComplete, onCancel);
                return;
            }

            if (wasEnabled)
                action.Enable();
        }

        public string GetBindingDisplayString(InputActionReference actionReference, string bindingId, InputBinding.DisplayStringOptions displayOptions)
        {
            if (!ResolveActionAndBinding(actionReference, bindingId, out var action, out var bindingIndex))
                return string.Empty;

            return GetBindingDisplayString(action, bindingIndex, displayOptions);
        }

        public string GetBindingDisplayString(InputAction action, int bindingIndex, InputBinding.DisplayStringOptions displayOptions)
        {
            if (action == null)
                return string.Empty;

            if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
                return string.Empty;

            return action.GetBindingDisplayString(bindingIndex, displayOptions);
        }

        private void PerformInteractiveRebind(InputAction action, int bindingIndex, bool allCompositeParts, bool restoreEnabledState, PlayerInput playerInput, string playerPrefsKey, Action onComplete, Action onCancel)
        {
            var rebindingOperation = action.PerformInteractiveRebinding(bindingIndex);

            rebindingOperation
                .OnCancel(operation =>
                {
                    FinishRebind(action, operation, restoreEnabledState);
                    onCancel?.Invoke();
                })
                .OnComplete(operation =>
                {
                    operation.Dispose();

                    if (allCompositeParts)
                    {
                        var nextBindingIndex = bindingIndex + 1;
                        if (nextBindingIndex < action.bindings.Count && action.bindings[nextBindingIndex].isPartOfComposite)
                        {
                            PerformInteractiveRebind(action, nextBindingIndex, allCompositeParts: true, restoreEnabledState, playerInput, playerPrefsKey, onComplete, onCancel);
                            return;
                        }
                    }

                    if (restoreEnabledState)
                        action.Enable();

                    SaveBindingOverrides(playerInput, playerPrefsKey);
                    onComplete?.Invoke();
                });

            rebindingOperation.Start();
        }

        private static void FinishRebind(InputAction action, InputActionRebindingExtensions.RebindingOperation operation, bool restoreEnabledState)
        {
            operation.Dispose();

            if (restoreEnabledState)
                action.Enable();
        }
    }
}
