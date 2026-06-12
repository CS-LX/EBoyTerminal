using Game;
using GameEntitySystem;
using TemplatesDatabase;

namespace EBoyTerminal {
    public class ComponentMoonTerminal : Component {
        public const string ScriptTextKey = "ScriptText";

        public string ScriptText = string.Empty;

        public ComponentBlockEntity m_componentBlockEntity;

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            ScriptText = valuesDictionary.GetValue(ScriptTextKey, string.Empty);
            m_componentBlockEntity = Entity.FindComponent<ComponentBlockEntity>(throwOnError: true);
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap) {
            valuesDictionary.SetValue(ScriptTextKey, ScriptText);
        }
    }
}
