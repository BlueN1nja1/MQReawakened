﻿using A2m.Server;
using Microsoft.Extensions.Logging;
using Server.Reawakened.Entities.Components;
using Server.Reawakened.Entities.Enums;
using Server.Reawakened.Network.Protocols;
using Server.Reawakened.Players;
using Server.Reawakened.Players.Extensions;
using Server.Reawakened.Players.Models;
using Server.Reawakened.Rooms.Extensions;
using Server.Reawakened.Rooms.Models.Planes;
using Server.Reawakened.XMLs.Bundles;
using Server.Reawakened.XMLs.BundlesInternal;

namespace Protocols.External._h__HotbarHandler;

public class UseSlot : ExternalProtocol
{
    public override string ProtocolName => "hu";

    public ILogger<UseSlot> Logger { get; set; }

    public ItemCatalog ItemCatalog { get; set; }
    public QuestCatalog QuestCatalog { get; set; }
    public ObjectiveCatalogInt ObjectiveCatalog { get; set; }

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
                HandleDrop(usedItem, position, direction);
                break;
            case ItemActionType.Throw:
                HandleRangedWeapon(position, direction, usedItem);
                break;
            case ItemActionType.Drink:
            case ItemActionType.Eat:
                HandleConsumable(usedItem, hotbarSlotId);
                break;
            case ItemActionType.Melee:
                HandleMeleeWeapon(position, direction);
                break;
            default:
                Logger.LogError("Could not find how to handle item action type {ItemAction} for user {UserId}",
                    usedItem.ItemActionType, targetUserId);
                break;
        }
    }

    private async Task HandleDrop(ItemDescription usedItem, Vector3Model position, int direction) //Needs XML system and revamp.
    {
        var isLeft = direction > 0;

        var dropDirection = isLeft ? 1 : -1;

        var platform = new GameObjectModel();

        var planeName = position.Z > 10 ? "Plane1" : "Plane0";
        position.Z = 0;

        await Task.Delay(1000); //Wait for drop animation to finish.

        var dropItem = new LaunchItem_SyncEvent(Player.GameObjectId.ToString(), Player.Room.Time,
            Player.TempData.Position.X + dropDirection, Player.TempData.Position.Y, Player.TempData.Position.Z,
            0, 0, 3, 0, usedItem.PrefabName);

        Player.Room.SendSyncEvent(dropItem);

        foreach (var effect in usedItem.ItemEffects)
            Console.WriteLine("ItemTypeId: " + effect.TypeId.ToString());

        foreach (var entity in Player.Room.Entities)
        {
            foreach (var component in entity.Value
                .Where(comp => Vector3Model.Distance(position, comp.Position) <= 3f))
            {
                var prefabName = component.PrefabName;
                var objectId = component.Id;

                if (component is HazardControllerComp || component is BreakableEventControllerComp breakableObj)
                {
                    await Task.Delay(2550); //Wait for bomb explosion.
                    DestroyObject(component.Entity.GameObject);
                    Logger.LogInformation("Found close hazard {PrefabName} with Id {ObjectId}", prefabName, objectId);
                }
            }
        }
    }

    private void HandleConsumable(ItemDescription usedItem, int hotbarSlotId)
    {
        StatusEffect_SyncEvent statusEffect = null;
        foreach (var effect in usedItem.ItemEffects)
        {
            if (effect.Type is ItemEffectType.Invalid or ItemEffectType.Unknown)
                return;

            if (effect.Type is ItemEffectType.Healing)         
                Player.HealCharacter(usedItem);
            
            statusEffect = new StatusEffect_SyncEvent(Player.GameObjectId.ToString(), Player.Room.Time,
                effect.TypeId, effect.Value, effect.Duration, true, Player.GameObjectId.ToString(), true);
        }
        Player.SendSyncEventToPlayer(statusEffect);

        var removeFromHotbar = true;

        if (usedItem.ItemId == 396) //Prevents Healing Staff from removing itself.
            removeFromHotbar = false;

        if (!usedItem.UniqueInInventory && removeFromHotbar)
            RemoveFromHotbar(Player.Character, usedItem, hotbarSlotId);
    }

    private void HandleRangedWeapon(Vector3Model position, int direction, ItemDescription usedItem)
    {
        var isLeft = direction > 0;

        position.X += isLeft ? -3 : 3;
        position.Y += 1;

        var bulletDirection = isLeft ? 7 : -7;

        var prefabName = usedItem.PrefabName;

        var projectile = new LaunchItem_SyncEvent(Player.GameObjectId.ToString(), Player.Room.Time,
            position.X, position.Y, position.Z, bulletDirection, 0, 4f, 0, prefabName);

        Player.Room.SendSyncEvent(projectile);
    }

    private void HandleMeleeWeapon(Vector3Model position, int direction)
    {
        AiHealth_SyncEvent aiEvent = null;

        var monsters = new List<GameObjectModel>();

        var planeName = position.Z > 10 ? "Plane1" : "Plane0";
        position.Z = 0;

        foreach (var obj in
                 Player.Room.Planes[planeName].GameObjects.Values
                     .Where(obj => Vector3Model.Distance(position, obj.ObjectInfo.Position) <= 3.4f)
                )
        {
            var isLeft = direction > 0;

            if (isLeft)
            {
                if (obj.ObjectInfo.Position.X < position.X)
                    continue;
            }
            else
            {
                if (obj.ObjectInfo.Position.X > position.X)
                    continue;
            }

            var objectId = obj.ObjectInfo.ObjectId;
            var prefabName = obj.ObjectInfo.PrefabName;

            if (Player.Room.Entities.TryGetValue(objectId, out var entityComponents))
                foreach (var component in entityComponents)
                    if (component is TriggerCoopControllerComp triggerCoopEntity)
                        triggerCoopEntity.TriggerInteraction(ActivationType.NormalDamage, Player);

            switch (prefabName)
            {
                case "PF_R01_Barrel01":
                case "PF_SHD_Barrel01":
                case "PF_CRS_BarrelNewbZone01":
                case "PF_CRS_BARREL01":
                case "PF_OUT_BARREL03":
                case "PF_WLD_Breakable01":
                case "PF_BON_BreakableUrn01":
                case "PF_EVT_HallwnPumpkinFloatBRK":
                case "PF_EVT_XmasBrkIceCubeLoot01":
                case "PF_EVT_XmasBrkIceCubeLoot02":
                case "PF_EVT_XmasBrkIceCube02":
                    DestroyObject(obj);
                    break;
            }

            aiEvent = new AiHealth_SyncEvent(objectId.ToString(),
                Player.Room.Time, 0, 100, 0, 0, "now", false, true);

            foreach (var entinty in Player.Room.Entities.Values)
                foreach (var component in entinty)
                    if (component is HazardControllerComp triggerCoopEntity ||
                        component.PrefabName.Contains("Spawner")) //Temp until BreakableEventControllerComp is added.              
                        Player.Room.SendSyncEvent(aiEvent);

            Logger.LogInformation("Found close game object {PrefabName} with Id {ObjectId}", prefabName, objectId);
        }
    }

    private void DestroyObject(GameObjectModel obj)
    {
        var random = new Random();
        var randomItem = random.Next(1, 4);
        var itemReward = 0;

        //Temp mini loot system.
        switch (randomItem)
        {
            case 1:
                itemReward = 404;
                break;
            case 2:
                itemReward = 1568;
                break;
            case 3:
                itemReward = 510;
                break;
        }

        var aiEvent = new AiHealth_SyncEvent(obj.ObjectInfo.ObjectId.ToString(),
                        Player.Room.Time, 0, 100, 0, 0, "now", false, false);


        Player.Room.SendSyncEvent(aiEvent);

        if (obj.ObjectInfo.PrefabName is not "PF_EVT_XmasBrkIceCube02" and not "PF_EVT_HallwnPumpkinFloatBRK")
            Player.Character.AddItem(ItemCatalog.GetItemFromId(itemReward), 1);

        switch (randomItem)
        {
            case 1:
                itemReward = 1613;
                break;
            case 2:
                itemReward = 1618;
                break;
            case 3:
                itemReward = 1616;
                break;
        }

        if (obj.ObjectInfo.PrefabName == "PF_EVT_HallwnPumpkinFloatBRK")
            Player.Character.AddItem(ItemCatalog.GetItemFromId(itemReward), 1);

        Player.SendUpdatedInventory(false);

        Logger.LogInformation("Object name: {args1} Object Id: {args2}",
            obj.ObjectInfo.PrefabName, obj.ObjectInfo.ObjectId);
    }

    private void RemoveFromHotbar(CharacterModel character, ItemDescription item, int hotbarSlotId)
    {
        character.Data.Inventory.Items[item.ItemId].Count--;

        if (character.Data.Inventory.Items[item.ItemId].Count <= 0)
        {
            character.Data.Hotbar.HotbarButtons.Remove(hotbarSlotId);

            SendXt("hu", character.Data.Hotbar);

            character.Data.Inventory.Items[item.ItemId].Count = -1;
        }

        Player.SendUpdatedInventory(false);
    }
}
