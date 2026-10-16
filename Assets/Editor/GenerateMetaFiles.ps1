$items = @(
    @{ Name = "Card_Frame_Cyan.png"; Guid = "a1000000000000000000000000000001" },
    @{ Name = "Card_Frame_Purple.png"; Guid = "a1000000000000000000000000000002" },
    @{ Name = "Card_Frame_Yellow.png"; Guid = "a1000000000000000000000000000003" },
    @{ Name = "Icon_Swift_Stride.png"; Guid = "a1000000000000000000000000000004" },
    @{ Name = "Icon_Flash_Step.png"; Guid = "a1000000000000000000000000000005" },
    @{ Name = "Icon_Rapid_Trigger.png"; Guid = "a1000000000000000000000000000006" },
    @{ Name = "Health_Frame_Cyan.png"; Guid = "a1000000000000000000000000000007" },
    @{ Name = "Health_Fill_Lime.png"; Guid = "a1000000000000000000000000000008" },
    @{ Name = "Weapon_Frame_Cyan.png"; Guid = "a1000000000000000000000000000009" },
    @{ Name = "Padlock_Cyan.png"; Guid = "a10000000000000000000000000000010" }
)

$template = @'
fileFormatVersion: 2
guid: __GUID__
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 100
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationMethod: 0
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings: []
  userData: 
  assetBundleName: 
  assetBundleVariant: 
'@

$dir = "C:\Users\User\MiniShooterGame\MiniShooterGame\Assets\_Game\Sprites\UI"
foreach ($item in $items) {
    $metaPath = Join-Path $dir "$($item.Name).meta"
    $content = $template.Replace("__GUID__", $item.Guid)
    [System.IO.File]::WriteAllText($metaPath, $content)
    Write-Output "Successfully wrote: $($item.Name).meta"
}
