using Hjg.Pngcs;
using Rysy.Components;
using Rysy.Graphics;

namespace Rysy.Helpers;

public static class SaveMapToImageHelper {
    public static void RenderMapToImage(string filepath, IComponentRegistry componentRegistry, IReadOnlyList<Room> rooms) {
        // FNA has a limit of 4098x4098 for textures, which is not sufficient even for some vanilla maps.
        // We'll use Hjg.Pngcs to write the png one row at a time.
        // We'll render the map into 2048x2048 chunks at a time, combine horizontal chunks until we have full rows,
        // then move a chunk downwards.
        // This is very slow, but works for large maps.
        const int chunkSize = 2048;

        var mapBounds = RectangleExt.Merge(rooms.Select(r => r.Bounds));
        var width = mapBounds.Width;
        var height = mapBounds.Height;
        var gd = RysyState.GraphicsDevice;
        using var watch = new ScopedStopwatch($"Saving map to image, {width}x{height} pixels");
        
        var info = new ImageInfo(width, height, 8, false);

        using FileStream fs = File.Create(filepath);
        var png = new PngWriter(fs, info);

        using RenderTarget2D renderTarget = new(gd, chunkSize, chunkSize, false, SurfaceFormat.Color, DepthFormat.None);

        Color[] chunkPixels = new Color[chunkSize * chunkSize];

        // Reused for every horizontal band.
        byte[][] bandRows = new byte[chunkSize][];

        for (int i = 0; i < chunkSize; i++)
            bandRows[i] = new byte[width * 3];

        for (int bandY = 0; bandY < height; bandY += chunkSize)
        {
            int bandHeight = int.Min(chunkSize, height - bandY);

            // Clear row cache.
            for (int y = 0; y < bandHeight; y++)
            {
                bandRows[y].AsSpan().Clear();
            }

            // Render all horizontal chunks that belong to this band.
            for (int chunkX = 0; chunkX < width; chunkX += chunkSize)
            {
                int chunkWidth = int.Min(chunkSize, width - chunkX);

                var anyRendered = RenderChunk(componentRegistry, rooms, renderTarget, mapBounds.Left + chunkX, mapBounds.Top + bandY, chunkWidth, bandHeight);
                if (!anyRendered) {
                    // No rooms were rendered, we can skip this chunk.
                    continue;
                }

                renderTarget.GetData(chunkPixels);

                for (int y = 0; y < bandHeight; y++)
                {
                    unsafe {
                        fixed (byte* rowStart = &bandRows[y][chunkX * 3]) {
                            byte* row = rowStart;
                            int srcRowStart = y * chunkSize;
                            fixed (Color* colorRow = &chunkPixels[srcRowStart]) {
                                for (int x = 0; x < chunkWidth; x++) {
                                    Color c = colorRow[x];
                                    *row++ = c.R;
                                    *row++ = c.G;
                                    *row++ = c.B;
                                }
                            }
                        }
                    }
                }
            }

            // Write completed rows.
            for (int y = 0; y < bandHeight; y++)
                png.WriteRowByte(bandRows[y], -1);
        }

        png.End();
    }

    static bool RenderChunk(IComponentRegistry componentRegistry, IReadOnlyList<Room> rooms, RenderTarget2D target, int x, int y, int width, int height) {
        var gd = target.GraphicsDevice;
        gd.SetRenderTarget(target);
        gd.Clear(Color.Transparent);
        
        var spriteProviders = componentRegistry.GetAll<IRoomSpriteProvider>();

        var camera = new Camera(new Viewport(0, 0, width, height));
        camera.Move(new XnaVector2(x, y));

        bool anyRendered = false;
        
        foreach (var room in rooms) {
            if (!camera.IsRectVisible(room.Bounds))
                continue;

            anyRendered = true;
            room.Render(camera, Room.RenderConfig.Preview, Colorgrade.None, spriteProviders);
        }
        
        gd.SetRenderTarget(null);

        return anyRendered;
    }
}