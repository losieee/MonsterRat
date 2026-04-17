using System.Collections.Generic;
using UnityEngine;

public class PlayerFootStepType : MonoBehaviour
{
    public FootStepRangeType defaultType = FootStepRangeType.Stone;

    private readonly List<FootStepType> Range = new List<FootStepType>();

    public FootStepRangeType CurrentRangeType
    {
        get
        {
            if (Range.Count > 0 && Range[Range.Count - 1] != null)
                return Range[Range.Count - 1].footStepType;

            return defaultType;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        FootStepType type = other.GetComponent<FootStepType>();
        if (type != null && !Range.Contains(type))
        {
            Range.Add(type);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        FootStepType type = other.GetComponent<FootStepType>();
        if (type != null)
        {
            Range.Remove(type);
        }
    }
}
