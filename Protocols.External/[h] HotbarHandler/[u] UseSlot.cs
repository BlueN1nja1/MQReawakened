using A2m.Server;
using Microsoft.Extensions.Logging;
using Server.Base.Timers.Services;
using Server.Reawakened.Configs;
using Server.Reawakened.Entities.Entity;
using Server.Reawakened.Network.Extensions;
using Server.Reawakened.Network.Protocols;
using Server.Reawakened.Players;
using Server.Reawakened.Players.Extensions;
using Server.Reawakened.Players.Models;
using Server.Reawakened.Rooms.Models.Planes;
using Server.Reawakened.XMLs.Bundles;
using Server.Reawakened.XMLs.BundlesInternal;
using Server.Reawakened.XMLs.Enums;

namespace Protocols.External._h__HotbarHandler;
public class UseSlot : ExternalProtocol
{
    public override string ProtocolName => "hu";

    public ItemCatalog ItemCatalog { get; set; }
    public ServerRConfig ServerRConfig { get; set; }
    public TimerThread TimerThread { get; set; }
    public ILogger<PlayerStatus> Logger { get; set; }
    public InternalAchievement InternalAchievement { get; set; }

    public override void Run(string[] message)
    {
        var hotbarSlotId = int.Parse(message[5]);
        var targetUserId = int.Parse(message[6]);
        var position = new Vector3Model()
        {
            X = Convert.ToSingle(message[7]),
            Y = Convert.ToSingle(message[8]),
            Z = Convert.ToSingle(message[9])
        };
        Logger.LogDebug("Player used hotbar slot {hotbarId} on {userId} at coordinates {position}",
            hotbarSlotId, targetUserId, position);
        var direction = Player.TempData.Direction;
        var slotItem = Player.Character.Data.Hotbar.HotbarButtons[hotbarSlotId];
        var usedItem = ItemCatalog.GetItemFromId(slotItem.ItemId);
        switch (usedItem.ItemActionType)
        {
            case ItemActionType.Drop:
                Player.HandleDrop(ServerRConfig, TimerThread, Logger, usedItem, position, direction);
                break;
            case ItemActionType.Throw:
                HandleRangedWeapon(usedItem, position, direction);
                break;
            case ItemActionType.Genericusing:
            case ItemActionType.Drink:
            case ItemActionType.Eat:
                HandleConsumable(usedItem, hotbarSlotId);
                break;
            case ItemActionType.Melee:
                HandleMeleeWeapon(usedItem, Player.TempData.Position, direction);
                break;
            case ItemActionType.Pet:
                HandlePet(usedItem);
                break;
            case ItemActionType.Relic:
                HandleRelic(usedItem);
                break;
            default:
                Logger.LogError("Could not find how to handle item action type {ItemAction} for user {UserId}",
                    usedItem.ItemActionType, targetUserId);
                break;
        }
    }

    private void HandlePet(ItemDescription usedItem)
    {
        Player.SendXt("ZE", Player.UserId, usedItem.ItemId, 1);
        Player.Character.Data.PetItemId = usedItem.ItemId;
    }

    private void HandleRelic(ItemDescription usedItem) //Needs rework.
    {
        StatusEffect_SyncEvent itemEffect = null;
        foreach (var effect in usedItem.ItemEffects)
            itemEffect = new StatusEffect_SyncEvent(Player.GameObjectId.ToString(), Player.Room.Time,
                    (int)effect.Type, effect.Value, effect.Duration, true, usedItem.PrefabName, false);
        Player.SendSyncEventToPlayer(itemEffect);
    }

    private void HandleConsumable(ItemDescription usedItem, int hotbarSlotId)
    {
        Player.HandleItemEffect(usedItem, TimerThread, ServerRConfig, Logger);
        var removeFromHotBar = true;

        if (usedItem.InventoryCategoryID is
            ItemFilterCategory.WeaponAndAbilities or
            ItemFilterCategory.Pets)
            removeFromHotBar = false;

        if (removeFromHotBar)
        {
            if (usedItem.ItemActionType == ItemActionType.Eat)
            {
                Player.CheckAchievement(AchConditionType.Consumable, string.Empty, InternalAchievement, Logger);
                Player.CheckAchievement(AchConditionType.Consumable, usedItem.PrefabName, InternalAchievement, Logger);
            }
            else if (usedItem.ItemActionType == ItemActionType.Drink)
            {
                Player.CheckAchievement(AchConditionType.Drink, string.Empty, InternalAchievement, Logger);
                Player.CheckAchievement(AchConditionType.Drink, usedItem.PrefabName, InternalAchievement, Logger);
            }

            RemoveFromHotBar(Player.Character, usedItem, hotbarSlotId);
        }
    }

    private void HandleRangedWeapon(ItemDescription usedItem, Vector3Model position, int direction)
    {
        var rand = new Random();
        var prjId = Math.Abs(rand.Next()).ToString();
        while (Player.Room.GameObjectIds.Contains(prjId))
            prjId = Math.Abs(rand.Next()).ToString();

        // Needs stat handler.
        var prj = new ProjectileEntity(Player, prjId, position, direction, 3, usedItem, ServerRConfig.DefaultRangedDamage, usedItem.Elemental, ServerRConfig);
        Player.Room.Projectiles.Add(prjId, prj);
    }

    private void HandleMeleeWeapon(ItemDescription usedItem, Vector3Model position, int direction)
    {
        var rand = new Random();
        var prjId = Math.Abs(rand.Next()).ToString();

        while (Player.Room.GameObjectIds.Contains(prjId))
            prjId = Math.Abs(rand.Next()).ToString();

        // Needs stat handler.
        var prj = new MeleeEntity(Player, prjId, position, direction, 1, usedItem, 10, usedItem.Elemental, ServerRConfig);
        Player.Room.Projectiles.Add(prjId, prj);
    }

    private void RemoveFromHotBar(CharacterModel character, ItemDescription item, int hotbarSlotId)
    {
        character.Data.Inventory.Items[item.ItemId].Count--;
        if (character.Data.Inventory.Items[item.ItemId].Count <= 0)
        {
            character.Data.Hotbar.HotbarButtons.Remove(hotbarSlotId);
            SendXt("hu", character.Data.Hotbar);
        }
        Player.SendUpdatedInventory(false);
    }
}
