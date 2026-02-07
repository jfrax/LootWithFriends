using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using LootWithFriends;
using Newtonsoft.Json;
using UniLinq;

public static class LootWaypointManager
{
    private static readonly List<LootContainerWaypointTracker> Trackers
        = new List<LootContainerWaypointTracker>();

    private static string LootWaypointsFile =>
        Path.Combine(Utilities.ModSaveDir, "lootwaypoints.json");

    public static void AddForLocalPlayer(EntityLootContainer container, EntityPlayer droppingPlayer)
    {
        AddForLocalPlayer(container, Utilities.GetStablePlayerId(droppingPlayer), droppingPlayer.PlayerDisplayName);
    }

    private static void AddForLocalPlayer(EntityLootContainer container, string playerDroppingStableId,
        string playerDroppingDisplayName)
    {
        var player = GameManager.Instance.myEntityPlayerLocal;
        RegisterLootBagClasses();

        var iDroppedIt = playerDroppingStableId == Utilities.GetStablePlayerId(player);

        var waypointName = iDroppedIt
            ? Localization.Get("lwf.waypoint.loot_drop.self")
            : string.Format(
                Localization.Get("lwf.waypoint.loot_drop.other"),
                playerDroppingDisplayName);

        waypointName = MakeUniqueWaypointName(waypointName);

        string MakeUniqueWaypointName(string baseName)
        {
            var existing = player.Waypoints.Collection.hashSet
                .Select(x => x.name.Text)
                .Where(n => n == baseName || n.StartsWith(baseName + " ("));

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

        var wp = new Waypoint
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

        player.Waypoints.Collection.Add(wp);
        var mapObj = new MapObjectWaypoint(wp)
        {
            iconName = iDroppedIt ? "ui_game_symbol_drop" : "ui_game_symbol_challenge_homesteading_place_storage"
        };
        GameManager.Instance.World.ObjectOnMapAdd(mapObj);

        Trackers.Add(new LootContainerWaypointTracker(container, wp, playerDroppingStableId,
            playerDroppingDisplayName));
    }


    public static void Update()
    {
        for (int i = Trackers.Count - 1; i >= 0; i--)
        {
            var t = Trackers[i];

            if (t.Container == null)
            {
                //Log.Warning("Removing waypoint!!");
                //RemoveWaypoint(t);
                //Trackers.RemoveAt(i);
            }
        }
    }

    private static void RemoveWaypoint(LootContainerWaypointTracker t)
    {
        GameManager.Instance.myEntityPlayerLocal?.Waypoints?.Collection?.Remove(t.Waypoint);
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
                    new XAttribute("value", "9999")),
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
                    new XAttribute("value", "9999")),
                new XElement("property", new XAttribute("name", "color"),
                    new XAttribute("value", "0,255,255,255")),
                new XElement("property", new XAttribute("name", "has_pulse"),
                    new XAttribute("value", "true"))
            )
        );

        NavObjectClassesFromXml.ParseNavObjectClass(friendXml);
    }

    public static void LoadWaypoints()
    {
        if (!Directory.Exists(Utilities.ModSaveDir))
            Directory.CreateDirectory(Utilities.ModSaveDir);

        if (File.Exists(LootWaypointsFile))
        {
            string json = File.ReadAllText(LootWaypointsFile);
            var saveInfos =
                JsonConvert.DeserializeObject<Dictionary<int, (string, string)>>(json); //entity id, stable player id

            if (saveInfos != null && saveInfos.Count > 0)
            {
                foreach (var saveInfo in saveInfos)
                {
                    var containerMatch = GameManager.Instance.World.GetEntity(saveInfo.Key) as EntityLootContainer;

                    if (containerMatch == null)
                        continue; //despawned or collected since we were last logged in - skip it

                    AddForLocalPlayer(containerMatch, saveInfo.Value.Item1, saveInfo.Value.Item2);
                }
            }
        }
    }

    public static void SaveWaypoints()
    {
        if (!Directory.Exists(Utilities.ModSaveDir))
            Directory.CreateDirectory(Utilities.ModSaveDir);
        
        Log.Out($"Saving waypoints to {LootWaypointsFile}");
        
        var toSave = new Dictionary<int, (string, string)>();

        if (Trackers?.Any() ?? false)
        {
            foreach (var t in Trackers)
            {
                toSave.Add(t.SaveInfo.Item1, (t.SaveInfo.Item2.Item1, t.SaveInfo.Item2.Item2));
            }
        }
        
        File.WriteAllText(LootWaypointsFile, JsonConvert.SerializeObject(toSave, Formatting.Indented));
    }
}

public class LootContainerWaypointTracker
{
    public readonly EntityLootContainer Container;
    public readonly Waypoint Waypoint;
    public readonly (int, (string, string)) SaveInfo;

    public LootContainerWaypointTracker(EntityLootContainer container, Waypoint waypoint, string droppingPlayerStableId,
        string droppingPlayerDisplayName)
    {
        Container = container;
        Waypoint = waypoint;
        SaveInfo = (container.entityId,
            (droppingPlayerStableId, droppingPlayerDisplayName));
    }
}