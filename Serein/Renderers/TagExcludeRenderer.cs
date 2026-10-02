namespace Serein;

public class TagExcludeRenderer(int excludeTag) : Renderer
{
    public int ExcludeTag = excludeTag;

    public override void Render(Scene scene)
    {
        PushState();
        foreach (Entity entity in scene.Entities)
            if (entity.Visible && (entity.Tag & ExcludeTag) == 0)
                entity.Render();

        if (Engine.Commands.Open)
            foreach (Entity entity in scene.Entities)
                if ((entity.Tag & ExcludeTag) == 0)
                    entity.DebugRender(Camera);
        PopState();
    }
}