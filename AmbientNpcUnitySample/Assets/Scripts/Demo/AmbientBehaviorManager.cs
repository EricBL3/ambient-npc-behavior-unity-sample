// Copyright 2026 Eric Buitron-Lopez
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

// DEMO-SPECIFIC IMPLEMENTATION
// This file contains logic specific to this sample scene and is not intended for direct reuse.

using System;
using System.Linq;
using AmbientBehaviorFramework;
using TMPro;
using UnityEngine;

public class AmbientBehaviorManager : BehaviorFrameworkManagerBase
{
    public static AmbientBehaviorManager Instance { get; private set; }
    
    #region Unity Lifecycle
    protected override void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        base.Awake();
    }
    
    #endregion

    protected override IEnvironmentalQueryProvider CreateEnvironmentalProvider()
    {
        
        return new EnvironmentalProvider();
    }

}