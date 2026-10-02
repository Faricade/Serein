using Foster.Framework;

namespace Serein;

public abstract class Renderer
{
    public bool Visible = true;
    public Camera Camera = new();

    public BlendMode? Blend = BlendMode.Premultiply;
    public TextureSampler? Sampler = new(TextureFilter.Linear, TextureWrap.Clamp);
    public Material? Material;

    public virtual void Update(Scene scene) { }
    public virtual void BeforeRender(Scene scene) { }
    public virtual void Render(Scene scene) { }
    public virtual void AfterRender(Scene scene) { }

    protected void PushState()
    {
        Draw.Batcher.PushMatrix(Camera.Matrix * Engine.ScreenMatrix);
        if (Blend is { } blend) Draw.Batcher.PushBlend(blend);
        if (Sampler is { } sampler) Draw.Batcher.PushSampler(sampler);
        if (Material is { } material) Draw.Batcher.PushMaterial(material);
    }

    protected void PopState()
    {
        if (Material is not null) Draw.Batcher.PopMaterial();
        if (Sampler is not null) Draw.Batcher.PopSampler();
        if (Blend is not null) Draw.Batcher.PopBlend();
        Draw.Batcher.PopMatrix();
    }
}
