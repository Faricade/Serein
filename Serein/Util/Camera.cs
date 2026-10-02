using Foster.Framework;

namespace Serein;

public class Camera
{
    private Matrix3x2 matrix = Matrix3x2.Identity;
    private Matrix3x2 inverse = Matrix3x2.Identity;
    private bool changed;

    private Vector2 position = Vector2.Zero;
    private Vector2 zoom = Vector2.One;
    private Vector2 origin = Vector2.Zero;
    private float angle = 0;

    public RectInt Viewport;

    public Camera()
    {
        Viewport = new(Engine.GameWidth, Engine.GameHeight);
        UpdateMatrices();
    }

    public Camera(int width, int height)
    {
        Viewport = new(width, height);
        UpdateMatrices();
    }

    public override string ToString()
    {
        return "Camera:\n\tViewport: { " + Viewport.X + ", " + Viewport.Y + ", " + Viewport.Width + ", " + Viewport.Height +
            " }\n\tPosition: { " + position.X + ", " + position.Y +
            " }\n\tOrigin: { " + origin.X + ", " + origin.Y +
            " }\n\tZoom: { " + zoom.X + ", " + zoom.Y +
            " }\n\tAngle: " + angle;
    }

    private void UpdateMatrices()
    {
        matrix = Matrix3x2.CreateTranslation(-MathF.Floor(position.X), -MathF.Floor(position.Y))
               * Matrix3x2.CreateRotation(angle)
               * Matrix3x2.CreateScale(zoom)
               * Matrix3x2.CreateTranslation(MathF.Floor(origin.X), MathF.Floor(origin.Y));

        Matrix3x2.Invert(matrix, out inverse);

        changed = false;
    }

    public void CopyFrom(Camera other)
    {
        position = other.position;
        origin = other.origin;
        angle = other.angle;
        zoom = other.zoom;
        changed = true;
    }

    public Matrix3x2 Matrix
    {
        get
        {
            if (changed)
                UpdateMatrices();
            return matrix;
        }
    }

    public Matrix3x2 Inverse
    {
        get
        {
            if (changed)
                UpdateMatrices();
            return inverse;
        }
    }

    public Vector2 Position
    {
        get { return position; }
        set
        {
            changed = true;
            position = value;
        }
    }

    public Vector2 Origin
    {
        get { return origin; }
        set
        {
            changed = true;
            origin = value;
        }
    }

    public float X
    {
        get { return position.X; }
        set
        {
            changed = true;
            position.X = value;
        }
    }

    public float Y
    {
        get { return position.Y; }
        set
        {
            changed = true;
            position.Y = value;
        }
    }

    public float Zoom
    {
        get { return zoom.X; }
        set
        {
            changed = true;
            zoom.X = zoom.Y = value;
        }
    }

    public float Angle
    {
        get { return angle; }
        set
        {
            changed = true;
            angle = value;
        }
    }

    public float Left
    {
        get
        {
            if (changed)
                UpdateMatrices();
            return Vector2.Transform(Vector2.Zero, Inverse).X;
        }

        set
        {
            if (changed)
                UpdateMatrices();
            X = Vector2.Transform(Vector2.UnitX * value, Matrix).X;
        }
    }

    public float Right
    {
        get
        {
            if (changed)
                UpdateMatrices();
            return Vector2.Transform(Vector2.UnitX * Viewport.Width, Inverse).X;
        }

        set
        {
            throw new NotImplementedException();
        }
    }

    public float Top
    {
        get
        {
            if (changed)
                UpdateMatrices();
            return Vector2.Transform(Vector2.Zero, Inverse).Y;
        }

        set
        {
            if (changed)
                UpdateMatrices();
            Y = Vector2.Transform(Vector2.UnitY * value, Matrix).Y;
        }
    }

    public float Bottom
    {
        get
        {
            if (changed)
                UpdateMatrices();
            return Vector2.Transform(Vector2.UnitY * Viewport.Height, Inverse).Y;
        }

        set
        {
            throw new NotImplementedException();
        }
    }

    /*
     *  Utils
     */

    public void CenterOrigin()
    {
        origin = new Vector2((float)Viewport.Width / 2, (float)Viewport.Height / 2);
        changed = true;
    }

    public void RoundPosition()
    {
        position.X = (float)Math.Round(position.X);
        position.Y = (float)Math.Round(position.Y);
        changed = true;
    }

    public Vector2 ScreenToCamera(Vector2 position)
    {
        return Vector2.Transform(position, Inverse);
    }

    public Vector2 CameraToScreen(Vector2 position)
    {
        return Vector2.Transform(position, Matrix);
    }

    public void Approach(Vector2 position, float ease)
    {
        Position += (position - Position) * ease;
    }

    public void Approach(Vector2 position, float ease, float maxDistance)
    {
        Vector2 move = (position - Position) * ease;
        if (move.Length() > maxDistance)
            Position += Vector2.Normalize(move) * maxDistance;
        else
            Position += move;
    }
}