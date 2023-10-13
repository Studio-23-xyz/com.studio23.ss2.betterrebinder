using Cysharp.Threading.Tasks;
using Studio23.SS2.BetterRebinder.Utility;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace Studio23.SS2.BetterRebinder
{
	[RequireComponent(typeof(ActionRebinderUiController))]
	public class ActionRebinder : MonoBehaviour
	{
		public InputActionReference TargetAction;
		public bool ExcludeMouse;
		public bool ControllerExpected;
		private ActionRebinderUiController _rebindUiController;

		private string _existingBindingPath;
		private InputAction _targetInputAction;
		private int _bindingIndex;

		private void Awake()
		{
			_rebindUiController = GetComponent<ActionRebinderUiController>();
			ResolveActionAndBinding(out _targetInputAction);
		}

		private void Start()
		{
			ResolveActionAndBinding(out _targetInputAction);
			_rebindUiController.SetupUi(_targetInputAction);
		}

		[ContextMenu("Test RebindButton")]
		public async void StartInteractiveRebind()
		{
			_bindingIndex = InputSystemUtility.Instance.GetBindingIndex();
			ResolveActionAndBinding(out _targetInputAction);
			_existingBindingPath = _targetInputAction.bindings[_bindingIndex].effectivePath;
			string rebindedInput = await CheckForInputs();
			FindAndSwapDuplicates(rebindedInput);
			_targetInputAction.ApplyBindingOverride(_bindingIndex, rebindedInput);
		}

		private void FindAndSwapDuplicates(string rebindedInput)
		{
			var allBindings = _targetInputAction.actionMap.bindings;
			_bindingIndex = InputSystemUtility.Instance.GetBindingIndex();
			_existingBindingPath = _targetInputAction.bindings[_bindingIndex].effectivePath;
			for (int i = 0; i < allBindings.Count; i++)
			{
				if (allBindings[i].effectivePath == rebindedInput)
				{
					var duplicateAction = _targetInputAction.actionMap.FindAction(allBindings[i].action);
					var duplicateBindings = duplicateAction.bindings;
					for (int j = 0; j < duplicateBindings.Count; j++)
					{
						if (duplicateAction.bindings[j].effectivePath == rebindedInput && !duplicateAction.name.Contains("0"))
						{
							duplicateAction.ApplyBindingOverride(j, _existingBindingPath);
							DebugController.DebugColored($"Swapped binding to {duplicateAction.bindings[j].effectivePath} for action {duplicateAction}", $"#FF461E");
						}
					}
				}
			}
		}

		private async UniTask<string> CheckForInputs()
		{
			string newBinding = string.Empty;
			var inputReader = InputSystem.onAnyButtonPress.Call(newControl =>
			{
				AuthenticateKeyPress(newControl);
				DebugController.DebugNormal($"Pressed button {newControl.name} from device {newControl.device.name} with shorter form {newControl.shortDisplayName}");
				newBinding = FormatInputToEffectivePath(newControl);
			});

			while (string.IsNullOrEmpty(newBinding))
			{
				await UniTask.Yield();
				await UniTask.NextFrame();
			}
			inputReader.Dispose();
			DebugController.DebugNormal($"Formatted binding: {newBinding}");
			return newBinding;
		}

		private bool AuthenticateKeyPress(InputControl newControl)
		{
			string deviceName = newControl.device.name;
			if (ExcludeMouse)
				return !deviceName.Contains("Mouse");
			if (ControllerExpected)
				if (deviceName.Contains("Keyboard") || deviceName.Contains("Mouse"))
					return false;
			return true;
		}

		private string FormatInputToEffectivePath(InputControl newControl)
		{
			string newBinding = newControl.name;
			string inputDevice = newControl.device.name.Replace("Windows", "");
			return $"<{inputDevice}>/{newBinding}";
		}

		public bool ResolveActionAndBinding(out InputAction action)
		{
			action = TargetAction?.action;
			if (action == null) 
				return false;
			return true;
		}
	}
}
