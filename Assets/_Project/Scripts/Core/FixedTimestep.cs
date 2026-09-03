using UnityEngine;

/// Accumulates real delta and drains fixed steps so simulation stays stable
/// when the frame rate dips (which would otherwise shrink telegraph windows).
public sealed class FixedTimestep
{
    private readonly float _step;
    private float _accumulator;

    public float Step => _step;
    public float Alpha => _step > 0f ? _accumulator / _step : 0f;

    public FixedTimestep(float step = 1f / 60f)
    {
        _step = Mathf.Max(1f / 120f, step);
    }

    public int Consume(float deltaTime)
    {
        _accumulator += Mathf.Min(deltaTime, _step * 5f);
        int steps = 0;
        while (_accumulator >= _step)
        {
            _accumulator -= _step;
            steps++;
        }

        return steps;
    }

    public void Reset()
    {
        _accumulator = 0f;
    }
}
