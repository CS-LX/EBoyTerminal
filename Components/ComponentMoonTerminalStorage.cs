using GameEntitySystem;
using MoonSharp.Interpreter;
using TemplatesDatabase;
using EBoyTerminal.Runtime;

namespace EBoyTerminal {
    /// <summary>月之终端 EEPROM 式键值存储；Lua 侧为 <c>terminal.storage</c>。</summary>
    public class ComponentMoonTerminalStorage : Component, ILuaScriptApiProvider {
        const string LegacyStorageEntriesKey = "StorageEntries";

        readonly Dictionary<string, object> m_entries = new(StringComparer.Ordinal);
        readonly HashSet<string> m_persistedKeys = new(StringComparer.Ordinal);

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_entries.Clear();
            m_persistedKeys.Clear();
            if (TryLoadLegacyNested(valuesDictionary)) {
                return;
            }
            foreach (KeyValuePair<string, object> pair in valuesDictionary) {
                if (pair.Key == LegacyStorageEntriesKey || pair.Value is ValuesDictionary) {
                    continue;
                }
                m_entries[pair.Key] = pair.Value;
                m_persistedKeys.Add(pair.Key);
            }
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap) {
            foreach (string key in m_persistedKeys) {
                if (!m_entries.ContainsKey(key)) {
                    valuesDictionary.Remove(key);
                }
            }
            foreach (KeyValuePair<string, object> pair in m_entries) {
                valuesDictionary.SetValue(pair.Key, pair.Value);
            }
            m_persistedKeys.Clear();
            foreach (string key in m_entries.Keys) {
                m_persistedKeys.Add(key);
            }
            if (valuesDictionary.ContainsKey(LegacyStorageEntriesKey)) {
                valuesDictionary.Remove(LegacyStorageEntriesKey);
            }
        }

        bool TryLoadLegacyNested(ValuesDictionary valuesDictionary) {
            if (!valuesDictionary.TryGetValue(LegacyStorageEntriesKey, out ValuesDictionary? nested) || nested == null) {
                return false;
            }
            foreach (string key in nested.Keys) {
                string keyText = key ?? string.Empty;
                if (string.IsNullOrEmpty(keyText)) {
                    continue;
                }
                m_entries[keyText] = nested.GetValue(keyText, string.Empty);
                m_persistedKeys.Add(keyText);
            }
            return true;
        }

        public void ContributeLuaApi(LuaScriptApiBuildContext context) {
            context.AddSubTable("storage", BuildApi(context));
        }

        Dictionary<string, DynValue> BuildApi(LuaScriptApiBuildContext context) => new(StringComparer.Ordinal) {
            ["get"] = context.Callback((_, args) => {
                if (args.Count < 1) {
                    throw new ScriptRuntimeException("storage.get(key) requires a key");
                }
                string key = RequireKey(args[0]);
                return m_entries.TryGetValue(key, out object? value) ? StorageToLua(value) : DynValue.Nil;
            }),
            ["set"] = context.Callback((_, args) => {
                if (args.Count < 2) {
                    throw new ScriptRuntimeException("storage.set(key, value) requires key and value");
                }
                string key = RequireKey(args[0]);
                m_entries[key] = LuaValueToStorage(args[1]);
                return DynValue.Nil;
            }),
            ["remove"] = context.Callback((_, args) => {
                if (args.Count < 1) {
                    throw new ScriptRuntimeException("storage.remove(key) requires a key");
                }
                m_entries.Remove(RequireKey(args[0]));
                return DynValue.Nil;
            }),
            ["clear"] = context.Callback((_, _) => {
                m_entries.Clear();
                return DynValue.Nil;
            }),
            ["has"] = context.Callback((_, args) => {
                if (args.Count < 1) {
                    throw new ScriptRuntimeException("storage.has(key) requires a key");
                }
                return DynValue.NewBoolean(m_entries.ContainsKey(RequireKey(args[0])));
            }),
            ["keys"] = context.Callback((executionContext, _) => {
                Table table = new(executionContext.OwnerScript);
                int index = 1;
                foreach (string key in m_entries.Keys.OrderBy(static key => key, StringComparer.Ordinal)) {
                    table.Set(index++, DynValue.NewString(key));
                }
                return DynValue.NewTable(table);
            }),
        };

        static string RequireKey(DynValue value) {
            if (value.Type != DataType.String || string.IsNullOrEmpty(value.String)) {
                throw new ScriptRuntimeException("storage key must be a non-empty string");
            }
            return value.String;
        }

        static object LuaValueToStorage(DynValue value) => value.Type switch {
            DataType.String => value.String ?? string.Empty,
            DataType.Number => value.Number,
            DataType.Boolean => value.Boolean,
            _ => throw new ScriptRuntimeException("storage value must be string, number, or boolean")
        };

        static DynValue StorageToLua(object value) => value switch {
            string text => DynValue.NewString(text),
            bool boolean => DynValue.NewBoolean(boolean),
            double number => DynValue.NewNumber(number),
            float number => DynValue.NewNumber(number),
            int number => DynValue.NewNumber(number),
            long number => DynValue.NewNumber(number),
            _ => DynValue.NewString(value.ToString() ?? string.Empty)
        };
    }
}
