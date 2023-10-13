using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
	public PlayerInput InputComponent;

	public delegate void OnActionEvent();

	public OnActionEvent OnCrouchAction;
	public OnActionEvent OnInteractAction;

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
