using Foster.Framework;

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Serein;

public class MTexture
{
    static public MTexture FromFile(string filename)
    {
        var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
        using var image = new Foster.Framework.Image(fileStream);
        var texture = new Texture(Engine.Instance.GraphicsDevice, image);
        fileStream.Close();

        return new MTexture(texture);
    }

    public MTexture() { }

    public MTexture(Texture texture)
    {
        Texture = texture;
        AtlasPath = null;
        ClipRect = new RectInt(0, 0, Texture.Width, Texture.Height);
        DrawOffset = Vector2.Zero;
        Width = ClipRect.Width;
        Height = ClipRect.Height;
        SetUtil();
    }

    public MTexture(MTexture parent, int x, int y, int width, int height)
    {
        Texture = parent.Texture;
        AtlasPath = null;

        ClipRect = parent.GetRelativeRect(x, y, width, height);
        DrawOffset = new Vector2(-Math.Min(x - parent.DrawOffset.X, 0), -Math.Min(y - parent.DrawOffset.Y, 0));
        Width = width;
        Height = height;
        SetUtil();
    }

    public MTexture(MTexture parent, RectInt clipRect)
        : this(parent, clipRect.X, clipRect.Y, clipRect.Width, clipRect.Height)
    {

    }

    public MTexture(MTexture parent, string atlasPath, RectInt clipRect, Vector2 drawOffset, int width, int height)
    {
        Texture = parent.Texture;
        AtlasPath = atlasPath;

        ClipRect = parent.GetRelativeRect(clipRect);
        DrawOffset = drawOffset;
        Width = width;
        Height = height;
        SetUtil();
    }

    public MTexture(MTexture parent, string atlasPath, RectInt clipRect)
        : this(parent, clipRect)
    {
        AtlasPath = atlasPath;
    }

    public MTexture(Texture texture, Vector2 drawOffset, int frameWidth, int frameHeight)
    {
        Texture = texture;
        ClipRect = new RectInt(0, 0, texture.Width, texture.Height);
        DrawOffset = drawOffset;
        Width = frameWidth;
        Height = frameHeight;
        SetUtil();
    }

    public MTexture(int width, int height, Color color)
    {
        Texture = new Texture(Engine.Instance.GraphicsDevice, width, height);
        var colors = new Color[width * height];
        for (int i = 0; i < width * height; i++)
            colors[i] = color;
        Texture.SetData<Color>(colors);

        ClipRect = new RectInt(0, 0, width, height);
        DrawOffset = Vector2.Zero;
        Width = width;
        Height = height;
        SetUtil();
    }

    private void SetUtil()
    {
        Center = new Vector2(Width, Height) * 0.5f;
        LeftUV = ClipRect.Left / (float)Texture.Width;
        RightUV = ClipRect.Right / (float)Texture.Width;
        TopUV = ClipRect.Top / (float)Texture.Height;
        BottomUV = ClipRect.Bottom / (float)Texture.Height;
    }

    public void Unload()
    {
        Texture.Dispose();
        Texture = null;
    }
   
    public MTexture GetSubtexture(int x, int y, int width, int height, MTexture applyTo = null)
    {
        if (applyTo == null)
            return new MTexture(this, x, y, width, height);
        else
        {
            applyTo.Texture = Texture;
            applyTo.AtlasPath = null;

            applyTo.ClipRect = GetRelativeRect(x, y, width, height);
            applyTo.DrawOffset = new Vector2(-Math.Min(x - DrawOffset.X, 0), -Math.Min(y - DrawOffset.Y, 0));
            applyTo.Width = width;
            applyTo.Height = height;
            applyTo.SetUtil();

            return applyTo;
        }
    }

    public MTexture GetSubtexture(RectInt rect)
    {
        return new MTexture(this, rect);
    }

    public void Dispose()
    {
        Texture.Dispose();
    }

    #region Properties

