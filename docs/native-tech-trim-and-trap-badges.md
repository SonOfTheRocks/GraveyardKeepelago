# Technologies ornaments and native trap badges

The user requested the middle journal ornaments exactly as in Technologies,
after the Character-tab ornaments and their split/inset copies remained incorrect.
NativeJournalPanels now resolves the verified scene paths under Techs:

- back/inner frame (above scroll)/triangle separator
- back/inner frame (above scroll)/triangle separator (1)
- back/inner frame (above scroll)/inner frame L
- back/inner frame (above scroll)/inner frame R

The complete native ornaments retain their sprite settings and mirroring. Their
offsets are measured from the rendered native rail edges and the technology back's
top edge, then applied to the journal's actual Game GUI background edges and footer
divider. The old Character footer copies, two-pixel adjustment and half-clipping
panels have been removed. The Character-derived top divider is unchanged.

The tests for fixed inward offsets and half-clip partitioning described the rejected
implementation, so they were replaced with native rail-offset and vertical placement
cases. They were not weakened to accommodate a failing result.

Trap main icons already came from the correct monster definitions. Their corner
badge used a GPU atlas readback/composition path that could return no visible pixels,
causing a null quality sprite. TrapBadgeSprite now returns the game's original
i_skull directly. No texture is read back, created or owned by this badge path, and
the atlas-preload patch is gone. The normal native Draw overload activates the badge;
a small patch fits its aspect ratio into the original quality-badge box and preserves
the top-right anchor. This applies to research previews and item-receipt rows. Reused
rows and runtime cleanup restore the original geometry before another category is
drawn. Server flag precedence is unchanged. The confirmed permanent-buff size fix
is unchanged. No APWorld contract or media asset changed.

Validation:

```powershell
dotnet test GraveyardKeepelago.sln --no-restore --filter 'FullyQualifiedName~Journal|FullyQualifiedName~Badge|FullyQualifiedName~Technology|FullyQualifiedName~BuffResearchIcon' -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-native-trim-skull/ --verbosity quiet
dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-native-trim-skull/ --verbosity quiet
```

198 focused tests and all 591 tests passed, with no failures or skips. Existing
compiler warnings remain. Logs: out/native-trim-skull-focused.log and
out/native-trim-skull-full.log. Build: out/codex-native-trim-skull/.
The actual Technologies-to-journal alignment and visible native skull require an
in-game rendering check.

Installed with DLL/PDB hashes verified after confirming the game was closed, then
restarted (PID 22284). Backup:
out/before-native-trim-skull-3e88f7c38d1546dd8f5b3501e252c582/.
DLL SHA256: 99E5D91FA56109BCEE0737364167A650461CBA9D3061EA9C30737A8C0AA7BBC6.

## Orientation, rendered badge footprint and input recall

The user confirmed the permanent buff icons but reported that the direct skull
still covered the main monster icon and the footer ornaments faced the wrong way.
The footer copies now reverse their horizontal orientation while retaining their
measured positions. The top separator remains unchanged.

The quality widget/canvas dimensions were too large a reference for packed sprites.
TrapQualityBadgePatch now measures UI2DSprite.drawingDimensions with the native
quality star and skull in the same unmodified slot. The star's visible reference is
also capped by its textureRect dimensions and pixelSize. The skull transform scales
uniformly to fit that reference, aligning the visible top-right corners; width and
height are left at their original values. Restore also includes transform scale,
fixedAspect and drawRegion. The quality-slot skull is recognized without depending
on TechnologyRuntime initialization, so early receipt rows are covered as well.
A once-per-row diagnostic logs the measured rectangles and resulting scale, without
chat or connection data. No GPU readback or new graphic is used.

The journal input now recalls the last 100 successfully sent lines with Up/Down.
It keeps the current unsent draft, stops at history boundaries, collapses consecutive
duplicates and retains edited recalled text as a new draft. History is transient,
owned by the current journal view and survives closing/reopening its tab. It is not
written to disk or shared with another save's view. Only a successful send records
a line. While suggestions are open, they retain arrow-key priority; Escape dismisses
them before input-history navigation. Recalled text does not reopen completion until
edited and can be sent directly. The same-frame input guard prevents double recall
from NGUI's two event routes.

Validation:

```powershell
dotnet test GraveyardKeepelago.sln --no-restore --filter 'FullyQualifiedName~Journal|FullyQualifiedName~Badge|FullyQualifiedName~Technology|FullyQualifiedName~BuffResearchIcon' -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-badge-history/ --verbosity quiet
dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-badge-history/ --verbosity quiet
```

212 focused and all 605 tests passed, with no failures or skips. Existing compiler
warnings remain. Logs: out/badge-history-focused.log, out/badge-history-full.log.
Build: out/codex-badge-history/. In-game verification of visible badge size, ornament
orientation and keyboard routing is still required.

Deployment is pending: the debug game was still running at the installation check,
so no loaded plugin files were replaced and the game was not interrupted.

After the user's shutdown confirmation, this build was installed with verified
DLL/PDB hashes and the debug game restarted (PID 83988). The fresh startup log
confirms GraveyardKeepelago loaded without a startup exception. Backup:
out/before-badge-history-099b5e158f4b442a9c27728d9595faf7/.
DLL SHA256: 2CC474D007020BA94B4D6F1BA234DF62DF7BD0808DEAC8FFF19A21F078DFBC24.
