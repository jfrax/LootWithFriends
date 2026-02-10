using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Newtonsoft.Json;
using UniLinq;
using UnityEngine;

namespace LootWithFriends
{
    public static class Waypoints
    {
        private static List<KnownDroppedBagSaveData> knownDroppedBags = new List<KnownDroppedBagSaveData>();

        private static string LootWaypointsFile =>
            Path.Combine(Utilities.ModSaveDir, "lootwaypoints.json");

        /* ==========================================================
         *  SERVER ENTRY POINTS
         * ========================================================== */

        public static void ServerLootContainerAdded(
            EntityLootContainer container,
            EntityPlayer droppedBy,
            EntityPlayer droppedFor)
        {
            NetGuards.ServerOnly(nameof(ServerLootContainerAdded));

            var saveData = CreateSaveData(container, droppedBy, droppedFor);
            knownDroppedBags.Add(saveData);

            SendBagUpdateToPlayers(saveData, isDelete: false);
            UpdateLocalPlayerWaypoint(saveData, forceCoordinates: false);
        }

        public static void ServerLootContainerRemoved(EntityLootContainer container)
        {
            NetGuards.ServerOnly(nameof(ServerLootContainerRemoved));

            var saveData = knownDroppedBags
                .FirstOrDefault(x => x.entityId == container.entityId);

            if (saveData == null)
                return;

            knownDroppedBags.Remove(saveData);

            SendBagUpdateToPlayers(saveData, isDelete: true);
            DeleteLocalWaypoint(saveData);
        }
   
        public static void ServerSyncWaypointsToPlayer(EntityPlayer player)
        {
            NetGuards.ServerOnly(nameof(ServerSyncWaypointsToPlayer));

            if (knownDroppedBags == null || knownDroppedBags.Count == 0)
                return;
            
            if (Utilities.LocalPlayerExists() && player.entityId == GameManager.Instance.myEntityPlayerLocal.entityId)
            {
                var myStableId = Utilities.GetStablePlayerId(player);
                //we are the host player. load up our waypoints here
                if (knownDroppedBags.Any())
                {
                    foreach (var saveData in knownDroppedBags.Where(x => 
                                 x.droppedForStableId == myStableId
                                 || x.droppedForStableId == myStableId)) 
                    {
                        ClientApplyBagState(saveData);
                    }    
                }
            }
            else
            {
                //a connecting client needs to know their waypoints, so send the netpkg
                foreach (var bag in knownDroppedBags)
                {
                    var pkg = NetPackageManager
                        .GetPackage<NetPackageServerSendClientKnownDroppedBagSaveData>()
                        .Setup(bag, beingDeleted: false);

                    ConnectionManager.Instance.SendPackage(
                        pkg,
                        _onlyClientsAttachedToAnEntity: true,
                        _attachedToEntityId: player.entityId);
                }
            }
            
            
        }


        /* ==========================================================
         *  LOCAL PLAYER WAYPOINT HANDLING
         * ========================================================== */

        public static void ClientApplyBagState(KnownDroppedBagSaveData saveData)
        {
            // ensure we track this bag locally
            if (!knownDroppedBags.Any(x => x.entityId == saveData.entityId))
                knownDroppedBags.Add(saveData);

            // try to render it in the best possible way
            UpdateLocalPlayerWaypoint(
                saveData,
                forceCoordinates: false);
        }

        
        public static void OnLootContainerLoaded(EntityLootContainer container)
        {
            var bag = knownDroppedBags
                .FirstOrDefault(x => x.entityId == container.entityId);

            if (bag != null)
            {
                UpdateLocalPlayerWaypoint(bag, forceCoordinates: false);
            }
        }

        public static void OnLootContainerUnloaded(EntityLootContainer container)
        {
            var bag = knownDroppedBags
                .FirstOrDefault(x => x.entityId == container.entityId);

            if (bag != null)
            {
                UpdateLocalPlayerWaypoint(bag, forceCoordinates: true);
            }
        }
        
