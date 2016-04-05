using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class DelayedEnable : MonoBehaviour 
{
    [SerializeField] private Behaviour behaviour;
    [SerializeField] private float delay;

    private IEnumerator Start()
    {
        behaviour.enabled = false;

        yield return new WaitForSeconds(delay);

        behaviour.enabled = true;
    }
}
