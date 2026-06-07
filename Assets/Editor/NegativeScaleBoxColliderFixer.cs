using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class NegativeScaleBoxColliderFixer
{
    private const string FixedChildName = "__PositiveScaleBoxCollider";

    static NegativeScaleBoxColliderFixer()
    {
        EditorApplication.delayCall += FixLoadedScenesOnce;
    }

    [MenuItem("Tools/Fix Negative Scale BoxColliders In Loaded Scenes")]
    public static void FixLoadedScenesOnce()
    {
        if (Application.isPlaying)
        {
            return;
        }

        int fixedCount = 0;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
            {
                continue;
            }

            int fixedInScene = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                fixedInScene += FixUnder(root);
            }

            if (fixedInScene > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                fixedCount += fixedInScene;
            }
        }

        if (fixedCount > 0)
        {
            Debug.Log($"Fixed {fixedCount} BoxCollider(s) under negative scale. Save the scene to keep the fix.");
        }
    }

    private static int FixUnder(GameObject root)
    {
        int fixedCount = 0;
        BoxCollider[] colliders = root.GetComponentsInChildren<BoxCollider>(true);

        foreach (BoxCollider source in colliders)
        {
            if (!source.enabled || !HasNegativeComponent(source.transform.lossyScale))
            {
                continue;
            }

            if (source.transform.Find(FixedChildName) != null)
            {
                source.enabled = false;
                continue;
            }

            GameObject child = new GameObject(FixedChildName);
            Undo.RegisterCreatedObjectUndo(child, "Fix Negative Scale BoxCollider");

            Transform childTransform = child.transform;
            childTransform.SetParent(source.transform, false);
            childTransform.localPosition = source.center;
            childTransform.localRotation = Quaternion.identity;
            childTransform.localScale = GetCompensatingScale(source.transform.lossyScale);
            child.layer = source.gameObject.layer;
            child.tag = source.gameObject.tag;
            GameObjectUtility.SetStaticEditorFlags(child, GameObjectUtility.GetStaticEditorFlags(source.gameObject));

            BoxCollider target = child.AddComponent<BoxCollider>();
            target.isTrigger = source.isTrigger;
            target.center = Vector3.zero;
            target.size = source.size;
            target.sharedMaterial = source.sharedMaterial;

            Undo.RecordObject(source, "Disable Negative Scale BoxCollider");
            source.enabled = false;
            EditorUtility.SetDirty(source);

            fixedCount++;
        }

        return fixedCount;
    }

    private static bool HasNegativeComponent(Vector3 value)
    {
        return value.x < 0f || value.y < 0f || value.z < 0f;
    }

    private static Vector3 GetCompensatingScale(Vector3 lossyScale)
    {
        return new Vector3(
            lossyScale.x < 0f ? -1f : 1f,
            lossyScale.y < 0f ? -1f : 1f,
            lossyScale.z < 0f ? -1f : 1f);
    }
}
