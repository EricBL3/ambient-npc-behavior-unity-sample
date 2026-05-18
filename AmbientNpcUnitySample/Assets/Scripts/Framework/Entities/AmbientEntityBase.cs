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
    /// Base implementation for ambient entities that can be registered with the framework.
    /// Handles automatic registration/unregistration lifecycle.
    /// </summary>
    public abstract class AmbientEntityBase : MonoBehaviour, IAmbientEntity
    {
        [Header("Framework Configuration")]
        [SerializeField] 
        [Tooltip("Relative path to this entity's JSON configuration file")]
        protected string entityConfigPath;
        
        [SerializeField] 
        [Tooltip("Automatically register with framework on Start")]
        protected bool autoRegister = true;
        
        private bool isRegistered = false;

        protected Transform movementTransform;
        
        #region IAmbientEntity Implementation
        
        public GameObject GameObject => gameObject;
        public string EntityConfigPath => entityConfigPath;
        public bool IsRegistered => isRegistered;
        
        public Transform MovementTransform => movementTransform;

        #endregion
        
        #region Unity Lifecycle
        
        protected virtual void Start()
        {
            if (autoRegister && !isRegistered)
            {
                RegisterWithFramework();
            }
        }
        
        protected virtual void OnDestroy()
        {
            if (isRegistered)
            {
                UnregisterFromFramework();
            }
        }
        
        #endregion
        
        #region Registration Management
        
        /// <summary>
        /// Manually register this entity with the framework
        /// </summary>
        public void RegisterWithFramework()
        {
            if (isRegistered)
            {
                Debug.LogWarning($"Entity {gameObject.name} already registered", this);
                return;
            }
            
            var manager = GetFrameworkManager();
            if (manager == null)
            {
                Debug.LogError($"Cannot register {gameObject.name}: Framework manager not found", this);
                return;
            }
            
            manager.RegisterEntity(this);
            isRegistered = true;
            OnRegistered();
        }
        
        /// <summary>
        /// Manually unregister this entity from the framework
        /// </summary>
        public void UnregisterFromFramework()
        {
            if (!isRegistered)
            {
                return;
            }
            
            var manager = GetFrameworkManager();
            if (manager != null)
            {
                manager.UnregisterEntity(this);
            }
            
            isRegistered = false;
            OnUnregistered();
        }
        
        #endregion
        
        #region Abstract Methods
        
        /// <summary>
        /// Get the framework manager instance.
        /// </summary>
        protected abstract BehaviorFrameworkManagerBase GetFrameworkManager();
        
        #endregion
        
        #region Virtual Hooks
        
        /// <summary>
        /// Called after successful registration with the framework
        /// </summary>
        protected virtual void OnRegistered() { }
        
        /// <summary>
        /// Called after unregistration from the framework
        /// </summary>
        protected virtual void OnUnregistered() { }
        
        #endregion
    }
}