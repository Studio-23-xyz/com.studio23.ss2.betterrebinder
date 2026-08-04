using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Studio23.SS2.BetterRebinder.Editor
{
	public sealed class GenerateRebinderUI : EditorWindow
	{
		private InputActionAsset _inputActionAsset;
		private readonly List<InputActionMap> _actionMaps = new List<InputActionMap>();
		private readonly List<InputControlScheme> _controlSchemes = new List<InputControlScheme>();
		private int _selectedActionMapIndex;
		private int _controlSchemeMask = -1;
		private string _outputPath = "Assets/RebindMenu.generated.uxml";

		[MenuItem("Tools/Better Rebinder/Generate Rebind UI")]
		public static void Open()
		{
			var window = GetWindow<GenerateRebinderUI>();
			window.titleContent = new GUIContent("Generate Rebind UI");
			window.minSize = new Vector2(480f, 260f);
			window.Show();
		}

		private void OnGUI()
		{
			EditorGUI.BeginChangeCheck();
			_inputActionAsset = (InputActionAsset)EditorGUILayout.ObjectField("Input Action Asset", _inputActionAsset, typeof(InputActionAsset), false);
			if (EditorGUI.EndChangeCheck())
				RefreshAssetData();

			using (new EditorGUI.DisabledScope(_inputActionAsset == null || _actionMaps.Count == 0))
			{
				if (_actionMaps.Count > 0)
				{
					var mapNames = _actionMaps.Select(map => map.name).ToArray();
					_selectedActionMapIndex = Mathf.Clamp(_selectedActionMapIndex, 0, mapNames.Length - 1);
					_selectedActionMapIndex = EditorGUILayout.Popup("Action Map", _selectedActionMapIndex, mapNames);
				}
			}

			using (new EditorGUI.DisabledScope(_inputActionAsset == null || _controlSchemes.Count == 0))
			{
				if (_controlSchemes.Count > 0)
				{
					var schemeLabels = _controlSchemes.Select(scheme => scheme.name).ToArray();
					_controlSchemeMask = EditorGUILayout.MaskField(new GUIContent("Control Schemes"), _controlSchemeMask, schemeLabels);
					EditorGUILayout.HelpBox("Leave all schemes selected to include every binding, or narrow the list to the schemes you want rendered.", MessageType.Info);
				}
			}

			EditorGUILayout.Space();
			_outputPath = EditorGUILayout.TextField("Output UXML Path", _outputPath);

			using (new EditorGUI.DisabledScope(!CanGenerate()))
			{
				if (GUILayout.Button("Generate UXML"))
					Generate();
			}
		}

		private void RefreshAssetData()
		{
			_actionMaps.Clear();
			_controlSchemes.Clear();
			_selectedActionMapIndex = 0;
			_controlSchemeMask = -1;

			if (_inputActionAsset == null)
				return;

			_actionMaps.AddRange(_inputActionAsset.actionMaps);
			_controlSchemes.AddRange(_inputActionAsset.controlSchemes);

			if (_controlSchemes.Count > 0)
				_controlSchemeMask = _controlSchemes.Count >= 31 ? int.MaxValue : (1 << _controlSchemes.Count) - 1;
		}

		private bool CanGenerate()
		{
			return _inputActionAsset != null
				&& _actionMaps.Count > 0
				&& _selectedActionMapIndex >= 0
				&& _selectedActionMapIndex < _actionMaps.Count
				&& !string.IsNullOrWhiteSpace(_outputPath);
		}

		private void Generate()
		{
			if (!CanGenerate())
				return;

			var assetPath = AssetDatabase.GetAssetPath(_inputActionAsset);
			var actionMap = _actionMaps[_selectedActionMapIndex];
			var selectedSchemes = GetSelectedControlSchemes();
			var referencesByAction = LoadActionReferences(assetPath);

			var uxml = BuildUxml(assetPath, actionMap, selectedSchemes, referencesByAction);

			var fullPath = Path.GetFullPath(_outputPath);
			var directory = Path.GetDirectoryName(fullPath);
			if (!string.IsNullOrEmpty(directory))
				Directory.CreateDirectory(directory);

			File.WriteAllText(fullPath, uxml, Encoding.UTF8);
			AssetDatabase.Refresh();
			Debug.Log($"Generated rebinder UI to '{_outputPath}'.", this);
		}

		private IReadOnlyCollection<string> GetSelectedControlSchemes()
		{
			if (_controlSchemes.Count == 0)
				return Array.Empty<string>();

			var selectedSchemes = new List<string>();
			for (var i = 0; i < _controlSchemes.Count; ++i)
			{
				if ((_controlSchemeMask & (1 << i)) != 0)
					selectedSchemes.Add(_controlSchemes[i].name);
			}

			return selectedSchemes.Count == 0 ? _controlSchemes.Select(scheme => scheme.name).ToArray() : selectedSchemes;
		}

		private static Dictionary<InputAction, InputActionReference> LoadActionReferences(string assetPath)
		{
			return AssetDatabase.LoadAllAssetsAtPath(assetPath)
				.OfType<InputActionReference>()
				.Where(reference => reference != null && !reference.hideFlags.HasFlag(HideFlags.HideInHierarchy) && reference.action != null)
				.GroupBy(reference => reference.action)
				.ToDictionary(group => group.Key, group => group.First());
		}

		private static string BuildUxml(
			string assetPath,
			InputActionMap actionMap,
			IReadOnlyCollection<string> selectedSchemes,
			IReadOnlyDictionary<InputAction, InputActionReference> referencesByAction)
		{
			var builder = new StringBuilder();
			builder.AppendLine("<ui:UXML xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:ui=\"UnityEngine.UIElements\" xmlns:uie=\"UnityEditor.UIElements\" noNamespaceSchemaLocation=\"../../../../UIElementsSchema/UIElements.xsd\" editor-extension-mode=\"False\">");
			builder.AppendLine("    <ui:ScrollView>");
			builder.AppendLine("        <ui:VisualElement>");

			foreach (var action in actionMap.actions)
			{
				if (!referencesByAction.TryGetValue(action, out var reference))
				{
					Debug.LogWarning($"Skipping action '{action.name}' because no InputActionReference sub-asset could be found in '{assetPath}'.");
					continue;
				}

				if (!TryGetReferenceString(reference, out var referenceString))
				{
					Debug.LogWarning($"Skipping action '{action.name}' because its reference could not be serialized.");
					continue;
				}

				var controlSchemes = BuildControlSchemeString(action, selectedSchemes);
				var controlSchemesAttribute = string.IsNullOrWhiteSpace(controlSchemes)
					? string.Empty
					: $" control-schemes=\"{EscapeXml(controlSchemes)}\"";

				builder.AppendLine($"        <com.studio23.ss2.betterrebinder.Control.InputActionField input-action=\"{EscapeXml(referenceString)}\"{controlSchemesAttribute} />");
			}

			builder.AppendLine("        </ui:VisualElement>");
			builder.AppendLine("    </ui:ScrollView>");
			builder.AppendLine("</ui:UXML>");
			return builder.ToString();
		}

		private static string BuildControlSchemeString(InputAction action, IReadOnlyCollection<string> selectedSchemes)
		{
			if (selectedSchemes == null || selectedSchemes.Count == 0)
				return string.Empty;

			var selectedSet = new HashSet<string>(selectedSchemes, StringComparer.OrdinalIgnoreCase);
			var bindingSchemeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (var binding in action.bindings)
			{
				if (string.IsNullOrEmpty(binding.groups))
					continue;

				foreach (var group in binding.groups.Split(InputBinding.Separator))
				{
					var schemeName = group.Trim();
					if (!string.IsNullOrEmpty(schemeName) && selectedSet.Contains(schemeName))
						bindingSchemeNames.Add(schemeName);
				}
			}

			if (bindingSchemeNames.Count == 0)
				bindingSchemeNames.UnionWith(selectedSet);

			return string.Join(";", bindingSchemeNames);
		}

		private static bool TryGetReferenceString(InputActionReference reference, out string referenceString)
		{
			referenceString = null;

			if (reference == null)
				return false;

			if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(reference, out var guid, out long localId))
				return false;

			referenceString = $"project://database/{EscapeUrlPath(AssetDatabase.GetAssetPath(reference))}?fileID={localId}&guid={guid}&type=3#{reference.name}";
			return true;
		}

		private static string EscapeUrlPath(string path)
		{
			return string.IsNullOrEmpty(path) ? string.Empty : path.Replace(" ", "%20");
		}

		private static string EscapeXml(string value)
		{
			if (string.IsNullOrEmpty(value))
				return string.Empty;

			return value.Replace("&", "&amp;")
				.Replace("\"", "&quot;")
				.Replace("<", "&lt;")
				.Replace(">", "&gt;");
		}
	}
}
