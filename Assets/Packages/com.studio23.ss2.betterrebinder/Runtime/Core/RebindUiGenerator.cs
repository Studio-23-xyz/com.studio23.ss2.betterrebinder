using Studio23.SS2.BetterRebinder;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class RebindUiGenerator : MonoBehaviour
{
	public InputActionAsset InputAsset;
	public ActionRebinder RebindAssetPrefab;
	public Transform ContentHolder;
	private List<string> _currentBindingText;

	[ContextMenu("Generate Rebind Assets")]
	private void GenerateRebindAssets()
	{
		var actionRef = new List<InputActionReference>();

		Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(InputAsset));
		List<InputActionReference> inputActionReferences = new List<InputActionReference>();
		foreach (Object obj in subAssets)
		{
			// there are 2 InputActionReference returned for each InputAction in the asset, need to filter to not had the hidden one generated for backward compatibility
			if (obj is InputActionReference inputActionReference && (inputActionReference.hideFlags & HideFlags.HideInHierarchy) == 0)
			{
				inputActionReferences.Add(inputActionReference);
			}
		}

		foreach (var inputActionReference in inputActionReferences)
		{
			if (inputActionReference.action.name.Contains("0"))
				continue;
			var rebindAction = Instantiate(RebindAssetPrefab, ContentHolder);
			rebindAction.InitializeRebindAction(inputActionReference);
		}
	}

	public void Initialize()
	{
		_currentBindingText = new List<string>();
		var rebindMenu = GetComponentsInChildren<ActionRebinder>(true);
		foreach (var menu in rebindMenu)
		{
			_currentBindingText.Add(menu.RebindUiController.ActionName.text);
		}
	}
}
