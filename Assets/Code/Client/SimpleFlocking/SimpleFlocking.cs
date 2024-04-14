/********************************************************************
created:    2024-04-14
author:     lixianmin

Copyright (C) - All Rights Reserved
*********************************************************************/

using System.Runtime.InteropServices;
using Unicorn;
using UnityEngine;
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

    private GameObject[] _boids;

    private void Awake()
    {
        _kernel = new ComputeKernel(shader, "CSMain");
        _boidsBuffer = new RWStructuredBuffer<Boid>("boids_buffer", Marshal.SizeOf(typeof(Boid)));
        
        _InitBoids();
        _InitShader();
    }

    private void _InitBoids()
    {
        _boids = new GameObject[boidsCount];
        
        var boidData = new Boid[boidsCount];
        for (var i = 0; i < boidsCount; i++)
        {
            var pos = transform.position + Random.insideUnitSphere * spawnRadius;
            boidData[i] = new Boid(pos);
            _boids[i] = Instantiate(boidPrefab, pos, Quaternion.identity);
            boidData[i].direction = _boids[i].transform.forward;
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
        for (var i = 0; i < boidData.Length; i++)
        {
            _boids[i].transform.localPosition = boidData[i].position;

            if (!boidData[i].direction.Equals(Vector3.zero))
            {
                _boids[i].transform.rotation = Quaternion.LookRotation(boidData[i].direction);
            }
        }
    }

    private void LateUpdate()
    {
        
    }

    private void OnDestroy()
    {
        _boidsBuffer.Dispose();
    }
}