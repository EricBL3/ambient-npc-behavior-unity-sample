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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace AmbientBehaviorFramework.Utilities
{
    /// <summary>
    /// Manages GCHandle lifecycle and bidirectional entity-handle mappings.
    /// Ensures entities remain alive while registered with unmanaged framework.
    /// </summary>
    internal class EntityHandleTracker
    {
        private readonly Dictionary<IAmbientEntity, GCHandle> entityToHandle = new Dictionary<IAmbientEntity, GCHandle>();
        private readonly Dictionary<IntPtr, IAmbientEntity> handleToEntity = new Dictionary<IntPtr, IAmbientEntity>();
        
        /// <summary>
        /// Get count of currently registered entities
        /// </summary>
        public int Count => entityToHandle.Count;
        
        /// <summary>
        /// Register an entity and return its handle for framework use
        /// </summary>
        public IntPtr Register(IAmbientEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }
            
            if (entityToHandle.ContainsKey(entity))
            {
                Debug.LogWarning($"Entity {entity.GameObject.name} already registered");
                return GCHandle.ToIntPtr(entityToHandle[entity]);
            }
            
            // Create weak GCHandle to GameObject (framework only needs reference)
            var handle = GCHandle.Alloc(entity.GameObject, GCHandleType.Weak);
            var handlePtr = GCHandle.ToIntPtr(handle);
            
            entityToHandle[entity] = handle;
            handleToEntity[handlePtr] = entity;
            
            return handlePtr;
        }
        
        /// <summary>
        /// Unregister an entity and free its handle
        /// </summary>
        public void Unregister(IAmbientEntity entity)
        {
            if (entity == null || !entityToHandle.TryGetValue(entity, out var handle))
            {
                return;
            }
            
            var handlePtr = GCHandle.ToIntPtr(handle);
            
            entityToHandle.Remove(entity);
            handleToEntity.Remove(handlePtr);
            handle.Free();
        }
        
        /// <summary>
        /// Unregister all entities with callback for each
        /// </summary>
        public void UnregisterAll(Action<IntPtr> unregisterCallback)
        {
            foreach (var kvp in entityToHandle)
            {
                var handlePtr = GCHandle.ToIntPtr(kvp.Value);
                unregisterCallback?.Invoke(handlePtr);
                kvp.Value.Free();
            }
            
            entityToHandle.Clear();
            handleToEntity.Clear();
        }
        
        /// <summary>
        /// Try to get the handle for a registered entity
        /// </summary>
        public bool TryGetHandle(IAmbientEntity entity, out IntPtr handle)
        {
            if (entity != null && entityToHandle.TryGetValue(entity, out var gcHandle))
            {
                handle = GCHandle.ToIntPtr(gcHandle);
                return true;
            }
            
            handle = IntPtr.Zero;
            return false;
        }
        
        /// <summary>
        /// Try to get a typed entity from a handle
        /// </summary>
        public bool TryGetEntity<T>(IntPtr handle, out T entity) where T : class, IAmbientEntity
        {
            if (handleToEntity.TryGetValue(handle, out var baseEntity) && baseEntity is T typedEntity)
            {
                entity = typedEntity;
                return true;
            }
            
            entity = null;
            return false;
        }
        
        /// <summary>
        /// Try to get the GameObject for a handle
        /// </summary>
        public bool TryGetGameObject(IntPtr handle, out GameObject gameObject)
        {
            if (handleToEntity.TryGetValue(handle, out var entity))
            {
                gameObject = entity.GameObject;
                return true;
            }
            
            gameObject = null;
            return false;
        }
    }
}