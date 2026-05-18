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

// REUSABLE WRAPPER LAYER
// This file can be used in other Unity projects as-is.
// Copy Assets/Scripts/Framework/ and Assets/Plugins/ into your project to integrate the framework.

using System;
using UnityEngine;

namespace AmbientBehaviorFramework
{
    /// <summary>
    /// Contract for entities that can execute actions
    /// </summary>
    public interface IBehavioralEntity : IAmbientEntity
    {
        /// <summary>
        /// The framework requests this entity to start executing an action.
        /// The entity is responsible for eventually calling the manager's
        /// CompleteCharacterAction method with the provided actionId and actionToken.
        /// </summary>
        /// <param name="actionId">Unique identifier for the action type</param>
        /// <param name="actionToken">Unique token for this specific action instance</param>
        /// <param name="actionDurationMs">Duration of the action in milliseconds</param>
        /// <param name="target">Optional target GameObject for the action</param>
        void OnActionRequested(Int32 actionId, Int64 actionToken, Int64 actionDurationMs, GameObject target);
    }
}