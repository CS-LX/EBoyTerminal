using Engine;
using Engine.Media;
using Game;
using GameEntitySystem;
using SCIENEW;
using SCIENEW.Screens;
using Screen = SCIENEW.Screens.Screen;

namespace EBoyTerminal.Terminal;

public readonly record struct TerminalScreenSnapshot(
    bool IsLinked,
    int Rows,
    int Columns,
    Vector3 Position
);

/// <summary>解析月之终端与 CRT 的链接关系及当前可见屏幕度量。</summary>
public sealed class TerminalScreenResolver {
    readonly ComponentMoonTerminal m_terminal;
    readonly Project m_project;

    ComponentMoonTerminalScreenProvider? m_provider;
    SubsystemScreens? m_subsystemScreens;

    public TerminalScreenResolver(ComponentMoonTerminal terminal, Project project) {
        m_terminal = terminal;
        m_project = project;
    }

    public TerminalScreenSnapshot GetSnapshot() {
        EnsureServices();
        Vector3 terminalPosition = GetTerminalPosition();
        if (m_provider == null || m_subsystemScreens == null || !m_subsystemScreens.TryGetContainer(m_provider, out IScreenContainerComponent? container)) {
            return new TerminalScreenSnapshot(false, 0, 0, terminalPosition);
        }

        Vector3 position = container.GetPosition();
        if (!TryGetPrimaryScreen(m_project, container, position, out Screen screen)) {
            return new TerminalScreenSnapshot(true, 0, 0, position);
        }

        BitmapFont font = IndustrialModLoader.PixelFont;
        return new TerminalScreenSnapshot(
            true,
            TerminalScreenLayout.CalculateRows(screen, font),
            TerminalScreenLayout.CalculateColumns(screen, font),
            position);
    }

    Vector3 GetTerminalPosition() {
        ComponentBlockEntity? blockEntity = m_terminal.Entity.FindComponent<ComponentBlockEntity>(throwOnError: false);
        return blockEntity != null
            ? new Vector3(blockEntity.Coordinates) + Vector3.One * 0.5f
            : Vector3.Zero;
    }

    void EnsureServices() {
        m_provider ??= m_terminal.Entity.FindComponent<ComponentMoonTerminalScreenProvider>(throwOnError: false);
        m_subsystemScreens ??= m_project.FindSubsystem<SubsystemScreens>(throwOnError: false);
    }

    static bool TryGetPrimaryScreen(Project project, IScreenContainerComponent container, Vector3 nearPosition, out Screen screen) {
        screen = default;
        SubsystemPlayers? players = project.FindSubsystem<SubsystemPlayers>(throwOnError: false);
        Camera? camera = players?.FindNearestPlayer(nearPosition)?.GameWidget.ActiveCamera;
        if (camera == null) {
            return false;
        }
        Screen[] screens = container.GetScreens(camera);
        if (screens.Length == 0) {
            return false;
        }
        screen = screens[0];
        return true;
    }
}
