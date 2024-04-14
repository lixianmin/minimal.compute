/********************************************************************
created:    2024-04-14
author:     lixianmin

Copyright (C) - All Rights Reserved
*********************************************************************/

using System.Runtime.InteropServices;
using Unicorn;
using UnityEngine;

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

    private Boid[] _boidsArray;
    private GameObject[] _boids;

    private void Start()
    {
        _kernel = new ComputeKernel(shader, "CSMain");
        _boidsBuffer = new RWStructuredBuffer<Boid>("boids_buffer", Marshal.SizeOf(typeof(Boid)));
        
        _InitBoids();
        _InitShader();
    }

    private void _InitBoids()
    {
        _boids = new GameObject[boidsCount];
        _boidsArray = new Boid[boidsCount];

        for (var i = 0; i < boidsCount; i++)
        {
            var pos = transform.position + Random.insideUnitSphere * spawnRadius;
            _boidsArray[i] = new Boid(pos);
            _boids[i] = Instantiate(boidPrefab, pos, Quaternion.identity);
            _boidsArray[i].direction = _boids[i].transform.forward;
        }
    }

    private void _InitShader()
    {
        _kernel.SetBuffer(_boidsBuffer, _boidsArray);

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

        _boidsArray = _boidsBuffer.GetDataAsync();

        for (var i = 0; i < _boidsArray.Length; i++)
        {
            _boids[i].transform.localPosition = _boidsArray[i].position;

            if (!_boidsArray[i].direction.Equals(Vector3.zero))
            {
                _boids[i].transform.rotation = Quaternion.LookRotation(_boidsArray[i].direction);
            }
        }
    }

    private void OnDestroy()
    {
        _boidsBuffer.Dispose();
    }
}