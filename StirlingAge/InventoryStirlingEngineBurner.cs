/* Based on InventorySmelting from vssurvivalmod */

using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

public class InventoryStirlingEngineBurner : InventoryBase, ISlotProvider
{
    ItemSlot[] slots;
    public BlockPos? pos;

    public ItemSlot[] Slots {
        get { return slots; }
    }

    public InventoryStirlingEngineBurner(string inventoryID, ICoreAPI api) : base(inventoryID, api) {
        // Empty because CSharp is stooooopid.    
    }

    public override void LateInitialize(string inventoryID, ICoreAPI api)
    {
        slots = GenEmptySlots(1);
        baseWeight = 4f;
        base.LateInitialize(inventoryID, api);
    }



    public override int Count {
        get { return slots.Length; }
    }

    public override ItemSlot this[int slotId] {
        get {
            if (slotId < 0 || slotId >= Count) throw new ArgumentOutOfRangeException(nameof(slotId));
            return slots[slotId];
        }
        set {
            if (slotId < 0 || slotId >= Count) throw new ArgumentOutOfRangeException(nameof(slotId));
            if (value == null) throw new ArgumentNullException(nameof(value));
            slots[slotId] = value;
        }
    }

    public override void FromTreeAttributes(ITreeAttribute tree) {
        List<ItemSlot> modifiedSlots = new List<ItemSlot>();
        slots = SlotsFromTreeAttributes(tree, slots, modifiedSlots);
        for (int i = 0; i < modifiedSlots.Count; i++) DidModifyItemSlot(modifiedSlots[i]);
    }
        
    

    public override void ToTreeAttributes(ITreeAttribute tree) {
        SlotsToTreeAttributes(slots, tree);
    }

    public override void OnItemSlotModified(ItemSlot slot) {
        base.OnItemSlotModified(slot);
    }

    protected override ItemSlot NewSlot(int i) {
        if (i == 0) return new ItemSlotSurvival(this); // Fuel
        else throw new ArgumentOutOfRangeException(nameof(i));
    }


    public override float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge)
    {
        ItemStack stack = sourceSlot.Itemstack;

        if (targetSlot == slots[0] && (stack.Collectible.CombustibleProps == null || stack.Collectible.CombustibleProps.BurnTemperature <= 0)) return 0;

        return base.GetSuitability(sourceSlot, targetSlot, isMerge);
    }
}
