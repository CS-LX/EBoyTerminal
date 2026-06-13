using Game;

namespace EBoyTerminal.Electric;

/// <summary>月之终端逻辑电路端口：相对朝向名 ↔ CellFace。</summary>
public static class MoonTerminalElectricPorts {
    public const int PortCount = 5;

    static readonly string[] s_portNames = ["back", "left", "right", "top", "bottom"];

    static readonly (int Left, int Right)[] s_horizontalSides = [
        (3, 2),
        (2, 3),
        (1, 0),
        (0, 1),
    ];

    public static IReadOnlyList<string> PortNames => s_portNames;

    public static int GetFacing(int blockData) => blockData & 3;

    public static bool IsFrontFace(int blockData, int face) => face == GetFacing(blockData);

    public static bool IsCircuitFace(int blockData, int face) => face is >= 0 and <= 5 && !IsFrontFace(blockData, face);

    public static ElectricConnectorType? GetConnectorType(int blockData, int face) {
        return IsCircuitFace(blockData, face) ? ElectricConnectorType.InputOutput : null;
    }

    public static bool TryResolveFace(int blockData, string portName, out int face) {
        face = -1;
        if (string.IsNullOrWhiteSpace(portName)) {
            return false;
        }
        int facing = GetFacing(blockData);
        switch (portName.Trim().ToLowerInvariant()) {
            case "back":
                face = facing ^ 1;
                return true;
            case "top":
                face = 4;
                return true;
            case "bottom":
                face = 5;
                return true;
            case "left":
                face = s_horizontalSides[facing].Left;
                return true;
            case "right":
                face = s_horizontalSides[facing].Right;
                return true;
            default:
                return false;
        }
    }

    public static bool TryGetPortName(int blockData, int face, out string portName) {
        portName = string.Empty;
        if (!IsCircuitFace(blockData, face)) {
            return false;
        }
        if (face == 4) {
            portName = "top";
            return true;
        }
        if (face == 5) {
            portName = "bottom";
            return true;
        }
        int facing = GetFacing(blockData);
        if (face == (facing ^ 1)) {
            portName = "back";
            return true;
        }
        if (face == s_horizontalSides[facing].Left) {
            portName = "left";
            return true;
        }
        if (face == s_horizontalSides[facing].Right) {
            portName = "right";
            return true;
        }
        return false;
    }
}
