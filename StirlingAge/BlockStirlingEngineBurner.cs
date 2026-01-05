/* Based on BlockCreativeRotor.cs and BlockFirepit.cs and BlockPulverizer.cs
   from vssurvivalmod */

// TODO: It may be impossible to keep this file as-is, it may need to be refactored into Base, Clay and Metal as with the BlockEntity classes.

using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;

public class BlockStirlingEngineBurner : BlockMPBase, IIgnitable, IWrenchOrientable {

    public bool IsExtinct;

    private BlockFacing our_orientation = default;

    WorldInteraction[] interactions = System.Array.Empty<WorldInteraction>();

    public override void OnLoaded(ICoreAPI api) {
        base.OnLoaded(api);
        List<String> validSides = new List<String>();
        validSides.Add("north");
        validSides.Add("south");
        validSides.Add("east");
        validSides.Add("west");

        if (!validSides.Contains(Variant["side"])){
            api.Logger.Log(EnumLogType.Error, "Tried to load a Burner block with a 'side' of " + Variant["side"]);
            return;
        }
        our_orientation = BlockFacing.FromFirstLetter(Variant["side"][0]);
        interactions = ObjectCacheUtil.GetOrCreate(api, "stirlingEngineInteractions", () => {
            List<ItemStack> canIgniteStacks = BlockBehaviorCanIgnite.CanIgniteStacks(api, true);

            return new WorldInteraction[] {
                new WorldInteraction() {
                    ActionLangCode = "blockhelp-firepit-open",
                    MouseButton = EnumMouseButton.Right,
                },
                new WorldInteraction() {
                    ActionLangCode = "blockhelp-firepit-ignite",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "shift",
                    Itemstacks = canIgniteStacks.ToArray(),
                    GetMatchingStacks = (wi, bs, es) => {
                        BlockEntityStirlingEngineBurnerBase bef = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityStirlingEngineBurnerBase;
                        if (bef?.fuelSlot != null && !bef.fuelSlot.Empty && !bef.IsBurning)
                        {
                            return wi.Itemstacks;
                        }
                        return null;
                    }
                },
                new WorldInteraction() {
                    ActionLangCode = "blockhelp-firepit-refuel",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "shift"
                }
            };
        });
    }

    public bool IsOrientedTo(BlockFacing facing)
    {
        return facing == our_orientation;
    }

