using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace Studio23.SS2.BetterRebinder.Utility
{
	public class InputSystemUtility : MonoBehaviour
	{
		private static InputSystemUtility _instance;

		public static InputSystemUtility Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = FindObjectOfType<InputSystemUtility>();
					if (_instance == null)
					{
						GameObject inputSystemUtility = new GameObject("InputSystemUtility");
						_instance = inputSystemUtility.AddComponent<InputSystemUtility>();
					}
				}
				return _instance;
			}
		}

		public PlayerInput PlayerInput;

		private string _inputDevice;

		public string InputDevice
		{
			get
			{
				if (PlayerInput == null)
					PlayerInput = FindObjectOfType<PlayerInput>();
				return _inputDevice = PlayerInput.currentControlScheme;
			}
		}

		public int GetBindingIndex(string inputDevice)
		{
			if (inputDevice == null)
			{
				Debug.Log($"_inputDevice string is null.");
				return -1;
			}

			inputDevice = inputDevice.ToLower();
			if (inputDevice.Contains("mouse") || inputDevice.Contains("keyboard"))
				return 0;
			else if (inputDevice.Contains("xinput") || inputDevice.Contains("xbox") || inputDevice.Contains("gamepad"))
				return 2;
			else
				return 1; //Anything other than xbox or generic controller.
		}

		public int GetBindingIndex()
		{
			if (InputDevice == null)
			{
				Debug.Log($"_inputDevice string is null.");
				return -1;
			}

			var deviceName = InputDevice.ToLower();
			if (deviceName.Contains("mouse") || deviceName.Contains("keyboard"))
				return 0;
			else if (deviceName.Contains("xinput") || deviceName.Contains("xbox") || deviceName.Contains("gamepad"))
				return 2;
			else
				return 1; //Anything other than xbox or generic controller.
		}

		public void GetPathAndLayout(InputAction targetAction, out string layout, out string bindingPath)
		{
			string displayString = string.Empty;
			int bindingIndex = GetBindingIndex(InputDevice);
			string controlPath = string.Empty;
			string deviceLayout = string.Empty;
			displayString = targetAction.GetBindingDisplayString(bindingIndex, out deviceLayout, out controlPath);
			layout = deviceLayout;
			bindingPath = controlPath;
		}


		public Sprite GetActionButtonSprite(string actionName)
		{
			PlayerInput = FindObjectOfType<PlayerInput>();
			var action = PlayerInput.actions.FindAction(actionName);
			var icon = default(Sprite);
			string activeScheme = PlayerInput.currentControlScheme;
			GetPathAndLayout(action, out var layout, out var bindingPath);

			return icon;
		}
	}
}
