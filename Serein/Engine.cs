using Foster.Framework;

namespace Serein;

public class Engine : App
{
    public string Title;
    public Version? Version;

    // references
    public static Engine Instance { get; private set; } = null!;
    public static Commands Commands { get; private set; } = null!;
    public static Pooler Pooler { get; private set; } = null!;
    public static Action? OverloadGameLoop { get; set; }

    // screen size
    public static Color ClearColor { get; set; } = Color.Black;
    public static int GameWidth { get; private set; }
    public static int GameHeight { get; private set; }
    public static float ViewScale { get; private set; } = 1;
    public static int ViewPadding
    {
        get;
        set
        {
            field = value;
            Instance?.UpdateView();
        }
    } = 0;

    public static RectInt Viewport { get; private set; }
    public static Matrix3x2 ScreenMatrix =>
        Matrix3x2.CreateScale(ViewScale) *
        Matrix3x2.CreateTranslation(Viewport.X, Viewport.Y);

    // time
    public static int FPS { get; private set; }
    private TimeSpan counterElapsed;
    private int fpsCounter = 0;

    // scene
    private Scene? scene;
    private Scene? nextScene;

    public Engine(AppConfig config, (int width, int height) gameDimensions)
        : base(config)
    {
        Instance = this;

        Title = config.WindowTitle;
        if (string.IsNullOrWhiteSpace(Title))
            Title = config.ApplicationName;
        GameWidth = gameDimensions.width;
        GameHeight = gameDimensions.height;

        Window.OnResize += OnResize;
        OnEvent += (ev) =>
        {
            RunOnMainThread(() =>
            {
                switch (ev)
                {
                    case AppEvents.EnterForeground:
                        scene?.GainFocus();
                        break;
                    case AppEvents.EnterBackground:
                        scene?.LoseFocus();
                        break;
                }
            });
        };

        MInput.Initialize(Input);
        Tracker.Initialize();
        Pooler = new Pooler();
        Commands = new Commands();
        Draw.Initialize(GraphicsDevice);

        System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.SustainedLowLatency;
    }

    protected override void Startup()
    {
        UpdateView();
    }

    protected override void Update()
    {
        MInput.Update();

        if (OverloadGameLoop is not null)
        {
            OverloadGameLoop();
            return;
        }

        //Update current scene
        if (scene != null)
        {
            scene.BeforeUpdate();
            scene.Update();
            scene.AfterUpdate();
        }

        //Debug Console
        if (Commands is not null && Commands.Open)
            Commands.UpdateOpen();
        else if (Commands is not null && Commands.Enabled)
            Commands.UpdateClosed();

        //Changing scenes
        if (scene != nextScene)
        {
            Scene? lastScene = scene;
            scene?.End();
            scene = nextScene;
            OnSceneTransition(lastScene, nextScene);
            scene?.Begin();
        }
    }

    protected override void Render()
    {
        RenderCore();

        if (Commands is not null && Commands.Open)
            Commands.Render();

        Window.Clear(ClearColor);
        Draw.Batcher.Render(Window);

        //Frame counter
        fpsCounter++;
        counterElapsed += Time.DeltaTimeSpan;
        if (counterElapsed >= TimeSpan.FromSeconds(1))
        {
#if DEBUG
            Window.Title = Title + " " + fpsCounter.ToString() + " fps - " + (GC.GetTotalMemory(false) / 1048576f).ToString("F") + " MB";
#endif
            FPS = fpsCounter;
            fpsCounter = 0;
            counterElapsed -= TimeSpan.FromSeconds(1);
        }
    }

    protected override void Shutdown()
    {
        MInput.Shutdown();
    }

    protected virtual void OnResize()
    {
        UpdateView();
    }

    /// <summary>
    /// Override if you want to change the core rendering functionality of Serein Engine.
    /// By default, this simply clears the screen, and queues render of the current Scene
    /// </summary>
    protected virtual void RenderCore()
    {
        scene?.BeforeRender();

        Draw.Batcher.Clear();

        Draw.Batcher.PushScissor(Viewport);
        if (scene != null)
        {
            scene.Render();
            scene.AfterRender();
        }
        Draw.Batcher.PopScissor();
    }

    public void RunWithLogging()
    {
        try
        {
            Run();
        }
        catch (Exception e)
        {
            ErrorLog.Write(e);
            ErrorLog.Open();
        }
    }

    /// <summary>
    /// Called after a Scene ends, before the next Scene begins
    /// </summary>
    protected virtual void OnSceneTransition(Scene? from, Scene? to)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    /// <summary>
    /// The currently active Scene. Note that if set, the Scene will not actually change until the end of the Update
    /// </summary>
    public static Scene? Scene
    {
        get { return Instance?.scene; }
        set { Instance?.nextScene = value; }
    }

    private void UpdateView()
    {
        int sw = Window.WidthInPixels, sh = Window.HeightInPixels;
        if (sw <= 0 || sh <= 0) return;

        float fit = MathF.Min(sw / (float)GameWidth, sh / (float)GameHeight);
        ViewScale = MathF.Max(0.01f, fit - (2f * ViewPadding / GameWidth));

        int w = Math.Max(1, (int)(GameWidth * ViewScale));
        int h = Math.Max(1, (int)(GameHeight * ViewScale));
        Viewport = new RectInt((sw - w) / 2, (sh - h) / 2, w, h);
    }
}
