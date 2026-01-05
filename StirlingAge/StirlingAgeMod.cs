using System;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.GameContent.Mechanics;

public class StirlingAgeContinuedMod : ModSystem {
    public override void Start(ICoreAPI api) {
        base.Start(api);

        // Note: renaming object classes to match the C# classes.  This is entirely allowed and is NEVER the source of
        // "no such class registered" errors.  These are far more likely the result of stirlingage.dll not being loaded AT ALL.
        api.RegisterBlockClass("BlockStirlingEngineBurner", typeof(BlockStirlingEngineBurner));
        api.RegisterBlockClass("BlockStirlingEngineRotor", typeof(BlockStirlingEngineRotor));
        api.RegisterBlockEntityClass("BlockEntityStirlingEngineBurnerClay", typeof(BlockEntityStirlingEngineBurnerClay));
        api.RegisterBlockEntityClass("BlockEntityStirlingEngineBurnerMetal", typeof(BlockEntityStirlingEngineBurnerMetal));
        api.RegisterBlockEntityBehaviorClass("BEBehaviorStirlingEngineRotor", typeof(BEBehaviorStirlingEngineRotor));

        // Register renderer on client side
        if (api.World is IClientWorldAccessor) {
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
