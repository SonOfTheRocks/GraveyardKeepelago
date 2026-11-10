# Native Graveyard Keeper UI Components — Handoff and Implementation Plan

## Goal

Build a small reusable native UI layer for GraveyardKeepelago before continuing work on the Archipelago connection dialog.

The connection dialog needs UI elements that Graveyard Keeper does not expose as a ready-made public form:

- text labels;
- editable text fields;
- a password field;
- a modern on/off toggle;
- a modal form layout inside the existing Graveyard Keeper UI.

These controls should look and behave like Graveyard Keeper controls. They must support the game's native scaling, bitmap font, input focus, modal behavior, mouse interaction, keyboard editing, and gamepad behavior where practical.

The connection dialog should become the first consumer of these reusable components, not a one-off mixture of unrelated UI systems.

## Current repository state

Repository:

`C:\Users\Kai\RiderProjects\GraveyardKeepelago`

Branch:

`test/init`

Existing committed implementation:

`494d31c Add title-screen Archipelago connection dialog`

That commit contains the initial IMGUI-based connection window and the functional connection model. Connecting to the local Archipelago server has already worked end to end.

There are currently uncommitted follow-up changes in at least:

- `GraveyardKeepelago/GameModifications/MainMenuConnectionWindow.cs`
- `GraveyardKeepelago/Plugin.cs`

Do not commit the current experimental UI state. Inspect `git status` and `git diff` before changing anything. Preserve the functional connection/model changes, but replace the experimental presentation code.

The last repeatedly executed test command was:

```powershell
dotnet test GraveyardKeepelago.sln --no-restore
```

Result: 42 passed, 0 failed. The build also copies the mod DLL to the Debug game installation.

The warnings in the test output predate this UI work: analyzer/compiler-version warnings, nullable warnings in test stubs, and several existing source warnings.

## What already works

The following behavior is known to work:

- A small Archipelago logo appears in the title screen.
- It is gray while disconnected and colored while connected.
- Clicking it can open Graveyard Keeper's native `DialogGUI`.
- The native dialog is opaque, fixed in place, and genuinely modal through `BaseGUI`'s window stack.
- Background main-menu controls cannot be used while the native dialog is open.
- Connect and disconnect behavior works.
- Server address, slot, password, and DeathLink configuration are persisted.
- A successful title-screen connection can be reused when loading or creating a save.
- Connection changes are locked after game initialization.
- `ConnectionMenuModel` and its parsing/validation tests are useful and should remain.

## Why the current approach failed

Several presentation approaches were tried after the initial implementation.

### Standalone IMGUI window

The initial committed window used `GUI.ModalWindow`.

Problems:

- visually inconsistent with Graveyard Keeper;
- partially transparent;
- draggable;
- not properly integrated into the game's modal `BaseGUI` stack;
- main-menu controls behind it could still be used.

The native `DialogGUI` solved these problems and should be retained as the modal shell.

### Native dialog plus ordinary IMGUI form controls

This produced correctly sized and interactive fields, but the IMGUI label and text-field font was smoother than Graveyard Keeper's native bitmap font. The dialog buttons looked correct because they were native NGUI controls, not because `GUI.skin.button.font` contained the same font.

Important conclusion:

`GUI.skin.button.font` is not a reliable source for the font used by native Graveyard Keeper dialog buttons.

### Dynamically constructed NGUI controls

Creating `UILabel`, `UIInput`, and `UITexture` objects manually caused several problems:

- fixed values such as width `380` were initially interpreted in NGUI coordinates rather than screen pixels, producing enormous controls;
- manually mapping screen rectangles into NGUI introduced scaling and subpixel artifacts in the bitmap font;
- default `UILabel.Overflow.ShrinkContent` silently rescaled text to non-native sizes;
- custom `UIInput` colliders did not reliably receive mouse input inside the dialog;
- mixing NGUI text with IMGUI backgrounds caused render-order problems: IMGUI backgrounds were rendered after NGUI and covered the visible field values;
- later experiments restored interaction through invisible IMGUI fields, but made the presentation increasingly fragile.

The latest experimental state is not a suitable foundation.

## Confirmed information from the decompiled game

Decompiled sources are under:

```text
GraveyardKeepelago/Dumps/Decompiled/Assembly-CSharp
GraveyardKeepelago/Dumps/Decompiled/Assembly-CSharp-firstpass
```

Relevant classes:

- `DialogGUI`
- `DialogButtonsGUI`
- `BaseGUI`
- `GUIElements`
- `ItemCountGUI`
- `SmartSlider`
- `UIInput`
- `UIInputOnGUI`
- `UILabel`
- `UIToggle`
- `UICamera`
- `UITable`
- `NGUITools`

### Native modal shell

`GUIElements.me.dialog` is a `DialogGUI`. Its `Open(...)` method:

