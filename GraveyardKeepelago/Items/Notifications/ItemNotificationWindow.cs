using System;
using System.Linq;
using GraveyardKeepelago.GameModifications.UI;

namespace GraveyardKeepelago.Items.Notifications;

public sealed class ItemNotificationWindow
{
    public static bool CanShow(bool allowBusy = false)
    {
        var game = MainGame.me;
        var gui = GUIElements.me;
        if (game == null || !MainGame.game_started || MainGame.game_starting ||
            MainGame.paused || MainGame.disable_all_game || game.player == null ||
            game.player_char == null || gui == null || GUIElements.gui_is_initializing ||
            gui.tech_dialog == null)
            return false;

        return new ItemNotificationContext
        {
            GameReady = !LoadingGUI.is_shown,
            PlayerControllable = game.player_char.control_enabled && game.player_char.can_be_locally_controlled,
            Busy = !allowBusy && ((game.player_char.anim_state != CharAnimState.Idle &&
                    game.player_char.anim_state != CharAnimState.Walking) ||
                   game.player_char.playing_animation || game.build_mode_logics.IsBuilding()),
            WindowOpen = !BaseGUI.all_guis_closed,
            Talking = gui.speech_bubble.gameObject.activeInHierarchy || gui.multi_answer.gameObject.activeInHierarchy ||
                      gui.corner_talk.is_shown || SpeechBubbleGUI.all.Values.Any(
                          bubble => bubble != null && bubble.gameObject.activeInHierarchy),
            Cinematic = gui.cinematic_text.is_shown || CameraTools.cinematics.IsPlaying || CameraTools.letterbox.Amount > 0,
        }.CanShow;
    }

    public static TechUnlock.TechUnlockData GetData(string type, string id)
    {
        if (type == "trap")
        {
            var trap = Traps.MonsterTrapDefinition.Get(id);
            return new TechUnlock.TechUnlockData
            {
                name = trap.Name,
                sprite = (trap.SpriteName == null ? null : EasySpritesCollection.GetSprite(trap.SpriteName))
                    ?? APSpriteLoader.GetSprite("archipelago_32x32"),
                quality_icon = TrapBadgeSprite.Get(),
            };
        }
        if (type == "research_points" || type == "ap_item")
        {
            var iconName = type == "research_points" ? ItemNotification.ResearchPointSpriteName(id) : null;
            return new TechUnlock.TechUnlockData
            {
                name = id,
                sprite = (iconName == null ? null : EasySpritesCollection.GetSprite(iconName))
                    ?? APSpriteLoader.GetSprite("archipelago_32x32"),
            };
        }
        if (type == "relation")
        {
            var npc = GameBalance.me.GetDataOrNull<ObjectDefinition>(id);
            return new TechUnlock.TechUnlockData
            {
                name = GJL.L(id),
                sprite = EasySpritesCollection.GetSprite(ItemNotification.PortraitSpriteName(id, npc?.npc_alias))
                    ?? APSpriteLoader.GetSprite("archipelago_32x32"),
            };
        }
        if (type == "buff")
        {
            var definition = GameBalance.me.GetData<BuffDefinition>(id);
            return new TechUnlock.TechUnlockData
            {
                name = definition.GetLocalizedName(),
                description = definition.GetDescriptionIfExists(),
                // Keep custom icons and fallback rules identical to BuffIcon.Draw.
                sprite = EasySpritesCollection.GetSprite(definition.GetIconName()),
            };
        }
        if (type == "item")
        {
            var definition = new Item(id).definition;
            return new TechUnlock.TechUnlockData
            {
                name = definition.GetItemName(), description = definition.GetItemDescription(),
                sprite = EasySpritesCollection.GetSprite(ItemNotification.ItemSpriteName(id, definition.GetIcon())),
                quality_icon = definition.TryGetQualitySprite(),
            };
        }
        if (type == "technology")
        {
            var tech = GameBalance.me.GetData<TechDefinition>(id);
            var icon = string.IsNullOrEmpty(tech.icon) ? null : EasySpritesCollection.GetSprite(tech.icon);
            if (icon == null)
            {
                // Read the preserved vanilla unlocks, not GetVisibleUnlocksList,
                // which deliberately shows randomized AP rewards in the tree.
                foreach (var unlock in tech.GetUnlocksList().Where(unlock => unlock.visible))
                {
                    try { icon = unlock.GetData()?.sprite; }
                    catch (Exception ex) { UnityEngine.Debug.LogWarning($"Cannot draw native technology icon for {id}: {ex.Message}"); }
                    if (icon != null) break;
                }
            }
            return new TechUnlock.TechUnlockData
                { name = GJL.L(id), sprite = icon ?? APSpriteLoader.GetSprite("archipelago_32x32") };
        }
        var unlockType = type == "craft" ? TechUnlock.TechUnlockType.Craft :
            type == "work" ? TechUnlock.TechUnlockType.Work : TechUnlock.TechUnlockType.Perk;
        if (type == "craft")
            id = RecipeIconFallbacks.Default.SelectCraft(id, craftID =>
            {
                var craft = GameBalance.me.GetDataOrNull<CraftDefinition>(craftID);
                var icon = craft?.GetCraftIcon();
                return !string.IsNullOrEmpty(icon) && EasySpritesCollection.GetSprite(icon, not_found_is_valid: true) != null;
            });
        return new TechUnlock(id, unlockType).GetData();
    }
}


