
using System.Collections.Generic;
using System.Linq;
using Studio23.SS2.BetterRebinder.Core;
using UnityEngine;
using UnityEngine.UI;

public class RebindIconUiBase : MonoBehaviour
{
    private List<Rebinder> _rebindUIComponents;
    [SerializeField] private bool _getChildRebinders = false;

    void Awake()
    {
        if (_getChildRebinders)
            _rebindUIComponents = new List<Rebinder>(transform.GetComponentsInChildren<Rebinder>().ToList());
    }

    void Start()
    {
        foreach (var component in _rebindUIComponents)
        {
            component.updateBindingUIEvent.AddListener(OnUpdateBindingDisplay);
            component.UpdateBindingDisplay();
        }
    }

    protected void OnUpdateBindingDisplay(Rebinder component, string bindingDisplayString, string deviceLayoutName, string controlPath)
    {
        if (string.IsNullOrEmpty(deviceLayoutName) || string.IsNullOrEmpty(controlPath))
            return;

        var icon = GetBindingIcon(deviceLayoutName, controlPath);

        var textComponent = component.bindingText;

        // Grab Image component.
        var imageGO = textComponent.transform.parent.Find("ActionBindingIcon");
        var imageComponent = imageGO.GetComponent<Image>();

        if (icon != null)
        {
            textComponent.gameObject.SetActive(false);
            imageComponent.sprite = icon;
            imageComponent.gameObject.SetActive(true);
        }
        else
        {
            textComponent.gameObject.SetActive(true);
            imageComponent.gameObject.SetActive(false);
        }
    }

    public virtual Sprite GetBindingIcon(string deviceLayoutName, string controlPath)
    {
        Sprite icon = null;
        return icon;
    }

    public void PopulateRebindes(List<Rebinder> _selectedRebinders)
    {
        _rebindUIComponents = new List<Rebinder>();
        _rebindUIComponents.AddRange(_selectedRebinders);
    }
}
