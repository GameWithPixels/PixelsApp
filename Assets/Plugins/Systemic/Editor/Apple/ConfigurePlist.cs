using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace Systemic.Unity.BluetoothLE.Internal.Apple
{
    /// <summary>
    /// Post process build step that adds required NSBluetoothAlwaysUsageDescription entry to Info.plist
    /// for iOS builds.
    /// </summary>
    public class ConfigurePlist : MonoBehaviour
    {
        /// <summary>
        /// Adds NSBluetoothAlwaysUsageDescription entry to Info.plist for iOS builds.
        /// </summary>
        /// <param name="buildTarget">Build platform target.</param>
        /// <param name="pathToBuiltProject">Path to the build files.</param>
        [PostProcessBuild]
        public static void ChangeXcodePlist(BuildTarget buildTarget, string pathToBuiltProject)
        {
            if (buildTarget == BuildTarget.iOS)
            {
                string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
                var plist = new PlistDocument();
                plist.ReadFromFile(plistPath);
                plist.root.SetString(
                    "NSBluetoothAlwaysUsageDescription",
                    "Uses Bluetooth to communicate with Pixel dice.");
                plist.WriteToFile(plistPath);
            }
        }
    }
}
