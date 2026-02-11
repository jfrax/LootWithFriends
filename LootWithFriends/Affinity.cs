using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UniLinq;

namespace LootWithFriends
{
    public class Affinity
    {
        public string PlayerPlatformId { get; set; } = "";
        public List<string> PreferReceiving { get; set; } = new List<string>();
        public List<string> PreferDropping { get; set; } = new List<string>();

        private static List<Affinity> _cache;
        private static bool _isDirty;

        private static string AffinityFile =>
            Path.Combine(Utilities.ModSaveDir, "affinities.json");

        public static void SetAffinity(EntityPlayer player, ItemClass itemClass, AffinityTypes affinityType)
        {
            LoadIfNeeded();

            if (!ConnectionManager.Instance.IsServer && _cache == null)
            {
                //local cache should have been set already from PlayerJoinedGame
                Log.Error("[LootWithFriends] Client Affinity Cache Was Null in SetAffinity");
                return;
            }

            //either way (client or server), update the local cache
            var playerPlatformId = Utilities.GetStablePlayerId(player);
            var entry = _cache.FirstOrDefault(p => p.PlayerPlatformId == playerPlatformId);
            if (entry == null)
            {
                entry = new Affinity() { PlayerPlatformId = playerPlatformId };
                _cache.Add(entry);
            }

            entry.PreferDropping.Remove(itemClass.Name);
            entry.PreferReceiving.Remove(itemClass.Name);

            if (affinityType == AffinityTypes.PreferDropping)
                entry.PreferDropping.Add(itemClass.Name);
            else if (affinityType == AffinityTypes.PreferReceiving)
                entry.PreferReceiving.Add(itemClass.Name);

            _isDirty = true;

            if (!ConnectionManager.Instance.IsServer)
            {

                //send NetPackage to let server know about change in affinity
                var pkg = NetPackageManager.GetPackage<NetPackageClientChangedAffinity>().Setup(
                    Utilities.GetStablePlayerId(player),
                    itemClass.Name,
                    affinityType);
                
                ConnectionManager.Instance.SendToServer(pkg);
            }
        }

        public static AffinityTypes GetAffinity(EntityPlayer player, string itemClassName)
        {
            LoadIfNeeded();

            if (_cache == null)
            {
                Log.Error("[LootWithFriends] Cache was null in GetAffinity");
                return AffinityTypes.NoPreference;
            }
            
            string playerPlatformId = Utilities.GetStablePlayerId(player);
            var entry = _cache.FirstOrDefault(p => p.PlayerPlatformId == playerPlatformId);

            if (entry == null)
            {
                return AffinityTypes.NoPreference;
            }


            if (entry.PreferDropping?.Contains(itemClassName) ?? false)
            {
                return AffinityTypes.PreferDropping;
            }


            if (entry.PreferReceiving?.Contains(itemClassName) ?? false)
            {
                return AffinityTypes.PreferReceiving;
            }
            
            return AffinityTypes.NoPreference;
        }

        public static Affinity GetAffinitiesForPlayer(EntityPlayer player)
        {
            LoadIfNeeded();
            var playerPlatformId = Utilities.GetStablePlayerId(player);
            return _cache.FirstOrDefault(x => x.PlayerPlatformId == playerPlatformId) ??
                   new Affinity() { PlayerPlatformId = playerPlatformId };
        }

        public static void ClientSetAffinitiesForPlayer(EntityPlayer player, Affinity affinities)
        {
            NetGuards.ClientOnly(nameof(ClientSetAffinitiesForPlayer));

            _cache = new List<Affinity>()
            {
                affinities
            };
        }

        public static void ServerUpdateAffinitiesForPlayer(AffinityChange change)
        {
            NetGuards.ServerOnly(nameof(ServerUpdateAffinitiesForPlayer));
            
            LoadIfNeeded();
            var affinity = _cache.FirstOrDefault(x => x.PlayerPlatformId == change.PlayerPlatformId);
            if (affinity == null)
            {
                //need to create entry for new player
                affinity = new Affinity() { PlayerPlatformId = change.PlayerPlatformId };
                _cache.Add(affinity);
            }
            
            affinity.PreferDropping.RemoveAll(x => x == change.ItemClassName);
            affinity.PreferReceiving.RemoveAll(x => x == change.ItemClassName);

            if (change.AffinityType == AffinityTypes.PreferDropping)
            {
                affinity.PreferDropping.Add(change.ItemClassName);
            }
            else if (change.AffinityType == AffinityTypes.PreferReceiving)
            {
                affinity.PreferReceiving.Add(change.ItemClassName);
            }

            _isDirty = true;

            //no need to update further if it is NoPref since we just removed all existing affinities of that class

        }

        public static void PreFetchPlayerAffinity()
        {
            if (ConnectionManager.Instance.IsServer)
            {
                LoadIfNeeded();
            }
            else
            {
                var pkg = NetPackageManager.GetPackage<NetPackageClientRequestingAffinities>()
                    .Setup(GameManager.Instance.myEntityPlayerLocal.entityId);
                ConnectionManager.Instance.SendToServer(pkg);
            }
        }

        private static void LoadIfNeeded()
        {
            if (_cache != null)
                return;

            if (!ConnectionManager.Instance.IsServer)
                return;

            if (!Directory.Exists(Utilities.ModSaveDir))
                Directory.CreateDirectory(Utilities.ModSaveDir);

            if (File.Exists(AffinityFile))
            {
                string json = File.ReadAllText(AffinityFile);
                _cache = JsonConvert.DeserializeObject<List<Affinity>>(json);
            }

            if (_cache == null)
                _cache = new List<Affinity>();
        }

        public static void FlushToDisk()
        {
            if (!ConnectionManager.Instance.IsServer)
                return;

            if (!_isDirty || _cache == null)
                return;

            if (!Directory.Exists(Utilities.ModSaveDir))
                Directory.CreateDirectory(Utilities.ModSaveDir);

            string json = JsonConvert.SerializeObject(_cache,  Formatting.Indented);
            File.WriteAllText(AffinityFile, json);

            _isDirty = false;
        }

        public static bool ShouldDropItemStack(EntityPlayer requestorPlayer, EntityPlayer otherPlayer, ItemStack itemStack)
        {
            NetGuards.ServerOnly(nameof(ShouldDropItemStack));
            
            var itemName = itemStack?.itemValue?.ItemClass?.Name;
            if (string.IsNullOrEmpty(itemName))
                return false;
            
            var aff = Affinity.GetAffinity(requestorPlayer, itemName);
            
            //1 - drop items that the player wants to get rid of
            if (aff == AffinityTypes.PreferDropping)
            {
                return true;
            }

            //2 - drop items that the other player wants to receive, and we do NOT also want to receive (dropper's preferences win)
            if (otherPlayer != null)
            {
                var otherPlayerAff = GetAffinity(otherPlayer, itemName);
                if (otherPlayerAff == AffinityTypes.PreferReceiving && aff != AffinityTypes.PreferReceiving)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public enum AffinityTypes
    {
        PreferDropping,
        PreferReceiving,
        NoPreference
    }
}