    public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode) {
        if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode)) {
            return false;
        }

        // HACK: HorizontalOrientable should be doing this for us but it isn't!
        // also, we want to face away anyway
        BlockFacing[] horVer = Block.SuggestedHVOrientation(byPlayer, blockSel);
        horVer[0] = horVer[0].Opposite;

        api.Logger.Log(EnumLogType.Notification, $"StirlingEngineBurner TryPlaceBlock: requested facing {horVer[0]}, current facing {our_orientation}");

        if(our_orientation != horVer[0]) {
            api.Logger.Log(EnumLogType.Notification, $"Orientation mismatch, trying to find block with variant {horVer[0].Code}");

            // Handle both clay and metal variants
            string variantKey = "side";
            string variantValue = horVer[0].Code;

            // For metal blocks, we also need to preserve the metal type variant
            if (Code.Path.Contains("metal")) {
                api.Logger.Log(EnumLogType.Notification, $"Detected metal burner, preserving metal variant: {Variant["metal"]}");
                // Need to get block with both side and metal variants
                Block b = api.World.BlockAccessor.GetBlock(CodeWithVariants(new string[]{"side", "metal"}, new string[]{variantValue, Variant["metal"]}));
                if(b != null) {
                    api.Logger.Log(EnumLogType.Notification, $"Found metal variant block: {b.Code}, delegating placement");
                    return b.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
                }
            } else {
                // Clay blocks only have side variant
                Block b = api.World.BlockAccessor.GetBlock(CodeWithVariant(variantKey, variantValue));
                if(b != null) {
                    api.Logger.Log(EnumLogType.Notification, $"Found clay variant block: {b.Code}, delegating placement");
                    return b.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
                }
            }

            api.Logger.Log(EnumLogType.Error, $"Failed to find variant block for orientation {horVer[0].Code}");
        }

        api.Logger.Log(EnumLogType.Notification, $"StirlingEngineBurner TryPlaceBlock called for {Code} at {blockSel.Position}");
        bool ok = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
        if (ok) {
            api.Logger.Log(EnumLogType.Notification, $"StirlingEngineBurner placement successful, calling WasPlaced");
            WasPlaced(world, blockSel.Position, null);
        } else {
            api.Logger.Log(EnumLogType.Error, $"StirlingEngineBurner placement failed");
        }
        return ok;
    }

    EnumIgniteState IIgnitable.OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
    {
        BlockEntityStirlingEngineBurnerBase burner = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityStirlingEngineBurnerBase;
        if (burner == null) return EnumIgniteState.NotIgnitable;
        if (burner.IsBurning) return secondsIgniting > 2 ? EnumIgniteState.IgniteNow : EnumIgniteState.Ignitable;
        return EnumIgniteState.NotIgnitable;
    }
    public EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting) {
        BlockEntityStirlingEngineBurnerBase burner = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityStirlingEngineBurnerBase;
        if (burner == null) return EnumIgniteState.NotIgnitable;
        return burner.GetIgnitableState(secondsIgniting);
    }

    public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling) {
        BlockEntityStirlingEngineBurnerBase burner = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityStirlingEngineBurnerBase;
        if (burner != null && !burner.canIgniteFuel)
        {
            burner.canIgniteFuel = true;
            burner.extinguishedTotalHours = api.World.Calendar.TotalHours;
        }

        handling = EnumHandling.PreventDefault;
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
        ItemStack stack = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack;

        BlockEntityStirlingEngineBurnerBase burner = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityStirlingEngineBurnerBase;
        
        if (burner!=null && stack?.Block != null && stack.Block.HasBehavior<BlockBehaviorCanIgnite>() && burner.GetIgnitableState(0) == EnumIgniteState.Ignitable)
        {
            return false;
        }

        if (burner != null && stack != null && byPlayer.Entity.Controls.ShiftKey)
        {
            if (stack.Collectible.CombustibleProps != null && stack.Collectible.CombustibleProps.BurnTemperature > 0)
            {
                ItemStackMoveOperation op = new ItemStackMoveOperation(world, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, 1);
                byPlayer.InventoryManager.ActiveHotbarSlot.TryPutInto(burner.fuelSlot, ref op);
                if (op.MovedQuantity > 0)
                {
                    (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

                    var loc = stack.ItemAttributes?["placeSound"].Exists == true ? AssetLocation.Create(stack.ItemAttributes["placeSound"].AsString(), stack.Collectible.Code.Domain) : null;

                    if (loc != null)
                    {
                        api.World.PlaySoundAt(loc.WithPathPrefixOnce("sounds/"), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer, 0.88f + (float)api.World.Rand.NextDouble() * 0.24f, 16);
                    }

                    return true;
                }
            }
        }
        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override void WasPlaced(IWorldAccessor world, BlockPos ownPos, BlockFacing? connectedOnFacing)
    {
        base.WasPlaced(world, ownPos, connectedOnFacing);
        api.Logger.Log(EnumLogType.Notification, $"StirlingEngineBurner placed at {ownPos}");
        PlaceFakeBlock(world, ownPos);
    }

    private void PlaceFakeBlock(IWorldAccessor world, BlockPos pos)
    {
        api.Logger.Log(EnumLogType.Notification, $"PlaceFakeBlock called for burner at {pos}, variant side: {Variant["side"]}");

        // Fix: Use proper AssetLocation construction with variant (hyphen format)
        AssetLocation loc = new AssetLocation("stirlingage:stirlingenginerotor-" + Variant["side"]);
        api.Logger.Log(EnumLogType.Notification, $"Attempting to place rotor with AssetLocation: {loc}");

        Block toPlaceBlock = world.GetBlock(loc);
        if(toPlaceBlock == null) {
            api.Logger.Log(EnumLogType.Error, "no block found for "+loc.ToString());
            // Try to list available rotor blocks for debugging
            foreach (var block in world.Blocks)
            {
                if (block.Code.Path.Contains("stirlingenginerotor"))
                {
                    api.Logger.Log(EnumLogType.Notification, "Available rotor block: " + block.Code);
                }
            }
        }
        else {
            api.Logger.Log(EnumLogType.Notification, $"Setting block {toPlaceBlock.Code} at position {pos.UpCopy()}");
            world.BlockAccessor.SetBlock(toPlaceBlock.BlockId, pos.UpCopy());

            // Verify the block was placed
            Block placedBlock = world.BlockAccessor.GetBlock(pos.UpCopy());
            if(placedBlock.BlockId == toPlaceBlock.BlockId) {
                api.Logger.Log(EnumLogType.Notification, $"Successfully placed rotor block {placedBlock.Code} at {pos.UpCopy()}");
            } else {
                api.Logger.Log(EnumLogType.Error, $"Failed to place rotor block. Expected {toPlaceBlock.Code}, got {placedBlock.Code} at {pos.UpCopy()}");
            }
        }
    }

    public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer? byPlayer, float dropQuantityMultiplier = 1)
    {
        Block upBlock = api.World.BlockAccessor.GetBlock(pos.UpCopy());
        if (upBlock.Code.BeginsWith("stirlingage", "stirlingenginerotor"))
        {
            world.BlockAccessor.SetBlock(0, pos.UpCopy());
        }

        base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
    }

    public override bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref string failureCode)
    {
        if (!base.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode)) return false;

        BlockSelection bs = blockSel.Clone();
        bs.Position = blockSel.Position.UpCopy();
        if (!base.CanPlaceBlock(world, byPlayer, bs, ref failureCode)) return false;

        return true;
    }

    public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face) {
        
    }

        public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face) {
            return false;
        }

    public void Rotate(EntityAgent byEntity, BlockSelection blockSel, int dir) {
        // TODO: Implement proper rotation logic later
    }
}
