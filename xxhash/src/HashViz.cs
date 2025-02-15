using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

public class HashViz : MonoBehaviour
{
    [SerializeField]
    private Mesh _mesh;
    [SerializeField]
    private Material _material;
    [SerializeField, Range(1, 512)]
    private int _resolution = 16;
    [SerializeField]
    private int _seed = 0;
    [SerializeField, Range(-2f, 2f)]
    private float _height = 1f;
    [SerializeField, Range(0f, 10f)]
    private float _animationSpeed = 2f;

    private NativeArray<uint> _hashes;
    private ComputeBuffer _hashBuffer;
    private MaterialPropertyBlock _matPropertyBlock;

    private static readonly int 
    _hashID      = Shader.PropertyToID("_Hashes"),
    _configID    = Shader.PropertyToID("_Config");

    private Bounds _origin;
    private const float MAX_HEIGHT = 2.0f, MIN_HEIGHT = -2.0f;
    private float _factor = 1;
    private float _t = 0.5f;

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct HashJob : IJobFor
    {
        [WriteOnly]
        public NativeArray<uint> hashes;

        public int resolution, seed;
        public float inverseRes;

        public xxHash hash;

        public void Execute(int i)
        {
            int v = (int)floor(inverseRes * i + 0.00001f);
            int u = i - resolution * v - resolution / 2;
            v -= resolution / 2;
            hashes[i] = hash.Eat(u).Eat(v);
        }
    }

    private void OnEnable()
    {
        int count = _resolution * _resolution;
        _hashes = new NativeArray<uint>(count, Allocator.Persistent);
        _hashBuffer = new ComputeBuffer(count, sizeof(uint));

        new HashJob
        {
            resolution = _resolution,
            seed = _seed,
            inverseRes = 1.0f / _resolution,
            hashes = _hashes,
            hash = xxHash.Seed(_seed)
        }.ScheduleParallel(_hashes.Length, _resolution, default).Complete();

        _hashBuffer.SetData(_hashes);

        _matPropertyBlock ??= new MaterialPropertyBlock();
        _matPropertyBlock.SetBuffer(_hashID, _hashBuffer);
        _matPropertyBlock.SetVector(_configID, new Vector4(_resolution, 1.0f / _resolution, _height / _resolution));
    }

    private void OnDisable()
    {
        _hashes.Dispose();
        _hashBuffer.Release();
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

    private void Awake()
    {
        _origin = new Bounds(Vector3.zero, Vector3.zero);
    }


    private void Update()
    {
        if (_height >= MAX_HEIGHT) _factor = -1f;
        else if (_height <= MIN_HEIGHT) _factor = 1f;
        _t += _factor * (Time.deltaTime) * _animationSpeed;
        _height = Mathf.SmoothStep(MIN_HEIGHT, MAX_HEIGHT, _t);
        OnValidate();   

        Graphics.DrawMeshInstancedProcedural(_mesh, 0, _material, _origin, _hashes.Length, _matPropertyBlock);   
    }
}
