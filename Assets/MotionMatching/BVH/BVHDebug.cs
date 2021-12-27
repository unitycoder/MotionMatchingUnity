using System.Collections;
using System.Collections.Generic;
using MotionMatching;
using UnityEngine;

public class BVHDebug : MonoBehaviour
{
    public TextAsset BVH;
    public bool Play;
    public float UnitScale = 1;
    public bool LockFPS = true;

    private BVHAnimation Animation;
    private Transform[] Skeleton;
    private int CurrentFrame;

    private void Awake()
    {
        BVHImporter importer = new BVHImporter();
        Animation = importer.Import(BVH);

        Skeleton = new Transform[Animation.Skeleton.Count];
        foreach (BVHAnimation.Joint joint in Animation.Skeleton)
        {
            Transform t = (new GameObject()).transform;
            t.name = joint.Name;
            if (joint.Index == 0) t.SetParent(transform, false);
            else t.SetParent(Skeleton[joint.ParentIndex], false);
            t.localPosition = joint.Offset * UnitScale;
            Skeleton[joint.Index] = t;
        }

        if (LockFPS)
        {
            Application.targetFrameRate = 60;
            Debug.Log("[BVHDebug] Updated Target FPS: " + Application.targetFrameRate);
        }
        else
        {
            Application.targetFrameRate = -1;
        }
    }

    private void Update()
    {
        if (Play)
        {
            BVHAnimation.Frame frame = Animation.Frames[CurrentFrame];
            Skeleton[0].localPosition = frame.RootMotion * UnitScale;
            for (int i = 0; i < frame.LocalRotations.Length; i++)
            {
                Skeleton[i].localRotation = frame.LocalRotations[i];
            }
            CurrentFrame = (CurrentFrame + 1) % Animation.Frames.Length;
        }
        else
        {
            CurrentFrame = 0;
            Skeleton[0].localPosition = Vector3.zero;
            for (int i = 0; i < Skeleton.Length; i++)
            {
                Skeleton[i].localRotation = Quaternion.identity;
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (Skeleton == null || Animation == null || Animation.EndSites == null) return;

        Gizmos.color = Color.red;
        for (int i = 1; i < Skeleton.Length; i++)
        {
            Transform t = Skeleton[i];
            Gizmos.DrawLine(t.parent.position, t.position);
        }
        foreach (BVHAnimation.EndSite endSite in Animation.EndSites)
        {
            Transform t = Skeleton[endSite.ParentIndex];
            Gizmos.DrawLine(t.position, t.TransformPoint(endSite.Offset * UnitScale));
        }

        Gizmos.color = new Color(1.0f, 0.3f, 0.1f, 1.0f);
        foreach (Transform t in Skeleton)
        {
            if (t.name == "End Site") continue;
            Gizmos.DrawWireSphere(t.position, 0.1f);
        }
    }
#endif
}
