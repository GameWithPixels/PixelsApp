﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class SkipEnumValueAttribute
    : System.Attribute
{
}

public class DropdowndAttribute
    : System.Attribute
{
}


public class UIParameterEnum : UIParameter
{
    [Header("Controls")]
    public Text nameText;
    public Text valueText;
    public Button valueButton;

    public static string GetNameAttribute(object enumVal, string fallback)
    {
        var nameAndOrder = EnumDisplayInfo.GetDisplayNameOf(enumVal);
        if (nameAndOrder.HasValue)
        {
            return nameAndOrder.Value.Name;
        }
        else
        {
            var type = enumVal.GetType();
            var memInfo = type.GetMember(enumVal.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(NameAttribute), false);
            return (attributes.Length > 0) ? ((NameAttribute)attributes[0]).name : fallback;
        }
    }

    public static bool ShouldSkipValue(object enumVal)
    {
        var nameAndOrder = EnumDisplayInfo.GetDisplayNameOf(enumVal);
        if (nameAndOrder.HasValue)
        {
            return nameAndOrder.Value.Name == null;
        }
        else
        {
            var type = enumVal.GetType();
            var memInfo = type.GetMember(enumVal.ToString());
            var skipAttribute = memInfo[0].GetCustomAttributes(typeof(SkipEnumValueAttribute), false);
            return skipAttribute.Length > 0;
        }
    }

    public static int? GetValueDisplayOrder(object enumVal)
    {
        var nameAndOrder = EnumDisplayInfo.GetDisplayNameOf(enumVal);
        if (nameAndOrder.HasValue)
        {
            return nameAndOrder.Value.Order;
        }
        else
        {
            var type = enumVal.GetType();
            var memInfo = type.GetMember(enumVal.ToString());
            var orderAttribute = memInfo[0].GetCustomAttributes(typeof(DisplayOrderAttribute), false);
            return (orderAttribute.FirstOrDefault() as DisplayOrderAttribute)?.Order;
        }
    }

    public override bool CanEdit(System.Type parameterType, IEnumerable<object> attributes = null)
    {
        return typeof(System.Enum).IsAssignableFrom(parameterType) && attributes.Any(a => a.GetType() == typeof(DropdowndAttribute));
    }

    protected override void SetupControls(string name, System.Func<object> getterFunc, System.Action<object> setterAction, IEnumerable<object> attributes = null)
    {
        // List all the enum values and populate the dropdown
        var initialValue = getterFunc();
        var enumType = initialValue.GetType();

        var vals = System.Enum.GetValues(enumType);
        var validValues = new List<System.Enum>();
        foreach (var val in vals)
        {
            if (!ShouldSkipValue(val))
            {
                validValues.Add(val as System.Enum);
            }
        }

        // Order values
        validValues = validValues.Select((v, i) => new { i, v })
            .OrderBy(kv => GetValueDisplayOrder(kv.v) ?? kv.i)
            .Select(kv => kv.v).ToList();

        // Set name
        nameText.text = name;
        valueText.text = GetNameAttribute(initialValue, initialValue.ToString());
        valueButton.onClick.AddListener(() => 
        {
            PixelsApp.Instance.ShowEnumPicker("Select " + name, (System.Enum)getterFunc(), (ret, newVal) =>
            {
                if (ret)
                {
                    valueText.text = GetNameAttribute(newVal, newVal.ToString());
                    setterAction(newVal);
                }
            },
            validValues);
        });
    }
}
