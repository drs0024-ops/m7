#ifndef EDITOR_PARALLAX_INCLUDED
#define EDITOR_PARALLAX_INCLUDED

float4 _EditorCameraDelta;
float _IsPlayingState;

void ApplyEditorParallax_float(float3 VertexPosLocal, float ParallaxSpeed, out float3 OutVertexPosLocal)
{
    if (_IsPlayingState > 0.5)
    {
        OutVertexPosLocal = VertexPosLocal;
        return;
    }

    float factor = (100.0 - ParallaxSpeed) / 100.0;
    float2 camDelta = _EditorCameraDelta.xy;
    float zoomRatio = _EditorCameraDelta.w == 0.0 ? 1.0 : _EditorCameraDelta.w;

    float2 offset = (camDelta * factor) / zoomRatio;
    OutVertexPosLocal = float3(VertexPosLocal.x + offset.x, VertexPosLocal.y + offset.y, VertexPosLocal.z);

  
}
#endif