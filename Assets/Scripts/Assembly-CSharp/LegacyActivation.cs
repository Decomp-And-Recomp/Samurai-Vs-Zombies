using UnityEngine;

public static class LegacyActivation
{
    // The original API forced every descendant to the same state, including inactive children.
    public static void SetRecursively(GameObject root, bool active)
    {
        foreach (Transform child in root.transform)
            SetRecursively(child.gameObject, active);
        root.SetActive(active);
    }
}
