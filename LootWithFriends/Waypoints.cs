using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Newtonsoft.Json;
using UniLinq;
using UnityEngine;

namespace LootWithFriends
{
    public class Waypoints
    {
        private static List<KnownDroppedBagSaveData> knownDroppedBags = new List<KnownDroppedBagSaveData>();

        private static string LootWaypointsFile =>
            Path.Combine(Utilities.ModSaveDir, "lootwaypoints.json");

        public static void LootContainerAdded(EntityLootContainer lootContainer, EntityPlayer droppedBy,
            EntityPlayer droppedFor)
        {
            NetGuards.ServerOnly("LootContainerAdded");
            var saveData = new KnownDroppedBagSaveData
            {
                entityId = lootContainer.entityId,
                posX = lootContainer.position.x,
                posY = lootContainer.position.y,
                posZ = lootContainer.position.z,
                droppedByStableId = Utilities.GetStablePlayerId(droppedBy),
                droppedForStableId = Utilities.GetStablePlayerId(droppedFor),
                droppedByDisplayName = droppedBy.PlayerDisplayName
            };

            //update server collection
            knownDroppedBags.Add(saveData);

            //update clients
            var pkg = NetPackageManager.GetPackage<NetPackageServerSendClientKnownDroppedBagSaveData>().Setup(saveData);

            var serverPlayerId = GameManager.Instance.myEntityPlayerLocal?.entityId ?? -1;
            var droppedForNonServerPlayer = droppedFor != null && droppedFor.entityId != serverPlayerId;
            var droppedByNonServerPlayer = droppedBy != null && droppedBy.entityId != serverPlayerId;
            
            if (droppedForNonServerPlayer)
            {
                ConnectionManager.Instance.SendPackage(
                    pkg,
                    _onlyClientsAttachedToAnEntity: true,
                    _attachedToEntityId: droppedFor.entityId
                );
            }

            if (droppedByNonServerPlayer)
            {
                ConnectionManager.Instance.SendPackage(
                    pkg,
                    _onlyClientsAttachedToAnEntity: true,
                    _attachedToEntityId: droppedBy.entityId
                );
            }

            //TODO: need to add this check everywhere in case local server player doesn't exist
            //update server player
            if (Utilities.LocalPlayerExists())
            {
                //not positive about this, but i think the server player should always have a reference to the bag at this point
                //we will assume so and optimistically create the waypoint as a container reference type (it falls back to coordinate-based anyway)
                AddUpdateOrDeleteWaypointForLocalPlayer(saveData, false, false);
            }
        }

