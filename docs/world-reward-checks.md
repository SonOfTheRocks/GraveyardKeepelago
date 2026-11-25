# World reward checks

The six shared `world_item_reward` mappings now share one check-and-consume
operation. Matching uses the active server location names and exact item IDs from
shared data; no second six-item mapping or fixed network ID table is introduced.

Native entry points differ:
- Ground finds enter `DropResGameObject.CollectDrop`. The check is saved before
  the drop is marked collected, removed from WorldMap's saved drop list and its
  hint removed. Native DropsList then destroys the collected object. No vanilla
  item, pickup quest side effect or vanilla reward notification is granted.
- The Legionary Helm and Ancient Keys are in the farmer's and Adam's house
  chests. `MultiInventory.MoveItemTo` is intercepted for a successful eligible
  transfer to the player's inventory or one of its bags. Both native overloads
  are covered. Availability and capacity checks still apply. The exact clicked
  source object is consumed, not another item with the same ID. All six native
  rewards are nonstackable single objects; malformed multiple-copy requests keep
  native behavior. Moving already owned items or moving into another chest does
  not claim a world check.

Only a currently bound versioned save supplies the LocationTriggerService.
Inactive/DLC-excluded locations and unbound/unversioned saves keep native rewards.
Offline checks use the existing SaveProgress record and replay on reconnect/load.
Persistence happens before consuming the source. Repeated matching finds are
consumed but the existing recorder sends each check only once per connection.

Physical AP grants mark their native Item parameters with
`graveyard_keepelago_archipelago_item=1`. Item copies and the native serializer
preserve these parameters. Both the existing item-application scope and this
persistent marker bypass the world check. This matters when an AP item is later
stored in a chest and retrieved after reload. The native `AddNotFoldedItem` helper
normally recreates an item from only ID/count; marked items instead use native
`AddItem(Item)` to preserve the parameter through that transfer as well. These
six item definitions have stack_count=1, avoiding stack-merge parameter loss.
The key is the same as the earlier PR #2 prototype; its old global runtime and
post-transfer deletion hooks were not imported.

Evidence: local Assembly-CSharp code for CollectDrop, DropsList.Update,
ChestGUI.MoveItem, both MultiInventory.MoveItemTo overloads, Item(Item),
Item.AddNotFoldedItem, Item.AddItem and native Item/GameRes serialization fields.
GameSave.LateSaveFixer identifies the two house-chest rewards; the original
items_data definitions confirm all six nonstackable, nonthrowable items.

Tests cover all six active and inactive mappings, exact names, ordinary items,
received-item bypass, item-application scope, inventory ownership (including
nested bags), native parameter/copy preservation, native exact-copy removal,
persistence failures, repeat checks and offline reload/identity rejection.
Tests inspect native parameter storage but do not claim a Unity save-file
roundtrip. In-game checks remain for all six finds, chest take-all/bag transfers,
full inventory, AP-item storage/retrieval across a save/load, and pickup hint/object
cleanup. No deployment was performed. Versioned login remains blocked pending
DLC/contract validation and the remaining game checks. Shared v9 data, IDs and
slot fields are unchanged; no APWorld change is required for this step.
