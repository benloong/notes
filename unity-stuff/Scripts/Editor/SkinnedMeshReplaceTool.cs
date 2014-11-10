using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

public class SkinnedMeshReplaceTool: ScriptableWizard {
    public SkinnedMeshRenderer[] prefabs;
    public SkinnedMeshRenderer[] targets;

    [MenuItem("Tools/ReplaceSkinnedMesh")]
    public static void SwapSkinnedMesh()
    {
        ScriptableWizard.DisplayWizard<SkinnedMeshReplaceTool>("ReplaceSkinnedMesh","Apply");

    }
    
    void OnWizardCreate()
    {
        for (int i = 0; i < prefabs.Length; i++)
        {
            var target = targets[i];
            var prefab = prefabs[i];

            Debug.Log("Replace skinned mesh " + target.name + " to " + prefab);
            //skeleton
            Dictionary<string, Transform> skeleton = new Dictionary<string, Transform>();
            Transform[] allTrans = target.transform.parent ? target.transform.parent.GetComponentsInChildren<Transform>(true) : target.transform.GetComponentsInChildren<Transform>(true);
            foreach (var item in allTrans)
            {
                skeleton[item.name] = item;
            }

            target.sharedMaterials = prefab.sharedMaterials;
            target.sharedMesh = prefab.sharedMesh;
            Transform[] bones = new Transform[prefab.bones.Length];
            for (int ii = 0; ii < bones.Length; ii++)
            {
                bones[ii] = skeleton[prefab.bones[ii].name];
            }

            target.bones = bones;
            target.rootBone = skeleton[prefab.rootBone.name];
            target.localBounds = prefab.localBounds;
        }
    }

    void OnWizardUpdate()
    {
        helpString = "Please set the prefab and target.";
        isValid = prefabs!= null && targets != null && prefabs.Length > 0 && prefabs.Length == targets.Length;
    }
    // When the user pressed the "Apply" button OnWizardOtherButton is called.
    void OnWizardOtherButton()
    {
        Debug.Log("Cancle Replace.");
    }
}
