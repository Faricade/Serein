namespace Serein;

public class SingleTagRenderer(BitTag tag) : Renderer
{
    public BitTag Tag = tag;

    public override void Render(Scene scene)
    {
        PushState();
        foreach (Entity entity in scene[Tag])
            if (entity.Visible)
                entity.Render();

        if (Engine.Commands.Open)
            foreach (Entity entity in scene[Tag])
                entity.DebugRender(Camera);
        PopState();
    }
}