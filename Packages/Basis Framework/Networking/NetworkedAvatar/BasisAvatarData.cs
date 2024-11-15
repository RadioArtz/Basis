﻿using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics; // Using Unity.Mathematics for math operations
using static Unity.Mathematics.math;

namespace Basis.Scripts.Networking.NetworkedAvatar
{
    [System.Serializable]
    public struct BasisAvatarData
    {
        public NativeArray<float3> Vectors; // hips position, player's position, scale (3 length)
        public quaternion Rotation; // hip rotation
        public NativeArray<float> Muscles; // 95 floats for each muscle (95 length)
        public float[] floatArray;
    }

    [BurstCompile]
    public struct UpdateAvatarPositionJob : IJob
    {
        public NativeArray<float3> positions;
        public NativeArray<float3> targetPositions;
        public float deltaTime;
        public float teleportThreshold;
        public float smoothingSpeed;

        public void Execute()
        {
            // Cache frequently used values
            float3 pos0 = positions[0];
            float3 pos1 = positions[1];
            float3 targetPos0 = targetPositions[0];
            float3 targetPos1 = targetPositions[1];

            // Calculate squared distance to avoid expensive sqrt operation
            float distanceSq = math.lengthsq(pos0 - targetPos0);

            // Check if we should teleport
            if (distanceSq > teleportThreshold)
            {
                // Teleport directly to the target position
                positions[0] = targetPos0;
                positions[1] = targetPos1;
                return; // Early exit to avoid unnecessary calculations
            }

            // Smoothly move towards target positions based on deltaTime and smoothing speed
            positions[0] = math.lerp(pos0, targetPos0, 1f - math.exp(-smoothingSpeed * deltaTime));
            positions[1] = math.lerp(pos1, targetPos1, 1f - math.exp(-smoothingSpeed * deltaTime));
        }
    }

    [BurstCompile]
    public struct UpdateAvatarMusclesJob : IJobParallelFor
    {
        public NativeArray<float> muscles;
        public NativeArray<float> targetMuscles;
        public float lerpTime;

        public void Execute(int index)
        {
            // Use math.lerp instead of Mathf.Lerp for Burst optimization
            muscles[index] = lerp(muscles[index], targetMuscles[index], lerpTime);
        }
    }
}