using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Map;

namespace GemTD.Tests.EditMode
{
    public class MapFogMaskTests
    {
        [Test]
        public void RevealChunk_ClearsTheSquare_AndADiscPastTheEdge()
        {
            const int chunk = 7;
            const float peek = MapFogMask.DefaultEdgePeekCells;
            var margin = MapFogMask.MarginFor(peek);
            var layout = new MapFogLayout(1, 1, chunk, margin, 1f);
            var pixels = new Color32[layout.Width * layout.Height];
            MapFogMask.FillFog(pixels);

            MapFogMask.RevealChunk(
                pixels, layout.Width, layout.Height, 0, 0, chunk, margin,
                MapFogMask.RevealRadius(chunk, peek));

            var mid = margin + chunk / 2;
            Assert.AreEqual(MapFogMask.Clear, pixels[margin * layout.Width + margin]);
            Assert.AreEqual(MapFogMask.Clear, pixels[(margin + chunk - 1) * layout.Width + (margin + chunk - 1)]);
            Assert.AreEqual(MapFogMask.Clear, pixels[mid * layout.Width + margin + chunk + 2]);
            var outsideCorner = margin + chunk + 2;
            Assert.AreEqual(MapFogMask.Fog, pixels[outsideCorner * layout.Width + outsideCorner]);
            Assert.AreEqual(MapFogMask.Fog, pixels[0]);
            Assert.AreEqual(MapFogMask.Fog, pixels[layout.Width - 1]);
            Assert.AreEqual(MapFogMask.Fog, pixels[(layout.Height - 1) * layout.Width]);
        }

        [Test]
        public void RevealChunk_DiscStopsBeforeTheFarSideOfTheNeighbor()
        {
            const int chunk = 7;
            const float peek = MapFogMask.DefaultEdgePeekCells;
            var margin = MapFogMask.MarginFor(peek);
            var layout = new MapFogLayout(2, 1, chunk, margin, 1f);
            var pixels = new Color32[layout.Width * layout.Height];
            MapFogMask.FillFog(pixels);

            MapFogMask.RevealChunk(
                pixels, layout.Width, layout.Height, 1, 0, chunk, margin,
                MapFogMask.RevealRadius(chunk, peek));

            var mid = margin + chunk / 2;
            Assert.AreEqual(MapFogMask.Fog, pixels[mid * layout.Width + margin]);
            Assert.AreEqual(MapFogMask.Clear, pixels[mid * layout.Width + margin + chunk - 1]);
            Assert.AreEqual(MapFogMask.Clear, pixels[mid * layout.Width + margin + chunk]);
            Assert.AreEqual(MapFogMask.Fog, pixels[mid * layout.Width + layout.Width - 1]);
        }

        [Test]
        public void TextureUv_CellOrigin_LandsOnFirstBoardTexel()
        {
            var layout = new MapFogLayout(1, 1, 7, 1, 1f);
            const float coverage = 21f;

            Assert.AreEqual(0f, layout.TextureU(-1f, coverage), 1e-4f);
            Assert.AreEqual(1f / 9f, layout.TextureU(0f, coverage), 1e-4f);
            Assert.AreEqual(0f, layout.TextureV(-1f, coverage), 1e-4f);
            Assert.AreEqual(1f / 9f, layout.TextureV(0f, coverage), 1e-4f);
        }

        [Test]
        public void TextureUv_OutsideTheBoard_ClampsToTheFogMargin()
        {
            var layout = new MapFogLayout(13, 13, ChunkMask.Size, 1, 1f);
            const float coverage = 512f;
            var cells = 13 * ChunkMask.Size;

            Assert.Less(layout.TextureU(-20f, coverage), 0f);
            Assert.Greater(layout.TextureU(cells + 20f, coverage), 1f);
            Assert.AreEqual(1f / layout.Width, layout.TextureU(0f, coverage), 1e-4f);
        }
    }
}
