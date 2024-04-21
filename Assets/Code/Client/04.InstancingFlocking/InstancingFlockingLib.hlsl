/********************************************************************
created:    2024-04-20
author:     lixianmin

Copyright (C) - All Rights Reserved
*********************************************************************/

#ifndef XML_INSTANCING_FLOCKING_LIB
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

// instancing_options procedural:setup_instancing_flocking

/********************************************************************
 
    https://www.awesometech.no/index.php/guide-adding-instanced-indirect-support-to-shaders/
    https://wiki.gurbu.com/index.php?title=GPU_Instancer:FAQ#Amplify_Shader_Editor_directives

方案1: 修改unity_ObjectToWorld:
    ASE -> Output Node -> Pass -> Additional Directives -> 加入以下三行代码:

        #include "Assets/Code/Client/04.InstancingFlocking/InstancingFlockingLib.hlsl"
        #pragma instancing_options procedural:setup_instancing_flocking
        #define UNITY_PROCEDURAL_INSTANCING_ENABLED
        
*********************************************************************/

void setup_instancing_flocking()
{
    // #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
    //
    //     const Boid boid = boids_buffer[unity_InstanceID];
    //     const float3 up = float3(0, 1, 0);
    //     const float4x4 object_to_world = create_trs_matrix(boid.position, boid.direction, up);
    //     unity_ObjectToWorld = object_to_world;
    //
    // #endif
}


/********************************************************************
方案2: 直接修改输入的vertex:
    1. 该方案仍然需要设置一下: #pragma instancing_options procedural:setup_instancing_flocking, 但不再需要在setup_instancing_flocking()中写任何代码
    2. 
*********************************************************************/
float4 get_vertex_position(float4 vertex)
{
    // 在ase中直接传入的instance_id仍然值为0, 不知道为什么
    // 因此需要使用unity_InstanceID, 因此只能写到 UNITY_PROCEDURAL_INSTANCING_ENABLED 代码块中
    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
    
        const Boid boid = boids_buffer[unity_InstanceID];
        const float3 up = float3(0, 1, 0);
        const float4x4 object_to_world = create_trs_matrix(boid.position, boid.direction, up);

        const float4 next_vertex_position = mul(object_to_world, vertex);
        return next_vertex_position;

    #endif

    return 0;
}

#endif