        public static void AddUpdateOrDeleteWaypointForLocalPlayer(KnownDroppedBagSaveData saveData, bool beingDeleted, bool asCoordinateReference)
        {
            Log.Warning(StackTraceUtility.ExtractStackTrace());
            
            var player = GameManager.Instance.myEntityPlayerLocal;
            if (player == null)
                return;

            RegisterLootBagClasses();

            
            var container = GameManager.Instance.World.GetEntity(saveData.entityId) as EntityLootContainer;
            if (container != null)
            {
                //container coordinates are better if available - let's ensure our savedata is up to date
                saveData.posX = container.position.x;
                saveData.posY = container.position.y;
                saveData.posZ = container.position.z;
            }
                
            
            var iDroppedIt = saveData.droppedByStableId == Utilities.GetStablePlayerId(player);
           
            
            //first, check if we have a waypoint either with the entity attached or at the position of the saveData
            Waypoint wp = null;

            if (player.Waypoints?.Collection?.hashSet != null)
            {
                foreach (var candidate in player.Waypoints.Collection.hashSet)
                {
                    var trackedEntityId = candidate.navObject?.trackedEntity?.entityId;
                    if (trackedEntityId != null && trackedEntityId == saveData.entityId)
                    {
                        wp = candidate;
                        break;
                    }

                    Log.Error("Trying to look up by coordinates!");
                    if (candidate.pos == Vector3i.FromVector3Rounded(new Vector3(saveData.posX, saveData.posY, saveData.posZ)))
                    {
                        wp = candidate;
                        break;
                    }
                }
            }
            
            if (beingDeleted)
            {
                Log.Out("beingDeleted");
                if (wp != null)
                {
                    Log.Out("The waypoint existed for us to delete");
                    
                    GameManager.Instance.myEntityPlayerLocal.RemoveNavObject(wp.navObject.name);

                    var mo = GameManager.Instance.World.GetObjectOnMapList(EnumMapObjectType.Entity)
                        .FirstOrDefault(x => x.key == wp.MapObjectKey);
                    if (mo != null)
                    {
                        Log.Out("Removing MapObject");
                        GameManager.Instance.World.ObjectOnMapRemove(EnumMapObjectType.Entity, mo.position);
                    }
                    
                    if (wp.navObject != null)
                    {
                        Log.Out("Unregistering NavObject");
                        NavObjectManager.Instance.UnRegisterNavObject(wp.navObject);
                        NavObjectManager.Instance.RefreshNavObjects();
                    }
                    
                    player.Waypoints.Collection.Remove(wp);
                }
                else
                {
                    Log.Error("The waypoint didn't exist for us to delete!!!");
                }

                knownDroppedBags.Remove(saveData);

                return;
            }
            
            //if the waypoint already existed, we want to note its name, then recreate it
            var waypointName = GetWaypointInitialName(saveData, iDroppedIt, wp);
            if (wp == null)
            {
                //didn't exist - let's ensure we have a good unique name
                waypointName = MakeUniqueWaypointName(waypointName, player);
            }
            else
            {
                //did exist - we'll leave its name alone as we noted before; delete and re-add
                player.Waypoints.Collection.Remove(wp);
            }

            if (asCoordinateReference || container == null || (container?.bRemoved ?? true))
            {
                Log.Warning("About to make location-based wp");
                var pos = new Vector3(saveData.posX, saveData.posY, saveData.posZ);
                
                //based on location (needed so it can still show on the map)
                wp = new Waypoint
                {
                    pos = Vector3i.FromVector3Rounded(pos),
                    icon = "ui_game_symbol_drop",
                    name = new AuthoredText() { Text = waypointName },
                    bTracked = true,
                    navObject = NavObjectManager.Instance.RegisterNavObject(
                        iDroppedIt ? "backpack_self" : "backpack_friend",
                        pos
                    ),
                    bIsAutoWaypoint = false,
                    IsSaved = false
                };
            }
            else
            {
                //based on reference to the actual container (it's loaded into the chunk on our world)
                Log.Warning("About to make container-based wp");
                wp = new Waypoint
                {
                    pos = Vector3i.FromVector3Rounded(container.position),
                    icon = "ui_game_symbol_drop",
                    name = new AuthoredText() { Text = waypointName },
                    bTracked = true,
                    navObject = NavObjectManager.Instance.RegisterNavObject(
                        iDroppedIt ? "backpack_self" : "backpack_friend",
                        container
                    ),
                    bIsAutoWaypoint = false,
                    IsSaved = false
                }; 
            }

            player.Waypoints.Collection.Add(wp);
            var mapObj = new MapObjectWaypoint(wp)
            {
                iconName = iDroppedIt ? "ui_game_symbol_drop" : "ui_game_symbol_challenge_homesteading_place_storage",
                type = EnumMapObjectType.Entity
            };

            GameManager.Instance.World.ObjectOnMapAdd(mapObj);
            
        }

        public static void LootContainerRemoved(EntityLootContainer lootContainer)
        {
            NetGuards.ServerOnly("LootContainerRemoved");
            Log.Out("LootContainerRemoved called");
            var toRemove = knownDroppedBags.FirstOrDefault(x => x.entityId == lootContainer.entityId);
            if (toRemove == null)
                return; //probably a container not created by our mod

            Log.Out("LootContainerRemoved - toRemove was not null!");

            var droppedBy = Utilities.FindPlayerByStableId(toRemove.droppedByStableId);
            var droppedFor = Utilities.FindPlayerByStableId(toRemove.droppedForStableId);
            
            if (droppedBy != null && droppedBy != GameManager.Instance.myEntityPlayerLocal)
            {
                Log.Out("Sending pkg to droppedBy player");
                var pkg = NetPackageManager.GetPackage<NetPackageServerSendClientKnownDroppedBagSaveData>().Setup(toRemove, true);
                ConnectionManager.Instance.SendPackage(
                    pkg,
                    _onlyClientsAttachedToAnEntity: true,
                    _attachedToEntityId: droppedBy.entityId
                );
            }
            
            if (droppedFor != null && droppedFor != GameManager.Instance.myEntityPlayerLocal)
            {
                Log.Out("Sending pkg to droppedFor player");
                var pkg = NetPackageManager.GetPackage<NetPackageServerSendClientKnownDroppedBagSaveData>().Setup(toRemove, true);
                ConnectionManager.Instance.SendPackage(
                    pkg,
                    _onlyClientsAttachedToAnEntity: true,
                    _attachedToEntityId: droppedFor.entityId
                );
            }
            
            AddUpdateOrDeleteWaypointForLocalPlayer(toRemove,true, false);
        }