    public Texture Texture { get; private set; }
    public RectInt ClipRect { get; private set; }
    public string AtlasPath { get; private set; }
    public Vector2 DrawOffset { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public Vector2 Center { get; private set; }
    public float LeftUV { get; private set; }
    public float RightUV { get; private set; }
    public float TopUV { get; private set; }
    public float BottomUV { get; private set; }

    #endregion

    #region Helpers

    public override string ToString()
    {
        if (AtlasPath != null)
            return AtlasPath;
        else
            return "MTexture [" + Texture.Width + " x " + Texture.Height + "]";
    }

    public RectInt GetRelativeRect(RectInt rect)
    {
        return GetRelativeRect(rect.X, rect.Y, rect.Width, rect.Height);
    }

    public RectInt GetRelativeRect(int x, int y, int width, int height)
    {
        int atX = (int)(ClipRect.X - DrawOffset.X + x);
        int atY = (int)(ClipRect.Y - DrawOffset.Y + y);

        int rX = (int)Foster.Framework.Calc.Clamp(atX, ClipRect.Left, ClipRect.Right);
        int rY = (int)Foster.Framework.Calc.Clamp(atY, ClipRect.Top, ClipRect.Bottom);
        int rW = Math.Max(0, Math.Min(atX + width, ClipRect.Right) - rX);
        int rH = Math.Max(0, Math.Min(atY + height, ClipRect.Bottom) - rY);

        return new RectInt(rX, rY, rW, rH);
    }
    

    public int TotalPixels
    {
        get { return Width * Height; }
    }

    #endregion

    #region Draw

    public void Draw(Vector2 position)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif
        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, -DrawOffset, Vector2.One, 0, Color.White);
    }

    public void Draw(Vector2 position, Vector2 origin)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif
        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, origin - DrawOffset, Vector2.One, 0, Color.White);
    }

    public void Draw(Vector2 position, Vector2 origin, Color color)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif
        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, origin - DrawOffset, Vector2.One, 0, color);
    }

    public void Draw(Vector2 position, Vector2 origin, Color color, float scale)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif
        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, origin - DrawOffset, new Vector2(scale, scale), 0, color);
    }

    public void Draw(Vector2 position, Vector2 origin, Color color, float scale, float rotation)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif
        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, origin - DrawOffset, new Vector2(scale, scale), rotation, color);
    }

    public void Draw(Vector2 position, Vector2 origin, Color color, Vector2 scale)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif
        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, origin - DrawOffset, scale, 0, color);
    }

    public void Draw(Vector2 position, Vector2 origin, Color color, Vector2 scale, float rotation)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif
        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, origin - DrawOffset, scale, rotation, color);
    }

    public void Draw(Vector2 position, Vector2 origin, Color color, Vector2 scale, float rotation, RectInt clip)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif
        Serein.Draw.Batcher.Image(new Subtexture(Texture, GetRelativeRect(clip)), position, origin - DrawOffset, scale, rotation, color);
    }

    #endregion

    #region Draw Centered

    public void DrawCentered(Vector2 position)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif
        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, Center - DrawOffset, Vector2.One, 0, Color.White);
    }

    public void DrawCentered(Vector2 position, Color color)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif
        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, Center - DrawOffset, Vector2.One, 0, color);
    }

    public void DrawCentered(Vector2 position, Color color, float scale)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif
        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, Center - DrawOffset, new Vector2(scale, scale), 0, color);
    }

    public void DrawCentered(Vector2 position, Color color, float scale, float rotation)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif
        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, Center - DrawOffset, new Vector2(scale, scale), rotation, color);
    }

    public void DrawCentered(Vector2 position, Color color, Vector2 scale)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif
        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, Center - DrawOffset, scale, 0, color);
    }

    public void DrawCentered(Vector2 position, Color color, Vector2 scale, float rotation)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif
        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, Center - DrawOffset, scale, rotation, color);
    }

    #endregion

    #region Draw Justified

    public void DrawJustified(Vector2 position, Vector2 justify)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif
        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, Vector2.One, 0, Color.White);
    }

    public void DrawJustified(Vector2 position, Vector2 justify, Color color)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif
        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, Vector2.One, 0, color);
    }

    public void DrawJustified(Vector2 position, Vector2 justify, Color color, float scale)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif
        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, new Vector2(scale, scale), 0, color);
    }

    public void DrawJustified(Vector2 position, Vector2 justify, Color color, float scale, float rotation)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif
        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, new Vector2(scale, scale), rotation, color);
    }

    public void DrawJustified(Vector2 position, Vector2 justify, Color color, Vector2 scale)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif
        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, 0, color);
    }

    public void DrawJustified(Vector2 position, Vector2 justify, Color color, Vector2 scale, float rotation)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif
        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, rotation, color);
    }

    #endregion

    #region Draw Outline

    public void DrawOutline(Vector2 position)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif

        for (var i = -1; i <= 1; i++)
            for (var j = -1; j <= 1; j++)
                if (i != 0 || j != 0)
                    Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position + new Vector2(i, j), -DrawOffset, Vector2.One, 0, Color.Black);

        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, -DrawOffset, Vector2.One, 0, Color.White);
    }

    public void DrawOutline(Vector2 position, Vector2 origin)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif

        for (var i = -1; i <= 1; i++)
            for (var j = -1; j <= 1; j++)
                if (i != 0 || j != 0)
                    Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position + new Vector2(i, j), origin - DrawOffset, Vector2.One, 0, Color.Black);

        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, origin - DrawOffset, Vector2.One, 0, Color.White);
    }

    public void DrawOutline(Vector2 position, Vector2 origin, Color color)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif

        for (var i = -1; i <= 1; i++)
            for (var j = -1; j <= 1; j++)
                if (i != 0 || j != 0)
                    Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position + new Vector2(i, j), origin - DrawOffset, Vector2.One, 0, Color.Black);

        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, origin - DrawOffset, Vector2.One, 0, color);
    }

    public void DrawOutline(Vector2 position, Vector2 origin, Color color, float scale)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif

        for (var i = -1; i <= 1; i++)
            for (var j = -1; j <= 1; j++)
                if (i != 0 || j != 0)
                    Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position + new Vector2(i, j), origin - DrawOffset, new Vector2(scale, scale), 0, Color.Black);

        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, origin - DrawOffset, new Vector2(scale, scale), 0, color);
    }

    public void DrawOutline(Vector2 position, Vector2 origin, Color color, float scale, float rotation)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif

        for (var i = -1; i <= 1; i++)
            for (var j = -1; j <= 1; j++)
                if (i != 0 || j != 0)
                    Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position + new Vector2(i, j), origin - DrawOffset, new Vector2(scale, scale), rotation, Color.Black);

        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, origin - DrawOffset, new Vector2(scale, scale), rotation, color);
    }

    public void DrawOutline(Vector2 position, Vector2 origin, Color color, Vector2 scale)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif

        for (var i = -1; i <= 1; i++)
            for (var j = -1; j <= 1; j++)
                if (i != 0 || j != 0)
                    Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position + new Vector2(i, j), origin - DrawOffset, scale, 0, Color.Black);

        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, origin - DrawOffset, scale, 0, color);
    }

    public void DrawOutline(Vector2 position, Vector2 origin, Color color, Vector2 scale, float rotation)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif

        for (var i = -1; i <= 1; i++)
            for (var j = -1; j <= 1; j++)
                if (i != 0 || j != 0)
                    Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position + new Vector2(i, j), origin - DrawOffset, scale, rotation, Color.Black);

        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, origin - DrawOffset, scale, rotation, color);
    }

    #endregion

    #region Draw Outline Centered

    public void DrawOutlineCentered(Vector2 position)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif

        for (var i = -1; i <= 1; i++)
            for (var j = -1; j <= 1; j++)
                if (i != 0 || j != 0)
                    Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position + new Vector2(i, j), Center - DrawOffset, Vector2.One, 0, Color.Black);

        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, Center - DrawOffset, Vector2.One, 0, Color.White);
    }

    public void DrawOutlineCentered(Vector2 position, Color color)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif

        for (var i = -1; i <= 1; i++)
            for (var j = -1; j <= 1; j++)
                if (i != 0 || j != 0)
                    Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position + new Vector2(i, j), Center - DrawOffset, Vector2.One, 0, Color.Black);

        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, Center - DrawOffset, Vector2.One, 0, color);
    }

    public void DrawOutlineCentered(Vector2 position, Color color, float scale)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif

        for (var i = -1; i <= 1; i++)
            for (var j = -1; j <= 1; j++)
                if (i != 0 || j != 0)
                    Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position + new Vector2(i, j), Center - DrawOffset, new Vector2(scale, scale), 0, Color.Black);

        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, Center - DrawOffset, new Vector2(scale, scale), 0, color);
    }

    public void DrawOutlineCentered(Vector2 position, Color color, float scale, float rotation)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif

        for (var i = -1; i <= 1; i++)
            for (var j = -1; j <= 1; j++)
                if (i != 0 || j != 0)
                    Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position + new Vector2(i, j), Center - DrawOffset, new Vector2(scale, scale), rotation, Color.Black);

        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, Center - DrawOffset, new Vector2(scale, scale), rotation, color);
    }

    public void DrawOutlineCentered(Vector2 position, Color color, Vector2 scale)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif

        for (var i = -1; i <= 1; i++)
            for (var j = -1; j <= 1; j++)
                if (i != 0 || j != 0)
                    Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position + new Vector2(i, j), Center - DrawOffset, scale, 0, Color.Black);

        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, Center - DrawOffset, scale, 0, color);
    }

    public void DrawOutlineCentered(Vector2 position, Color color, Vector2 scale, float rotation)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif

        for (var i = -1; i <= 1; i++)
            for (var j = -1; j <= 1; j++)
                if (i != 0 || j != 0)
                    Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position + new Vector2(i, j), Center - DrawOffset, scale, rotation, Color.Black);

        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, Center - DrawOffset, scale, rotation, color);
    }

    #endregion

    #region Draw Outline Justified

    public void DrawOutlineJustified(Vector2 position, Vector2 justify)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif

        for (var i = -1; i <= 1; i++)
            for (var j = -1; j <= 1; j++)
                if (i != 0 || j != 0)
                    Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position + new Vector2(i, j), new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, Vector2.One, 0, Color.Black);

        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, Vector2.One, 0, Color.White);
    }

    public void DrawOutlineJustified(Vector2 position, Vector2 justify, Color color)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif

        for (var i = -1; i <= 1; i++)
            for (var j = -1; j <= 1; j++)
                if (i != 0 || j != 0)
                    Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position + new Vector2(i, j), new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, Vector2.One, 0, Color.Black);

        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, Vector2.One, 0, color);
    }

    public void DrawOutlineJustified(Vector2 position, Vector2 justify, Color color, float scale)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif

        for (var i = -1; i <= 1; i++)
            for (var j = -1; j <= 1; j++)
                if (i != 0 || j != 0)
                    Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position + new Vector2(i, j), new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, new Vector2(scale, scale), 0, Color.Black);

        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, new Vector2(scale, scale), 0, color);
    }

    public void DrawOutlineJustified(Vector2 position, Vector2 justify, Color color, float scale, float rotation)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif

        for (var i = -1; i <= 1; i++)
            for (var j = -1; j <= 1; j++)
                if (i != 0 || j != 0)
                    Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position + new Vector2(i, j), new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, new Vector2(scale, scale), rotation, Color.Black);

        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, new Vector2(scale, scale), rotation, color);
    }

    public void DrawOutlineJustified(Vector2 position, Vector2 justify, Color color, Vector2 scale)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif

        for (var i = -1; i <= 1; i++)
            for (var j = -1; j <= 1; j++)
                if (i != 0 || j != 0)
                    Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position + new Vector2(i, j), new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, 0, Color.Black);

        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, 0, color);
    }

    public void DrawOutlineJustified(Vector2 position, Vector2 justify, Color color, Vector2 scale, float rotation)
    {
#if DEBUG
        if (Texture.IsDisposed)
            throw new Exception("Texture Is Disposed");
#endif

        for (var i = -1; i <= 1; i++)
            for (var j = -1; j <= 1; j++)
                if (i != 0 || j != 0)
                    Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position + new Vector2(i, j), new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, rotation, Color.Black);

        Serein.Draw.Batcher.Image(new Subtexture(Texture, ClipRect), position, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, rotation, color);
    }

    #endregion
}