        private static void UpdateLocalPlayerWaypoint(
            KnownDroppedBagSaveData saveData,
            bool forceCoordinates)
        {
            var player = GameManager.Instance.myEntityPlayerLocal;
            if (player == null)
                return;

            RegisterLootBagClasses();

            ResolveContainerPosition(saveData, out var container);

            var existing = FindExistingWaypoint(player, saveData);
            var waypointName = GetWaypointName(player, saveData, existing);

            if (existing != null)
                FullyDeleteWaypoint(existing, player);

            CreateWaypoint(
                player,
                saveData,
                waypointName,
                container,
                forceCoordinates);
        }

        public static void DeleteLocalWaypoint(KnownDroppedBagSaveData saveData)
        {
            var player = GameManager.Instance.myEntityPlayerLocal;
            if (player == null)
                return;

            var wp = FindExistingWaypoint(player, saveData);
            if (wp != null)
                FullyDeleteWaypoint(wp, player);
            
            var match = knownDroppedBags.FirstOrDefault(x => x.entityId == saveData.entityId);
            if (match != null)
                knownDroppedBags.Remove(match);
            
        }

        /* ==========================================================
         *  WAYPOINT CREATION
         * ========================================================== */

        private static void CreateWaypoint(
            EntityPlayerLocal player,
            KnownDroppedBagSaveData saveData,
            string name,
            EntityLootContainer container,
            bool forceCoordinates)
        {
            bool iDroppedIt =
                saveData.droppedByStableId == Utilities.GetStablePlayerId(player);

            if (iDroppedIt && saveData.droppedByDeletedWaypoint)
                return; //we already deleted it locally. don't recreate
            
            if (!iDroppedIt && saveData.droppedForDeletedWaypoint)
                return; //we already deleted it locally. don't recreate
            
            if (iDroppedIt && !UserPreferences.CreateWaypointsForBagsIDrop)
            {
                MarkWaypointAsDeleted(saveData, saveData.droppedForStableId);
                return;
            }

            if (!iDroppedIt && !UserPreferences.CreateWaypointsForBagsFriendsDrop)
            {
                MarkWaypointAsDeleted(saveData, saveData.droppedForStableId);
                return;
            }
             
            Log.Warning("Creating Waypoint!");
            
            bool useCoordinates =
                forceCoordinates || container == null || container.bRemoved;

            Vector3 pos = useCoordinates
                ? new Vector3(saveData.posX, saveData.posY, saveData.posZ)
                : container.position;

            var navObject = useCoordinates
                ? NavObjectManager.Instance.RegisterNavObject(
                    iDroppedIt ? "backpack_self" : "backpack_friend",
                    pos)
                : NavObjectManager.Instance.RegisterNavObject(
                    iDroppedIt ? "backpack_self" : "backpack_friend",
                    container);

            var wp = new Waypoint
            {
                pos = Vector3i.FromVector3Rounded(pos),
                icon = "ui_game_symbol_drop",
                name = new AuthoredText { Text = name },
                bTracked = true,
                navObject = navObject,
                bIsAutoWaypoint = false,
                IsSaved = false
            };

            player.Waypoints.Collection.Add(wp);

            GameManager.Instance.World.ObjectOnMapAdd(
                new MapObjectWaypoint(wp)
                {
                    iconName = iDroppedIt
                        ? "ui_game_symbol_drop"
                        : "ui_game_symbol_challenge_homesteading_place_storage",
                    type = EnumMapObjectType.Entity
                });
        }

        /* ==========================================================
         *  WAYPOINT UTILITIES
         * ========================================================== */

        private static Waypoint FindExistingWaypoint(
            EntityPlayerLocal player,
            KnownDroppedBagSaveData saveData)
        {
            return player.Waypoints?.Collection?.hashSet?
                .FirstOrDefault(wp =>
                    wp.navObject?.trackedEntity?.entityId == saveData.entityId ||
                    wp.pos == Vector3i.FromVector3Rounded(
                        new Vector3(saveData.posX, saveData.posY, saveData.posZ)));
        }

        private static string GetWaypointName(
            EntityPlayer player,
            KnownDroppedBagSaveData saveData,
            Waypoint existing)
        {
            if (existing != null)
                return existing.name.Text;

            var baseName = saveData.droppedByStableId ==
                           Utilities.GetStablePlayerId(player)
                ? Localization.Get("lwf.waypoint.loot_drop.self")
                : string.Format(
                    Localization.Get("lwf.waypoint.loot_drop.other"),
                    saveData.droppedByDisplayName);

            return MakeUniqueWaypointName(baseName, player);
        }

