namespace LootWithFriends
{
    public class ItemActionEntryLootAffinity : BaseItemActionEntry
    {
        //private string actionName = "LWF: No Pref";

        public static string GetTextForAffinity(AffinityTypes affinity)
        {
            string result = "LWF: No Pref";
            
            switch (affinity)
            {
                case AffinityTypes.NoPreference:
                    result = "LWF: No Pref";
                    break;
                case AffinityTypes.PreferDropping:
                    result = "LWF: PreferDropping";
                    break;
                case  AffinityTypes.PreferReceiving:
                    result = "LWF: PreferReceiving";
                    break;
            }
            return result;
        }
        
        private void SetTextForAffinity(AffinityTypes affinity)
        {
            this.ActionName = GetTextForAffinity(affinity);
            this.ParentActionList.RefreshActionList();
        }

        public ItemActionEntryLootAffinity(XUiController controller, string initialActionName)
            : base(
                controller,
                initialActionName,
                "ui_game_symbol_allies",
                GamepadShortCut.None,
                "crafting/craft_click_craft",
                "ui/ui_denied")
        {
            
        }
        
        

        public override void OnActivated()
        {
            Log.Out("LootWithFriends: Loot Affinity clicked");

            var player = GameManager.Instance.myEntityPlayerLocal;

            if (ItemController is XUiC_ItemStack stackController)
            {
                ItemStack stack = stackController.ItemStack;
                ItemValue itemValue = stack.itemValue;
                ItemClass itemClass = itemValue.ItemClass;

                var currentAffinity = Affinity.GetAffinity(player, itemClass.Name);

                if (currentAffinity == AffinityTypes.NoPreference)
                {
                    Affinity.SetAffinity(player, itemClass, AffinityTypes.PreferDropping);
                    SetTextForAffinity(AffinityTypes.PreferDropping);
                    Log.Out($"Affinity for {itemClass?.Name} has been set to PreferDropping.");
                }
                else if (currentAffinity == AffinityTypes.PreferDropping)
                {
                    Affinity.SetAffinity(player, itemClass, AffinityTypes.PreferReceiving);
                    SetTextForAffinity(AffinityTypes.PreferReceiving);
                    Log.Out($"Affinity for {itemClass?.Name} has been set to PreferReceiving.");
                }
                else if (currentAffinity == AffinityTypes.PreferReceiving)
                {
                    Affinity.SetAffinity(player, itemClass, AffinityTypes.NoPreference);
                    SetTextForAffinity(AffinityTypes.NoPreference);
                    Log.Out($"Affinity for {itemClass?.Name} has been set to NoPreference.");
                }

                Log.Out($"Clicked item: {itemClass?.Name} (id={itemClass?.Id})");
            }
        }
    }
}