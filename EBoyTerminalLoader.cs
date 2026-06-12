using Engine;
using Engine.Graphics;
using Game;

namespace EBoyTerminal {
    public class EBoyTerminalLoader : ModLoader {
        static Texture2D? m_blockTexture;

        /// <summary>方块图集 <c>Assets/EBoyTerminal.png</c>。</summary>
        public static Texture2D BlockTexture => m_blockTexture ?? ContentManager.Get<Texture2D>("EBoyTerminal");

        public override void __ModInitialize() {
            base.__ModInitialize();
            ModsManager.RegisterHook("OnLoadingFinished", this);
            EBoyTerminalMod.Initialize();
        }

        public override void OnLoadingFinished(List<Action> actions) {
            m_blockTexture = ContentManager.Get<Texture2D>("EBoyTerminal");
        }
    }
}
