

namespace LootWithFriends
{
    public class ItemActionList  : XUiController
    {
        // Called when the grid is initialized
        public override void Init()
        {
            Log.Error("ItemActionList controller initialized");
            base.Init();
            
        }

        // Optional: called when you want to populate your items
        public void PopulateActions(ItemStack stack)
        {
            Log.Out($"Populating actions for {stack.itemValue?.ItemClass?.Name}");
            // Add your logic here to dynamically add buttons, etc.
        }
    }
}