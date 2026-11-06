# Trap badge size

The first transform-based correction did not change the technology-tree display
in the user's gameplay check. It has been removed. A sprite's atlas rectangle
can include transparent padding, so widget and rectangle dimensions alone do not
establish how large the visible skull is relative to the visible quality star.

`TrapBadgeSprite` now builds a dedicated corner sprite from the native skull and
gold-star pixels. It reconstructs each sprite's canvas from its texture rectangle
and trim offset, reads actual alpha bounds, and fits the skull into the star's
visible bounding box. The new image has the star's canvas size and transparent
padding; the skull is aligned to the same upper-right visible corner. Scaling is
uniform with nearest-neighbour sampling for pixel art.

Both technology previews and receipt rows request this sprite directly when
selecting the trap category. This avoids reliance on a particular Draw overload,
widget scale, sprite-name comparison or later table/layout updates. Main monster
and AP icons are unchanged. Ordinary quality stars are unchanged. The generated
sprite is cached and disposed with the plugin; original game textures are not
modified. GPU readback handles native atlases that are not CPU-readable. Temporary
render targets and texture copies are released after construction.

Preloading runs after the icons atlas is loaded. A Debug log reports star/skull
canvas dimensions and actual visible bounds, plus the resulting badge bounds.
An unavailable/empty source is not cached and can be retried when previews draw.
The previous row-transform patch is no longer registered or present.

Regression tests include identical large source/reference canvases with a small
star only in one corner: the skull's visible pixels must fit that corner, rather
than filling the canvas. Tests also cover non-square skulls, empty sources,
unchanged source arrays, color preservation, fitting and upper-right placement.

Validation from the client root, 2026-10-28:

```powershell
dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-badges-save-list/ --verbosity quiet
```

Result: **450 passed**, no failures or skips. Existing compiler/analyzer warnings
remain. This also includes the save-list filtering changes. Manual inspection
still needs to confirm both monster types and foreign-game traps in the technology
tree and receipt rows. No seed, item classification or shared-data change.
