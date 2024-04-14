/********************************************************************
created:    2024-04-14
author:     lixianmin

Copyright (C) - All Rights Reserved
*********************************************************************/

using System.Runtime.InteropServices;
using Unicorn;
using Unicorn.Collections;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;
using Random = UnityEngine.Random;

public class SimpleFlocking : MonoBehaviour
{
    public struct Boid
    {
        public Vector3 position;
        public Vector3 direction;

        public Boid(Vector3 pos)
        {
            position = pos;
            direction = Vector3.zero;
        }
    }

    [BurstCompile]
    private struct BoidTransformJob : IJobParallelForTransform
    {
        [ReadOnly] public UnsafeReadonlyArray<Boid> boids;

        public void Execute(int index, TransformAccess transform)
        {
            var boid = boids[index];
            transform.localPosition = boid.position;

            if (boid.direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(boid.direction);
            }
        }
    }

    public ComputeShader shader;

    public float rotationSpeed = 1f;
    public float boidSpeed = 1f;
    public float neighbourDistance = 1f;
    public float boidSpeedVariation = 1f;
    public GameObject boidPrefab;
    public int boidsCount;
    public float spawnRadius;
    public Transform target;

    private ComputeKernel _kernel;
    private RWStructuredBuffer<Boid> _boidsBuffer;

    private Transform[] _boidTransforms;
    private InstanceRenderer _instanceRenderer;
    private readonly Slice<Matrix4x4> _matrices = new();

    private void Awake()
    {
        _kernel = new ComputeKernel(shader, "CSMain");
        _boidsBuffer = new RWStructuredBuffer<Boid>("boids_buffer", Marshal.SizeOf(typeof(Boid)));

        var meshRenderer = boidPrefab.GetComponent<MeshRenderer>();
        _instanceRenderer = new InstanceRenderer(meshRenderer);

        _InitBoids();
        _InitShader();
    }

    private void _InitBoids()
    {
        _boidTransforms = new Transform[boidsCount];

        var boidData = new Boid[boidsCount];
        for (var i = 0; i < boidsCount; i++)
        {
            var pos = transform.position + Random.insideUnitSphere * spawnRadius;
            boidData[i] = new Boid(pos);
            _boidTransforms[i] = Instantiate(boidPrefab, pos, Quaternion.identity).transform;
            boidData[i].direction = _boidTransforms[i].forward;
        }

        _kernel.SetBuffer(_boidsBuffer, boidData);
    }

    private void _InitShader()
    {
        shader.SetFloat("rotationSpeed", rotationSpeed);
        shader.SetFloat("boidSpeed", boidSpeed);
        shader.SetFloat("boidSpeedVariation", boidSpeedVariation);
        shader.SetVector("flockPosition", target.transform.position);
        shader.SetFloat("neighbourDistance", neighbourDistance);
        shader.SetInt("boidsCount", boidsCount);
    }

    private void Update()
    {
        shader.SetFloat("time", Time.time);
        shader.SetFloat("deltaTime", Time.deltaTime);
        _kernel.Dispatch(boidsCount);

        var boidData = _boidsBuffer.GetDataAsync();
        _matrices.Clear();
        for (var i = 0; i < boidData.Length; i++)
        {
            var boid = boidData[i];
            var rotation = Quaternion.identity;
            if (boid.direction != Vector3.zero)
            {
                rotation = Quaternion.LookRotation(boid.direction);
            }

            var matrix = Matrix4x4.TRS(boid.position, rotation, Vector3.one);
            _matrices.Add(matrix);
        }

        _instanceRenderer.RenderMeshInstanced(_matrices);
    }
    
    private void OnDestroy()
    {
        _boidsBuffer.Dispose();
    }
}