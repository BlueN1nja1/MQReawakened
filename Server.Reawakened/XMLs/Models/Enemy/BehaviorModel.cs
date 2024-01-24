﻿using UnityEngine;

public class BehaviorModel(Dictionary<string, BehaviorDataModel> behaviorData)
{
    public Dictionary<string, BehaviorDataModel> BehaviorData { get; } = behaviorData;

    public int IndexOf(string behaviorName)
    {
        var index = 0;
        foreach (var behavior in BehaviorData)
        {
            if (behavior.Key.Equals(behaviorName))
            {
                return index;
            }
            index++;
        }

        return 0;
    }

    public object GetBehaviorStat(string behaviorName, string statName)
    {
        foreach (var behavior in BehaviorData)
        {
            if (behavior.Key.Equals(behaviorName))
            {
                foreach (var data in behavior.Value.DataList)
                {
                    if (data.Key.Equals(statName))
                        return data.Value;
                }
            }
        }

        return 0;
    }
}
