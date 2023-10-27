using Studio23.SS2.BetterRebinder.Core;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace Studio23.SS2.BetterRebinder.Utility
{
	public class RebindMenu : MonoBehaviour
	{
		private static RebindMenu _instance;

		public RebindMenu Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = FindObjectOfType<RebindMenu>();
					if (_instance == null)
					{
						GameObject rebindMenu = new GameObject("Rebind Menu");
						_instance = rebindMenu.AddComponent<RebindMenu>();
						return _instance;
					}
				}

				return _instance;
			}
		}

		public Transform UiElementParent;
		public Rebinder RebindActionPrefab;
		public InputActionAsset InputAsset;
		public InputDeviceMonitor InputDeviceObserver;

		public GameObject RebindOverlay;
		public TextMeshProUGUI RebindOverlayText;

		private List<string> _currentBindingText;
		private List<Rebinder> _rebindingAssets;

		[ContextMenu("Generate Rebinding UI")]
		public void GenerateRebindingElements()
		{
#if UNITY_EDITOR
			_rebindingAssets = new List<Rebinder>();
			var actionRef = new List<InputActionReference>();

			Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(InputAsset));
			List<InputActionReference> inputActionReferences = new List<InputActionReference>();
			foreach (Object obj in subAssets)
			{
				// there are 2 InputActionReference returned for each InputAction in the asset, need to filter to not had the hidden one generated for backward compatibility
				if (obj is InputActionReference inputActionReference &&
					(inputActionReference.hideFlags & HideFlags.HideInHierarchy) == 0)
				{
					inputActionReferences.Add(inputActionReference);
				}
			}

			foreach (var inputActionReference in inputActionReferences)
			{
				if (inputActionReference.name.Contains("0"))
					continue;
				var rebindAction = Instantiate(RebindActionPrefab, UiElementParent);
				rebindAction.Initialize(inputActionReference);
				_rebindingAssets.Add(rebindAction);
			}
#endif
		}

		private void RefreshBindingText()
		{
			if (_rebindingAssets == null)
				_rebindingAssets = FindObjectsOfType<Rebinder>().ToList();
			foreach (var rebindingAsset in _rebindingAssets)
			{
				rebindingAsset.UpdateBindingDisplay();
			}
		}


		public void Initialize()
		{
			InputDeviceObserver.ActiveDeviceChanged += RefreshBindingText;

			if (_rebindingAssets == null)
			{
				_rebindingAssets = FindObjectsOfType<Rebinder>().ToList();
				Debug.Log($"Rebinding assets found {_rebindingAssets.Count}");
			}
			foreach (var rebindingAsset in _rebindingAssets)
			{
				rebindingAsset.OnRebindActionComplete.AddListener(RefreshBindingText);
			}
		}

		private void OnDisable()
		{
			InputDeviceObserver.ActiveDeviceChanged -= RefreshBindingText;
		}

		public bool IsValueChanged()
		{
			var rebindMenu = GetComponentsInChildren<Rebinder>(true);
			for (var index = 0; index < rebindMenu.Length; index++)
			{
				if (!_currentBindingText[index].Equals(rebindMenu[index].ActionBindingText.text))
				{
					return true;
				}
			}

			return false;
		}

		private void Start()
		{
			Initialize();
			//LoadKeybinds();
		}

		public void UpdateRebindElements()
		{
			foreach (Rebinder rebindElement in UiElementParent.GetComponentsInChildren<Rebinder>(true))
			{
				rebindElement.UpdateBindingDisplay();
			}
		}

		[ContextMenu("Clear Rebind Elements")]
		public void DebugClearRebindElements()
		{
			foreach (Transform child in UiElementParent)
			{
				DestroyImmediate(child.gameObject);
			}
		}

		/// <summary>
		/// Used to store changed keybinds in PlayerPrefs under the key "rebinds" 
		/// </summary>
		public void SaveKeybinds()
		{
			var rebinds = InputAsset.SaveBindingOverridesAsJson();
			PlayerPrefs.SetString("rebinds", rebinds);
		}

		/// <summary>
		/// Reset all keybind overrides
		/// </summary>
		/// 
		[ContextMenu("Restore Defaults")]
		public void RestoreDefaultKeybinds()
		{
			foreach (InputAction inputAction in InputAsset)
			{
				inputAction.RemoveAllBindingOverrides();
			}

			UpdateRebindElements();
		}

		/// <summary>
		/// Responsible for loading keybinds from PlayerPrefs saved under the key "rebinds"
		/// </summary>
		public void LoadKeybinds()
		{
			var rebinds = PlayerPrefs.GetString("rebinds");
			if (!string.IsNullOrEmpty(rebinds))
				InputAsset.LoadBindingOverridesFromJson(rebinds);
		}
	}
}
