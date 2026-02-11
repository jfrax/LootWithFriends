using System;
using System.IO;
using Newtonsoft.Json;
// ReSharper disable MemberHidesStaticFromOuterClass

namespace LootWithFriends
{
    public static class UserPreferences
    {
        private static readonly object Lock = new object();
        private static bool _loaded;

        // Defaults
        private static bool _createWaypointsForBagsIDrop;
        private static bool _createWaypointsForBagsFriendsDrop = true;
        private static bool _allowDropWhenNoAlliesPresent = true;

        private static string PreferencesPath =>
            Path.Combine(Utilities.ModInstallDir, "Preferences.json");

        #region Public Properties

        public static bool CreateWaypointsForBagsIDrop
        {
            get
            {
                EnsureLoaded();
                return _createWaypointsForBagsIDrop;
            }
        }

        public static bool CreateWaypointsForBagsFriendsDrop
        {
            get
            {
                EnsureLoaded();
                return _createWaypointsForBagsFriendsDrop;
            }
        }

        public static bool AllowDropWhenNoAlliesPresent
        {
            get
            {
                EnsureLoaded();
                return _allowDropWhenNoAlliesPresent;
            }
        }

        #endregion

        private static void EnsureLoaded()
        {
            if (_loaded)
                return;

            lock (Lock)
            {
                if (_loaded)
                    return;

                LoadOrCreate();
                _loaded = true;
            }
        }

        private static void LoadOrCreate()
        {
            if (!File.Exists(PreferencesPath))
            {
                Log.Out("[LootWithFriends] Preferences.json missing, creating with defaults.");
                WriteDefaults();
                return;
            }

            try
            {
                var json = File.ReadAllText(PreferencesPath);
                var data = JsonConvert.DeserializeObject<PreferencesDto>(json);

                if (data == null)
                    throw new Exception("[LootWithFriends] Deserialized PreferencesDto was null.");

                _createWaypointsForBagsIDrop = data.CreateWaypointsForBagsIDrop;
                _createWaypointsForBagsFriendsDrop = data.CreateWaypointsForBagsFriendsDrop;
                _allowDropWhenNoAlliesPresent = data.AllowDropWhenNoAlliesPresent;

                Log.Out($"[LootWithFriends] Preferences loaded successfully from {PreferencesPath}.");
            }
            catch (Exception ex)
            {
                Log.Warning($"[LootWithFriends] Preferences.json invalid, recreating with defaults. Reason: {ex.Message}");
                WriteDefaults();
            }
        }

        private static void WriteDefaults()
        {
            try
            {
                var dto = new PreferencesDto
                {
                    CreateWaypointsForBagsIDrop = _createWaypointsForBagsIDrop,
                    CreateWaypointsForBagsFriendsDrop = _createWaypointsForBagsFriendsDrop,
                    AllowDropWhenNoAlliesPresent = _allowDropWhenNoAlliesPresent
                };

                var json = JsonConvert.SerializeObject(dto, Formatting.Indented);

                var directory = Path.GetDirectoryName(PreferencesPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(PreferencesPath, json);

                Log.Out("[LootWithFriends] Preferences.json written with default values.");
            }
            catch (Exception ex)
            {
                Log.Error($"[LootWithFriends] Failed to write Preferences.json: {ex}");
            }
        }

        private class PreferencesDto
        {
            public bool CreateWaypointsForBagsIDrop { get; set; }
            public bool CreateWaypointsForBagsFriendsDrop { get; set; } = true;
            public bool AllowDropWhenNoAlliesPresent { get; set; } = true;
        }
    }
}
