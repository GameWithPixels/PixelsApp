using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Systemic.Unity.Pixels;

public class TestDataSet : MonoBehaviour
{
    public void TestData()
    {
        var gradientkf = new List<EditRGBKeyframe>();
        gradientkf.Add(new EditRGBKeyframe() { time = 0.2f, color = Color.red});
        gradientkf.Add(new EditRGBKeyframe() { time = 0.4f, color = Color.blue});
        gradientkf.Add(new EditRGBKeyframe() { time = 0.7f, color = Color.yellow});
        PixelsApp.Instance.ShowGradientEditor("Edit Gradient", new EditRGBGradient(){keyframes = gradientkf}, null);


        // var appset = AppDataSet.CreateTestDataSet();
        // var jsonText = appset.ToJson();
        // var filePath = Path.Combine(Application.persistentDataPath, $"test_dataset.json");
        // File.WriteAllText(filePath, jsonText);
        // Debug.Log($"File written to {filePath}");
    }

    public void TestImportGradient()
    {
        var filePath = Path.Combine(Application.persistentDataPath, $"gradientLine.png");
        byte[] fileData = File.ReadAllBytes(filePath);
        var tex = new Texture2D(2, 2);
        tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        var keyframes = ColorUtils.extractKeyframes(tex.GetPixels());
        var gradient = new EditRGBGradient() { keyframes = keyframes };
        PixelsApp.Instance.ShowGradientEditor("test", gradient, null);
    }
}
