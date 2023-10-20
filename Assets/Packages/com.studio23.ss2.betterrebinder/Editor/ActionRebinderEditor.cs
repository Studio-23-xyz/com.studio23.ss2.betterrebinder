using System;
using System.Linq;
using Studio23.SS2.BetterRebinder;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

[CustomEditor(typeof(ActionRebinder))]
public class ActionRebinderEditor : Editor
{
	private SerializedProperty m_BindingIdProperty;
	private SerializedProperty m_ActionProperty;

	private int m_SelectedBindingOption;
	private string[] m_BindingOptionValues;
	private GUIContent m_BindingLabel = new GUIContent("Binding");
	private GUIContent[] m_BindingOptions;

	protected void OnEnable()
	{
		m_ActionProperty = serializedObject.FindProperty("_targetActionReference");
		m_BindingIdProperty = serializedObject.FindProperty("_bindingId"); 
		RefreshBindingOptions();
	}

	public override void OnInspectorGUI()
	{
		EditorGUI.BeginChangeCheck();

		EditorGUILayout.IntField(m_SelectedBindingOption);
		EditorGUILayout.PropertyField(m_ActionProperty);
		var newSelectedBinding = EditorGUILayout.Popup(m_BindingLabel, m_SelectedBindingOption, m_BindingOptions);
		if (newSelectedBinding != m_SelectedBindingOption)
		{
			var bindingId = m_BindingOptionValues[newSelectedBinding];
			m_BindingIdProperty.stringValue = bindingId;
			m_SelectedBindingOption = newSelectedBinding;
		}

		if (EditorGUI.EndChangeCheck())
		{
			serializedObject.ApplyModifiedProperties();
			RefreshBindingOptions();
		}
	}

	private void RefreshBindingOptions()
	{
		var actionReference = (InputActionReference)m_ActionProperty.objectReferenceValue;
		var action = actionReference?.action;

		if (action == null)
		{
			m_BindingOptions = Array.Empty<GUIContent>();
			m_BindingOptionValues = Array.Empty<string>();
			m_SelectedBindingOption = -1;
			return;
		}

		var bindings = action.bindings;
		var bindingCount = bindings.Count;

		m_BindingOptions = new GUIContent[bindingCount];
		m_BindingOptionValues = new string[bindingCount];
		m_SelectedBindingOption = -1;

		var currentBindingId = m_BindingIdProperty.stringValue;
		for (int i = 0; i < bindingCount; i++)
		{
			var binding = bindings[i];
			var bindingId = binding.id.ToString();
			var haveBindingGroups = !string.IsNullOrEmpty(binding.groups);

			var displayOptions = InputBinding.DisplayStringOptions.DontUseShortDisplayNames |
			                     InputBinding.DisplayStringOptions.IgnoreBindingOverrides;
			if (!haveBindingGroups)
				displayOptions |= InputBinding.DisplayStringOptions.DontOmitDevice;

			var displayString = action.GetBindingDisplayString(i, displayOptions);

			if (binding.isPartOfComposite)
				displayString = $"{ObjectNames.NicifyVariableName(binding.name)}: {displayString}";

			displayString = displayString.Replace('/', '\\');

			// If the binding is part of control schemes, mention them.
			if (haveBindingGroups)
			{
				var asset = action.actionMap?.asset;
				if (asset != null)
				{
					var controlSchemes = string.Join(", ",
						binding.groups.Split(InputBinding.Separator)
							.Select(x => asset.controlSchemes.FirstOrDefault(c => c.bindingGroup == x).name));

					displayString = $"{displayString} ({controlSchemes})";
				}
			}

			m_BindingOptions[i] = new GUIContent(displayString);
			m_BindingOptionValues[i] = bindingId;

			if (currentBindingId == bindingId)
				m_SelectedBindingOption = i;
		}
	}
}
