// based on CreativeRotorRenderer.cs

using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

public class StirlingEngineRotorRenderer : MechBlockRenderer {
    CustomMeshDataPartFloat malfStatic;
    CustomMeshDataPartFloat malfAxle;
    CustomMeshDataPartFloat malfWork;
    CustomMeshDataPartFloat malfDisplace;
    MeshRef meshStatic;
    MeshRef meshAxle;
    MeshRef meshWork;
    MeshRef meshDisplace;
    Vec3f axisCenter = new Vec3f(0.5f, 0.5f, 0.5f);

    public StirlingEngineRotorRenderer(ICoreClientAPI capi, MechanicalPowerMod mechanicalPowerMod, Block textureSourceBlock, CompositeShape shapeLoc) : base(capi, mechanicalPowerMod) {
        // COMPREHENSIVE SHAPE AND TEXTURE ANALYSIS
        capi.Logger.Notification($"=== ROTOR RENDERER DEBUGGING START ===");
        capi.Logger.Notification($"Texture Source Block: {textureSourceBlock?.Code}");
        capi.Logger.Notification($"Texture Source Block ID: {textureSourceBlock?.BlockId}");

        // Analyze texture source block textures
        if (textureSourceBlock != null && textureSourceBlock.Textures != null)
        {
            capi.Logger.Notification($"Texture Source Block Textures:");
            foreach (var textureKey in textureSourceBlock.Textures.Keys)
            {
                var compositeTexture = textureSourceBlock.Textures[textureKey];
                capi.Logger.Notification($"  {textureKey}: {compositeTexture?.Base?.ToString()}");

                // Test texture resolution
                if (compositeTexture?.Base != null)
                {
                    try
                    {
                        var textureAsset = capi.Assets.TryGet(compositeTexture.Base.Clone().WithPathPrefixOnce("textures/"));
                        capi.Logger.Notification($"    Exists: {textureAsset != null}");
                    }
                    catch (Exception ex)
                    {
                        capi.Logger.Error($"    Check Error: {ex.Message}");
                    }
                }
            }
        }

        // Analyze all shapes used by this renderer
        string[] shapeNames = {
            "stirlingengine-static.json",
            "stirlingengine-axle.json",
            "stirlingengine-workpiston.json",
            "stirlingengine-displacerpiston.json",
            "stirlingengine-whole.json"
        };

        foreach (var shapeName in shapeNames)
        {
            try
            {
                Shape shape = Vintagestory.API.Common.Shape.TryGet(capi, new AssetLocation($"stirlingage:shapes/{shapeName}"));
                capi.Logger.Notification($"=== SHAPE ANALYSIS: {shapeName} ===");

                if (shape != null)
                {
                    capi.Logger.Notification($"Shape Elements: {shape.Elements?.Length}");
                    capi.Logger.Notification($"Shape Texture Definitions: {shape.TextureWidth}x{shape.TextureHeight}");

                    // Analyze each element and its textures
                    if (shape.Elements != null)
                    {
                        foreach (var element in shape.Elements)
                        {
                            capi.Logger.Notification($"Element: {element.Name}");
                            if (element.Faces != null)
                            {
                                foreach (var faceEntry in element.Faces)
                                {
                                    var face = faceEntry.Value;
                                    capi.Logger.Notification($"  Face {faceEntry.Key}: texture='#{face.Texture}', uv=[{string.Join(",", face.Uv)}]");

                                    // Check if this is a placeholder texture
                                    if (!string.IsNullOrEmpty(face.Texture) && face.Texture.StartsWith("#"))
                                    {
                                        string textureKey = face.Texture.Substring(1); // Remove #
                                        capi.Logger.Notification($"    Placeholder texture: {textureKey}");

                                        // Check if textureSourceBlock has this texture
                                        if (textureSourceBlock != null && textureSourceBlock.Textures != null &&
                                            textureSourceBlock.Textures.ContainsKey(textureKey))
                                        {
                                            var sourceTexture = textureSourceBlock.Textures[textureKey];
                                            capi.Logger.Notification($"    Resolved to: {sourceTexture?.Base?.ToString()}");
                                        }
                                        else
                                        {
                                            capi.Logger.Warning($"    UNRESOLVED TEXTURE: {textureKey} - No mapping found in texture source block!");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    capi.Logger.Warning($"Shape not found: {shapeName}");
                }
            }
            catch (Exception ex)
            {
                capi.Logger.Error($"Shape Analysis Error for {shapeName}: {ex.Message}");
            }
        }

        capi.Logger.Notification($"=== SHAPE ANALYSIS COMPLETE ===");

        // Now proceed with normal mesh creation
        MeshData meshDataStatic;
        MeshData meshDataAxle;
        MeshData meshDataWork;
        MeshData meshDataDisplace;

        Vec3f rotVec = new Vec3f(shapeLoc.rotateX, shapeLoc.rotateY, shapeLoc.rotateZ);

        Shape shapeStatic = Vintagestory.API.Common.Shape.TryGet(capi, new AssetLocation("stirlingage:shapes/stirlingengine-static.json"));
        capi.Tesselator.TesselateShape(textureSourceBlock, shapeStatic, out meshDataStatic, rotVec);

        Shape shapeAxle = Vintagestory.API.Common.Shape.TryGet(capi, new AssetLocation("stirlingage:shapes/stirlingengine-axle.json"));
        capi.Tesselator.TesselateShape(textureSourceBlock, shapeAxle, out meshDataAxle, rotVec);

        Shape shapeWork = Vintagestory.API.Common.Shape.TryGet(capi, new AssetLocation("stirlingage:shapes/stirlingengine-workpiston.json"));
        capi.Tesselator.TesselateShape(textureSourceBlock, shapeWork, out meshDataWork, rotVec);

        Shape shapeDisplace = Vintagestory.API.Common.Shape.TryGet(capi, new AssetLocation("stirlingage:shapes/stirlingengine-displacerpiston.json"));
        capi.Tesselator.TesselateShape(textureSourceBlock, shapeDisplace, out meshDataDisplace, rotVec);

        int count = (16 + 4) * 2100;
        // 16 floats matrix, 4 floats light rgbs
        // (whatever that means)
        meshDataStatic.CustomFloats = malfStatic = createCustomFloats(count);
        meshDataAxle.CustomFloats = malfAxle = createCustomFloats(count);
        meshDataWork.CustomFloats = malfWork = createCustomFloats(count);
        meshDataDisplace.CustomFloats = malfDisplace = createCustomFloats(count);

        this.meshStatic = capi.Render.UploadMesh(meshDataStatic);
        this.meshAxle = capi.Render.UploadMesh(meshDataAxle);
        this.meshWork = capi.Render.UploadMesh(meshDataWork);
        this.meshDisplace = capi.Render.UploadMesh(meshDataDisplace);
    }

    private CustomMeshDataPartFloat createCustomFloats(int count) {
        CustomMeshDataPartFloat result = new CustomMeshDataPartFloat(count) {
            Instanced = true,
            InterleaveOffsets = new int[] { 0, 16, 32, 48, 64 },
            InterleaveSizes = new int[] { 4, 4, 4, 4, 4 },
            InterleaveStride = 16 + 4 * 16,
            StaticDraw = false,
        };
        result.SetAllocationSize(count);
        return result;
    }

    protected override void UpdateLightAndTransformMatrix(int index, Vec3f distToCamera, float rotation, IMechanicalPowerRenderable dev) {
            float rotX = rotation * dev.AxisSign[0];
            float rotY = rotation * dev.AxisSign[1];
            float rotZ = rotation * dev.AxisSign[2];
            float displacerFlip = 1.0f;
            if(dev is BEBehaviorStirlingEngineRotor rot && (rot.OutFacingForNetworkDiscovery == BlockFacing.SOUTH || rot.OutFacingForNetworkDiscovery == BlockFacing.EAST)) { displacerFlip = -1.0f; }
            UpdateLightAndTransformMatrix(malfStatic.Values, index, distToCamera, dev.LightRgba, 0.0f, 0.0f, 0.0f);
            UpdateLightAndTransformMatrix(malfAxle.Values, index, distToCamera, dev.LightRgba, rotX, rotY, rotZ);
            UpdateLightAndTransformMatrix(malfWork.Values, index, distToCamera + new Vec3f(0.0f, (float)Math.Cos(rotation) * (1.5f / 16.0f) - (1.5f / 16.0f), 0.0f), dev.LightRgba, 0.0f, 0.0f, 0.0f);
            UpdateLightAndTransformMatrix(malfDisplace.Values, index, distToCamera + new Vec3f(0.0f, (float)Math.Sin(rotation * displacerFlip) * (2.5f / 16.0f), 0.0f), dev.LightRgba, 0.0f, 0.0f, 0.0f);
    }

    public override void OnRenderFrame(float deltaTime, IShaderProgram prog) {
        UpdateCustomFloatBuffer();
        if(quantityBlocks > 0) {
            malfStatic.Count = quantityBlocks * 20;
            updateMesh.CustomFloats = malfStatic;
            capi.Render.UpdateMesh(meshStatic, updateMesh);
            capi.Render.RenderMeshInstanced(meshStatic, quantityBlocks);

            malfAxle.Count = quantityBlocks * 20;
            updateMesh.CustomFloats = malfAxle;
            capi.Render.UpdateMesh(meshAxle, updateMesh);
            capi.Render.RenderMeshInstanced(meshAxle, quantityBlocks);

            malfWork.Count = quantityBlocks * 20;
            updateMesh.CustomFloats = malfWork;
            capi.Render.UpdateMesh(meshWork, updateMesh);
            capi.Render.RenderMeshInstanced(meshWork, quantityBlocks);

            malfDisplace.Count = quantityBlocks * 20;
            updateMesh.CustomFloats = malfDisplace;
            capi.Render.UpdateMesh(meshDisplace, updateMesh);
            capi.Render.RenderMeshInstanced(meshDisplace, quantityBlocks);
        }
    }

    public override void Dispose() {
        base.Dispose();

        meshStatic?.Dispose();
        meshAxle?.Dispose();
        meshWork?.Dispose();
        meshDisplace?.Dispose();
    }
}
