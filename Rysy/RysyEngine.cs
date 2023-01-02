using Microsoft.Xna.Framework.Graphics;
using Rysy;
using Rysy.Graphics;
using Rysy.Scenes;
using System.Text.Json;

public sealed class RysyEngine : Game
{
    public static RysyEngine Instance { get; private set; } = null!;

    public static GraphicsDeviceManager GDM { get; private set; } = null!;

    private static Scene _scene = new BlankScene();
    public static Scene Scene
    {
        get => _scene;
        set => _scene = value;
    }

    private static SmartFramerate smartFramerate = new(5);

    /// <summary>
    /// Action that will be called on the UI thread once this frame ends.
    /// Afterwards, this will be set to null.
    /// </summary>
    public static event Action? OnFrameEnd = null;

    public RysyEngine()
    {
        Instance = this;

        GDM = new GraphicsDeviceManager(this);
        
        Window.AllowUserResizing = true;
        IsMouseVisible = true;

        Window.ClientSizeChanged += Window_ClientSizeChanged;

        IsFixedTimeStep = true;
    }

    public static Action<Viewport>? OnViewportChanged;

    private void Window_ClientSizeChanged(object? sender, EventArgs e)
    {
        OnViewportChanged?.Invoke(GDM.GraphicsDevice.Viewport);

#warning TODO: save resize to settings
    }

    protected override void Initialize()
    {
        base.Initialize();

        Reload();

        ResizeWindow(Settings.Instance.StartingWindowWidth, Settings.Instance.StartingWindowHeight);
    }

    public void Reload()
    {
        using var reloadTimer = new ScopedStopwatch("Loading");

        Scene = new BlankScene();

        try
        {
            Settings.Instance = SettingsHelper.Load();
        }
        catch
        {
            return; // No point in loading any further. Error is already logged by .Load()
        }

        if (Settings.Instance.CelesteDirectory is null or "")
        {
            Logger.Write("Engine", LogLevel.Error, $"CelesteDirectory is {"empty"}! For now, please go to {SettingsHelper.SettingsFileLocation.CorrectSlashes()} and edit it manually.");
            return;
        }
        if (Settings.Instance.LastEditedMap is null or "")
        {
            Logger.Write("Engine", LogLevel.Error, $"LastEditedMap is {"empty"}! For now, please go to {SettingsHelper.SettingsFileLocation.CorrectSlashes()} and edit it manually.");
            return;
        }

        GFX.Load(this);

        EntityRegistry.Register();

        //Scene = new DynamicAtlasTestScene();
        //return;

        try
        {
            var mapBin = BinaryPacker.FromBinary(Settings.Instance.LastEditedMap);
            var map = Map.FromBinaryPackage(mapBin);
            Scene = new EditorScene(map);
        }
        catch (Exception e)
        {
            Logger.Write("Engine", LogLevel.Error, $"Failed to load last edited map: {Settings.Instance.LastEditedMap.CorrectSlashes()} {e}");
            // TODO: Better handling
            return;
        }
    }

    private void ResizeWindow(int w, int y)
    {
        OnFrameEnd += () =>
        {
            GDM.PreferredBackBufferWidth = w;
            GDM.PreferredBackBufferHeight = y;
            GDM.ApplyChanges();
            Window.Position = new Point(0, Window.Position.Y);
            Window_ClientSizeChanged(null, new());
        };
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (IsActive)
        {
            Input.Update(gameTime);

            Scene.Update();

            OnFrameEnd?.Invoke();
            OnFrameEnd = null;
        }

    }

    bool printedfps = false;
    protected override void Draw(GameTime gameTime)
    {
        if (IsActive)
        {
            base.Draw(gameTime);


            GraphicsDevice.Clear(Color.Black);

            Scene.Render();
        }


        smartFramerate.Update(gameTime.ElapsedGameTime.TotalSeconds);
        if (gameTime.TotalGameTime.Seconds % 2 == 0)
        {
            if (!printedfps)
            {
                printedfps = true;
                //var framerate = (1 / gameTime.ElapsedGameTime.TotalSeconds);
                Console.WriteLine(smartFramerate.framerate);
                Console.WriteLine(JsonSerializer.Serialize(GraphicsDevice.Metrics));
            }

        }
        else
        {
            printedfps = false;
        }


    }

    //https://stackoverflow.com/a/44689035
    class SmartFramerate
    {
        double currentFrametimes;
        double weight;
        int numerator;

        public double framerate => (numerator / currentFrametimes);

        public SmartFramerate(int oldFrameWeight)
        {
            numerator = oldFrameWeight;
            weight = (double)oldFrameWeight / ((double)oldFrameWeight - 1d);
        }

        public void Update(double timeSinceLastFrame)
        {
            currentFrametimes = currentFrametimes / weight;
            currentFrametimes += timeSinceLastFrame;
        }
    }
}