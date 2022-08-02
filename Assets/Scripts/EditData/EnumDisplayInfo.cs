using System;
using Systemic.Unity.Pixels.Animations;
using Systemic.Unity.Pixels.Profiles;
using UnityEngine;


public static class EnumDisplayInfo
{
    /// <summary>
    /// Display name and order of an enum value.
    /// The enum should not be displayed in the name is null.
    /// </summary>
    public struct NameAndOrder
    {
        public string Name;
        public int Order;
    }

    public static NameAndOrder? GetDisplayNameOf(object enumValue)
    {
        var type = enumValue.GetType();
        if (type == typeof(AnimationType))
        {
            return GetDisplayNameOf((AnimationType)enumValue);
        }
        else if (type == typeof(ActionType))
        {
            return GetDisplayNameOf((ActionType)enumValue);
        }
        else if (type == typeof(ConditionType))
        {
            return GetDisplayNameOf((ConditionType)enumValue);
        }
        
        return null;
    }


    public static NameAndOrder GetDisplayNameOf(AnimationType animType)
    {
        switch (animType)
        {
            case AnimationType.Unknown:
                return new NameAndOrder();
            case AnimationType.Simple:
                return new NameAndOrder { Name = "Simple Flashes", Order = 0 };
            case AnimationType.Rainbow:
                return new NameAndOrder { Name = "Colorful Rainbow", Order = 1 };
            case AnimationType.Keyframed:
                return new NameAndOrder { Name = "Color LED Pattern", Order = 3 };
            case AnimationType.GradientPattern:
                return new NameAndOrder { Name = "Gradient LED Pattern", Order = 4 };
            case AnimationType.Gradient:
                return new NameAndOrder { Name = "Simple Gradient", Order = 2 };
            default:
                Debug.LogError($"Unknown value for enum {nameof(AnimationType)}: {animType}");
                return new NameAndOrder();
        }
    }

    public static NameAndOrder GetDisplayNameOf(ActionType actionType)
    {
        switch (actionType)
        {
            case ActionType.Unknown:
                return new NameAndOrder();
            case ActionType.PlayAnimation:
                return new NameAndOrder { Name = "Trigger Pattern", Order = 0 };
            case ActionType.PlayAudioClip:
                return new NameAndOrder { Name = "Play Audio Clip", Order = 1 };
            default:
                Debug.LogError($"Unknown value for enum {nameof(ActionType)}: {actionType}");
                return new NameAndOrder();
        }
    }

    public static NameAndOrder GetDisplayNameOf(ConditionType conditionType)
    {
        switch (conditionType)
        {
            case ConditionType.Unknown:
                return new NameAndOrder();
            case ConditionType.HelloGoodbye:
                return new NameAndOrder { Name = "Pixel wakes up / sleeps", Order = 0 };
            case ConditionType.Handling:
                return new NameAndOrder { Name = "Pixel is picked up", Order = 1 };
            case ConditionType.Rolling:
                return new NameAndOrder { Name = "Pixel is rolling", Order = 2 };
            case ConditionType.FaceCompare:
                return new NameAndOrder { Name = "Pixel roll is...", Order = 3 };
            case ConditionType.Crooked:
                return new NameAndOrder { Name = "Pixel is crooked", Order = 4 };
            case ConditionType.ConnectionState:
                return new NameAndOrder { Name = "Bluetooth Event...", Order = 5 };
            case ConditionType.BatteryState:
                return new NameAndOrder { Name = "Battery Event...", Order = 6 };
            case ConditionType.Idle:
                return new NameAndOrder { Name = "Pixel is idle for...", Order = 7 };
            default:
                Debug.LogError($"Unknown value for enum {nameof(ConditionType)}: {conditionType}");
                return new NameAndOrder();
        }
    }
}
