using Foster.Framework;
using static System.Net.Mime.MediaTypeNames;


namespace Serein;

public class OutlineText : Text
{
    public Color OutlineColor = Color.Black;
    public int OutlineOffset = 1;

    public OutlineText(SpriteFont font, string text, Vector2 position, Color color, HorizontalAlign horizontalAlign = HorizontalAlign.Center, VerticalAlign verticalAlign = VerticalAlign.Center)
        : base(font, text, position, color, horizontalAlign, verticalAlign)
    {

    }

    public OutlineText(SpriteFont font, string text, Vector2 position, HorizontalAlign horizontalAlign = HorizontalAlign.Center, VerticalAlign verticalAlign = VerticalAlign.Center)
        : this(font, text, position, Color.White, horizontalAlign, verticalAlign)
    {

    }

    public OutlineText(SpriteFont font, string text)
        : this(font, text, Vector2.Zero, Color.White, HorizontalAlign.Center, VerticalAlign.Center)
    {

    }

    public override void Render()
    {
        for (int i = -1; i < 2; i++)
            for (int j = -1; j < 2; j++)
                if (i != 0 || j != 0)
                {
                    Draw.Batcher.PushMatrix(position: RenderPosition, origin: Origin, scale: Scale, rotation: Rotation);
                    Draw.Batcher.Text(Font, DrawText, Vector2.Zero, Color);
                    Draw.Batcher.PopMatrix();
                }
        base.Render();
    }
}