        private static string GetWaypointInitialName(KnownDroppedBagSaveData saveData, bool iDroppedIt,
            Waypoint existingWaypoint)
        {
            if (existingWaypoint != null)
                return existingWaypoint.name.Text;

            return iDroppedIt
                ? Localization.Get("lwf.waypoint.loot_drop.self")
                : string.Format(
                    Localization.Get("lwf.waypoint.loot_drop.other"),
                    saveData.droppedByDisplayName);
        }


        private static string MakeUniqueWaypointName(string baseName, EntityPlayer player)
        {
            var existing = player.Waypoints.Collection.hashSet
                .Select(x => x.name.Text)
                .Where(n => n == baseName || n.StartsWith(baseName + " ("))
                .ToList();

            if (!existing.Contains(baseName))
                return baseName;

            int i = 2;
            string candidate;
            do
            {
                candidate = $"{baseName} ({i})";
                i++;
            } while (existing.Contains(candidate));

            return candidate;
        }

        private static void RegisterLootBagClasses()
        {
            if (NavObjectClass.NavObjectClassList.Any(x => x.NavObjectClassName == "backpack_self"))
                return;

            // Self backpack
            var selfXml = new XElement("nav_object_class",
                new XAttribute("name", "backpack_self"),
                new XElement("onscreen_settings",
                    new XElement("property", new XAttribute("name", "sprite_name"),
                        new XAttribute("value", "ui_game_symbol_drop")),
                    new XElement("property", new XAttribute("name", "color"),
                        new XAttribute("value", "0,50,199,50")),
                    new XElement("property", new XAttribute("name", "has_pulse"),
                        new XAttribute("value", "false")),
                    new XElement("property", new XAttribute("name", "text_type"),
                        new XAttribute("value", "Distance")),
                    new XElement("property", new XAttribute("name", "offset"),
                        new XAttribute("value", "0,0.4,0"))
                ),
                new XElement("map_settings",
                    new XElement("property", new XAttribute("name", "sprite_name"),
                        new XAttribute("value", "ui_game_symbol_drop")),
                    new XElement("property", new XAttribute("name", "min_distance"),
                        new XAttribute("value", "0")),
                    new XElement("property", new XAttribute("name", "max_distance"),
                        new XAttribute("value", "-1")),
                    new XElement("property", new XAttribute("name", "color"),
                        new XAttribute("value", "0,50,199,255")),
                    new XElement("property", new XAttribute("name", "has_pulse"),
                        new XAttribute("value", "false"))
                )
            );

            NavObjectClassesFromXml.ParseNavObjectClass(selfXml);

            // Friend backpack
            var friendXml = new XElement("nav_object_class",
                new XAttribute("name", "backpack_friend"),
                new XElement("onscreen_settings",
                    new XElement("property", new XAttribute("name", "sprite_name"),
                        new XAttribute("value", "ui_game_symbol_challenge_homesteading_place_storage")),
                    new XElement("property", new XAttribute("name", "color"),
                        new XAttribute("value", "0,255,255,255")),
                    new XElement("property", new XAttribute("name", "has_pulse"),
                        new XAttribute("value", "true")),
                    new XElement("property", new XAttribute("name", "text_type"),
                        new XAttribute("value", "Distance")),
                    new XElement("property", new XAttribute("name", "offset"),
                        new XAttribute("value", "0,0.4,0"))
                ),
                new XElement("map_settings",
                    new XElement("property", new XAttribute("name", "sprite_name"),
                        new XAttribute("value", "ui_game_symbol_challenge_homesteading_place_storage")),
                    new XElement("property", new XAttribute("name", "min_distance"),
                        new XAttribute("value", "0")),
                    new XElement("property", new XAttribute("name", "max_distance"),
                        new XAttribute("value", "-1")),
                    new XElement("property", new XAttribute("name", "color"),
                        new XAttribute("value", "0,255,255,255")),
                    new XElement("property", new XAttribute("name", "has_pulse"),
                        new XAttribute("value", "true"))
                )
            );

            NavObjectClassesFromXml.ParseNavObjectClass(friendXml);
        }