- enters the native `BaseGUI` modal stack;
- configures native dialog buttons;
- calls `Hide()` and the supplied callbacks in the expected order;
- updates anchors and table layout.

This should remain the outer dialog implementation.

### Native text

`DialogGUI.label_1` is a native `UILabel` and renders `Not connected` cleanly. Its bitmap font, actual transform hierarchy, overflow configuration, spacing, material, effect, and panel association collectively determine the correct appearance.

Copying only selected properties is insufficient. Prefer cloning a known-good object or subtree.

### Existing native editable input

Graveyard Keeper already contains a working NGUI `UIInput` in the item-count dialog:

```text
GUIElements.me.item_count
  -> ItemCountGUI
  -> SmartSlider
  -> UIInput
```

`SmartSlider.Init()` obtains:

- the `UIInput`;
- its `UILabel`;
- its `Collider2D`;
- native change and submit callbacks.

This is the best known runtime prototype for a native editable input. Clone the appropriate source object or minimal parent subtree instead of recreating `UIInput`, collider, label, font, and event setup manually.

### Toggle support

NGUI provides `UIToggle`, but a Graveyard-Keeper-specific modern switch may not exist in the exact desired visual form. Existing `UIToggle` objects and option-menu controls should be inventoried before creating a custom switch.

If a custom switch is necessary, it should still be a wholly native NGUI component with:

- native panel membership;
- native collider and click handling;
- native label cloned from an existing label;
- game-compatible sprites or textures;
- no IMGUI participation.

## Proposed architecture

Create a small UI package, for example:

```text
GraveyardKeepelago/GameModifications/UI/
```

Suggested responsibilities follow. Exact names may be adjusted to existing repository conventions.

### `NativeUiPrototypeCatalog`

Find and cache runtime prototypes after `GUIElements` has initialized.

Candidate sources:

- modal shell: `GUIElements.me.dialog`;
- ordinary label: `GUIElements.me.dialog.label_1` or a smaller native label with a stable natural size;
- editable input: `GUIElements.me.item_count` → `SmartSlider` → `UIInput` subtree;
- buttons: the `DialogButtonsGUI` already owned by `DialogGUI`;
- toggle/switch: a suitable existing `UIToggle`, if one exists.

The catalog should fail with a clear diagnostic if an expected prototype cannot be found. It should not silently fall back to a visually unrelated control.

### `NativeTextInput`

A reusable wrapper around a cloned native input prototype.

Required behavior:

- initial value;
- read/write value;
- standard and password modes;
- enabled/disabled state;
- native mouse focus;
- keyboard entry, cursor movement, selection, Backspace, and Delete;
- correct native bitmap text;
- callback or polling support for changed values;
- cleanup when the owner is destroyed.

The wrapper should retain the prototype's native collider type, transform scale, label settings, and input event path.

Do not rebuild `UIInput` from an empty `GameObject` unless investigation proves cloning is impossible.

### `NativeToggle`

A reusable on/off control.

Required behavior:

- native click handling;
- enabled/disabled state;
- `Value` property and change callback;
- clear on/off presentation;
- Graveyard Keeper-compatible colors, texture filtering, and label font;
- no dependency on IMGUI.

Prefer cloning a suitable `UIToggle`. If no suitable prototype exists, build this once as a reusable NGUI component rather than inside the connection dialog.

### `NativeFormLayout`

A layout container operating exclusively in the coordinate system of the native dialog panel.

Responsibilities:

- rows containing caption plus control;
- consistent vertical spacing;
- width derived from the dialog's available native width;
- no `Screen.width / 2` positioning;
- no conversion back and forth between screen coordinates and NGUI coordinates;
- resolution and UI-scale changes handled through native anchors/layout.

Investigate whether `UITable`, anchors, or a simple fixed native-coordinate layout is most consistent with the source dialog. Avoid adding form children directly to a `UITable` that will unexpectedly reposition them.

### `NativeModalForm`

A thin owner/coordinator for:

- opening and closing `DialogGUI`;
- mounting reusable form controls into the dialog;
- enabling/disabling controls;
- reading values before the native primary callback executes;
- deactivating or destroying mounted controls on close;
- handling repeated opens without duplicate objects.

The dialog's native title/status text and native buttons should remain owned by `DialogGUI`.

## Recommended implementation sequence

### 1. Establish a clean baseline

- Inspect the current diff.
- Preserve connection parsing, persistence, native `DialogGUI` lifecycle, and plugin integration.
- Remove the experimental mixture of generated NGUI labels, generated NGUI textures, invisible IMGUI inputs, and screen-to-world layout calculations.
- Do not use a destructive repository-wide reset; retain unrelated work.

### 2. Add a temporary native-UI inspection utility

Inspect the runtime hierarchy of:

- `GUIElements.me.item_count`;
- its `SmartSlider` and `UIInput`;
- candidate `UIToggle` controls;
- `DialogGUI.label_1` and dialog panel parents.