        private static void FullyDeleteWaypoint(
            Waypoint wp,
            EntityPlayerLocal player)
        {
            player.RemoveNavObject(wp.navObject?.name);

            var mapObj = GameManager.Instance.World
                .GetObjectOnMapList(EnumMapObjectType.Entity)
                .FirstOrDefault(x => x.key == wp.MapObjectKey);

            if (mapObj != null)
                GameManager.Instance.World.ObjectOnMapRemove(
                    EnumMapObjectType.Entity,
                    mapObj.position);

            if (wp.navObject != null)
                NavObjectManager.Instance.UnRegisterNavObject(wp.navObject);

            player.Waypoints.Collection.Remove(wp);
            NavObjectManager.Instance.RefreshNavObjects();
        }
        
        private static string MakeUniqueWaypointName(
            string baseName,
            EntityPlayer player)
        {
            var existingNames = player.Waypoints?.Collection?.hashSet?
                .Select(wp => wp.name.Text)
                .Where(n =>
                    n == baseName ||
                    n.StartsWith(baseName + " ("))
                .ToList();

            if (existingNames == null || !existingNames.Contains(baseName))
                return baseName;

            int suffix = 2;
            string candidate;

            do
            {
                candidate = $"{baseName} ({suffix++})";
            }
            while (existingNames.Contains(candidate));

            return candidate;
        }
        
        public static void RemoveSaveDataFromWaypoint(Waypoint selectedWaypoint, int deletingPlayerEntityId)
        {
            if (!knownDroppedBags?.Any() ?? true)
                return;
            
            var deleterStableId = Utilities.GetStablePlayerId(deletingPlayerEntityId);
            for (int i = 0; i < knownDroppedBags.Count; i++)
            {
                var sd = knownDroppedBags[i];
                if (sd.entityId == selectedWaypoint.navObject?.trackedEntity?.entityId
                    || Vector3i.FromVector3Rounded(new Vector3(sd.posX, sd.posY, sd.posZ)) == selectedWaypoint.pos)
                {
                    // the waypoint being removed is one created by our mod - proceed to mark is as deleted for this player so it doesn't reappear
                    MarkWaypointAsDeleted(sd, deleterStableId);
                    return;
                }
            }
            
        }


        /* ==========================================================
         *  NETWORKING
         * ========================================================== */

        private static void SendBagUpdateToPlayers(
            KnownDroppedBagSaveData saveData,
            bool isDelete)
        {
            void Send(EntityPlayer player)
            {
                if (player == null ||
                    player == GameManager.Instance.myEntityPlayerLocal)
                    return;

                var pkg = NetPackageManager
                    .GetPackage<NetPackageServerSendClientKnownDroppedBagSaveData>()
                    .Setup(saveData, isDelete);

                ConnectionManager.Instance.SendPackage(
                    pkg,
                    true,
                    player.entityId);
            }

            Send(Utilities.FindPlayerByStableId(saveData.droppedByStableId));
            Send(Utilities.FindPlayerByStableId(saveData.droppedForStableId));
        }

        /* ==========================================================
         *  HELPERS
         * ========================================================== */

        private static KnownDroppedBagSaveData CreateSaveData(
            EntityLootContainer container,
            EntityPlayer droppedBy,
            EntityPlayer droppedFor)
        {
            return new KnownDroppedBagSaveData
            {
                entityId = container.entityId,
                posX = container.position.x,
                posY = container.position.y,
                posZ = container.position.z,
                droppedByStableId = Utilities.GetStablePlayerId(droppedBy),
                droppedForStableId = Utilities.GetStablePlayerId(droppedFor),
                droppedByDisplayName = droppedBy.PlayerDisplayName
            };
        }

        private static void ResolveContainerPosition(
            KnownDroppedBagSaveData saveData,
            out EntityLootContainer container)
        {
            container = GameManager.Instance.World
                .GetEntity(saveData.entityId) as EntityLootContainer;

            if (container != null)
            {
                saveData.posX = container.position.x;
                saveData.posY = container.position.y;
                saveData.posZ = container.position.z;
            }
        }
        
        private static void RegisterLootBagClasses()
        {
            if (NavObjectClass.NavObjectClassList
                .Any(x => x.NavObjectClassName == "backpack_self"))
                return;

            RegisterSelfBackpackClass();
            RegisterFriendBackpackClass();
        }
        
