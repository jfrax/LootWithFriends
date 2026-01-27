using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LootWithFriends
{
    public class Class1 : IModApi
    {
        public void InitMod(Mod _modInstance)
        {
            //ModEvents.PlayerSpawnedInWorld.RegisterHandler(PlayerSpawnedInWorld);
        }

        private void PlayerSpawnedInWorld(ref ModEvents.SPlayerSpawnedInWorldData _data)
        {
            Log.Warning($"This is your mod saying hello!");
            DumpLocalPlayerInventory();
        }

        public static void DumpLocalPlayerInventory()
        {
            var player = GameManager.Instance.World.GetPrimaryPlayer();
            if (player == null)
            {
                Log.Out("No local player");
                return;
            }

            var bag = player.bag;

            for (int i = 0; i < bag.GetSlots().Length; i++)
            {
                ItemStack stack = bag.GetSlots()[i];

                if (stack.IsEmpty())
                    continue;

                string itemName = stack.itemValue.ItemClass.GetItemName();
                int count = stack.count;

                Log.Out($"Slot {i}: {itemName} x{count}");
            }


            //foreach (var slot in player.bag.GetSlots())
            //{
            //    Log.Out($"ITEM IN BAG: {slot?.itemValue. ?.GetItemName()}");
            //}

            //foreach (var slot in player.inventory.slots)
            //{
            //    Log.Out($"ITEM IN INVENTORY: {slot?.item?.GetItemName()}");
            //}

            //string path = Path.Combine(
            //    Application.persistentDataPath,
            //    "inventory_dump.txt");

            //ReflectionDumper.DumpObject(player.inventory, path);

            //Log.Out($"Inventory dump written to {path}");
        }
    }
}
