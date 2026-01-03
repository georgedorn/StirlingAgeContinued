using System;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.GameContent.Mechanics;

public class StirlingAgeContinuedMod : ModSystem {
    public override void Start(ICoreAPI api) {
        base.Start(api);
        api.RegisterBlockClass("BlockStirlingEngineBurner", typeof(BlockStirlingEngineBurner));
        api.RegisterBlockClass("StirlingEngineRotor", typeof(BlockStirlingEngineRotor));
        api.RegisterBlockEntityClass("BlockEntityStirlingEngineBurner", typeof(BlockEntityStirlingEngineBurner));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorStirlingEngineRotor", typeof(BEBehaviorStirlingEngineRotor));

        // Register renderer on client side
        if(api.World is IClientWorldAccessor) {
            try {
                MechanicalPowerMod mpmod = api.ModLoader.GetModSystem<MechanicalPowerMod>();
                if(mpmod != null && !MechNetworkRenderer.RendererByCode.ContainsKey("stirlingenginerotor")) {
                    MechNetworkRenderer.RendererByCode.Add("stirlingenginerotor", typeof(StirlingEngineRotorRenderer));
                }
            } catch (Exception e) {
                api.World.Logger.Error("Failed to register stirling engine rotor renderer: " + e.Message);
            }
        }
    }
}
