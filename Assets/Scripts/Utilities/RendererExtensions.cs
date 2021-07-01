using System.Threading;
using UnityEngine;

// Taken from this thread: https://forum.unity.com/threads/test-if-ui-element-is-visible-on-screen.276549/

public static class RectTransformExtension
{
    const int numCorners = 4;
    static ThreadLocal<Vector3[]> perThreadObjectCorners = new ThreadLocal<Vector3[]>(() => new Vector3[numCorners]);

    /// <summary>
    /// Counts the bounding box corners of the given RectTransform that are visible in screen space.
    /// </summary>
    /// <returns>The amount of bounding box corners that are visible.</returns>
    /// <param name="rectTransform">Rect transform.</param>
    /// <param name="camera">Camera. Leave it null for Overlay Canvasses.</param>
    /// <param name="stopOnFirstVisibleCorner">Set to either return 0 or 1 to save CPU cycles.</param>
    private static int CountCornersVisibleFrom(this RectTransform rectTransform, Camera camera = null, bool stopOnFirstVisibleCorner = false)
    {
        var objectCorners = perThreadObjectCorners.Value;

        Rect screenBounds = new Rect(0f, 0f, Screen.width, Screen.height); // Screen space bounds (assumes camera renders across the entire screen)
        rectTransform.GetWorldCorners(objectCorners);

        int visibleCorners = 0;
        for (var i = 0; i < numCorners; i++) // For each corner in rectTransform
        {
            // If the corner is inside the screen
            if (screenBounds.Contains(
                camera?.WorldToScreenPoint(objectCorners[i]) // Transform world space position of corner to screen space
                ?? objectCorners[i])) // If no camera is provided we assume the canvas is Overlay and world space == screen space
            {
                visibleCorners++;
                if (stopOnFirstVisibleCorner)
                {
                    return 1;
                }
            }
        }
        return visibleCorners;
    }

    /// <summary>
    /// Determines if this RectTransform is fully visible.
    /// Works by checking if each bounding box corner of this RectTransform is inside the screen space view frustum.
    /// </summary>
    /// <returns><c>true</c> if is fully visible; otherwise, <c>false</c>.</returns>
    /// <param name="rectTransform">Rect transform.</param>
    /// <param name="camera">Camera. Leave it null for Overlay Canvasses.</param>
    public static bool IsFullyVisibleFrom(this RectTransform rectTransform, Camera camera = null)
    {
        if (!rectTransform.gameObject.activeInHierarchy)
            return false;

        return CountCornersVisibleFrom(rectTransform, camera) == 4; // True if all 4 corners are visible
    }

    /// <summary>
    /// Determines if this RectTransform is at least partially visible.
    /// Works by checking if any bounding box corner of this RectTransform is inside the screen space view frustum.
    /// </summary>
    /// <returns><c>true</c> if is at least partially visible; otherwise, <c>false</c>.</returns>
    /// <param name="rectTransform">Rect transform.</param>
    /// <param name="camera">Camera. Leave it null for Overlay Canvasses.</param>
    public static bool IsVisibleFrom(this RectTransform rectTransform, Camera camera = null)
    {
        if (!rectTransform.gameObject.activeInHierarchy)
            return false;

        return CountCornersVisibleFrom(rectTransform, camera, stopOnFirstVisibleCorner: true) > 0; // True if any corners are visible
    }
}