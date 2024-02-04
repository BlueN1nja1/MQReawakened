﻿using Server.Reawakened.Players;

namespace Server.Reawakened.Entities.AbstractComponents;

public interface ITriggerComp
{
    public void RunTrigger(Player player);
    public void AddPhysicalInteractor(string playerId);
    public void RemovePhysicalInteractor(string playerId);
    public int GetPhysicalInteractorCount();
    public string[] GetPhysicalInteractorIds();
}
