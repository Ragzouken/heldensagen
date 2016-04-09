﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class ConflictView : MonoBehaviour 
{
    [SerializeField] private Transform center;

    private void OnEnable()
    {
        center.rotation = UnityEngine.Random.rotationUniform;
    }
}
