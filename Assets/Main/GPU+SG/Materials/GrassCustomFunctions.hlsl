#ifndef GrassCustomFunctions
#define GrassCustomFunctions
#endif 
StructuredBuffer<float4> _Positions;

void GetPosition_float(float instanceID, float3 positionOS, out float3 position)
{
    position = positionOS * _Positions[(uint)instanceID].w + _Positions[(uint)instanceID].xyz;
}