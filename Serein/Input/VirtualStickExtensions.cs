namespace Serein;

public static class VirtualStickExtensions
{
    /// <summary>
    /// Foster's <see cref="VirtualStick"/> reports the raw stick value. This applies the post-processing that
    /// Monocle's VirtualJoystick had: optionally normalizing it, and/or snapping it to a number of slices.
    /// </summary>
    public static Vector2 GetValue(this VirtualStick stick, bool normalized, float? snapSlices = null)
    {
        var value = stick.Value;
        if (value == Vector2.Zero)
            return value;

        if (normalized)
        {
            if (snapSlices.HasValue)
                return value.SnappedNormal(snapSlices.Value);
            return Vector2.Normalize(value);
        }

        if (snapSlices.HasValue)
            return value.Snapped(snapSlices.Value);

        return value;
    }
}