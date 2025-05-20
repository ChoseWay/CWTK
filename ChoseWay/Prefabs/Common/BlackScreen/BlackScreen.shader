// Shader created with Shader Forge v1.38 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.38;sub:START;pass:START;ps:flbk:,iptp:0,cusa:False,bamd:0,cgin:,lico:1,lgpr:1,limd:3,spmd:1,trmd:0,grmd:0,uamb:True,mssp:True,bkdf:False,hqlp:False,rprd:False,enco:False,rmgx:True,imps:True,rpth:0,vtps:1,hqsc:True,nrmq:0,nrsp:0,vomd:1,spxs:False,tesm:0,olmd:1,culm:2,bsrc:3,bdst:7,dpts:6,wrdp:False,dith:0,atcv:False,rfrpo:True,rfrpn:Refraction,coma:15,ufog:False,aust:False,igpj:True,qofs:1,qpre:4,rntp:5,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,stcl:False,atwp:False,stva:128,stmr:255,stmw:255,stcp:6,stps:0,stfa:0,stfz:0,ofsf:0,ofsu:0,f2p0:False,fnsp:False,fnfb:False,fsmp:False;n:type:ShaderForge.SFN_Final,id:9361,x:33209,y:32712,varname:node_9361,prsc:2|emission-4343-RGB,alpha-4338-OUT,voffset-415-OUT;n:type:ShaderForge.SFN_Color,id:4343,x:32773,y:32617,ptovrint:False,ptlb:Coller,ptin:_Coller,varname:node_4343,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:0.5,c2:0.5,c3:0.5,c4:1;n:type:ShaderForge.SFN_TexCoord,id:6221,x:33020,y:33176,varname:node_6221,prsc:2,uv:0,uaff:False;n:type:ShaderForge.SFN_RemapRange,id:415,x:33206,y:33176,varname:node_415,prsc:2,frmn:0,frmx:1,tomn:-1,tomx:1|IN-6221-UVOUT;n:type:ShaderForge.SFN_Distance,id:1999,x:32313,y:32770,varname:node_1999,prsc:2|A-8312-UVOUT,B-2937-OUT;n:type:ShaderForge.SFN_ScreenPos,id:8312,x:31926,y:32794,varname:node_8312,prsc:2,sctp:0;n:type:ShaderForge.SFN_Vector2,id:2937,x:32052,y:32966,varname:node_2937,prsc:2,v1:0,v2:0;n:type:ShaderForge.SFN_RemapRange,id:5430,x:32490,y:32770,varname:node_5430,prsc:2,frmn:0,frmx:2,tomn:1,tomx:0|IN-1999-OUT;n:type:ShaderForge.SFN_Multiply,id:5407,x:32685,y:32803,varname:node_5407,prsc:2|A-5430-OUT,B-6966-OUT;n:type:ShaderForge.SFN_Slider,id:7876,x:32206,y:32975,ptovrint:False,ptlb:Range,ptin:_Range,varname:node_7876,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:0,max:1;n:type:ShaderForge.SFN_RemapRange,id:6966,x:32376,y:33054,varname:node_6966,prsc:2,frmn:0,frmx:1,tomn:1,tomx:0|IN-7876-OUT;n:type:ShaderForge.SFN_Smoothstep,id:208,x:32890,y:32897,varname:node_208,prsc:2|A-7541-OUT,B-7312-OUT,V-5407-OUT;n:type:ShaderForge.SFN_Add,id:7312,x:32610,y:33048,varname:node_7312,prsc:2|A-7876-OUT,B-2511-OUT;n:type:ShaderForge.SFN_Subtract,id:7541,x:32610,y:33188,varname:node_7541,prsc:2|A-7876-OUT,B-2511-OUT;n:type:ShaderForge.SFN_Slider,id:2511,x:32150,y:33245,ptovrint:False,ptlb:Smooth,ptin:_Smooth,varname:node_2511,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:0,max:1;n:type:ShaderForge.SFN_OneMinus,id:4338,x:32930,y:33035,varname:node_4338,prsc:2|IN-208-OUT;proporder:4343-7876-2511;pass:END;sub:END;*/

Shader "/Custom/BlackScreen" {
    Properties {
        _Coller ("Coller", Color) = (0.5,0.5,0.5,1)
        _Range ("Range", Range(0, 1)) = 0
        _Smooth ("Smooth", Range(0, 1)) = 0
        [HideInInspector]_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
    }
    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "Queue"="Overlay+1"
            "RenderType"="Overlay"
        }
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZTest Always
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #include "UnityPBSLighting.cginc"
            #include "UnityStandardBRDF.cginc"
            #pragma multi_compile_fwdbase
            #pragma only_renderers d3d9 d3d11 glcore gles gles3 
            #pragma target 3.0
            uniform float4 _Coller;
            uniform float _Range;
            uniform float _Smooth;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
                float4 projPos : TEXCOORD3;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.normalDir = UnityObjectToWorldNormal(v.normal);
                v.vertex.xyz = float3((o.uv0*2.0+-1.0),0.0);
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                o.pos = v.vertex;
                o.projPos = ComputeScreenPos (o.pos);
                COMPUTE_EYEDEPTH(o.projPos.z);
                return o;
            }
            float4 frag(VertexOutput i, float facing : VFACE) : COLOR {
                float isFrontFace = ( facing >= 0 ? 1 : 0 );
                float faceSign = ( facing >= 0 ? 1 : -1 );
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float3 normalDirection = i.normalDir;
                float3 viewReflectDirection = reflect( -viewDirection, normalDirection );
                float2 sceneUVs = (i.projPos.xy / i.projPos.w);
////// Lighting:
////// Emissive:
                float3 emissive = _Coller.rgb;
                float3 finalColor = emissive;
                return fixed4(finalColor,(1.0 - smoothstep( (_Range-_Smooth), (_Range+_Smooth), ((distance((sceneUVs * 2 - 1).rg,float2(0,0))*-0.5+1.0)*(_Range*-1.0+1.0)) )));
            }
            ENDCG
        }
        Pass {
            Name "ShadowCaster"
            Tags {
                "LightMode"="ShadowCaster"
            }
            Offset 1, 1
            Cull Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_SHADOWCASTER
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "UnityPBSLighting.cginc"
            #include "UnityStandardBRDF.cginc"
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma multi_compile_shadowcaster
            #pragma only_renderers d3d9 d3d11 glcore gles gles3 
            #pragma target 3.0
            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                V2F_SHADOW_CASTER;
                float2 uv0 : TEXCOORD1;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                v.vertex.xyz = float3((o.uv0*2.0+-1.0),0.0);
                o.pos = v.vertex;
                TRANSFER_SHADOW_CASTER(o)
                return o;
            }
            float4 frag(VertexOutput i, float facing : VFACE) : COLOR {
                float isFrontFace = ( facing >= 0 ? 1 : 0 );
                float faceSign = ( facing >= 0 ? 1 : -1 );
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