Record for each relevant object:

- hierarchy path;
- active state;
- components;
- parent panel;
- local position and scale;
- widget dimensions and depth;
- collider type and dimensions;
- label font, font size, final font size, overflow mode, spacing, and effect;
- sprite/atlas names where applicable.

Use a short-lived diagnostic helper or a debug-only command. Remove noisy diagnostics after the prototypes are understood.

### 3. Prototype one cloned text input in isolation

- Clone the known-good native input prototype.
- Attach it to a native dialog or a small dedicated preview owner.
- Change it from numeric validation to unrestricted text.
- Verify clicking, typing, cursor movement, selection, Backspace, Delete, and submission.
- Verify the bitmap font at more than one resolution/UI scale.
- Only after this works, wrap it in `NativeTextInput`.

### 4. Prototype the toggle

- Search live `GUIElements` descendants for usable `UIToggle` instances.
- If a suitable switch exists, clone and restyle only its text/state.
- Otherwise build a single NGUI-native switch component and verify its collider and state visuals independently.

### 5. Build native form layout

- Create three input rows and one toggle row in a preview dialog.
- Derive size and placement from the native dialog hierarchy.
- Verify that controls remain within the dialog and do not move relative to the title/buttons.
- Verify at the user's current resolution and at least one alternate resolution or UI scale.

### 6. Rebuild `MainMenuConnectionWindow`

Keep it as orchestration only:

- logo button state;
- opening the native modal form;
- transferring values to/from `ConnectionMenuModel`;
- calling connect/disconnect delegates;
- displaying status;
- locking controls after a save is loaded.

It should not contain low-level label construction, texture generation, collider setup, font copying, or coordinate conversion.

### 7. Verify behavior

At minimum verify manually:

- disconnected logo is gray;
- connected logo is colored;
- native dialog is opaque and modal;
- server, slot, and password fields are readable and editable;
- selection, Delete, Backspace, arrows, Home/End where supported, and password masking work;
- DeathLink toggle changes state;
- invalid endpoint shows an error and preserves entered values;
- successful connection closes or refreshes the dialog appropriately;
- disconnect works before a save is loaded;
- fields are locked after game initialization;
- main-menu buttons cannot be clicked through the modal;
- reopening does not duplicate UI objects;
- scaling remains correct after resolution/UI-scale changes.

Run:

```powershell
dotnet test GraveyardKeepelago.sln --no-restore
```

Then perform an in-game smoke test with the Debug game installation.

## Testing strategy

The low-level visuals cannot be meaningfully covered by the existing headless unit tests, but logic around them can.

Keep or add unit tests for:

- server endpoint parsing;
- validation errors;
- settings transfer between form state and `ConnectionSettings`;
- enabled/disabled state decisions;
- primary-action behavior for connect versus disconnect;
- form state preservation after a failed connection.

For native UI construction, add cheap runtime assertions where useful:

- prototypes were found;
- cloned input contains `UIInput`, `UILabel`, and the expected collider;
- all mounted widgets share the expected UI panel/layer;
- no duplicate mounted form exists after reopening.

Do not pretend these replace a visual smoke test.

## Design constraints

- Use Graveyard Keeper's NGUI system consistently inside the modal dialog.
- Do not mix visible IMGUI and NGUI controls in the same form.
- Do not copy extracted game assets into the mod when the same assets can be reused from loaded runtime objects.
- Do not guess sprite names such as `gui_back`; inspect actual runtime prototypes.
- Do not use arbitrary screen-pixel dimensions as native widget dimensions.
- Do not allow `UILabel.Overflow.ShrinkContent` to rescale bitmap labels accidentally.
- Preserve the game's native `BaseGUI` modal stack.
- Keep UI construction reusable and separate from Archipelago connection logic.
- Do not commit until the user has visually approved the resulting dialog.

## Local test context

A local Archipelago server was previously run at:

```text
127.0.0.1:38281
slot: Player1
password: empty
```

The generated slot uses Graveyard Keeper data version 2. The server may no longer be running when a new thread starts; verify rather than assume.

Debug game executable:

```text
C:\Users\Kai\RiderProjects\Graveyard Keeper Debug\Graveyard Keeper.exe
```

BepInEx log:

```text
C:\Users\Kai\RiderProjects\Graveyard Keeper Debug\BepInEx\LogOutput.log
```

## Definition of done

This UI foundation is complete when:

- the connection dialog uses only the native modal shell and reusable native controls;
- all text has the same stable bitmap appearance as native dialog text such as `Not connected`;
- fields support normal mouse and keyboard editing;
- controls stay correctly positioned and scaled;
- no click-through, dragging, transparency, or render-order problems remain;
- the reusable components contain no connection-specific behavior;
- all 42 existing tests and any new tests pass;
- the user has visually approved the result;
- the changes are committed in a focused follow-up commit after approval.
