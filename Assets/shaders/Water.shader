HEADER
{
    Description = "Water Shader";
}

FEATURES
{
	#include "common/features.hlsl"
}

MODES
{
	VrForward();
	Depth();
}

COMMON
{
	#define S_SPECULAR 1
	#include "common/shared.hlsl"
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
};

VS
{
	#include "common/vertex.hlsl"
	
	float g_flWaveAmplitude < UiGroup( "Waves,0/,0/0" ); Default1( 2.0 ); Range1( 0, 10 ); >;
	float g_flWaveFrequency < UiGroup( "Waves,0/,0/0" ); Default1( 0.5 ); Range1( 0.01, 5 ); >;
	float g_flWaveSpeed < UiGroup( "Waves,0/,0/0" ); Default1( 1.0 ); Range1( 0, 5 ); >;
	float g_flWaveSteepness < UiGroup( "Waves,0/,0/0" ); Default1( 0.2 ); Range1( 0, 2 ); >;
	bool g_bEnableVertexDisplacement < UiGroup( "Waves,0/,0/0" ); Default( 0 ); >;

	PixelInput MainVs( VertexInput v )
	{
		PixelInput i = ProcessVertex( v );
		
		if (g_bEnableVertexDisplacement)
		{
			float3 worldPos = i.vPositionWs.xyz;
			float2 worldUV = worldPos.xy;
			float time = g_flTime * g_flWaveSpeed;
			
			float heightZ = 0.0;
			float2 horizontalOffset = float2(0, 0);
			
			float2 direction = float2(0.857, 0.515);
			float frequency = g_flWaveFrequency * 0.3;
			float amplitude = g_flWaveAmplitude;
			float phase = dot(direction, worldUV) * frequency + time * 1.2;
			
			heightZ += amplitude * sin(phase);
			horizontalOffset += g_flWaveSteepness * amplitude * direction * cos(phase);
			
			direction = float2(-0.514, 0.858);
			frequency = g_flWaveFrequency * 0.6;
			amplitude = g_flWaveAmplitude * 0.6;
			phase = dot(direction, worldUV) * frequency - time * 1.5;
			
			heightZ += amplitude * sin(phase);
			horizontalOffset += g_flWaveSteepness * amplitude * direction * cos(phase);
			
			i.vPositionWs.xy += horizontalOffset;
			i.vPositionWs.z += heightZ;
			
			i.vPositionPs.xyzw = Position3WsToPs( i.vPositionWs.xyz );
		}
		
		return FinalizeVertex( i );
	}
}

