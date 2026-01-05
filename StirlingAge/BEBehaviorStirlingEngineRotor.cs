/* based on BEBehaviorWindmillRotor from vssurvivalmod */

using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

public class BEBehaviorStirlingEngineRotor : BEBehaviorMPRotor {
    float target_torque;

    const float BASIS_TEMP = 700.0f;
    const float ΤORQUE_AT_BASIS_TEMP = 0.25f;

    protected override float Resistance => 0.002f;
    protected override double AccelerationFactor => 0.05d;
    protected override float TargetSpeed => 0.5f;
    protected override float TorqueFactor => target_torque;

    BlockEntity our_entity;
    IStirlingBurner? burner;

    public BEBehaviorStirlingEngineRotor(BlockEntity blockEntity) : base(blockEntity) {
        our_entity = blockEntity;
        // burner is deferred to UpdateMech
    }

    public override void Initialize(ICoreAPI api, JsonObject properties) {
        base.Initialize(api, properties);
        our_entity.RegisterGameTickListener(UpdateMech, 1000);  // Unknown if this change is needed; used to be Blockentity.RegisterGameTickListener, change it back if this breaks.  THIS USED TO WORK.
    }

    private void UpdateMech(float dt) {
        if(burner == null) {
            BlockPos down_pos = our_entity.Pos.DownCopy();
            burner = our_entity.Api.World.BlockAccessor.GetBlockEntity(down_pos) as IStirlingBurner;
            if(burner == null) {
                // hopefully it will appear soon...
                target_torque = 0.0f;
                return;
            }
        }

        // Get material-specific power multiplier
        float materialMultiplier = GetMaterialPowerMultiplier();

        // Log material, temperatures, and power calculations
        our_entity.Api.Logger.Debug($"Stirling Engine: Material={burner.Material}, HotTemp={burner.HotSideTemperature}°C, ColdTemp={burner.ColdSideTemperature}°C, MaterialMultiplier={materialMultiplier}x");

        // float world_temperature = api.World.BlockAccessor.GetClimateAt(entity.Pos.AsBlockPos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, api.World.Calendar.TotalDays).Temperature;
        target_torque = (burner.HotSideTemperature - burner.ColdSideTemperature) * ΤORQUE_AT_BASIS_TEMP / BASIS_TEMP * materialMultiplier;

        // Log the calculated power output
        our_entity.Api.Logger.Debug($"Stirling Engine: Calculated torque={target_torque}, Max possible torque={(burner.HotSideTemperature - burner.ColdSideTemperature) * ΤORQUE_AT_BASIS_TEMP / BASIS_TEMP}");
    }

    private float GetMaterialPowerMultiplier() {
        // Get material from the burner interface
        string material = burner.Material;
        if (string.IsNullOrEmpty(material)) return 1.0f; // Default to clay

        // Engine parts material-specific power multipliers - NOT the hot/cold plates
        // Higher is better?  TODO: Explain this better.
        switch (material) {
            case "copper": return 1.2f;
            case "brass": return 1.1f;
            case "tinbronze": return 1.3f;
            case "bismuthbronze": return 1.25f;
            case "blackbronze": return 1.4f;
            case "silver": return 1.8f;
            case "gold": return 2.0f;
            case "iron": return 1.5f;
            case "chromium": return 1.6f;
            case "electrum": return 1.7f;
            case "titanium": return 1.9f;
            case "molybdochalkos": return 1.8f;
            case "meteoriciron": return 1.7f;
            case "steel": return 1.6f;
            case "cupronickel": return 1.4f;
            case "nickel": return 1.5f;
            case "platinum": return 2.2f;
            case "stainlesssteel": return 1.7f;
            case "uranium": return 2.5f;
            case "zinc": return 1.1f;
            default: return 1.0f; // clay
        }
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb) {
        base.GetBlockInfo(forPlayer, sb);
        if(burner == null) {
            sb.AppendLine(Lang.Get("Temperature: {0}°C", "???"));
        }
        else {
            sb.AppendLine(Lang.Get("Temperature: {0}°C", (int)burner.HotSideTemperature));
        }
        sb.AppendLine(Lang.Get("Max Torque: {0} kNm", (int)(target_torque * 20 / 0.25)));
        if(network == null) {
            sb.AppendLine(Lang.Get("Speed: {0} rpm", 0));
        }
        else {
            sb.AppendLine(Lang.Get("Speed: {0} rpm", (int)(network.Speed * 48 + 0.5)));
        }
    }
}
