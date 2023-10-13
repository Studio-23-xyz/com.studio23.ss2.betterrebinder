using System.Threading.Tasks;
using Studio23.SS2.BetterRebinder.Utility;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace Studio23.SS2.BetterRebinder
{
	[RequireComponent(typeof(ActionRebinderUiController))]
	public class ActionRebinder : MonoBehaviour
	{
		public InputAction TargetAction;
		public bool ExcludeMouse;
		private ActionRebinderUiController _rebindUiController;

		private string _existingBindingPath;

		private void Start()
		{
			_rebindUiController.SetupUi();
		}

		public async void StartInteractiveRebind()
		{
			int bindingIndex = InputSystemUtility.Instance.GetBIndingIndex();
			_existingBindingPath = TargetAction.bindings[bindingIndex].effectivePath;
			string rebindedInput = await CheckForInputs();
		}

		private async Task<string> CheckForInputs()
		{
			string newBinding = string.Empty;
			var inputReader = InputSystem.onAnyButtonPress.Call(newControl =>
			{
				DebugController.DebugNormal($"Pressed button {newControl.name}");
				newBinding = newControl.name;
			});

			await Task.Delay(5000);
			inputReader.Dispose();
			return newBinding;
		}
	}
}
