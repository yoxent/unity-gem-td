using NUnit.Framework;
using GemTD.Gameplay.Grid;

namespace GemTD.Tests.EditMode
{
    public sealed class GridBoardTests
    {
        [Test]
        public void NewBoard_CellsNotBuildable()
        {
            var board = new GridBoard(4, 3);
            Assert.AreEqual(4, board.Width);
            Assert.AreEqual(3, board.Height);
            Assert.IsFalse(board.IsBuildable(0, 0));
            Assert.IsFalse(board.IsBuildable(3, 2));
            Assert.IsFalse(board.InBounds(4, 0));
        }

        [Test]
        public void SetBuildable_UpdatesCell()
        {
            var board = new GridBoard(2, 2);
            board.SetBuildable(1, 1, true);
            Assert.IsTrue(board.IsBuildable(1, 1));
            Assert.IsFalse(board.IsBuildable(0, 0));
        }
    }
}
