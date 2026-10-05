namespace Serein;

public static class MInput
{
    public static Input GameInput { get; private set; } = null!;

    public static KeyboardState Keyboard => GameInput.Keyboard;
    public static MouseState Mouse => GameInput.Mouse;
    public static ControllerState[] GamePads => GameInput.Controllers;

    /// <summary>
    /// Set to true to suppress all game input (in addition to the automatic suppression while the window is inactive or the console is open).
    /// Foster keeps tracking the real devices underneath, so inputs still held when this is turned off again are immediately seen as held.
    /// </summary>
    public static bool Disabled = false;

    /// <summary>
    /// True while game input is being suppressed (inactive window, open console, <see cref="Disabled"/>)
    /// </summary>
    public static bool IsSuppressed { get; private set; }

    /// <summary>
    /// The Mouse position in game/screen space in Pixel Coordinates.
    /// This is the live position and is not affected by suppression.
    /// </summary>
    public static Vector2 MousePosition
    {
        get
        {
            if (!Matrix3x2.Invert(Engine.ScreenMatrix, out var inverse))
                return Vector2.Zero;
            return Vector2.Transform(source.Mouse.Position, inverse);
        }
    }

    private static Input source = null!;
    private static readonly float[] rumbleStrength = new float[InputState.MaxControllers];
    private static readonly float[] rumbleTime = new float[InputState.MaxControllers];

    /// <param name="input">Foster's main Input module (the one the application receives events on)</param>
    internal static void Initialize(Input input)
    {
        source = input;
        GameInput = input.CreateEcho();
        IsSuppressed = false;
        Array.Clear(rumbleStrength);
        Array.Clear(rumbleTime);
    }

    internal static void Shutdown()
    {
        if (GameInput == null)
            return;

        for (int i = 0; i < rumbleTime.Length; i++)
            StopRumble(i);
    }

    internal static void Update()
        => Apply(Disabled || Engine.Instance.Windows.All(x => !x.Focused) || Engine.Commands.Open);

    /// <summary>
    /// Use to suppress game input for this frame
    /// </summary>
    public static void UpdateNull()
        => Apply(true);

    private static void Apply(bool suppress)
    {
        IsSuppressed = suppress;

        for (int i = 0; i < rumbleTime.Length; i++)
            if (rumbleTime[i] > 0)
            {
                if (suppress)
                    StopRumble(i);
                else
                    rumbleTime[i] -= Engine.Instance.Time.Delta;
            }

        if (!suppress)
            return;

        // Only the stepped State is cleared. Foster keeps tracking the real devices underneath, so
        // anything still held when suppression ends shows up as held again (but not as a new press).
        GameInput.State.Clear();

        // Virtual inputs were already updated by Foster's step, from the state we just cleared
        var virtualInputs = GameInput.VirtualInputs;
        for (int i = 0; i < virtualInputs.Count; i++)
        {
            if (!virtualInputs[i].TryGetTarget(out var virtualInput))
                continue;

            switch (virtualInput)
            {
                case VirtualAction action: action.Clear(); break;
                case VirtualAxis axis: axis.Clear(); break;
                case VirtualStick stick: stick.Clear(); break;
            }
        }
    }

    #region Rumble

    /// <summary>
    /// Rumbles the controller in the given slot. Foster handles the duration; this adds Monocle's priority rule:
    /// a new rumble only replaces the current one if that has finished, or the new one is stronger (or equal and longer).
    /// </summary>
    public static void Rumble(int gamepadIndex, float strength, float time)
    {
        if (IsSuppressed || (uint)gamepadIndex >= (uint)rumbleTime.Length)
            return;

        if (rumbleTime[gamepadIndex] <= 0 ||
            strength > rumbleStrength[gamepadIndex] ||
            (strength == rumbleStrength[gamepadIndex] && time > rumbleTime[gamepadIndex]))
        {
            GameInput.Rumble(gamepadIndex, strength, time);
            rumbleStrength[gamepadIndex] = strength;
            rumbleTime[gamepadIndex] = time;
        }
    }

    public static void RumbleFirst(float strength, float time)
        => Rumble(0, strength, time);

    public static void StopRumble(int gamepadIndex)
    {
        if ((uint)gamepadIndex >= (uint)rumbleTime.Length)
            return;

        GameInput.Rumble(gamepadIndex, 0f, 0f);
        rumbleStrength[gamepadIndex] = 0;
        rumbleTime[gamepadIndex] = 0;
    }

    #endregion

    #region Helpers

    public static int Axis(bool negative, bool positive, int bothValue)
    {
        if (negative)
        {
            if (positive)
                return bothValue;
            else
                return -1;
        }
        else if (positive)
            return 1;
        else
            return 0;
    }

    public static int Axis(float axisValue, float deadzone)
    {
        if (MathF.Abs(axisValue) >= deadzone)
            return MathF.Sign(axisValue);
        else
            return 0;
    }

    public static int Axis(bool negative, bool positive, int bothValue, float axisValue, float deadzone)
    {
        int ret = Axis(axisValue, deadzone);
        if (ret == 0)
            ret = Axis(negative, positive, bothValue);
        return ret;
    }

    #endregion
}