using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

public class TextureDebuggingTool
{
    private ICoreClientAPI capi;
    private ILogger logger;

    public TextureDebuggingTool(ICoreClientAPI capi)
    {
        this.capi = capi;
        this.logger = capi.Logger;
    }

    /// <summary>
    /// Comprehensive texture resolution testing and debugging
    /// </summary>
    public void DebugTextureResolution(Block block)
    {
        logger.Notification($"=== TEXTURE RESOLUTION DEBUGGING FOR {block.Code} ===");

        // 1. Test basic texture existence
        TestBlockTextures(block);

        // 2. Test shape texture resolution
        TestShapeTextureResolution(block);

        // 3. Test composite texture baking
        TestCompositeTextureBaking(block);

        logger.Notification($"=== TEXTURE RESOLUTION DEBUGGING COMPLETE ===");
    }

    private void TestBlockTextures(Block block)
    {
        if (block.Textures == null || block.Textures.Count == 0)
        {
            logger.Warning($"Block has NO textures defined!");
            return;
        }

        foreach (var textureKey in block.Textures.Keys)
        {
            var compositeTexture = block.Textures[textureKey];
            if (compositeTexture?.Base != null)
            {
                TestTextureExistence(textureKey, compositeTexture.Base);
            }
        }
    }

    private void TestShapeTextureResolution(Block block)
    {
        logger.Notification($"--- SHAPE TEXTURE RESOLUTION TESTING ---");

        if (block.Shape == null)
        {
            logger.Warning($"Block has NO shape defined!");
            return;
        }

        try
        {
            var shape = Shape.TryGet(capi, block.Shape.Base.Clone());
            if (shape == null)
            {
                logger.Warning($"Failed to load shape: {block.Shape}");
                return;
            }

            logger.Notification($"SHAPE_ANALYSIS: Loaded={shape.Elements?.Length} elements");

            // Analyze each element's faces
            if (shape.Elements != null)
            {
                foreach (var element in shape.Elements)
                {
                    if (element.Faces != null)
                    {
                        foreach (var faceEntry in element.Faces)
                        {
                            var face = faceEntry.Value;
                            if (!string.IsNullOrEmpty(face.Texture) && face.Texture.StartsWith("#"))
                            {
                                string textureKey = face.Texture.Substring(1); // Remove #

                                // Test if block has this texture mapping
                                if (block.Textures != null && block.Textures.ContainsKey(textureKey))
                                {
                                    var mappedTexture = block.Textures[textureKey];
                                    string resolutionStatus = mappedTexture?.Base != null ? $"Mapped={mappedTexture.Base}" : "NullMapping";

                                    logger.Notification($"SHAPE_FACE [{element.Name}.{faceEntry.Key}]: Placeholder=#{textureKey}, {resolutionStatus}");

                                    if (mappedTexture?.Base != null)
                                    {
                                        TestTextureExistence(mappedTexture.Base, "");
                                    }
                                }
                                else
                                {
                                    logger.Error($"SHAPE_FACE_ERROR [{element.Name}.{faceEntry.Key}]: Unresolved placeholder #{textureKey}");
                                }
                            }
                            else
                            {
                                logger.Notification($"SHAPE_FACE [{element.Name}.{faceEntry.Key}]: DirectTexture='{face.Texture}'");
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error($"SHAPE_ANALYSIS_ERROR: {ex}");
        }
    }

    private void TestCompositeTextureBaking(Block block)
    {
        logger.Notification($"--- COMPOSITE TEXTURE BAKING TESTING ---");

        if (block.Textures == null || block.Textures.Count == 0)
        {
            logger.Warning($"No textures to bake");
            return;
        }

        foreach (var textureKey in block.Textures.Keys)
        {
            var compositeTexture = block.Textures[textureKey];
            if (compositeTexture == null || compositeTexture.Base == null)
            {
                logger.Warning($"Texture '{textureKey}' is null or has no base");
                continue;
            }

            try
            {
                // Test if texture can be baked
                var bakedTexture = CompositeTexture.Bake(capi.Assets, compositeTexture);

                // Create comprehensive single line with all baking information
                string bakedName = bakedTexture?.BakedName ?? "null";
                int fileCount = bakedTexture?.TextureFilenames?.Length ?? 0;
                string[] filenameStrings = new string[fileCount];
                if (fileCount > 0)
                {
                    for (int i = 0; i < fileCount; i++)
                    {
                        filenameStrings[i] = bakedTexture.TextureFilenames[i].ToString();
                    }
                }
                string filenames = fileCount > 0 ? string.Join(", ", filenameStrings) : "none";

                logger.Notification($"TEXTURE_BAKE [{textureKey} -> {compositeTexture.Base}]: Baked={bakedName}, Files={fileCount}[{filenames}]");

                // Test texture existence for each filename
                if (bakedTexture?.TextureFilenames != null)
                {
                    foreach (var filename in bakedTexture.TextureFilenames)
                    {
                        TestTextureExistence(filename, "");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"TEXTURE_BAKE_ERROR [{textureKey} -> {compositeTexture.Base}]: {ex.Message}");
            }
        }
    }

    private HashSet<string> reportedMissingTextures = new HashSet<string>();
    private HashSet<string> reportedFoundTextures = new HashSet<string>();

    private void TestTextureExistence(AssetLocation textureLocation, string indent)
    {
        try
        {
            // Test different possible paths
            var testLocations = new AssetLocation[]
            {
                textureLocation.Clone().WithPathPrefixOnce("textures/"),
                textureLocation.Clone().WithPathPrefixOnce("textures/block/"),
                textureLocation.Clone().WithPathPrefixOnce("textures/item/"),
                textureLocation.Clone()
            };

            bool found = false;
            string foundPath = null;
            foreach (var testLoc in testLocations)
            {
                var asset = capi.Assets.TryGet(testLoc);
                if (asset != null)
                {
                    foundPath = testLoc.ToString();
                    found = true;
                    break;
                }
            }

            string textureKey = textureLocation.ToString();
            if (found)
            {
                if (reportedFoundTextures.Add(textureKey))
                {
                    logger.Notification($"✓ {textureLocation} -> {foundPath}");
                }
            }
            else
            {
                if (reportedMissingTextures.Add(textureKey))
                {
                    logger.Error($"✗ MISSING: {textureLocation}");
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error($"✗ Error checking {textureLocation}: {ex.Message}");
        }
    }

    /// <summary>
    /// Debug texture resolution for a specific shape file
    /// </summary>
    public void DebugShapeFile(AssetLocation shapeLocation)
    {
        logger.Notification($"=== SHAPE FILE DEBUGGING: {shapeLocation} ===");

        try
        {
            Shape shape = Shape.TryGet(capi, shapeLocation);
            if (shape == null)
            {
                logger.Error($"SHAPE_LOAD_ERROR: Failed to load shape!");
                return;
            }

            // Consolidate shape info into single line
            int elementCount = shape.Elements?.Length ?? 0;
            int textureCount = shape.Textures?.Count ?? 0;
            logger.Notification($"SHAPE_INFO: Size={shape.TextureWidth}x{shape.TextureHeight}, Elements={elementCount}, Textures={textureCount}");

            // Analyze shape's own texture definitions
            if (textureCount > 0)
            {
                List<string> textureDefList = new List<string>();
                foreach (var textureEntry in shape.Textures)
                {
                    textureDefList.Add($"{textureEntry.Key}={textureEntry.Value.ToString()}");
                }
                string textureDefs = string.Join("; ", textureDefList);
                logger.Notification($"SHAPE_TEXTURES: {textureDefs}");
            }

            // Analyze all elements and their texture references
            if (shape.Elements != null)
            {
                foreach (var element in shape.Elements)
                {
                    if (element.Faces != null)
                    {
                        foreach (var faceEntry in element.Faces)
                        {
                            var face = faceEntry.Value;
                            string textureType = !string.IsNullOrEmpty(face.Texture) && face.Texture.StartsWith("#") ? "Placeholder" : "Direct";
                            logger.Notification($"SHAPE_ELEMENT [{element.Name}.{faceEntry.Key}]: TextureType={textureType}, Texture='#{face.Texture}'");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error($"SHAPE_DEBUG_ERROR: {ex}");
        }

        logger.Notification($"=== SHAPE FILE DEBUGGING COMPLETE ===");
    }

    /// <summary>
    /// Test texture resolution with a specific block and shape combination
    /// </summary>
    public void DebugBlockShapeCombination(Block block, AssetLocation shapeLocation)
    {
        logger.Notification($"=== BLOCK+SHAPE COMBINATION DEBUGGING ===");
        logger.Notification($"BLOCK_SHAPE_COMBO: Block={block.Code}, Shape={shapeLocation}");

        try
        {
            // Load the shape
            Shape shape = Shape.TryGet(capi, shapeLocation);
            if (shape == null)
            {
                logger.Error($"BLOCK_SHAPE_ERROR: Failed to load shape!");
                return;
            }

            // Test tessellation (this is where texture resolution happens)
            Vec3f rotation = new Vec3f(0, 0, 0);
            MeshData meshData;

            try
            {
                capi.Tesselator.TesselateShape(block, shape, out meshData, rotation);

                if (meshData != null)
                {
                    // Consolidate mesh data into single line
                    int textureIdCount = meshData.TextureIndices?.Length ?? 0;
                    string textureIdsPreview = "none";

                    if (textureIdCount > 0)
                    {
                        int previewCount = Math.Min(5, textureIdCount);
                        string[] previewIds = new string[previewCount];
                        for (int i = 0; i < previewCount; i++)
                        {
                            previewIds[i] = meshData.TextureIndices[i].ToString();
                        }
                        textureIdsPreview = string.Join(", ", previewIds) + (textureIdCount > 5 ? ",..." : "");
                    }

                    logger.Notification($"TESSELLATION_SUCCESS: Vertices={meshData.VerticesCount}, Indices={meshData.IndicesCount}, TextureIDs={textureIdCount}[{textureIdsPreview}]");
                }
                else
                {
                    logger.Notification($"TESSELLATION_SUCCESS: No mesh data generated");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"TESSELLATION_ERROR: {ex}");
            }
        }
        catch (Exception ex)
        {
            logger.Error($"BLOCK_SHAPE_COMBO_ERROR: {ex}");
        }

        logger.Notification($"=== BLOCK+SHAPE COMBINATION DEBUGGING COMPLETE ===");
    }
}
