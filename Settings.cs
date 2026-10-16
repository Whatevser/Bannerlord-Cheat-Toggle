using System;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using MCM.Common;
using TaleWorlds.InputSystem;

namespace CheatToggle
{
    internal sealed class Settings : AttributeGlobalSettings<Settings>
    {
        public override string Id => "CheatToggle_v1";
        public override string DisplayName => "Cheat Toggle";
        public override string FolderName => "CheatToggle";
        public override string FormatType => "json";

        // InputKey enum names. Order only ever grows at the end so saved selections stay valid.
        private static readonly string[] KeyNames =
        {
            "A","B","C","D","E","F","G","H","I","J","K","L","M",
            "N","O","P","Q","R","S","T","U","V","W","X","Y","Z",
            "D0","D1","D2","D3","D4","D5","D6","D7","D8","D9",
            "F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12",
            "Numpad0","Numpad1","Numpad2","Numpad3","Numpad4","Numpad5","Numpad6","Numpad7","Numpad8","Numpad9",
            "NumpadSlash","NumpadMultiply","NumpadMinus","NumpadPlus","NumpadPeriod",
            "Insert","Delete","Home","End","PageUp","PageDown",
            "Tilde","Minus","Equals","OpenBraces","CloseBraces","SemiColon","Apostrophe",
            "Comma","Period","Slash","BackSlash"
        };

        // ── Hotkey ──────────────────────────────────────────────
        [SettingPropertyDropdown("Key", Order = 0, RequireRestart = false,
            HintText = "Main key of the cheat mode toggle. D0-D9 are the number row.")]
        [SettingPropertyGroup("Hotkey", GroupOrder = 0)]
        public Dropdown<string> Key { get; set; } = new Dropdown<string>(KeyNames, Array.IndexOf(KeyNames, "C"));

        [SettingPropertyBool("Ctrl", Order = 1, RequireRestart = false, HintText = "Require Ctrl.")]
        [SettingPropertyGroup("Hotkey", GroupOrder = 0)]
        public bool Ctrl { get; set; } = true;

        [SettingPropertyBool("Shift", Order = 2, RequireRestart = false, HintText = "Require Shift.")]
        [SettingPropertyGroup("Hotkey", GroupOrder = 0)]
        public bool Shift { get; set; } = true;

        [SettingPropertyBool("Alt", Order = 3, RequireRestart = false, HintText = "Require Alt.")]
        [SettingPropertyGroup("Hotkey", GroupOrder = 0)]
        public bool Alt { get; set; } = false;

        // ── Auto-suspend ────────────────────────────────────────
        [SettingPropertyBool("Party screen", Order = 0, RequireRestart = false,
            HintText = "If cheat mode is on, suspend it while the party screen opens (skips loading every troop in the game) and resume it when the screen closes.")]
        [SettingPropertyGroup("Auto-suspend cheat mode", GroupOrder = 1)]
        public bool SuspendForParty { get; set; } = true;

        [SettingPropertyBool("Inventory screen", Order = 1, RequireRestart = false,
            HintText = "If cheat mode is on, suspend it while the inventory opens (skips adding 10 of every item in the game) and resume it when the screen closes.")]
        [SettingPropertyGroup("Auto-suspend cheat mode", GroupOrder = 1)]
        public bool SuspendForInventory { get; set; } = true;

        // ── Notifications ───────────────────────────────────────
        [SettingPropertyBool("Game log message", Order = 0, RequireRestart = false,
            HintText = "Show 'Cheat mode: ON/OFF' in the bottom-left log.")]
        [SettingPropertyGroup("Notifications", GroupOrder = 2)]
        public bool ShowLog { get; set; } = true;

        [SettingPropertyBool("Top banner", Order = 1, RequireRestart = false,
            HintText = "Show a short banner at the top of the screen (campaign map and battles).")]
        [SettingPropertyGroup("Notifications", GroupOrder = 2)]
        public bool ShowBanner { get; set; } = true;

        // Parsed hotkey, cached per selection.
        private int _parsedIndex = -1;
        private InputKey _parsedKey = InputKey.C;

        public InputKey GetKey()
        {
            int idx = Key?.SelectedIndex ?? -1;
            if (idx != _parsedIndex)
            {
                _parsedIndex = idx;
                string name = (idx >= 0 && idx < Key.Count) ? Key[idx] : "C";
                _parsedKey = Enum.TryParse(name, out InputKey k) ? k : InputKey.C;
            }
            return _parsedKey;
        }
    }
}
