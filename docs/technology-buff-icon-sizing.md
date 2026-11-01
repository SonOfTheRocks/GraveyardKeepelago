# Permanent buff preview size

Permanent buff icons in technology research tiles used the existing technology
widget dimensions, which can enlarge the native buff artwork. The game's
CraftItemPerkGUI.DrawBuff calls MakePixelPerfect; UI2DSprite.MakePixelPerfect sizes
the widget from sprite.rect and pixelSize. Exported b_cross, b_plant, b_sword and
b_techpoint_red assets have a 26-by-26 canvas.

TechnologyBuffIconPatch applies equivalent native sizing to AP previews whose local
contract grant is permanent_buff, capped by the existing research icon box. Aspect
ratio and the icon's center are preserved. Width/height change, not the parent
transform scale, leaving frames and category badges untouched. The technology
details/receipt dialog, HUD buff icons, vanilla rewards and foreign-game previews
are excluded. No game assets are generated or modified.

Each reused tile restores its original dimensions and position before drawing.
Runtime disposal and patch cleanup also restore affected widgets. The pure sizing
tests cover native size, small boxes, non-square sprites, pixel size, pivots and
invalid dimensions; preview tests exclude foreign/missing/non-buff definitions.

Validation:

```powershell
dotnet test GraveyardKeepelago.sln --no-restore --filter 'FullyQualifiedName~BuffResearchIcon|FullyQualifiedName~Technology|FullyQualifiedName~UnlockPresentation' -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-buff-icons/ --verbosity quiet
dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-buff-icons/ --verbosity quiet
```

66 focused tests and all 583 tests passed with no failures or skips. Existing
compiler warnings remain. Logs: out/buff-icons-focused.log, out/buff-icons-full.log.
Build: out/codex-buff-icons/. A technology-tree visual check is still required.

Installed after confirming the game had closed, with DLL/PDB hash verification;
debug game restarted (PID 21260). Previous plugin/log backup:
out/before-buff-icons-c3708b63179140478f027e79dcc88fdd/.
DLL SHA256: FB25274640D742011C2F0D40CC5C73616111104BDC159184052AD1AE8ADDAB7D.