        private static void RegisterSelfBackpackClass()
        {
            var xml = new XElement("nav_object_class",
                new XAttribute("name", "backpack_self"),
                new XElement("onscreen_settings",
                    Prop("sprite_name", "ui_game_symbol_drop"),
                    Prop("color", "0,50,199,50"),
                    Prop("has_pulse", "false"),
                    Prop("text_type", "Distance"),
                    Prop("offset", "0,0.4,0")
                ),
                new XElement("map_settings",
                    Prop("sprite_name", "ui_game_symbol_drop"),
                    Prop("min_distance", "0"),
                    Prop("max_distance", "-1"),
                    Prop("color", "0,50,199,255"),
                    Prop("has_pulse", "false")
                )
            );

            NavObjectClassesFromXml.ParseNavObjectClass(xml);
        }
        
        private static void RegisterFriendBackpackClass()
        {
            var xml = new XElement("nav_object_class",
                new XAttribute("name", "backpack_friend"),
                new XElement("onscreen_settings",
                    Prop("sprite_name",
                        "ui_game_symbol_challenge_homesteading_place_storage"),
                    Prop("color", "0,255,255,255"),
                    Prop("has_pulse", "true"),
                    Prop("text_type", "Distance"),
                    Prop("offset", "0,0.4,0")
                ),
                new XElement("map_settings",
                    Prop("sprite_name",
                        "ui_game_symbol_challenge_homesteading_place_storage"),
                    Prop("min_distance", "0"),
                    Prop("max_distance", "-1"),
                    Prop("color", "0,255,255,255"),
                    Prop("has_pulse", "true")
                )
            );

            NavObjectClassesFromXml.ParseNavObjectClass(xml);
        }
        
        private static XElement Prop(string name, string value)
        {
            return new XElement(
                "property",
                new XAttribute("name", name),
                new XAttribute("value", value));
        }
        
        /* ==========================================================
         *  PERSISTENCE
         * ========================================================== */
        public static void LoadWaypoints()
        {
            NetGuards.ServerOnly(nameof(LoadWaypoints));

            if (!Directory.Exists(Utilities.ModSaveDir))
                Directory.CreateDirectory(Utilities.ModSaveDir);

            if (!File.Exists(LootWaypointsFile))
            {
                knownDroppedBags = new List<KnownDroppedBagSaveData>();
                return;
            }

            var json = File.ReadAllText(LootWaypointsFile);
            knownDroppedBags =
                JsonConvert.DeserializeObject<List<KnownDroppedBagSaveData>>(json)
                ?? new List<KnownDroppedBagSaveData>();
        }
        
        public static void SaveWaypoints()
        {
            NetGuards.ServerOnly(nameof(SaveWaypoints));

            if (!Directory.Exists(Utilities.ModSaveDir))
                Directory.CreateDirectory(Utilities.ModSaveDir);

            File.WriteAllText(
                LootWaypointsFile,
                JsonConvert.SerializeObject(
                    knownDroppedBags,
                    Formatting.Indented));
        }
        
        public static void MarkWaypointAsDeleted(KnownDroppedBagSaveData sd, string deleterStableId)
        {
            if (ConnectionManager.Instance.IsServer)
            {
                //if both players don't care anymore, delete it. otherwise, we need to leave it around
                if (sd.droppedByStableId == deleterStableId)
                    sd.droppedByDeletedWaypoint = true;

                if (sd.droppedForStableId == deleterStableId)
                    sd.droppedForDeletedWaypoint = true;

                if (sd.droppedByDeletedWaypoint &&
                    (string.IsNullOrEmpty(sd.droppedForStableId)
                     || sd.droppedForDeletedWaypoint))
                {
                    SendBagUpdateToPlayers(sd, true);
                    knownDroppedBags.Remove(sd);
                }
            }
            else //let server know
            {
                var deleter = Utilities.FindPlayerByStableId(deleterStableId);
                var pkg = NetPackageManager
                    .GetPackage<NetPackageClientOnTrackedWaypointRemoved>()
                    .Setup(sd,deleter);

                ConnectionManager.Instance.SendToServer(pkg);
            }
        }

        

        
    }
}
