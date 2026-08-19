using GemTD.Gameplay.Gems;

namespace GemTD.UI
{
    /// <summary>
    /// Cross-socket/inventory drag state so drop targets can decide what to do.
    /// Lives in UI layer; domain mutations happen in <see cref="GemTD.Gameplay.GameCompositionRoot"/>.
    /// </summary>
    public static class GemDragState
    {
        public enum SourceKind
        {
            None = 0,
            Inventory = 1,
            Socket = 2
        }

        public static SourceKind Kind { get; private set; } = SourceKind.None;
        public static int InventoryIndex { get; private set; } = -1;
        public static int SocketIndex { get; private set; } = -1;
        public static GemDefinition Gem { get; private set; }

        public static bool HasDrag => Kind != SourceKind.None;

        public static void SetInventory(int inventoryIndex, GemDefinition gem)
        {
            Kind = SourceKind.Inventory;
            InventoryIndex = inventoryIndex;
            SocketIndex = -1;
            Gem = gem;
        }

        public static void SetSocket(int socketIndex, GemDefinition gem)
        {
            Kind = SourceKind.Socket;
            SocketIndex = socketIndex;
            InventoryIndex = -1;
            Gem = gem;
        }

        public static void Clear()
        {
            Kind = SourceKind.None;
            InventoryIndex = -1;
            SocketIndex = -1;
            Gem = null;
        }
    }
}
