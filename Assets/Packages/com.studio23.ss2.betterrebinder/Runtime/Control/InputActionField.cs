using System;
using System.Collections.Generic;
using System.Linq;
using com.studio23.ss2.betterrebinder.Core;
using Studio23.SS2.ButtonIconResourceManager.core;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace com.studio23.ss2.betterrebinder.Control
{
    [UxmlElement]
    public partial class InputActionField: VisualElement
    {
        private const string UssClassName = "input-action-field";
        private const string LabelUssClassName = UssClassName + "__label";
        private const string BindingContainerUssClassName = UssClassName + "__bindings";
        private const string BindingGroupUssClassName = UssClassName + "__binding-group";
        private const string CompositeBindingGroupUssClassName = BindingGroupUssClassName + "--composite";
        private const string SingleBindingGroupUssClassName = BindingGroupUssClassName + "--single";
        private const string BindingButtonUssClassName = UssClassName + "__binding-button";
        private const string ResetButtonUssClassName = UssClassName + "__reset-button";
        public const string WaitingForRebindUssClassName = "waiting";

        private InputActionReference _inputAction;
        private string _controlSchemes;

        [UxmlAttribute]
        public InputActionReference inputAction
        {
            get => _inputAction;
            set
            {
                _inputAction = value;
                Refresh();
            }
        }

        [UxmlAttribute]
        public string controlSchemes
        {
            get => _controlSchemes;
            set
            {
                _controlSchemes = value;
                Refresh();
            }
        }

        [UxmlAttribute]
        private bool useSpriteAsset { get; set; } = true;
   
        
        private readonly Label _label;
        private readonly VisualElement _bindingContainer;
        private Button[] _buttons;

        public event Action<BindingClickedEvent> BindingClicked;
        public event Action<ResetClickedEvent> ResetClicked;

        public InputActionField()
        {
            AddToClassList(UssClassName);
            style.flexDirection = FlexDirection.Row;
            
            _label = new Label();
            _label.AddToClassList(LabelUssClassName);
            
            _bindingContainer = new VisualElement();
            _bindingContainer.AddToClassList(BindingContainerUssClassName);
            _bindingContainer.style.flexDirection = FlexDirection.Row;

            var resetButton = new Button { text = "Reset" };
            resetButton.AddToClassList(ResetButtonUssClassName);
            resetButton.clicked += () =>
            {
                var evt = new ResetClickedEvent().Init(_inputAction, resetButton);
                ResetClicked?.Invoke(evt);
            };
            
            Add(_label);
            Add(_bindingContainer);
            Add(resetButton);
        }

        public void Refresh()
        {
            _label.text = _inputAction?.action?.name ?? string.Empty;

            _bindingContainer.Clear();
            _buttons = Array.Empty<Button>();

            var action = _inputAction?.action;
            if (action == null)
                return;

            var selectedSchemes = ParseControlSchemes(_controlSchemes);
            var buttonList = new List<Button>();

            var displayOptions =
                InputBinding.DisplayStringOptions.DontIncludeInteractions;

            for (var i = 0; i < action.bindings.Count; ++i)
            {
                var binding = action.bindings[i];
                if (binding.isPartOfComposite)
                    continue;

                if (binding.isComposite)
                {
                    var bindingGroup = new VisualElement();
                    bindingGroup.AddToClassList(BindingGroupUssClassName);
                    bindingGroup.AddToClassList(CompositeBindingGroupUssClassName);
                    bindingGroup.style.flexDirection = FlexDirection.Row;
                    bindingGroup.style.flexWrap = Wrap.Wrap;

                    for (var j = i + 1; j < action.bindings.Count && action.bindings[j].isPartOfComposite; ++j)
                    {
                        var partBinding = action.bindings[j];
                        if (!ShouldIncludeBinding(partBinding, selectedSchemes))
                            continue;

                        var partDisplayString = action.GetBindingDisplayString(j, displayOptions);
                        if (string.IsNullOrWhiteSpace(partDisplayString))
                            continue;

                        var bindingIndex = j;
                        // var button = new Button { text = partDisplayString };
                        var spriteName = KeyIconManager.Instance.GetSpriteName(partDisplayString);

                        var button = new Button();
                        if (useSpriteAsset)
                        {
                            button.text = !string.IsNullOrEmpty(spriteName)
                                ? $"<sprite name=\"{spriteName}\">"
                                : partDisplayString; 
                        }
                        else
                        {
                            button.text = partDisplayString;
                        }
                        button.AddToClassList(BindingButtonUssClassName);
                        
                        button.clicked += () =>
                        {
                            var evt = new BindingClickedEvent().Init(_inputAction, bindingIndex, partBinding.effectivePath, button);
                            BindingClicked?.Invoke(evt);
                        };
                        button.tooltip = partBinding.effectivePath;
                        bindingGroup.Add(button);
                        buttonList.Add(button);
                    }

                    if (bindingGroup.childCount > 0)
                        _bindingContainer.Add(bindingGroup);

                    continue;
                }

                if (!ShouldIncludeBinding(binding, selectedSchemes))
                    continue;

                var displayString = action.GetBindingDisplayString(i, displayOptions);
                if (string.IsNullOrWhiteSpace(displayString))
                    continue;

                var singleBindingGroup = new VisualElement();
                singleBindingGroup.AddToClassList(BindingGroupUssClassName);
                singleBindingGroup.AddToClassList(SingleBindingGroupUssClassName);
                singleBindingGroup.style.flexDirection = FlexDirection.Row;
                singleBindingGroup.style.flexWrap = Wrap.Wrap;

                var _bindingIndex = i;
                var singleSpriteName = KeyIconManager.Instance.GetSpriteName(displayString);

                var singleButton = new Button();
                if(useSpriteAsset)
                {
                    singleButton.text = !string.IsNullOrEmpty(singleSpriteName)
                        ? $"<sprite name=\"{singleSpriteName}\">"
                        : displayString;
                }
                else
                {
                    singleButton.text = singleSpriteName;
                }
                singleButton.AddToClassList(BindingButtonUssClassName);
                singleButton.clicked += () =>
                {
                    var evt = new BindingClickedEvent().Init(_inputAction, _bindingIndex, binding.effectivePath, singleButton);
                    BindingClicked?.Invoke(evt);
                };
                singleButton.tooltip = binding.effectivePath;
                singleBindingGroup.Add(singleButton);
                _bindingContainer.Add(singleBindingGroup);
                buttonList.Add(singleButton);
            }

            _buttons = buttonList.ToArray();
        }

        private static HashSet<string> ParseControlSchemes(string controlSchemes)
        {
            var schemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(controlSchemes))
                return schemes;

            var tokens = controlSchemes.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                var scheme = token.Trim();
                if (!string.IsNullOrEmpty(scheme))
                    schemes.Add(scheme);
            }

            return schemes;
        }

        private static bool ShouldIncludeBinding(InputBinding binding, ISet<string> selectedSchemes)
        {
            if (selectedSchemes == null || selectedSchemes.Count == 0)
                return true;

            if (string.IsNullOrEmpty(binding.groups))
                return true;

            var bindingGroups = binding.groups.Split(InputBinding.Separator);
            return bindingGroups.Any(group => selectedSchemes.Contains(group.Trim()));
        }
    }
}

