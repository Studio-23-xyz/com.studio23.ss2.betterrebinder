using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using Image = UnityEngine.UI.Image;

public class RebindAction : MonoBehaviour
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

	public bool excludeMouse
	{
		get => m_ExcludeMouse;
		set
		{
			m_ExcludeMouse = value;
		}
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

	private int _effectiveBindingIndex;

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

	public Image BindingSprite
	{
		get => m_BindingImage;
		set => m_BindingImage = value;
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

	public List<Image> selectionImages
	{
		get => m_SelectionImages;
		set => m_SelectionImages = value;
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

	public bool IsControllerExpected
	{
		get => _isControllerExpected;
		set => _isControllerExpected = value;
	}

	/// <summary>
	/// When an interactive rebind is in progress, this is the rebind operation controller.
	/// Otherwise, it is <c>null</c>.
	/// </summary>
	public InputActionRebindingExtensions.RebindingOperation ongoingRebind => m_RebindOperation;

	private string _existingBindingPath = "";

	/// <summary>
	/// Trigger a refresh of the currently displayed binding.
	/// </summary>
	public void UpdateBindingDisplay()
	{
		var action = m_Action?.action;
		m_ActionLabel.text = action.name;
		var ind = action.bindings.IndexOf(x => x.id.ToString() == m_BindingId);
		var displayString = string.Empty;
		var deviceLayoutName = default(string);
		var controlPath = default(string);

		if (action != null)
		{
			var bindingIndex = action.bindings.IndexOf(x => x.id.ToString() == m_BindingId);
			if (bindingIndex != -1)
				displayString = action.GetBindingDisplayString(bindingIndex, out deviceLayoutName, out controlPath, displayStringOptions);
			m_BindingText.text = controlPath;
		}

		// Give listeners a chance to configure UI in response.
		m_UpdateBindingUIEvent?.Invoke(this, displayString, deviceLayoutName, controlPath);
	}

	/// <summary>
	/// Initiate an interactive rebind that lets the player actuate a control to choose a new binding
	/// for the action.
	/// </summary>
	public async void StartInteractiveRebind()
	{
		if (!ResolveActionAndBinding(out var action, out var bindingIndex))
			return;

		UpdateRebindOverlayAndHightlights(true);
		SetOverlay(action.name);
		_existingBindingPath = action.bindings[bindingIndex].effectivePath;

		var rebindedInput = await CheckForInputs();
		if (rebindedInput.Equals("escape") || rebindedInput.Equals("rightButton"))
		{
			Debug.Log($"Rebinding cancelled");
			UpdateRebindOverlayAndHightlights(false);
			return;
		}

		CheckDuplicateBindings(rebindedInput, action, bindingIndex);

		var temp = action.bindings;

		action.ApplyBindingOverride(bindingIndex, rebindedInput);

		Debug.Log($"Effective path of the selected action and binding index {temp[bindingIndex].effectivePath}");
		Debug.Log($"Overrided the action {action.bindings[bindingIndex]}");

		UpdateRebindOverlayAndHightlights(false);
    }

	[ContextMenu("Print Effective Binding")]
	public void GetCurrentlyEffectiveBinding()
	{
		if (!ResolveActionAndBinding(out var action, out var bindingIndex))
			return;
		Debug.Log($"Overrided the action {action.bindings[bindingIndex]}");
	}

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
		_effectiveBindingIndex = bindingIndex;
		if (bindingIndex == -1)
		{
			Debug.LogError($"Cannot find binding with ID '{bindingId}' on '{action}'", this);
			return false;
		}

		return true;
	}

	/// <summary>
	/// Responsible for enabling and disabling overlay and hints during rebinding
	/// </summary>
	/// <param name="state"></param>
	private void UpdateRebindOverlayAndHightlights(bool state)
	{
		SetRebindingHighlight(state);
		m_RebindOverlay?.SetActive(state);
	}

	private void CheckDuplicateBindings(string rebindedInput, InputAction action, int bindingIndex)
	{
		var actionMapBindings = action.actionMap.bindings;
		for (int i = 0; i < actionMapBindings.Count; i++)
		{
			if (actionMapBindings[i].effectivePath == rebindedInput)
			{
				InputAction targetAction = action.actionMap.FindAction(actionMapBindings[i].action);
				var duplicateBindings = targetAction.bindings;
				for (int j = 0; j < duplicateBindings.Count; j++)
				{
					if (targetAction.bindings[j].effectivePath == rebindedInput && !targetAction.name.Contains("0"))
					{
						targetAction.ApplyBindingOverride(j, _existingBindingPath);
						Debug.Log($"<color=#FF461E>Swap occurred for {targetAction} & is now set to {targetAction.bindings[j].effectivePath}</color>");
					}
				}
			}
		}
	}

	private async UniTask<string> CheckForInputs()
	{
		string newInput = null;
		var inputReader = InputSystem.onAnyButtonPress.Call(newControl =>
		{
			newInput = FormatInputToEffectivePath(newControl);
		});

		if (_isControllerExpected)
			await UniTask.Delay(TimeSpan.FromSeconds(7f));
		else
		{
			while (string.IsNullOrEmpty(newInput))
			{
				await UniTask.Yield();
				await UniTask.NextFrame();
			}
		}
		inputReader.Dispose();
		return newInput;
	}

	private string FormatInputToEffectivePath(InputControl newControl)
	{
		string newBinding = newControl.name;
		string inputDeviceName = newControl.device.name;
		if (inputDeviceName.Contains("Windows"))
			inputDeviceName = newControl.device.name.Replace("Windows", "");
		else if (inputDeviceName.Contains("Dual"))
			inputDeviceName = $"DualShockGamepad";
		else if (!inputDeviceName.Contains("Keyboard") && !inputDeviceName.Contains("Mouse"))
			inputDeviceName = $"Gamepad";
		
		return $"<{inputDeviceName}>/{newBinding}";
	}

	/// <summary>
	/// The following async function waits and listens for four unique keypresses. Duplicate check for this array needs to be implemented if we want to use it. 
	/// </summary>
	/// <returns></returns>
	private async UniTask<string[]> CheckForCompositeInputs()
	{
		string[] compositeInputs = new string[4];
		int i = 0;
		var inputReader = InputSystem.onAnyButtonPress.Call(control =>
		{
			if (!compositeInputs.Contains(control.name))
			{
				Debug.Log($"{control.name}");
				compositeInputs[i] = control.name;
				i++;
			}
		});
		while (i < 4)
		{
			await UniTask.Yield();
			await UniTask.NextFrame();
		}
		inputReader.Dispose();
		return compositeInputs;
	}

	private void SetOverlay(string partName)
	{
		m_RebindText.text = $"Waiting for input...";
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

	private void SetRebindingHighlight(bool state)
	{
		foreach (Image mSelectionImage in m_SelectionImages)
		{
			mSelectionImage.gameObject.SetActive(state);
		}
	}

	protected void OnEnable()
	{
		if (s_RebindActionUIs == null)
			s_RebindActionUIs = new List<RebindAction>();
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

	[Tooltip("Text label that will receive the current, formatted binding string.")]
	[SerializeField]
	private TextMeshProUGUI m_BindingText;

	[Tooltip("Optional UI that will be shown while a rebind is in progress.")]
	[SerializeField]
	private GameObject m_RebindOverlay;

	[Tooltip("Optional text label that will be updated with prompt for user input.")]
	[SerializeField]
	private TextMeshProUGUI m_RebindText;

	[SerializeField]
	private Image m_BindingImage;

	[Tooltip("Should this action exclude mouse buttons")]
	[SerializeField]
	private bool m_ExcludeMouse;

	[Tooltip("Prevents passing of any inputs other than controllers")]
	[SerializeField] 
	private bool _isControllerExpected;

	[SerializeField] private List<Image> m_SelectionImages = new List<Image>();

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

	private static List<RebindAction> s_RebindActionUIs;

	// We want the label for the action name to update in edit mode, too, so
	// we kick that off from here.
#if UNITY_EDITOR
	protected void OnValidate()
	{
		//UpdateActionLabel();
		UpdateBindingDisplay();
	}

#endif

	private void UpdateActionLabel()
	{
		if (m_ActionLabel != null)
		{
			var action = m_Action?.action;
			if (action == null || action.bindings[0].isComposite)
			{
				Debug.LogWarning($"Action not set!");
				return;
			}
			m_ActionLabel.text = action != null ? action.name : string.Empty;
		}
	}

	[Serializable]
	public class UpdateBindingUIEvent : UnityEvent<RebindAction, string, string, string>
	{
	}

	[Serializable]
	public class InteractiveRebindEvent : UnityEvent<RebindAction, InputActionRebindingExtensions.RebindingOperation>
	{
	}
}