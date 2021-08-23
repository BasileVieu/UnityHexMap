#include "HexMetrics.cginc"

float Foam(float _shore, float2 _worldXZ, sampler2D _noiseTex)
{
	_shore = sqrt(_shore) * 0.9;

	float2 noiseUV = _worldXZ + _Time.y * 0.25;
	float4 noise = tex2D(_noiseTex, noiseUV * (2 * TILING_SCALE));

	float distortion1 = noise.x * (1 - _shore);
	float foam1 = sin((_shore + distortion1) * 10 - _Time.y);
	foam1 *= foam1;

	float distortion2 = noise.y * (1 - _shore);
	float foam2 = sin((_shore + distortion2) * 10 + _Time.y + 2);
	foam2 *= foam2 * 0.7;

	return max(foam1, foam2) * _shore;
}

float Waves(float2 _worldXZ, sampler2D _noiseTex)
{
	float2 uv1 = _worldXZ;
	uv1.y += _Time.y;
	float4 noise1 = tex2D(_noiseTex, uv1 * (3 * TILING_SCALE));

	float2 uv2 = _worldXZ;
	uv2.x += _Time.y;
	float4 noise2 = tex2D(_noiseTex, uv2 * (3 * TILING_SCALE));

	float blendWave = sin((_worldXZ.x + _worldXZ.y) * 0.1 + (noise1.y + noise2.z) + _Time.y);
	blendWave *= blendWave;

	float waves = lerp(noise1.z, noise1.w, blendWave) + lerp(noise2.x, noise2.y, blendWave);
	
	return smoothstep(0.75, 2, waves);
}

float River(float2 _riverUV, sampler2D _noiseTex)
{
	float2 uv = _riverUV;
	uv.x = uv.x * 0.0625 + _Time.y * 0.005;
	uv.y -= _Time.y * 0.25;

	float4 noise = tex2D(_noiseTex, uv);

	float2 uv2 = _riverUV;
	uv2.x = uv2.x * 0.0625 - _Time.y * 0.0052;
	uv2.y -= _Time.y * 0.23;

	float4 noise2 = tex2D(_noiseTex, uv2);

	return noise.x * noise2.w;
}