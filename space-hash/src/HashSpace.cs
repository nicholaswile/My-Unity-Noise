using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

public class HashSpace : MonoBehaviour
{
    [SerializeField]
    private Mesh _mesh;
    [SerializeField]
    private Material _material;
    [SerializeField, Range(1, 512)]
    private int _resolution = 16;
    [SerializeField]
    private int _seed = 0;
    [SerializeField, Range(-.5f, .5f)]
    private float _displacement = 0.1f;
    [SerializeField, Range(.1f, 10f)]
    private float _animationSpeed = 2f;
    [SerializeField]
    private SpaceTRS _domain = new SpaceTRS
    {
        scale = 1f
    };
    [SerializeField]
    private bool _animate = false;
    [SerializeField]
    private Shapes _shape;
    [SerializeField, Range(.1f, 10f)]
    private float _instanceScale = 1f;
    
    public enum Shapes { Plane, Sphere, OctoSphere, Torus}
    private static Shape.ScheduleDelegate[] _shapeJobs =
    {
        Shape.Job<Shape.Plane>.ScheduleParallel,
        Shape.Job<Shape.Sphere>.ScheduleParallel,
        Shape.Job<Shape.OctoSphere>.ScheduleParallel,
        Shape.Job<Shape.Torus>.ScheduleParallel
    };

    private NativeArray<uint4> _hashes;
    private NativeArray<float3x4> _positions, _normals;

    private ComputeBuffer _hashBuffer, _posBuffer, _normsBuffer;
    private MaterialPropertyBlock _matPropertyBlock;

    private static readonly int 
    _hashID      = Shader.PropertyToID("_Hashes"),
    _configID    = Shader.PropertyToID("_Config"),
    _posID       = Shader.PropertyToID("_Positions"),
    _normsID     = Shader.PropertyToID("_Normals");

    private Bounds _bounds;
    private const float MAX_HEIGHT = .5f, MIN_HEIGHT = -.5f;
    private float _factor = 1;
    private float _t = 0.5f;
    private bool _needsRefresh = false;

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct HashJob : IJobFor
    {
        [WriteOnly]
        public NativeArray<uint4> hashes;
        [ReadOnly]
        public NativeArray<float3x4> positions;

        public xxHash4 hash;
        public float3x4 domain;
        public int seed;

        public void Execute(int i)
        {
            float4x3 p = TransformPositions(domain, transpose(positions[i]));
            int4 u = (int4)floor(p.c0);
            int4 v = (int4)floor(p.c1);
            int4 w = (int4)floor(p.c2);

            hashes[i] = hash.Eat(u).Eat(v).Eat(w);
        }

        // Multiply a 3x4 matrix by a 4x3 matrix
        private float4x3 TransformPositions(float3x4 trs, float4x3 p) => float4x3 (
            trs.c0.x * p.c0 + trs.c1.x * p.c1 + trs.c2.x * p.c2 + trs.c3.x,
            trs.c0.y * p.c0 + trs.c1.y * p.c1 + trs.c2.y * p.c2 + trs.c3.y,
            trs.c0.z * p.c0 + trs.c1.z * p.c1 + trs.c2.z * p.c2 + trs.c3.z
            );
    }

    private void OnEnable()
    {
        _needsRefresh = true;
        int count = _resolution * _resolution;
        count /= 4 + (count & 1); // Allows odd resolutions
        _hashes = new NativeArray<uint4>(count, Allocator.Persistent);
        _positions = new NativeArray<float3x4>(count, Allocator.Persistent);
        _normals = new NativeArray<float3x4>(count, Allocator.Persistent);
        _hashBuffer = new ComputeBuffer(4 * count, sizeof(uint));
        _posBuffer = new ComputeBuffer(4 * count, 3 * sizeof(float));
        _normsBuffer = new ComputeBuffer(4 * count, 3 * sizeof(float)); 

        _matPropertyBlock ??= new MaterialPropertyBlock();
        _matPropertyBlock.SetBuffer(_hashID, _hashBuffer);
        _matPropertyBlock.SetBuffer(_posID, _posBuffer);
        _matPropertyBlock.SetBuffer(_normsID, _normsBuffer);
        _matPropertyBlock.SetVector(_configID, new Vector4(_resolution, _instanceScale / _resolution, _displacement));
    }

    private void OnDisable()
    {
        _hashes.Dispose();
        _positions.Dispose();
        _normals.Dispose();
        _hashBuffer.Release();
        _posBuffer.Release();   
        _normsBuffer.Release();
        _matPropertyBlock = null;
    }

    private void OnValidate()
    {
        if (_hashBuffer != null && enabled)
        {
            OnDisable();
            OnEnable();
        }
    }

    private void Update()
    {
        if (_needsRefresh || transform.hasChanged) 
            Draw();

        if (_animate)
        {
            if (_displacement >= MAX_HEIGHT) _factor = -1f;
            else if (_displacement <= MIN_HEIGHT) _factor = 1f;
            _t += _factor * (Time.deltaTime) * _animationSpeed;
            _displacement = Mathf.SmoothStep(MIN_HEIGHT, MAX_HEIGHT, _t);
            OnValidate(); // Buggy because it keeps calling Disable/Enable repeatedly.
        }

        Graphics.DrawMeshInstancedProcedural(_mesh, 0, _material, _bounds, _resolution * _resolution, _matPropertyBlock);   
    }

    private void Draw()
    {
        JobHandle handle = _shapeJobs[(int)_shape](_positions, _normals, _resolution, transform.localToWorldMatrix, default);

        // Generate 4 hashes in parallel - SIMD
        new HashJob
        {
            positions = _positions,
            seed = _seed,
            hashes = _hashes,
            hash = xxHash4.Seed(_seed),
            domain = _domain.Matrix
        }.ScheduleParallel(_hashes.Length, _resolution, handle).Complete();

        _hashBuffer.SetData(_hashes.Reinterpret<uint>(4 * sizeof(uint)));
        _posBuffer.SetData(_positions.Reinterpret<float3>(3 * 4 * sizeof(float)));
        _normsBuffer.SetData(_normals.Reinterpret<float3>(3 * 4 * sizeof(float)));  

        _bounds = new Bounds(transform.position, float3(2f * cmax(abs(transform.lossyScale)) + _displacement));
    }
}
