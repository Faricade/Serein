namespace Serein;

public class EverythingRenderer : Renderer
{
    public override void Render(Scene scene)
    {
        PushState();
        scene.Entities.Render();
        if (Engine.Commands.Open)
            scene.Entities.DebugRender(Camera);
        PopState();
    }
}