        public static void UpdateWaypointWithBagReference(EntityLootContainer lootContainer)
        {
            var bagMatch = knownDroppedBags.FirstOrDefault(x => x.entityId == lootContainer.entityId);
            if (bagMatch != null)
            {
                AddUpdateOrDeleteWaypointForLocalPlayer(bagMatch, false, false);
            }
        }

        public static void UpdateWaypointWithCoordinateReference(EntityLootContainer lootContainer)
        {
            var bagMatch = knownDroppedBags.FirstOrDefault(x => x.entityId == lootContainer.entityId);
            if (bagMatch != null)
            {
                AddUpdateOrDeleteWaypointForLocalPlayer(bagMatch, false, true);
            }
        }

        public static void LoadWaypoints()
        {
            NetGuards.ServerOnly("LoadWaypoints");
            if (!Directory.Exists(Utilities.ModSaveDir))
                Directory.CreateDirectory(Utilities.ModSaveDir);

            if (File.Exists(LootWaypointsFile))
            {
                string json = File.ReadAllText(LootWaypointsFile);
                knownDroppedBags = JsonConvert.DeserializeObject<List<KnownDroppedBagSaveData>>(json) ??
                                   new List<KnownDroppedBagSaveData>();
            }
            else
            {
                knownDroppedBags = new List<KnownDroppedBagSaveData>();
            }
        }

        public static void SaveWaypoints()
        {
            NetGuards.ServerOnly("SaveWaypoints");
            if (!Directory.Exists(Utilities.ModSaveDir))
                Directory.CreateDirectory(Utilities.ModSaveDir);

            Log.Out($"Saving waypoints to {LootWaypointsFile}");
            File.WriteAllText(LootWaypointsFile, JsonConvert.SerializeObject(knownDroppedBags, Formatting.Indented));
        }

        public static void FetchPlayerWaypoints(EntityPlayer entityPlayer)
        {
            NetGuards.ServerOnly("FetchPlayerWaypoints");
            if (entityPlayer == GameManager.Instance.myEntityPlayerLocal)
            {
                //host player can just directly load up from here
                if (knownDroppedBags?.Any() ?? false)
                {
                    foreach (var bag in knownDroppedBags)
                    {
                        AddUpdateOrDeleteWaypointForLocalPlayer(bag, false, false);
                    }
                }
            }
            else
            {
                //all other clients will need to be sent their waypoints
                if (knownDroppedBags?.Any() ?? false)
                {
                    foreach (var saveData in knownDroppedBags)
                    {
                        var droppedBy = Utilities.FindPlayerByStableId(saveData.droppedByStableId);
                        var droppedFor = Utilities.FindPlayerByStableId(saveData.droppedForStableId);
                        
                        //dropped by should always be present.
                        var pkg = NetPackageManager.GetPackage<NetPackageServerSendClientKnownDroppedBagSaveData>().Setup(saveData);
                        ConnectionManager.Instance.SendPackage(
                            pkg,
                            _onlyClientsAttachedToAnEntity: true,
                            _attachedToEntityId: droppedBy.entityId
                        );
                       
                        //droppedfor is optional, and we might need to send to that person too if they are different
                        if (droppedFor != null && droppedFor != droppedBy)
                        {
                            var pkg2 = NetPackageManager.GetPackage<NetPackageServerSendClientKnownDroppedBagSaveData>().Setup(saveData);
                            ConnectionManager.Instance.SendPackage(
                                pkg2,
                                _onlyClientsAttachedToAnEntity: true,
                                _attachedToEntityId: droppedFor.entityId
                            );
                        }
                    }
                }
            }
        }
    }
}