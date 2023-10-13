using System;
using Studio23.SS2.BetterRebinder.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ActionRebinderUiController : MonoBehaviour
{
	public TextMeshProUGUI ActionName;
	public TextMeshProUGUI ActionControlText;
	public Image ActionBindingIcon;

	private void Awake()
	{
		Setup();
	}

	private void Setup()
	{
		ActionName = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
		ActionControlText = transform.GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>();
	}

	public bool SetupUi(InputAction action)
	{
		ActionName.text = action.name;
		int index = InputSystemUtility.Instance.GetBindingIndex();
		ActionControlText.text = action.GetBindingDisplayString(index, InputBinding.DisplayStringOptions.DontIncludeInteractions);
		return true;
	}
}
