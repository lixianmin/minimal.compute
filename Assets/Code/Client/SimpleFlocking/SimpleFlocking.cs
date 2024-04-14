/********************************************************************
created:    2024-04-14
author:     lixianmin

Copyright (C) - All Rights Reserved
*********************************************************************/

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

    private int kernelHandle;
    private ComputeBuffer boidsBuffer;
    private Boid[] boidsArray;
    private GameObject[] boids;
    private int groupSizeX;
    private int numOfBoids;

    private void Start()
    {
        kernelHandle = shader.FindKernel("CSMain");

        uint x;
        shader.GetKernelThreadGroupSizes(kernelHandle, out x, out _, out _);
        groupSizeX = Mathf.CeilToInt(boidsCount / (float)x);
        numOfBoids = groupSizeX * (int)x;

        _InitBoids();
        _InitShader();
    }

    private void _InitBoids()
    {
        boids = new GameObject[numOfBoids];
        boidsArray = new Boid[numOfBoids];

        for (var i = 0; i < numOfBoids; i++)
        {
            var pos = transform.position + Random.insideUnitSphere * spawnRadius;
            boidsArray[i] = new Boid(pos);
            boids[i] = Instantiate(boidPrefab, pos, Quaternion.identity);
            boidsArray[i].direction = boids[i].transform.forward;
        }
    }

    private void _InitShader()
    {
        boidsBuffer = new ComputeBuffer(numOfBoids, 6 * sizeof(float));
        boidsBuffer.SetData(boidsArray);

        shader.SetBuffer(kernelHandle, "boidsBuffer", boidsBuffer);
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
        shader.Dispatch(kernelHandle, groupSizeX, 1, 1);

        boidsBuffer.GetData(boidsArray);

        for (var i = 0; i < boidsArray.Length; i++)
        {
            boids[i].transform.localPosition = boidsArray[i].position;

            if (!boidsArray[i].direction.Equals(Vector3.zero))
            {
                boids[i].transform.rotation = Quaternion.LookRotation(boidsArray[i].direction);
            }
        }
    }

    private void OnDestroy()
    {
        boidsBuffer?.Dispose();
    }
}