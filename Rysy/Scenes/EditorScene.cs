using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Rysy.Graphics;

namespace Rysy.Scenes;

public sealed class EditorScene : Scene
{
    private Map _map = null!;
    public Map Map
    {
        get => _map;
        private set
        {
            _map = value;
            Camera = new();
            CurrentRoom = _map.Rooms.First().Value;

            GC.Collect(3);
        }
    }

    private Room _currentRoom = null!; // will be set in Map.set
    public Room CurrentRoom
    {
        get => _currentRoom;
        set
        {
            _currentRoom = value;
            Camera.CenterOnRealPos(CurrentRoom.Bounds.Center.ToVector2());
        }
    }

    public Camera Camera { get; set; } = null!; // will be set in Map.set

    public EditorScene(Map map)
    {
        Map = map;
    }

    public string CurrentRoomName
    {
        get => CurrentRoom.Name;
        set => CurrentRoom = Map.Rooms[value];
    }

    public override void Update()
    {
        base.Update();

        Camera.HandleMouseMovement();
    }

    private static RenderTarget2D __temp = new(RysyEngine.GDM.GraphicsDevice, 1920, 1080, false, SurfaceFormat.Color, DepthFormat.None);

    //TODO REMOVE
    public override void Render()
    {
        base.Render();


        GFX.Batch.GraphicsDevice.SetRenderTarget(__temp);

        foreach (var item in Map.Rooms)
        {
            item.Value.Render(Camera);
        }


        GFX.Batch.GraphicsDevice.SetRenderTarget(null);
        GFX.Batch.Begin();
        GFX.Batch.Draw(__temp, Vector2.Zero, Color.White);
        GFX.Batch.End();


        if (Input.Keyboard.IsKeyClicked(Keys.Up))
        {
            CurrentRoom = Map.Rooms.ElementAt(Map.Rooms.Values.ToList().IndexOf(CurrentRoom) + 1).Value;
            Logger.Write("DebugHotkey", LogLevel.Debug, $"Switching to room {CurrentRoom.Name}");
        }
        if (Input.Keyboard.IsKeyClicked(Keys.Down))
        {
            CurrentRoom = Map.Rooms.ElementAt(Map.Rooms.Values.ToList().IndexOf(CurrentRoom) - 1).Value;
            Logger.Write("DebugHotkey", LogLevel.Debug, $"Switching to room {CurrentRoom.Name}");
        }

        if (Input.Keyboard.Ctrl())
        {
            // Reload everything
            if (Input.Keyboard.IsKeyClicked(Keys.F5))
            {
                Task.Run(() =>
                {
                    RysyEngine.Instance.Reload();
                    GC.Collect(3);
                });
            }
        } else
        {
            // clear render cache
            if (Input.Keyboard.IsKeyClicked(Keys.F4))
            {
                foreach (var item in Map.Rooms)
                    item.Value.ClearRenderCache();
            }

            // Reload textures
            if (Input.Keyboard.IsKeyClicked(Keys.F5))
            {
                GFX.Atlas.DisposeTextures();
                foreach (var item in Map.Rooms)
                    item.Value.ClearRenderCache();
                GC.Collect(3);
            }

            // Re-register entities
            if (Input.Keyboard.IsKeyClicked(Keys.F6))
            {
                EntityRegistry.Register();
                CurrentRoom.ClearRenderCache();
                GC.Collect(3);
            }
        }
    }
}
