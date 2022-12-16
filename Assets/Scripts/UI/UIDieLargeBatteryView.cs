using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIDieLargeBatteryView : MonoBehaviour
{
    [Header("Controls")]
    public Image batteryImage;
    public Text batteryLevelText;
    public Text batteryNiceText;
    public Image chargingImage;

    [Header("Properties")]
    public Sprite[] batteryLevelImages;
    public float[] batteryLevels;
    public Sprite notAvailableImage;

    public void SetLevel(int? levelPercent, bool? charging)
    {
        batteryNiceText.text = "Battery";

        if (levelPercent.HasValue)
        {
            // Find the first keyframe
            int index = 0;
            while (index < batteryLevels.Length && batteryLevels[index] > levelPercent.Value)
            {
                index++;
            }

            var sprite = batteryLevelImages[index];
            batteryImage.sprite = sprite;
            batteryLevelText.text = $"{levelPercent} %";
        }
        else
        {
            int index = batteryLevels.Length - 1;
            var sprite = batteryLevelImages[index];
            batteryImage.sprite = sprite;
            batteryLevelText.text = "Unknown";
        }

        if (charging.HasValue && charging.Value)
        {
            chargingImage.gameObject.SetActive(true);
            batteryNiceText.text = "Charging";
        }
        else
        {
            chargingImage.gameObject.SetActive(false);
        }
    }
}
