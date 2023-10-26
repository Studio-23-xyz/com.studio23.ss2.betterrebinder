using Cysharp.Threading.Tasks;
using Studio23.SS2.BetterRebinder.Data;
using Studio23.SS2.BetterRebinder.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;

namespace Studio23.SS2.BetterRebinder.Core
{
	public class Rebinder : MonoBehaviour
	{
		[SerializeField] private InputActionReference _targetActionReference;
		private InputAction _targetAction;
		private string _existingBindingPath;
		private int _targetIndex;
		public TextMeshProUGUI ActionName;

		public TextMeshProUGUI ActionBindingText;
		public GameObject ActionBindingImage;
		public bool IsDynamic;

		#region Four Part Composite

		[Tooltip("This tick enables iteration of four part composites which is used for movement based actions.")]
		public bool IsFourPartComposite;

		[Tooltip("Specify which one of the four part composite should this action target specifically.")]
		public FourPartComposite CompositeIndex;

		#endregion

		public int BindingIndex => DeviceIndexResolver.DeviceIndex;

		private void Awake()
		{
			GetComponent<Button>().onClick.AddListener(StartRebinding);
		}

		public void Initialize(InputActionReference targetActionReference)
		{
			_targetActionReference = targetActionReference;
			ActionName.text = _targetActionReference.name.Split('/')[1];
			name = ActionName.text;
			UpdateBindingDisplay();
		}

		public async void StartRebinding()
		{
			DebugController.DebugColored($"Rebinding started", $"#8ab7ff");
			if (ResolveActionAndBindingIndex(out _targetIndex) == false)
			{
				DebugController.DebugWarning($"Failed to resolve action or binding for {gameObject.name}");
			}

			_existingBindingPath = _targetAction.bindings[_targetIndex].effectivePath;
			string inputOverride = await CheckForInputs();
			if (inputOverride == null)
			{
				DebugController.DebugWarning($"Input device changed / Rebind window passed without any input.");
				CancelRebindOperation();
				return;
			}

			FindAndSwapDuplicates(inputOverride);
			_targetAction.ApplyBindingOverride(_targetIndex, inputOverride);
			UpdateBindingDisplay();
		}

		private async UniTask<string> CheckForInputs()
		{
			string newBinding = string.Empty;
			int olderIndex = DeviceIndexResolver.DeviceIndex;
			var inputReader = InputSystem.onAnyButtonPress.Call(newControl =>
			{
				DebugController.DebugNormal(
					$"Pressed button {newControl.name} from device {newControl.device.name} with shorter form {newControl.shortDisplayName}");
				newBinding = FormatInputToEffectivePath(newControl);
			});

			while (string.IsNullOrEmpty(newBinding))
			{
				await UniTask.Yield();
				await UniTask.NextFrame();
			}

			inputReader.Dispose();
			int newControlIndex = DeviceIndexResolver.DeviceIndex;
			if (olderIndex != newControlIndex)
			{
				DebugController.DebugWarning($"New input is from a different device! Cancelling rebind operation.");
				return "";
			}

			DebugController.DebugNormal($"Formatted binding: {newBinding}");
			return newBinding;
		}

		private void CancelRebindOperation()
		{
			//TODO print cancellation message on UI and handle necessities
		}

		private string FormatInputToEffectivePath(InputControl newControl)
		{
			//TODO Improve ControlPath formatting
			string newBinding = newControl.name;
			string inputDevice = newControl.device.name;
			int deviceIndex = DeviceIndexResolver.ResolveDeviceIndex(inputDevice);
			if (deviceIndex == 2)
				inputDevice = $"XInputController";
			else if (deviceIndex == 1)
				inputDevice = $"DualShockGamepad";
			else if (deviceIndex == 0 && inputDevice.Contains("keyboard"))
				inputDevice = $"Keyboard";
			else if (deviceIndex == 0 && inputDevice.Contains("mouse"))
				inputDevice = $"Mouse";
			return $"<{inputDevice}>/{newBinding}";
		}

		private void FindAndSwapDuplicates(string rebindedInput)
		{
			var allBindings = _targetAction.actionMap.bindings;
			ResolveActionAndBindingIndex(out _targetIndex);
			for (int i = 0; i < allBindings.Count; i++)
			{
				if (allBindings[i].effectivePath == rebindedInput)
				{
					InputAction duplicateAction = _targetAction.actionMap.FindAction(allBindings[i].action);
					if (!duplicateAction.name.Contains("0"))
					{
						DebugController.DebugColored(
							$"Action {duplicateAction.name} has duplicate binding {duplicateAction.bindings[_targetIndex].effectivePath}",
							$"#de8aff");

						duplicateAction.ApplyBindingOverride(_targetIndex, _existingBindingPath);

						DebugController.DebugColored(
							$"Swapped binding to {duplicateAction.bindings[_targetIndex].effectivePath} for action {duplicateAction}",
							$"#FF461E");
					}
				}
			}
		}

		public void UpdateBindingDisplay()
		{
			if (!IsDynamic)
				return;
			if (!ResolveActionAndBindingIndex(out int index) || BindingIndex < 0)
				return;

			string displayString = _targetAction.GetBindingDisplayString(index, out _, out string controlPath,
				InputBinding.DisplayStringOptions.DontIncludeInteractions);
			ActionBindingText.text = controlPath;
		}

		private bool ResolveActionAndBindingIndex(out int index)
		{
			index = -1;
			if (_targetActionReference == null)
			{
				DebugController.DebugColored($"Action reference is null! Please fix for {gameObject.name}", $"#D66666");
				return false;
			}

			_targetAction = _targetActionReference.action;

			if (IsFourPartComposite)
				index = ((4 * BindingIndex) + BindingIndex) + (int)CompositeIndex;
			else
				index = BindingIndex;
			if (_targetAction == null)
				return false;
			return true;
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			UpdateBindingDisplay();
		}
#endif
	}
}

namespace Studio23.SS2.BetterRebinder.Data
{
	public enum FourPartComposite
	{
		Up = 1,
		Down = 2,
		Left = 3,
		Right = 4
	}
}