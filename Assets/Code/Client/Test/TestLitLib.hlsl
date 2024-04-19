#ifndef XML_TEST_LIT_LIB
#define XML_TEST_LIT_LIB

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

float3 transform_object_to_world_dir(float4x4 object_to_world, float3 dirOS, bool doNormalize = true)
{
    float3 dirWS = mul((float3x3)object_to_world, dirOS);
    if (doNormalize)
        return SafeNormalize(dirWS);

    return dirWS;
}


StructuredBuffer<Boid> boids_buffer;

float3 GetVertexPosition(int instance_id, float4 vertex)
{
    // 计算positionWS
    const Boid boid = boids_buffer[instance_id];
    const float3 up = float3(0, 1, 0);
    const float4x4 object_to_world = create_trs_matrix(boid.position, boid.direction, up);
    const float3 positionWS = mul(object_to_world, vertex);

    return positionWS;
}

float4 GetRedColor(float3 input)
{
    return float4(1, 0, 0, 1);
}

#endif
