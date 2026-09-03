using System;
using System.Collections.Generic;
using UnityEngine;

/// A corner the player is about to reach.
public struct TurnPrompt
{
    public int Direction;
    public float WindowStart;
    public float TurnDistance;
    public Vector3 CornerCenter;
}

/// Ordered list of live track tiles plus the spawn cursor. Straights are axis
/// aligned; corners use a smooth Bezier so the runner arcs like Temple Run.
public class TrackPath
{
    private readonly List<TrackSegment> _active = new List<TrackSegment>();

    public Vector3 CursorPosition { get; private set; }
    public float CursorYaw { get; private set; }
    public float CursorDistance { get; private set; }

    public int Count => _active.Count;
    public TrackSegment Oldest => _active.Count > 0 ? _active[0] : null;

    public void Reset(Vector3 origin, float yaw)
    {
        _active.Clear();
        CursorPosition = origin;
        CursorYaw = yaw;
        CursorDistance = 0f;
    }

    /// Snaps the tile onto the cursor and advances the cursor past its exit.
    public void Place(TrackSegment segment)
    {
        if (segment == null)
            return;

        Quaternion rot = Quaternion.Euler(0f, CursorYaw, 0f);
        segment.transform.SetPositionAndRotation(CursorPosition, rot);
        segment.PathStart = CursorDistance;
        _active.Add(segment);

        CursorPosition += rot * segment.ExitLocalPosition;
        CursorYaw = Mathf.Repeat(CursorYaw + segment.ExitYawDelta, 360f);
        CursorDistance += segment.PathLength;
    }

    public TrackSegment RemoveOldest()
    {
        if (_active.Count == 0)
            return null;

        TrackSegment segment = _active[0];
        _active.RemoveAt(0);
        return segment;
    }

    public void ForEach(Action<TrackSegment> visitor)
    {
        if (visitor == null)
            return;

        for (int i = 0; i < _active.Count; i++)
        {
            if (_active[i] != null)
                visitor(_active[i]);
        }
    }

    public TrackSegment SegmentAt(float distance)
    {
        for (int i = 0; i < _active.Count; i++)
        {
            TrackSegment s = _active[i];
            if (s != null && distance >= s.PathStart && distance <= s.PathEnd)
                return s;
        }

        return null;
    }

    /// The first corner the player has not fully passed yet.
    public bool TryGetTurn(float pathDistance, out TurnPrompt prompt)
    {
        for (int i = 0; i < _active.Count; i++)
        {
            TrackSegment s = _active[i];
            if (s == null || !s.IsCorner || s.PathEnd < pathDistance)
                continue;

            prompt = new TurnPrompt
            {
                Direction = s.TurnDirection,
                WindowStart = s.PathStart,
                TurnDistance = s.TurnDistance,
                CornerCenter = s.CornerCenterWorld
            };
            return true;
        }

        prompt = default;
        return false;
    }

    /// World pose of the centre line at a given path distance.
    public bool TryGetPoint(float distance, out Vector3 position, out float yaw)
    {
        for (int i = 0; i < _active.Count; i++)
        {
            TrackSegment s = _active[i];
            if (s == null || distance < s.PathStart || distance > s.PathEnd)
                continue;

            float local = distance - s.PathStart;
            float segmentYaw = s.transform.eulerAngles.y;

            if (!s.IsCorner)
            {
                position = s.transform.TransformPoint(new Vector3(0f, 0f, local));
                yaw = segmentYaw;
                return true;
            }

            float arm = s.PathLength * 0.5f;
            if (local <= arm)
            {
                position = s.transform.TransformPoint(new Vector3(0f, 0f, local));
                yaw = segmentYaw;
                return true;
            }

            float exitYaw = Mathf.Repeat(segmentYaw + s.ExitYawDelta, 360f);
            Quaternion segRot = Quaternion.Euler(0f, segmentYaw, 0f);
            Vector3 forward = segRot * Vector3.forward;
            Vector3 exitDir = Quaternion.Euler(0f, exitYaw, 0f) * Vector3.forward;
            Vector3 corner = s.CornerCenterWorld;
            Vector3 exit = corner + exitDir * arm;

            float t = Mathf.Clamp01((local - arm) / arm);
            float u = 1f - t;
            Vector3 p1 = corner + forward * (arm * 0.55f);
            Vector3 p2 = exit - exitDir * (arm * 0.55f);
            position = u * u * u * corner
                + 3f * u * u * t * p1
                + 3f * u * t * t * p2
                + t * t * t * exit;

            float yawT = t * t * (3f - 2f * t);
            yaw = Mathf.LerpAngle(segmentYaw, exitYaw, yawT);
            return true;
        }

        position = Vector3.zero;
        yaw = 0f;
        return false;
    }
}
