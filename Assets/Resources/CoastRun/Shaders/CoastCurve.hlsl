#ifndef COAST_CURVE_INCLUDED
#define COAST_CURVE_INCLUDED

// Curved-world bend shared by every Coast Run shader.
//
// The run itself stays a straight line along +Z — lanes, hit tests and spawn
// planning never change. What bends is the picture: every vertex ahead of the
// player is pushed sideways (and up/down) by curvature × distance², so the road
// visibly sweeps left, straightens, then sweeps right as CurveDirector drives
// the globals. This is the same trick endless runners have used since Subway
// Surfers; it costs one multiply-add per vertex and needs no bent geometry.
//
//   _CoastCurve.x   lateral curvature  (m per m²)   + = bends right
//   _CoastCurve.y   vertical curvature (m per m²)   + = climbs
//   _CoastCurve.z   origin Z: bend starts here (the player)
//   _CoastCurve.w   max distance the bend keeps growing (clamps far geometry)
//   _CurveWeight    per-material multiplier — 0 pins the sky / clouds in place

float4 _CoastCurve;
// 146차(마을 디오라마): 방사형 곡면 — 주인공(origin)에서 멀어질수록 사방이 아래로 휜다(동물의 숲 「둥근 지구」).
//   _CoastCurveRadial.x  곡률(m per m²)  .y/.z  원점 XZ  .w  최대 거리(클램프)
float4 _CoastCurveRadial;

float3 CoastCurveWorld(float3 positionWS, float weight)
{
    float dz = clamp(positionWS.z - _CoastCurve.z, 0.0, _CoastCurve.w);
    float d2 = dz * dz * weight;
    positionWS.x += _CoastCurve.x * d2;
    positionWS.y += _CoastCurve.y * d2;
    float dr = clamp(length(positionWS.xz - _CoastCurveRadial.yz), 0.0, _CoastCurveRadial.w);
    positionWS.y -= _CoastCurveRadial.x * dr * dr * weight;
    return positionWS;
}

#endif
