using Studio23.SS2.BetterRebinder.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Studio23.SS2.BetterRebinder.Utility
{
	public class InputDeviceMonitor : MonoBehaviour
	{
		private PlayerInput _inputComponent;

		public delegate void OnActionEvent();

		public OnActionEvent OnCrouchAction;
		public OnActionEvent OnInteractAction;

		public delegate void OnDeviceChange();

		public OnDeviceChange ActiveDeviceChanged;
		public InputDevice LastUsedDevice;
		public string LastUsedDeviceName;

		private void OnEnable()
		{
			_inputComponent = GetComponent<PlayerInput>();
			_inputComponent.controlsChangedEvent.AddListener(InputDeviceChangedEvent);
		}

		private void OnDisable()
		{
			_inputComponent.controlsChangedEvent.RemoveAllListeners();
		}

		private void Awake()
		{
			LastUsedDeviceName = "";
		}

		private void InputDeviceChangedEvent(PlayerInput inputComponent)
		{
			var inputDevice = _inputComponent.GetDevice<InputDevice>();
			if (inputDevice.name == LastUsedDeviceName)
				return;
			LastUsedDeviceName = inputDevice.name;
			if (string.IsNullOrEmpty(LastUsedDeviceName))
				LastUsedDeviceName = $"Keyboard";
			DeviceIndexResolver.ResolveDeviceIndex(LastUsedDeviceName);
			Debug.Log($"Device changed to {LastUsedDeviceName}");
			ActiveDeviceChanged?.Invoke();
		}

		public void InteractionPress(InputAction.CallbackContext context)
		{
			if (context.performed)
			{
				OnInteractAction?.Invoke();
				DebugController.DebugNormal($"Interact pressed");
			}
		}

		public void CrouchPress(InputAction.CallbackContext context)
		{
			if (context.performed)
			{
				OnCrouchAction?.Invoke();
				DebugController.DebugNormal($"Crouch pressed");
			}
		}
	}
}