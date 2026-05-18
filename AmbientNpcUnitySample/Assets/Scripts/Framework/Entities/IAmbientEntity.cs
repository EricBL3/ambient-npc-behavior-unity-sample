// Copyright 2026 Eric Buitron Lopez
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

// REUSABLE WRAPPER LAYER
// This file can be used in other Unity projects as-is.
// Copy Assets/Scripts/Framework/ and Assets/Plugins/ into your project to integrate the framework.

using UnityEngine;

namespace AmbientBehaviorFramework
{
    /// <summary>
    /// Minimal contract for any entity that can be registered with the framework
    /// </summary>
    public interface IAmbientEntity
    {
        /// <summary>
        /// The Unity GameObject this entity represents
        /// </summary>
        GameObject GameObject { get; }
        
        /// <summary>
        /// Relative path to the entity's JSON configuration file
        /// </summary>
        string EntityConfigPath { get; }
        
        /// <summary>
        /// Whether this entity is currently registered with the framework
        /// </summary>
        bool IsRegistered { get; }
        
        /// <summary>
        /// The Transform used to track this entity's position in the world.
        /// Typically the root transform or a dedicated movement pivot.
        /// </summary>
        Transform MovementTransform { get; }
    }
}