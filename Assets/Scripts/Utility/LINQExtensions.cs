using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public static partial class LINQExtensions
{
    public static T MaxBy<T>(this IEnumerable<T> sequence,
                             Func<T, float> Value)
    {
        float maxValue = Mathf.NegativeInfinity;
        T maxElement = default(T);

        foreach (T element in sequence)
        {
            float value = Value(element);

            if (value > maxValue)
            {
                maxValue = value;
                maxElement = element;
            } 
        }

        return maxElement;
    }
}
