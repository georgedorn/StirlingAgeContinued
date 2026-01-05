/* Based on BlockEntityFirepit from vssurvivalmod */

using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

public class BlockEntityStirlingEngineBurnerClay : BlockEntityStirlingEngineBurnerBase {
    public override string Material => "clay";

    protected override void setBlockState(string state)
    {
        AssetLocation loc = Block.CodeWithVariants(new string[]{"burnstate", "side"}, new string[]{state, Block.Variant["side"]});
        Block block = Api.World.GetBlock(loc);
        if (block == null) {
            return;
        }

        Api.World.BlockAccessor.ExchangeBlock(block.Id, Pos);
        this.Block = block;
    }
}
