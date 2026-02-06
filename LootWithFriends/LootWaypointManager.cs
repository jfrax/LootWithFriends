using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UniLinq;

public static class LootWaypointManager
{
    private static readonly List<LootContainerWaypointTracker> Trackers
        = new List<LootContainerWaypointTracker>();

    public static void AddForLocalPlayer(EntityLootContainer container, string creatorName)
    {
        var player = GameManager.Instance.myEntityPlayerLocal;
        RegisterLootBagClasses();
        var iDroppedIt = creatorName == player.PlayerDisplayName;
        
        var wp = new Waypoint
        {
            icon = "ui_game_symbol_drop",
            name = new AuthoredText() { Text = iDroppedIt ? "My Loot Drop" : $"{creatorName}'s Loot Drop" },
            bTracked = true,
            navObject = NavObjectManager.Instance.RegisterNavObject(
                 iDroppedIt ? "backpack_self" : "backpack_friend",
                container
            ),
            bIsAutoWaypoint = true,
            IsSaved = false
        };

        player.Waypoints.Collection.Add(wp);

        Trackers.Add(new LootContainerWaypointTracker
        {
            Container = container,
            Owner = player,
            Waypoint = wp
        });
    }

    public static void Update()
    {
        for (int i = Trackers.Count - 1; i >= 0; i--)
        {
            var t = Trackers[i];

            if (t.Container == null ||
                t.Container.bRemoved ||
                !t.Container.IsSpawned())
            {
                RemoveWaypoint(t);
                Trackers.RemoveAt(i);
            }
        }
    }

    private static void RemoveWaypoint(LootContainerWaypointTracker t)
    {
        t.Owner?.Waypoints?.Collection?.Remove(t.Waypoint);
    }

    public static void RegisterLootBagClasses()
    {
        if (NavObjectClass.NavObjectClassList.Any(x => x.NavObjectClassName == "backpack_self"))
            return;
        
        // Self backpack
        var selfXml = new XElement("nav_object_class",
            new XAttribute("name", "backpack_self"),
            new XElement("onscreen_settings",
                new XElement("property", new XAttribute("name", "sprite_name"),
                    new XAttribute("value", "ui_game_symbol_drop")),
                new XElement("property", new XAttribute("name", "color"), new XAttribute("value", "0,50,199,50")),
                new XElement("property", new XAttribute("name", "has_pulse"), new XAttribute("value", "false")),
                new XElement("property", new XAttribute("name", "text_type"), new XAttribute("value", "Distance")),
                new XElement("property", new XAttribute("name", "offset"), new XAttribute("value", "0,0.4,0"))
            )
        );

        NavObjectClassesFromXml.ParseNavObjectClass(selfXml);

        // Friend backpack
        var friendXml = new XElement("nav_object_class",
            new XAttribute("name", "backpack_friend"),
            new XElement("onscreen_settings",
                new XElement("property", new XAttribute("name", "sprite_name"),
                    new XAttribute("value", "ui_game_symbol_challenge_homesteading_place_storage")),
                new XElement("property", new XAttribute("name", "color"), new XAttribute("value", "0,255,255,255")),
                new XElement("property", new XAttribute("name", "has_pulse"), new XAttribute("value", "true")),
                new XElement("property", new XAttribute("name", "text_type"), new XAttribute("value", "Distance")),
                new XElement("property", new XAttribute("name", "offset"), new XAttribute("value", "0,0.4,0"))
            )
        );
        NavObjectClassesFromXml.ParseNavObjectClass(friendXml);
    }
}

public class LootContainerWaypointTracker
{
    public EntityLootContainer Container;
    public Waypoint Waypoint;
    public EntityPlayer Owner;
}