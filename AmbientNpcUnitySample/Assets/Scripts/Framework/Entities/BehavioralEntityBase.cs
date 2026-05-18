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

using System;
using UnityEngine;

namespace AmbientBehaviorFramework
{
    /// <summary>
    /// Base implementation for behavioral entities that can execute actions.
    /// Provides routing between framework callbacks and user implementation.
    /// </summary>
    public abstract class BehavioralEntityBase : AmbientEntityBase, IBehavioralEntity
    {
        // Current action tracking for completion callback
        private Int32 currentActionId;
        private Int64 currentActionToken;
        
        #region IBehavioralEntity Implementation
        
        void IBehavioralEntity.OnActionRequested(Int32 actionId, Int64 actionToken, Int64 actionDurationMs, 
            GameObject target)
        {
            // Store action info for completion
            currentActionId = actionId;
            currentActionToken = actionToken;
            
            // Route to user implementation
            OnActionRequested(actionId, actionToken, actionDurationMs, target);
        }
        
        #endregion
        
        #region Abstract Methods
        
        /// <summary>
        /// Called when the framework requests this entity to execute an action.
        /// Override this to implement a specific action execution logic.
        /// CompleteCurrentAction() MUST be eventually called or the framework will time out.
        /// </summary>
        /// <param name="actionId">Unique identifier for the action type</param>
        /// <param name="actionToken">Unique token for this specific action instance</param>
        /// <param name="actionDurationMs">Duration of the action in milliseconds</param>
        /// <param name="target">Optional target GameObject for the action (can be null)</param>
        protected abstract void OnActionRequested(Int32 actionId, Int64 actionToken, Int64 actionDurationMs, 
            GameObject target);
        
        #endregion
        
        #region Protected API
        
        /// <summary>
        /// Get the currently executing action ID (-1 if no action is running)
        /// </summary>
        protected Int32 CurrentActionId => currentActionId;
        
        /// <summary>
        /// Get the currently executing action token (-1 if no action is running)
        /// </summary>
        protected Int64 CurrentActionToken => currentActionToken;
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Notifies the framework that the current action has completed.
        /// This must be called when action execution finishes.
        /// </summary>
        public void CompleteCurrentAction()
        {
            var manager = GetFrameworkManager();
            if (manager == null)
            {
                Debug.LogWarning($"Cannot complete action: Framework manager not found", this);
                return;
            }
            
            manager.CompleteCharacterAction(this, currentActionId, currentActionToken);
        }
        
        #endregion
        
    }
}