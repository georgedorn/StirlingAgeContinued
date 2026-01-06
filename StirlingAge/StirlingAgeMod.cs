using System;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.GameContent.Mechanics;

public class StirlingAgeContinuedMod : ModSystem {
    private TextureDebuggingTool textureDebugTool;
    private ICoreClientAPI clientApi;

    public override void Start(ICoreAPI api) {
        base.Start(api);
        api.Logger.Notification("StirlingAge mod starting up");

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

        // Initialize texture debugging tool on client side
        if (api is ICoreClientAPI capi) {
            clientApi = capi;
            textureDebugTool = new TextureDebuggingTool(capi);
            RegisterDebugCommands(capi);
        }
    }

    private void RegisterDebugCommands(ICoreClientAPI capi) {
        // Register texture debugging commands
        capi.Input.RegisterHotKey("stirlingage-debug-textures", "Debug Stirling Engine Textures", GlKeys.F12, HotkeyType.GUIOrOtherControls);
        capi.Input.SetHotKeyHandler("stirlingage-debug-textures", (keyComb) => {
            if (keyComb.KeyCode == (int)GlKeys.F12) {
                DebugAllStirlingEngineTextures();
                return true;
            }
            return false;
        });

        // Register chat commands
        capi.ChatCommands.Create("stirlingage-debug-block")
            .WithDescription("Debug texture resolution for a specific Stirling Engine block")
            .WithArgs(capi.ChatCommands.Parsers.Word("blockcode"))
            .HandleWith(DebugSpecificBlock);

        capi.ChatCommands.Create("stirlingage-debug-shape")
            .WithDescription("Debug a specific shape file")
            .WithArgs(capi.ChatCommands.Parsers.Word("shapename"))
            .HandleWith(DebugSpecificShape);

        capi.ChatCommands.Create("stirlingage-debug-combination")
            .WithDescription("Debug block+shape combination")
            .WithArgs(capi.ChatCommands.Parsers.Word("blockcode"), capi.ChatCommands.Parsers.Word("shapename"))
            .HandleWith(DebugBlockShapeCombination);
    }

    private void DebugAllStirlingEngineTextures() {
        if (textureDebugTool == null) return;

        clientApi.Logger.Notification("=== STARTING COMPREHENSIVE TEXTURE DEBUGGING ===");

        // Debug all Stirling Engine blocks
        foreach (var block in clientApi.World.Blocks) {
            if (block.Code.Path.Contains("stirlingengine")) {
                clientApi.Logger.Notification($"Debugging block: {block.Code}");
                textureDebugTool.DebugTextureResolution(block);
            }
        }

        // Debug key shape files
        string[] shapeFiles = {
            "stirlingengine-whole.json",
            "stirlingengine-axle.json",
            "stirlingengine-workpiston.json",
            "stirlingengine-displacerpiston.json",
            "stirlingengine-static.json"
        };

        foreach (var shapeFile in shapeFiles) {
            clientApi.Logger.Notification($"Debugging shape: {shapeFile}");
            textureDebugTool.DebugShapeFile(new AssetLocation($"stirlingage:shapes/{shapeFile}"));
        }

        clientApi.Logger.Notification("=== COMPREHENSIVE TEXTURE DEBUGGING COMPLETE ===");
    }

    private TextCommandResult DebugSpecificBlock(TextCommandCallingArgs args) {
        if (textureDebugTool == null) return TextCommandResult.Success("Debugging tool not available");

        string blockCode = (string)args[0];
        Block block = clientApi.World.GetBlock(new AssetLocation(blockCode));

        if (block == null) {
            return TextCommandResult.Error($"Block not found: {blockCode}");
        }

        textureDebugTool.DebugTextureResolution(block);
        return TextCommandResult.Success($"Debugged block: {blockCode}");
    }

    private TextCommandResult DebugSpecificShape(TextCommandCallingArgs args) {
        if (textureDebugTool == null) return TextCommandResult.Success("Debugging tool not available");

        string shapeName = (string)args[0];
        AssetLocation shapeLocation = new AssetLocation($"stirlingage:shapes/{shapeName}");

        textureDebugTool.DebugShapeFile(shapeLocation);
        return TextCommandResult.Success($"Debugged shape: {shapeName}");
    }

    private TextCommandResult DebugBlockShapeCombination(TextCommandCallingArgs args) {
        if (textureDebugTool == null) return TextCommandResult.Success("Debugging tool not available");

        string blockCode = (string)args[0];
        string shapeName = (string)args[1];

        Block block = clientApi.World.GetBlock(new AssetLocation(blockCode));
        if (block == null) {
            return TextCommandResult.Error($"Block not found: {blockCode}");
        }

        AssetLocation shapeLocation = new AssetLocation($"stirlingage:shapes/{shapeName}");
        textureDebugTool.DebugBlockShapeCombination(block, shapeLocation);

        return TextCommandResult.Success($"Debugged combination: {blockCode} + {shapeName}");
    }
}
