﻿#ifndef XML_INSTANCING_FLOCKING_LIB
#define XML_INSTANCING_FLOCKING_LIB

struct Bounds
{
    float3 center;
    float3 size;
};

struct Boid
{
    float3 position;
    float3 direction;
    Bounds local_bounds;
};

float4x4 create_trs_matrix(float3 pos, float3 dir, float3 up)
{
    const float3 zaxis = normalize(dir);
    const float3 xaxis = normalize(cross(up, zaxis));
    const float3 yaxis = cross(zaxis, xaxis);
    return float4x4(
        xaxis.x, yaxis.x, zaxis.x, pos.x,
        xaxis.y, yaxis.y, zaxis.y, pos.y,
        xaxis.z, yaxis.z, zaxis.z, pos.z,
        0, 0, 0, 1
    );
}

StructuredBuffer<Boid> boids_buffer;

void setup_instancing_flock()
{
    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
    
    const Boid boid = boids_buffer[unity_InstanceID];
    const float3 up = float3(0, 1, 0);
    const float4x4 object_to_world = create_trs_matrix(boid.position, boid.direction, up);
    unity_ObjectToWorld = object_to_world;
    
    #endif
}

#endif