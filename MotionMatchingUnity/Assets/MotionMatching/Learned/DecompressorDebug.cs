using System.Collections;
using System.Collections.Generic;
using Unity.Barracuda;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MotionMatching
{
    public class DecompressorDebug : MonoBehaviour
    {
        public MotionMatchingData MMData;
        public NNModel ModelSource;
        public int StartFrame;
        public bool Reset;
        public bool GroundTruth;
        public bool RootMotion;

        private Decompressor Decompressor;
        private PoseSet PoseSet;
        private FeatureSet FeatureSet;
        private Transform[] DecompressorTransforms;
        private Transform[] SkeletonTransforms;
        private float3 InitPos;
        private quaternion InitRot;
        private PoseVector DecompressorPose;
        private NativeArray<float> CurrentFeatureVector;

        private int CurrentFrame;

        private void Awake()
        {
            // PoseSet
            PoseSet = MMData.GetOrImportPoseSet();

            // FeatureSet
            FeatureSet = MMData.GetOrImportFeatureSet();

            InitPos = transform.position;
            InitRot = transform.rotation;
            CurrentFrame = StartFrame;
            CurrentFeatureVector = new NativeArray<float>(FeatureSet.FeatureSize, Allocator.Persistent);
            DecompressorPose = new PoseVector();
            DecompressorPose.JointLocalPositions = new float3[24];
            DecompressorPose.JointLocalRotations = new quaternion[24];
            DecompressorPose.JointVelocities = new float3[24];
            DecompressorPose.JointAngularVelocities = new float3[24];

            // Skeleton
            SkeletonTransforms = new Transform[PoseSet.Skeleton.Joints.Count];
            foreach (Skeleton.Joint joint in PoseSet.Skeleton.Joints)
            {
                Transform t = (new GameObject()).transform;
                t.name = joint.Name;
                if (joint.Index == 0) t.SetParent(transform, false);
                else t.SetParent(SkeletonTransforms[joint.ParentIndex], false);
                t.localPosition = joint.LocalOffset;
                SkeletonTransforms[joint.Index] = t;
            }
            DecompressorTransforms = new Transform[PoseSet.Skeleton.Joints.Count];
            foreach (Skeleton.Joint joint in PoseSet.Skeleton.Joints)
            {
                Transform t = (new GameObject()).transform;
                t.name = joint.Name;
                if (joint.Index == 0) t.SetParent(transform, false);
                else t.SetParent(DecompressorTransforms[joint.ParentIndex], false);
                t.localPosition = joint.LocalOffset;
                DecompressorTransforms[joint.Index] = t;
            }

            // Decompressor
            Decompressor = new Decompressor(ModelSource, 27);

            // Lock FPS
            Application.targetFrameRate = (int)(1.0f / PoseSet.FrameTime);
            Debug.Log("[Motion Matching] Updated Target FPS: " + Application.targetFrameRate);
        }

        private void Update()
        {
            if (GroundTruth)
            {
                // Ground Truth
                PoseSet.GetPose(CurrentFrame, out PoseVector pose);
                UpdateTransforms(SkeletonTransforms, pose);
            }
            else
            {
                // Decompressor
                FeatureSet.GetFeature(CurrentFeatureVector, CurrentFrame);
                Decompressor.Decompress(CurrentFeatureVector, ref DecompressorPose);
                UpdateTransforms(DecompressorTransforms, DecompressorPose);
            }
        }

        private void UpdateTransforms(Transform[] skeleton, PoseVector pose)
        {
            if (!Reset)
            {
                // Simulation Bone
                // transform.position += transform.TransformDirection(pose.RootDisplacement);
                // transform.rotation = transform.rotation * pose.RootRotDisplacement;
                if (RootMotion)
                {
                    transform.position = pose.JointLocalPositions[0];
                    transform.rotation = pose.JointLocalRotations[0];
                }
                // Joints
                for (int i = 1; i < pose.JointLocalRotations.Length; i++)
                {
                    skeleton[i].localRotation = pose.JointLocalRotations[i];
                }
                // Root Y Position
                // TODO: revisar... después del refactor ya no creo que funcione
                // skeleton[0].localPosition = new float3(0, pose.RootWorld.y, 0);
                CurrentFrame = (CurrentFrame + 1) % PoseSet.NumberPoses;
            }
            else
            {
                CurrentFrame = StartFrame;

                transform.position = InitPos;
                transform.rotation = InitRot;

                for (int i = 0; i < pose.JointLocalRotations.Length; i++)
                {
                    skeleton[i].localRotation = pose.JointLocalRotations[i];
                }
            }
        }

        private void OnDestroy()
        {
            if (FeatureSet != null) FeatureSet.Dispose();
            if (CurrentFeatureVector != null && CurrentFeatureVector.IsCreated) CurrentFeatureVector.Dispose();
        }

        private void OnApplicationQuit()
        {
            OnDestroy();
        }

        private void OnDrawGizmos()
        {
            if (SkeletonTransforms == null || DecompressorTransforms == null) return;

            if (GroundTruth)
            {
                Gizmos.color = Color.red;
                for (int i = 1; i < SkeletonTransforms.Length; i++)
                {
                    Transform t = SkeletonTransforms[i];
                    GizmosExtensions.DrawLine(t.parent.position, t.position, 3);
                }
            }
            else
            {
                Gizmos.color = Color.green;
                for (int i = 1; i < DecompressorTransforms.Length; i++)
                {
                    Transform t = DecompressorTransforms[i];
                    GizmosExtensions.DrawLine(t.parent.position, t.position, 3);
                }
            }
        }
    }
}