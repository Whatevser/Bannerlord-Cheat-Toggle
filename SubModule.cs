using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace CheatToggle
{
    public class SubModule : MBSubModuleBase
    {
        private static readonly Color Red = new Color(1f, 0.3f, 0.3f);
        private static readonly Color Green = new Color(0.4f, 1f, 0.4f);
        private static readonly Color Orange = new Color(1f, 0.6f, 0.3f);

        private bool _patched;
        private static string _patchWarning;

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            if (_patched) return;
            _patched = true;
            _patchWarning = ScreenHooks.Apply();
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            if (_patchWarning != null) InformationManager.DisplayMessage(new InformationMessage(_patchWarning, Orange));
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);

            ScreenHooks.Tick();

            Settings s = Settings.Instance;
            if (s == null) return;

            if (!Input.IsKeyPressed(s.GetKey())) return;
            if (s.Ctrl != (Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl))) return;
            if (s.Shift != (Input.IsKeyDown(InputKey.LeftShift) || Input.IsKeyDown(InputKey.RightShift))) return;
            if (s.Alt != (Input.IsKeyDown(InputKey.LeftAlt) || Input.IsKeyDown(InputKey.RightAlt))) return;

            try { Toggle(); }
            catch (Exception e) { Notify("CheatToggle error: " + e.Message, Red); }
        }

        private static void Toggle()
        {
            // While auto-suspended the user's real setting is still ON.
            bool target = !(NativeConfig.CheatMode || ScreenHooks.IsSuspended);
            ScreenHooks.CancelSuspend();

            Cheat.SetEngine(target);

            if (NativeConfig.CheatMode != target)
            {
                Notify("CheatToggle: couldn't change cheat mode", Red);
                return;
            }
            Notify(target ? "Cheat mode: ON" : "Cheat mode: OFF", target ? Green : Orange);
        }

        internal static void Notify(string text, Color color)
        {
            Settings s = Settings.Instance;
            if (s == null || s.ShowLog)
                InformationManager.DisplayMessage(new InformationMessage(text, color));

            if (s == null || s.ShowBanner)
            {
                try { MBInformationManager.AddQuickInformation(new TextObject(text)); }
                catch { /* no UI layer yet, e.g. main menu */ }
            }
        }
    }

    internal static class Cheat
    {
        private const BindingFlags Static = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        private static MethodInfo _exec;
        private static MethodInfo _setter;

        // Real toggle: same as typing config.cheat_mode in the console (persists to engine config).
        public static void SetEngine(bool value)
        {
            if (_exec == null)
                _exec = AccessTools.Method("TaleWorlds.Engine.Utilities:ExecuteCommandLineCommand", new[] { typeof(string) });
            _exec?.Invoke(null, new object[] { "config.cheat_mode " + (value ? "1" : "0") });

            NativeConfig.OnConfigChanged(); // refresh the managed copy
            if (NativeConfig.CheatMode != value) SetManaged(value);
        }

        // Managed-only flag (what Game.CheatMode reads). Instant, engine config untouched.
        public static void SetManaged(bool value)
        {
            if (_setter == null)
                _setter = AccessTools.PropertySetter(typeof(NativeConfig), "CheatMode");
            if (_setter != null) { _setter.Invoke(null, new object[] { value }); return; }

            FieldInfo f = typeof(NativeConfig).GetField("<CheatMode>k__BackingField", Static);
            f?.SetValue(null, value);
        }
    }

    // Where cheat mode makes these screens heavy (verified against v1.4.6 decompiled source):
    //   Helpers.PartyScreenHelper.OpenScreenAsNormal -> OpenScreenAsCheat (roster of every troop in the game)
    //   Helpers.InventoryScreenHelper.OpenInventoryPresentation (adds 10x every item in the game)
    // Party: OpenScreenAsNormal is tiny and may be JIT-inlined, so OpenScreenAsCheat is hooked instead and
    //        redirected to the normal OpenPartyScreen(false).
    // Both:  the managed cheat flag is switched off for the screen's lifetime; Tick() restores it on close.
    internal static class ScreenHooks
    {
        private static bool _suspended;
        private static bool _screenSeen;
        private static int _suspendedAt;
        private static MethodInfo _openNormalParty;

        public static bool IsSuspended => _suspended;

        public static string Apply()
        {
            var harmony = new Harmony("CheatToggle.ScreenHooks");
            string missing = null;

            MethodBase partyCheat = Find(
                "Helpers.PartyScreenHelper:OpenScreenAsCheat",
                "Helpers.PartyScreenManager:OpenScreenAsCheat",
                "TaleWorlds.CampaignSystem.GameState.PartyScreenManager:OpenScreenAsCheat");
            if (partyCheat != null)
                _openNormalParty = AccessTools.Method(partyCheat.DeclaringType, "OpenPartyScreen", new[] { typeof(bool) });
            if (partyCheat != null && _openNormalParty != null)
                harmony.Patch(partyCheat, prefix: new HarmonyMethod(typeof(ScreenHooks), nameof(PartyCheatPrefix)));
            else missing = "party";

            MethodBase inv = Find(
                "Helpers.InventoryScreenHelper:OpenInventoryPresentation",
                "Helpers.InventoryManager:OpenInventoryPresentation",
                "TaleWorlds.CampaignSystem.Inventory.InventoryManager:OpenInventoryPresentation");
            if (inv != null) harmony.Patch(inv, prefix: new HarmonyMethod(typeof(ScreenHooks), nameof(InventoryPrefix)));
            else missing = missing == null ? "inventory" : missing + " and inventory";

            return missing == null ? null : "CheatToggle: auto-suspend unavailable for " + missing + " screen on this game version";
        }

        private static MethodBase Find(params string[] names)
        {
            foreach (string n in names)
            {
                string[] p = n.Split(':');
                if (AccessTools.TypeByName(p[0]) == null) continue;
                MethodBase m = AccessTools.Method(n);
                if (m != null) return m;
            }
            return null;
        }

        // Returning false skips the original (the all-troops roster build).
        private static bool PartyCheatPrefix()
        {
            if (Settings.Instance?.SuspendForParty != true || !NativeConfig.CheatMode) return true;
            Suspend();
            _openNormalParty.Invoke(null, new object[] { false });
            return false;
        }

        private static void InventoryPrefix() { if (Settings.Instance?.SuspendForInventory == true) Suspend(); }

        private static void Suspend()
        {
            if (!NativeConfig.CheatMode) return;
            Cheat.SetManaged(false);
            _suspended = true;
            _screenSeen = false;
            _suspendedAt = Environment.TickCount;
        }

        public static void CancelSuspend() => _suspended = false;

        public static void Tick()
        {
            if (!_suspended) return;

            string state = Game.Current?.GameStateManager?.ActiveState?.GetType().Name;
            bool onScreen = state == "PartyState" || state == "InventoryState";

            if (onScreen) { _screenSeen = true; return; }

            // Restore once the screen has closed, or if it never opened within 3 s.
            if (_screenSeen || Environment.TickCount - _suspendedAt > 3000 || Game.Current == null)
            {
                _suspended = false;
                Cheat.SetManaged(true);
            }
        }
    }
}
