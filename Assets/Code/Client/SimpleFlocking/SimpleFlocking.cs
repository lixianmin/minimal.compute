/********************************************************************
created:    2024-04-14
author:     lixianmin

Copyright (C) - All Rights Reserved
*********************************************************************/

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unicorn;
using Unicorn.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using Random = UnityEngine.Random;

public class SimpleFlocking : MonoBehaviour
{
    public struct Boid
    {
        public Vector3 position;
        public Vector3 direction;
        public Bounds localBounds;

        public Boid(Vector3 position, Bounds localBounds)
        {
            this.position = position;
            direction = Vector3.forward;
            this.localBounds = localBounds;
        }
    }

    [BurstCompile]
    private struct TestVisibleJobFilter : IJobFilter
    {
        [ReadOnly] public UnsafeReadonlyArray<Plane> frustumPlanes;
        [ReadOnly] public NativeList<Boid> boidList;
    
        public bool Execute(int index)
        {
            var boid = boidList[index];
            
            var rotation = Quaternion.identity;
            if (boid.direction != Vector3.zero)
            {
                rotation = Quaternion.LookRotation(boid.direction);
            }
            
            var matrix = Matrix4x4.TRS(boid.position, rotation, Vector3.one);
            var worldBounds = new Bounds(matrix.MultiplyPoint(boid.localBounds.center), boid.localBounds.size);
            
            var isVisible = FrustumTools.TestPlanesAABB(frustumPlanes, worldBounds);
            return isVisible;
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

    private void _InitBoids(MeshRenderer meshRenderer)
    {
        var prefabBounds = meshRenderer.localBounds;
        var boidData = new Boid[boidsCount];
        for (var i = 0; i < boidsCount; i++)
        {
            var position = transform.position + Random.insideUnitSphere * spawnRadius;
            var matrix = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);
            var localBounds = new Bounds(matrix.MultiplyPoint(prefabBounds.center), prefabBounds.size);

            boidData[i] = new Boid(position, localBounds);
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
        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        GeometryUtility.CalculateFrustumPlanes(mainCamera, _frustumPlane);

        shader.SetFloat("time", Time.time);
        shader.SetFloat("deltaTime", Time.deltaTime);
        _kernel.Dispatch(boidsCount);

        var boidArray = _boidsBuffer.GetDataAsync();
        
        _nativeBoidList.Clear();
        _nativeBoidList.AddRange(boidArray);

        _nativeVisibleIndices.Clear();
        var handle = new TestVisibleJobFilter
        {
            frustumPlanes = _unsafeFrustumPlanes,
            boidList = _nativeBoidList,
        }.ScheduleAppend(_nativeVisibleIndices, boidArray.Length);
        
        handle.Complete();
        
        _matrices.Clear();
        for (var i = 0; i < _nativeVisibleIndices.Length; i++)
        {
            var index = _nativeVisibleIndices[i];
            var boid = boidArray[index];
            var rotation = Quaternion.identity;
            if (boid.direction != Vector3.zero)
            {
                rotation = Quaternion.LookRotation(boid.direction);
            }

            var matrix = Matrix4x4.TRS(boid.position, rotation, Vector3.one);
            _matrices.Add(matrix);
        }

        _meshInstanced.Render(_matrices);
    }

    private void OnEnable()
    {
        _kernel = new ComputeKernel(shader, "CSMain");
        _boidsBuffer = new RWStructuredBuffer<Boid>("boids_buffer", Marshal.SizeOf(typeof(Boid)));

        var meshRenderer = boidPrefab.GetComponent<MeshRenderer>();
        _meshInstanced = MeshInstanced.Create(meshRenderer);

        _InitBoids(meshRenderer);
        _InitShader();

        _unsafeFrustumPlanes = new UnsafeReadonlyArray<Plane>(_frustumPlane);
        _nativeBoidList = new NativeList<Boid>(Allocator.Persistent);
        _nativeVisibleIndices = new NativeList<int>(Allocator.Persistent);
    }

    private void OnDisable()
    {
        _boidsBuffer.Dispose();
        
        _unsafeFrustumPlanes.Dispose();
        _nativeBoidList.Dispose();
        _nativeVisibleIndices.Dispose();
    }
    
    
    private ComputeKernel _kernel;
    private RWStructuredBuffer<Boid> _boidsBuffer;

    private MeshInstanced _meshInstanced;
    private readonly Slice<Matrix4x4> _matrices = new();
    
    private readonly Plane[] _frustumPlane = new Plane[6];
    private UnsafeReadonlyArray<Plane> _unsafeFrustumPlanes;

    private NativeList<Boid> _nativeBoidList;
    private NativeList<int> _nativeVisibleIndices;
}