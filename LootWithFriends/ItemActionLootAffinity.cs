namespace LootWithFriends
{
    public class ItemActionEntryLootAffinity : BaseItemActionEntry
    {
        private XUiController itemController;

        public ItemActionEntryLootAffinity(XUiController controller)
        :base (
            controller,
            "Loot Affinity",
            "",
            GamepadShortCut.None,
            "crafting/craft_click_craft",
            "ui/ui_denied")
        {
            
        }

        public override void OnActivated()
        {
            
            Log.Out("LootWithFriends: Loot Affinity clicked");

            if (ItemController is XUiC_ItemStack stackController)
            {
                ItemStack stack = stackController.ItemStack;
                ItemValue itemValue = stack.itemValue;
                ItemClass itemClass = itemValue.ItemClass;

                Log.Out($"Clicked item: {itemClass?.Name} (id={itemClass?.Id})");
            }

            // You have access to:
            // itemController (XUiC_ItemStack)
            // ItemClass via stackController.ItemStack.itemValue.ItemClass
        }
    }

}