using System;
using System.Collections.Generic;
using System.IO;
using UniLinq;
using UnityEngine;

namespace LootWithFriends
{
    public class Affinities
    {
        
        private static List<Affinities> _cache;
        private static bool _isDirty;

        private static List<Affinities> GetAll()
        {
            if (_cache != null)
                return _cache;

            if (!Directory.Exists(ModSaveDir))
                Directory.CreateDirectory(ModSaveDir);

            if (File.Exists(AffinityFile))
            {
                string json = File.ReadAllText(AffinityFile);
                _cache = JsonUtility
                    .FromJson<AffinitySaveData>(json)?
                    .Affinities ?? new List<Affinities>();
            }
            else
            {
                _cache = new List<Affinities>();
            }

            return _cache;
        }

        
        public int PlayerEntityId { get; set; }
        public List<string> PreferReceiving { get; set; } = new List<string>();
        public List<string> PreferDropping { get; set; } = new List<string>();

        private static string ModSaveDir => Path.Combine(
            GameIO.GetSaveGameDir(),
            "Mods",
            "LootWithFriends");

        private static string AffinityFile => Path.Combine(ModSaveDir, "affinities.json");

        public static void SetAffinity(EntityPlayer player, ItemClass itemClass, AffinityTypes affinityType)
        {
            var all = GetAll();

            var playerAffinities = all.FirstOrDefault(x => x.PlayerEntityId == player.entityId);
            if (playerAffinities == null)
            {
                playerAffinities = new Affinities
                {
                    PlayerEntityId = player.entityId
                };
                all.Add(playerAffinities);
            }

            playerAffinities.PreferDropping.Remove(itemClass.Name);
            playerAffinities.PreferReceiving.Remove(itemClass.Name);

            if (affinityType == AffinityTypes.PreferDropping)
                playerAffinities.PreferDropping.Add(itemClass.Name);
            else if (affinityType == AffinityTypes.PreferReceiving)
                playerAffinities.PreferReceiving.Add(itemClass.Name);

            _isDirty = true;
        }


        public static AffinityTypes GetAffinity(EntityPlayer player, ItemClass itemClass)
        {
            var playerAffinity = GetAll()
                .FirstOrDefault(x => x.PlayerEntityId == player.entityId);

            if (playerAffinity == null)
                return AffinityTypes.NoPreference;

            if (playerAffinity.PreferDropping.Contains(itemClass.Name))
                return AffinityTypes.PreferDropping;

            if (playerAffinity.PreferReceiving.Contains(itemClass.Name))
                return AffinityTypes.PreferReceiving;

            return AffinityTypes.NoPreference;
        }

        public static void FlushToDisk()
        {
            if (!_isDirty || _cache == null)
                return;

            if (!Directory.Exists(ModSaveDir))
                Directory.CreateDirectory(ModSaveDir);

            File.WriteAllText(
                AffinityFile,
                JsonUtility.ToJson(new AffinitySaveData { Affinities = _cache }, true)
            );


            _isDirty = false;
        }
    }

    public enum AffinityTypes
    {
        PreferDropping,
        PreferReceiving,
        NoPreference
    }
    
    [Serializable]
    public class AffinitySaveData
    {
        public List<Affinities> Affinities = new  List<Affinities>();
    }

}