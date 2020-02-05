cbuffer buf : register(b0)
{
	matrix worldMatrix;
	matrix viewMatrix;
	matrix invViewMatrix;
	matrix projMatrix;
};

cbuffer buf2 : register(b1)
{
	float4 bezierPoints[64];
};

struct VSInput
{
	float3 pos : POSITION;
	float3 norm : NORMAL0;
};

struct PSInput
{
	float4 pos : SV_POSITION;
	float3 worldPos : POSITION0;
	float3 norm : NORMAL0;
	float3 viewVec : TEXCOORD0;
};

float GetBernsteinValue(int index, float t)
{
	switch (index)
	{
	case 0:
		return (1.0f - t) * (1.0f - t) * (1.0f - t);
	case 1:
		return 3 * t * (1.0f - t) * (1.0f - t);
	case 2:
		return 3 * t * t * (1.0f - t);
	case 3:
		return t * t * t;
	}

	return 0;
}

PSInput main(VSInput input)
{
	PSInput o;
	o.worldPos = mul(worldMatrix, float4(input.pos, 1.0f)).xyz;
	float worldx = o.worldPos.x;
	float worldy = o.worldPos.y;
	float worldz = o.worldPos.z;

	float4 spoint=float4(0,0,0,1);
	for (int i = 0; i < 4; i++)
	{
		for (int j = 0; j < 4; j++)
		{
			for (int k = 0; k < 4; k++)
			{
				spoint += bezierPoints[i * 16 + j * 4 + k] * GetBernsteinValue(i, worldx) * GetBernsteinValue(j, worldy) * GetBernsteinValue(k, worldz);
			}
		}
	}

	float3 small = o.worldPos - (0.01 * input.norm);

	float4 dpoint = float4(0, 0, 0, 1);
	for (int i = 0; i < 4; i++)
	{
		for (int j = 0; j < 4; j++)
		{
			for (int k = 0; k < 4; k++)
			{
				dpoint += bezierPoints[i * 16 + j * 4 + k] * GetBernsteinValue(i, small.x) * GetBernsteinValue(j, small.y) * GetBernsteinValue(k, small.z);
			}
		}
	}

	float3 norm = normalize((spoint - dpoint).xyz);
	o.worldPos = spoint;

	o.pos = mul(viewMatrix, float4(o.worldPos, 1.0f));
	o.pos = mul(projMatrix, o.pos);

	o.norm = mul(worldMatrix, float4(norm, 0.0f)).xyz;
	o.norm = normalize(o.norm);
	float3 camPos = mul(invViewMatrix, float4(0.0f, 0.0f, 0.0f, 1.0f)).xyz;
	o.viewVec = camPos - o.worldPos;
	return o;
}
