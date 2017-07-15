Shader "Custom/MapShader" {
	Properties {
		testTexture("Texture", 2D) = "white"{}
		testScale("Scale", Float) = 1
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		const static int maxLayerCountOnShader=8;
		const static float epsilon = 1E-4;
		int baseLayerCount;
		float3 baseColors[maxLayerCountOnShader];
		float baseStartHeights[maxLayerCountOnShader];
		float baseBlendsUp[maxLayerCountOnShader];
		float baseBlendsDown[maxLayerCountOnShader];
		float baseColorsStrenght[maxLayerCountOnShader];
		float baseTextureScales[maxLayerCountOnShader];

		sampler2D testTexture;
		float testScale;

		UNITY_DECLARE_TEX2DARRAY(baseTextures);

		struct Input {
			float3 worldPos;
			float3 worldNormal;
		};

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_CBUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_CBUFFER_END

		float inverseLerp(float a, float b, float value) {
			return saturate((value-a)/(b-a));
		}

		// Display correctly the texture tiled base on normal direction (
		float3 triplanar(float3 wolrdPos, float scale, float3 blendAxes, int textureIdx) {
			float3 scaledWorldPos = wolrdPos /scale;

			float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIdx)) * blendAxes.x;
			float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIdx)) * blendAxes.y;
			float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIdx)) * blendAxes.z;
			return xProjection + yProjection + zProjection;
		}

		void surf (Input IN, inout SurfaceOutputStandard o) {
			float height = IN.worldPos.y / 1200;

			float3 blendAxes = abs(IN.worldNormal);
			blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;

			for (int i = 0; i < baseLayerCount; i++) {
				float drawStrenght = inverseLerp(-baseBlendsDown[i]/2 - epsilon, baseBlendsUp[i]/2, height - baseStartHeights[i]);

				float3 baseColor = baseColors[i] * baseColorsStrenght[i];
				float3 textureColor = triplanar(IN.worldPos, baseTextureScales[i], blendAxes, i) * (1 - baseColorsStrenght[i]);

				o.Albedo = o.Albedo * (1-drawStrenght) + (baseColor + textureColor) * drawStrenght;
			}

		}
		ENDCG
	}
	FallBack "Diffuse"
}