PS
{
	#include "common/utils/Material.CommonInputs.hlsl"
	#include "common/pixel.hlsl"
	
	SamplerState g_sSampler < Filter( MIN_MAG_MIP_LINEAR ); AddressU( WRAP ); AddressV( WRAP ); >;
	
	CreateInputTexture2D( NormalMapA, Linear, 8, "NormalizeNormals", "_normal", "Normals,0/,0/0", Default4( 0.5, 0.5, 1.0, 1.0 ) );
	CreateInputTexture2D( NormalMapB, Linear, 8, "NormalizeNormals", "_normal", "Normals,0/,0/0", Default4( 0.5, 0.5, 1.0, 1.0 ) );
	CreateInputTexture2D( FoamTexture, Srgb, 8, "None", "_mask", "Foam,0/,0/0", Default4( 1.0, 1.0, 1.0, 1.0 ) );
	
	Texture2D g_tNormalMapA < Channel( RGBA, Box( NormalMapA ), Linear ); OutputFormat( BC7 ); SrgbRead( False ); >;
	Texture2D g_tNormalMapB < Channel( RGBA, Box( NormalMapB ), Linear ); OutputFormat( BC7 ); SrgbRead( False ); >;
	Texture2D g_tFoamTexture < Channel( RGBA, Box( FoamTexture ), Srgb ); OutputFormat( BC7 ); SrgbRead( True ); >;
	
	bool g_bReflection < Default(0.0f); Attribute( "HasReflectionTexture" ); >;
	CreateTexture2D( g_tReflectionTexture ) < Attribute("ReflectionTexture"); SrgbRead( true ); Filter(MIN_MAG_MIP_LINEAR); AddressU( CLAMP ); AddressV( CLAMP ); >;
	
	BoolAttribute( bWantsFBCopyTexture, true );
	CreateTexture2D( g_tFrameBufferCopyTexture ) < Attribute("FrameBufferCopyTexture"); SrgbRead( true ); Filter(MIN_MAG_MIP_LINEAR); AddressU( CLAMP ); AddressV( CLAMP ); >;
	
	float g_flNormalScale < UiGroup( "Surface,0/,0/0" ); Default1( 200.0 ); Range1( 10, 1000 ); >;
	float g_flNormalSpeed < UiGroup( "Surface,0/,0/0" ); Default1( 0.3 ); Range1( 0, 2 ); >;
	float g_flNormalStrength < UiGroup( "Surface,0/,0/0" ); Default1( 1.0 ); Range1( 0, 3 ); >;
	float g_flGlossiness < UiGroup( "Surface,0/,0/0" ); Default1( 0.98 ); Range1( 0, 1 ); >;
	float g_flMetalness < UiGroup( "Surface,0/,0/0" ); Default1( 0.85 ); Range1( 0, 1 ); >;
	
	bool g_bEnableFoam < UiGroup( "Foam,0/,0/0" ); Default( 1 ); >;
	float g_flFoamDepth < UiGroup( "Foam,0/,0/0" ); Default1( 20.0 ); Range1( 1, 200 ); >;
	float g_flFoamSharpness < UiGroup( "Foam,0/,0/0" ); Default1( 3.0 ); Range1( 0.1, 10 ); >;
	float g_flFoamScale < UiGroup( "Foam,0/,0/0" ); Default1( 50.0 ); Range1( 10, 300 ); >;
	float g_flFoamSpeed < UiGroup( "Foam,0/,0/0" ); Default1( 0.5 ); Range1( 0, 2 ); >;
	float4 g_vFoamColor < UiType( Color ); UiGroup( "Foam,0/,0/0" ); Default4( 0.95, 0.95, 0.95, 1.0 ); >;
	float g_flFoamContrast < UiGroup( "Foam,0/,0/0" ); Default1( 1.5 ); Range1( 0.1, 5 ); >;
	
	float g_flWaterDepth < UiGroup( "Water Color,0/,0/0" ); Default1( 200.0 ); Range1( 10, 1000 ); >;
	float g_flWaterStart < UiGroup( "Water Color,0/,0/0" ); Default1( 0.0 ); Range1( 0, 100 ); >;
	float4 g_vRefractionTint < UiType( Color ); UiGroup( "Water Color,0/,0/0" ); Default4( 0.4, 0.7, 0.8, 1.0 ); >;
	float4 g_vWaterFogColor < UiType( Color ); UiGroup( "Water Color,0/,0/0" ); Default4( 0.0, 0.3, 0.5, 1.0 ); >;
	
	float g_flRefractionAmount < UiGroup( "Refraction,0/,0/0" ); Default1( 0.1 ); Range1( 0, 1 ); >;
	
	float g_flReflectance < UiGroup( "Reflection,0/,0/0" ); Default1( 0.5 ); Range1( 0, 1 ); >;
	float g_flReflectionDistortion < UiGroup( "Reflection,0/,0/0" ); Default1( 0.05 ); Range1( 0, 1 ); >;
	float g_flSSRStrength < UiGroup( "Reflection,0/,0/0" ); Default1( 1.0 ); Range1( 0, 1 ); >;
	float g_flEnvReflectionStrength < UiGroup( "Reflection,0/,0/0" ); Default1( 0.3 ); Range1( 0, 1 ); >;
	
	bool g_bEnableFresnel < UiGroup( "Fresnel,0/,0/0" ); Default( 1 ); >;
	float3 g_vReflectionDir < UiGroup( "Fresnel,0/,0/0" ); Default3( 0.707, 0.707, 0.0 ); Range3( -1, -1, -1, 1, 1, 1 ); >;
	float3 g_vReflectionColor < UiType( Color ); UiGroup( "Fresnel,0/,0/0" ); Default3( 1, 1, 1 ); >;
	
	RenderState( CullMode, F_RENDER_BACKFACES ? NONE : DEFAULT );
	
	float4 MainPs( PixelInput i ) : SV_Target0
	{
		float3 worldPos = i.vPositionWithOffsetWs.xyz + g_vHighPrecisionLightingOffsetWs.xyz;
		float time = g_flTime * g_flNormalSpeed;
		
		float2 baseUV1 = worldPos.xy / g_flNormalScale;
		float2 baseUV2 = worldPos.xy / g_flNormalScale;
		float2 baseUV3 = worldPos.xy / (g_flNormalScale * 0.7);
		
		float2 uv1 = baseUV1 + float2(time * 0.5, time * 0.3);
		float2 uv2 = baseUV2 + float2(-time * 0.4, time * 0.6);
		float2 uv3 = baseUV3 + float2(time * 0.3, -time * 0.5);
		
		float3 normal1 = g_tNormalMapA.Sample(g_sSampler, uv1).xyz * 2.0 - 1.0;
		float3 normal2 = g_tNormalMapB.Sample(g_sSampler, uv2).xyz * 2.0 - 1.0;
		float3 normal3 = g_tNormalMapA.Sample(g_sSampler, uv3).xyz * 2.0 - 1.0;
		
		float3 blendedNormal = normalize((normal1 + normal2 + normal3) * 0.33);
		blendedNormal.xy *= g_flNormalStrength;
		blendedNormal = normalize(blendedNormal);
		
		float3 worldNormal = TransformNormal(blendedNormal, i.vNormalWs, i.vTangentUWs, i.vTangentVWs);
		
		#if F_RENDER_BACKFACES
			if (!i.bIsFrontFace)
				worldNormal = -worldNormal;
		#endif
		
		float2 screenUv = i.vPositionSs.xy * g_vInvViewportSize;
		float sceneDepth = Depth::GetLinear( i.vPositionSs.xy );
		float waterDepth = i.vPositionSs.w;
		
		float3 viewDir = normalize(worldPos - g_vCameraPositionWs);
		float3 sceneWorldPos = Depth::GetWorldPosition( i.vPositionSs.xy );
		
		float depthRange = 1.0 / max(0.001, g_flWaterDepth - g_flWaterStart);
		float depthFactor = saturate(((worldPos.z - g_flWaterStart) - sceneWorldPos.z) * depthRange);
		
		float2 refractionUv = screenUv - (worldNormal.xy * g_flRefractionAmount * depthFactor);
		float refractionDepth = Depth::Get( refractionUv * g_vViewportSize.xy );
		bool depthTest = refractionDepth < i.vPositionSs.z;
		refractionUv = depthTest ? screenUv : refractionUv;
		refractionUv = clamp(refractionUv, 0.0, 1.0);
		
		float3 refractedColor = g_tFrameBufferCopyTexture.Sample(g_tFrameBufferCopyTexture_sampler, refractionUv).rgb;
		
		float tintFactor = saturate(3.5 * depthFactor);
		float3 tintedRefraction = lerp(refractedColor, refractedColor * g_vRefractionTint.rgb, tintFactor);
		float3 waterColor = lerp(tintedRefraction, g_vWaterFogColor.rgb, depthFactor);
		
		float IOR = 1.333;
		float F0 = pow((1.0 - IOR) / (1.0 + IOR), 2.0);
		float cosTheta = saturate(dot(-viewDir, worldNormal));
		float fresnel = g_bEnableFresnel ? F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0) : 1.0;
		
		float3 envReflection = EnvMap::From( worldPos, i.vPositionSs, worldNormal, 0 );
		float envReflectionStrength = g_flEnvReflectionStrength;
		if (g_bEnableFresnel) envReflectionStrength *= fresnel;
		
		float3 finalReflection = envReflection * envReflectionStrength;
		
		if (g_bEnableFresnel)
		{
			float3 reflectDir = reflect(-viewDir, worldNormal);
			float3 normalizedReflDir = normalize(g_vReflectionDir);
			float sunSpec = pow(saturate(dot(reflectDir, normalizedReflDir)), 256.0);
			finalReflection += g_vReflectionColor * sunSpec * fresnel;
		}
		
		float foam = 0.0;
		if (g_bEnableFoam)
		{
			float depthDiff = abs(sceneDepth - waterDepth);
			float foamMask = saturate(1.0 - (depthDiff / g_flFoamDepth));
			foamMask = pow(foamMask, g_flFoamSharpness);
			
			float2 foamUV = worldPos.xy / g_flFoamScale + float2(time * g_flFoamSpeed, time * g_flFoamSpeed * 0.7);
			float foamTex = g_tFoamTexture.Sample( g_sSampler, foamUV ).r;
			
			foamTex = saturate((foamTex - 0.5) * g_flFoamContrast + 0.5);
			
			foam = foamMask * foamTex;
			waterColor = lerp(waterColor, g_vFoamColor.rgb, foam);
		}
		
		Material m = Material::Init();
		m.Albedo = waterColor;
		m.Normal = worldNormal;
		m.Roughness = g_bEnableFoam ? lerp(max(0.01, 1.0 - g_flGlossiness), 0.8, foam) : max(0.01, 1.0 - g_flGlossiness);
		m.Metalness = g_bEnableFoam ? lerp(g_flMetalness, 0.0, foam) : g_flMetalness;
		m.AmbientOcclusion = 1;
		m.Emission = float3(0, 0, 0);
		m.WorldTangentU = i.vTangentUWs;
		m.WorldTangentV = i.vTangentVWs;
		m.TextureCoords = i.vTextureCoords.xy;
		
		if(DepthNormals::WantsDepthNormals())
			return DepthNormals::Output(m.Normal, m.Roughness, 1);
		
		
		m.Emission.rgb = finalReflection;
		
		float4 outCol = ShadingModelStandard::Shade(i, m);
		outCol.rgb = Fog::Apply(worldPos, i.vPositionSs.xy, outCol.rgb);
		
		return outCol;
	}
}
