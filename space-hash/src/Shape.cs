using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor.VersionControl;
using UnityEngine.EventSystems;
using static Unity.Mathematics.math;

public static class Shape
{
    public delegate JobHandle ScheduleDelegate(NativeArray<float3x4> positions, NativeArray<float3x4> normals, int resolution, float4x4 trs, JobHandle handle);

    public interface IShape
    {
        public Point4 GetPoint4(int i, float resolution, float invResolution);
    }

    public struct Point4
    {
        public float4x3 positions, normals;
    }

    public static float4x2 IndexToUV4(int i, float resolution, float invResolution)
    {
        float4x2 uv;
        float4 i4 = 4f * i + float4(0f, 1f, 2f, 3f);
        uv.c1 = floor (invResolution * i4 + .00001f);
        uv.c0 = invResolution * (i4 - resolution * uv.c1 + 0.5f);
        uv.c1 = invResolution * (uv.c1 + 0.5f);
        return uv;
    }

    public struct Plane : IShape
    { 
        public Point4 GetPoint4(int i, float resolution, float invResolution)
        {
            float4x2 uv = IndexToUV4(i, resolution, invResolution);
            return new Point4
            {
                positions = float4x3(uv.c0 - 0.5f, 0f, uv.c1 - 0.5f),
                normals = float4x3(0f, 1f, 0f)
            };
        }
    }

    public struct Sphere : IShape
    {
        public Point4 GetPoint4(int i, float resolution, float invResolution)
        {
            float4x2 uv = IndexToUV4(i, resolution, invResolution);
            
            // Uniform radius
            float r = 0.5f;
            float4 s = r * sin(PI * uv.c1);

            Point4 p;
            p.positions.c0 = s * sin(2f * PI * uv.c0);
            p.positions.c1 = r * cos(PI * uv.c1);
            p.positions.c2 = s * cos(2f * PI * uv.c0);
            p.normals = p.positions;
            return p;
        }
    }

    public struct OctoSphere : IShape
    {
        public Point4 GetPoint4(int i, float resolution, float invResolution)
        {
            float4x2 uv = IndexToUV4(i, resolution, invResolution);

            Point4 p;

            // Construct plane
            p.positions.c0 = uv.c0 - 0.5f;
            p.positions.c1 = uv.c1 - 0.5f;

            // Half Octahedron 
            p.positions.c2 = 0.5f - abs(p.positions.c0) - abs(p.positions.c1);

            // Whole octahedron 
            float4 offset = max(-p.positions.c2, 0f);
            p.positions.c0 += select(-offset, offset, p.positions.c0 < 0f);
            p.positions.c1 += select(-offset, offset, p.positions.c1 < 0f);

            // Scale to sphere
            float4 scale = 0.5f * rsqrt(
                p.positions.c0 * p.positions.c0 +
                p.positions.c1 * p.positions.c1 +
                p.positions.c2 * p.positions.c2
                );
            p.positions.c0 *= scale;
            p.positions.c1 *= scale;
            p.positions.c2 *= scale;

            p.normals = p.positions;
            return p;
        }
    }

    public struct Torus : IShape
    {
        public Point4 GetPoint4(int i, float resolution, float invResolution)
        {
            float4x2 uv = IndexToUV4(i, resolution, invResolution);

            // Major radus, size of hole
            float r1 = 0.375f;
            // Minor radius, thickness of ring
            float r2 = 0.125f;
            float4 s = r1 + r2 * cos(2 * PI * uv.c1);

            Point4 p;
            p.positions.c0 = s * sin(2f * PI * uv.c0);
            p.positions.c1 = r2 * sin(2f * PI * uv.c1);
            p.positions.c2 = s * cos(2f * PI * uv.c0);
            p.normals = p.positions;
            p.normals.c0 -= r1 * sin(2f * PI * uv.c0);
            p.normals.c2 -= r1 * cos(2f * PI * uv.c0);

            return p;
        }
    }

    [BurstCompile(FloatPrecision = FloatPrecision.Standard, FloatMode = FloatMode.Fast, CompileSynchronously = true)]
    public struct Job<S> : IJobFor where S : struct, IShape
    {

        [WriteOnly]
        public NativeArray<float3x4> positions, normals;
        public float resolution, invResolution;
        public float3x4 positionTRS, normalTRS;

        public void Execute(int i)
        {
            Point4 p = default(S).GetPoint4(i, resolution, invResolution);
            positions[i] = transpose(TransformVectors(positionTRS, p.positions));
            float3x4 n = transpose(TransformVectors(normalTRS, p.normals, 0f));
            normals[i] = float3x4(normalize(n.c0), normalize(n.c1), normalize(n.c2), normalize(n.c3));
        }

    public static JobHandle ScheduleParallel (NativeArray<float3x4> positions, NativeArray<float3x4> normals, int resolution, float4x4 trs, JobHandle handle)
        {
            float4x4 tim = transpose(inverse(trs));
            return new Job<S>
            {
                positions = positions,
                normals = normals,
                resolution = resolution,
                invResolution = 1f / resolution,
                positionTRS = float3x4(trs.c0.xyz, trs.c1.xyz, trs.c2.xyz, trs.c3.xyz),
                normalTRS = float3x4(tim.c0.xyz, tim.c1.xyz, tim.c2.xyz, tim.c3.xyz)
            }.ScheduleParallel(positions.Length, resolution, handle);
        }

        float4x3 TransformVectors(float3x4 trs, float4x3 p, float w = 1f) => float4x3(
             trs.c0.x * p.c0 + trs.c1.x * p.c1 + trs.c2.x * p.c2 + trs.c3.x * w,
             trs.c0.y * p.c0 + trs.c1.y * p.c1 + trs.c2.y * p.c2 + trs.c3.y * w,
             trs.c0.z * p.c0 + trs.c1.z * p.c1 + trs.c2.z * p.c2 + trs.c3.z * w
            );
    }
}