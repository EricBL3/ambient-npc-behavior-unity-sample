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
using System.Diagnostics;
using AmbientBehaviorFramework.Utilities;
using UnityEngine;
using UnityEngine.AI;
using Debug = UnityEngine.Debug;

namespace AmbientBehaviorFramework
{
    /// <summary>
    /// Base class for framework manager implementations.
    /// Handles framework lifecycle, entity management, and callback routing.
    /// Users must implement CreateEnvironmentalProvider().
    /// </summary>
    public abstract class BehaviorFrameworkManagerBase : MonoBehaviour
    {
        #region Configuration
        
        [SerializeField]
        [Tooltip("Framework configuration asset")]
        protected BehaviorFrameworkConfig config;
        
        #endregion
        
        #region Framework State
        
        protected IntPtr frameworkHandle = IntPtr.Zero;
        protected bool isInitialized = false;
        
        private readonly EntityHandleTracker handleTracker = new EntityHandleTracker();
        private IEnvironmentalQueryProvider environmentalProvider;
        
        // These delegates are passed to unmanaged code and must remain alive
        // for the entire lifetime of the framework. Do not make them local variables
        // or the garbage collector will collect them and cause a crash.
        private BehaviorFrameworkWrapper.QueryEnvironmentalConditionDelegate queryDelegate;
        private BehaviorFrameworkWrapper.StartCharacterActionDelegate actionDelegate;
        private BehaviorFrameworkWrapper.QueryEntityPositionDelegate positionDelegate;
        
        // Performance timing
        private readonly Stopwatch frameworkUpdateStopwatch = new Stopwatch();
        private double lastFrameworkUpdateMs;
        
        #endregion
        
        #region Public Properties
        
        /// <summary>
        /// Whether the framework has been successfully initialized
        /// </summary>
        public bool IsInitialized => isInitialized;
        
        /// <summary>
        /// Number of currently registered entities
        /// </summary>
        public int RegisteredEntityCount => handleTracker.Count;
        
        /// <summary>
        /// Duration of the last framework DLL Update call in milliseconds
        /// </summary>
        public double LastFrameworkUpdateMs => lastFrameworkUpdateMs;
        
        #endregion
        
        #region Unity Lifecycle
        
        protected virtual void Awake()
        {
            InitializeFramework();
        }
        
        protected virtual void Update()
        {
            if (!isInitialized || frameworkHandle == IntPtr.Zero)
            {
                return;
            }
            
            var realTime = Time.realtimeSinceStartupAsDouble * 1000;
            BehaviorFrameworkWrapper.TracyFrameMarkWithTime(realTime, Time.frameCount);
            
            var currentTimeMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            
            frameworkUpdateStopwatch.Restart();
            
            BehaviorFrameworkWrapper.Update(
                frameworkHandle,
                config.characterBatchSize,
                currentTimeMs
            );
            
            frameworkUpdateStopwatch.Stop();
            lastFrameworkUpdateMs = frameworkUpdateStopwatch.Elapsed.TotalMilliseconds;
        }
        
        protected virtual void OnDestroy()
        {
            ShutdownFramework();
        }
        
        protected virtual void OnApplicationQuit()
        {
            ShutdownFramework();
        }
        
        #endregion
        
        #region Framework Lifecycle
        
        private void InitializeFramework()
        {
            if (config == null)
            {
                Debug.LogError("BehaviorFrameworkConfig is null! Assign a config asset.", this);
                return;
            }
            
            // Create environmental provider from user implementation
            environmentalProvider = CreateEnvironmentalProvider();
            if (environmentalProvider == null)
            {
                Debug.LogError("CreateEnvironmentalProvider() returned null!", this);
                return;
            }
            
            // Create callback delegates (must persist for DLL lifetime)
            queryDelegate = OnQueryEnvironmentalCondition;
            actionDelegate = OnStartCharacterAction;
            positionDelegate = OnQueryEntityPosition;
            
            // Create framework instance
            frameworkHandle = BehaviorFrameworkWrapper.CreateAmbientBehaviorFramework(
                queryDelegate,
                actionDelegate,
                positionDelegate
            );
            
            if (frameworkHandle == IntPtr.Zero)
            {
                Debug.LogError("Failed to create framework handle", this);
                return;
            }
            
            // Initialize with config files
            var paths = config.GetResolvedPaths();
            isInitialized = BehaviorFrameworkWrapper.InitializeAmbientBehaviorFramework(
                frameworkHandle,
                paths.schemaPath,
                paths.sequencesPath,
                paths.actionsPath,
                paths.environmentalPath,
                paths.logPath,
                (int)config.logLevel
            );
            
            if (!isInitialized)
            {
                Debug.LogError("Framework initialization failed. Check framework.log for details.", this);
                return;
            }
            
            Debug.Log("Behavior Framework initialized successfully", this);
            OnFrameworkInitialized();
        }
        
        private void ShutdownFramework()
        {
            if (frameworkHandle == IntPtr.Zero)
            {
                return;
            }
            
            // Unregister all entities
            handleTracker.UnregisterAll(entityHandle =>
            {
                BehaviorFrameworkWrapper.UnregisterEntity(frameworkHandle, entityHandle);
            });
            
            // Shutdown framework
            BehaviorFrameworkWrapper.ShutdownAmbientBehaviorFramework(frameworkHandle);
            frameworkHandle = IntPtr.Zero;
            isInitialized = false;
            
            Debug.Log("Behavior Framework shutdown successfully", this);
            OnFrameworkShutdown();
        }
        
        #endregion
        
        #region Entity Management
        
        /// <summary>
        /// Register an entity with the framework
        /// </summary>
        public void RegisterEntity(IAmbientEntity entity)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("Cannot register entity: Framework not initialized", this);
                return;
            }
            
            if (entity == null)
            {
                Debug.LogWarning("Cannot register null entity", this);
                return;
            }
            
            try
            {
                var fullConfigPath = config.GetFullEntityConfigPath(entity.EntityConfigPath);
                var position = entity.GameObject.transform.position;
                var handle = handleTracker.Register(entity);
                
                BehaviorFrameworkWrapper.RegisterEntity(
                    frameworkHandle,
                    handle,
                    fullConfigPath,
                    (Int32)position.x,
                    (Int32)position.y,
                    (Int32)position.z
                );
                
                Debug.Log($"Registered entity: {entity.GameObject.name}", entity.GameObject);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to register entity {entity.GameObject.name}: {e.Message}", entity.GameObject);
            }
        }
        
        /// <summary>
        /// Unregister an entity from the framework
        /// </summary>
        public void UnregisterEntity(IAmbientEntity entity)
        {
            if (entity == null)
            {
                return;
            }
            
            if (!handleTracker.TryGetHandle(entity, out var handle))
            {
                Debug.LogWarning($"Entity {entity.GameObject.name} not registered", entity.GameObject);
                return;
            }
            
            try
            {
                BehaviorFrameworkWrapper.UnregisterEntity(frameworkHandle, handle);
                handleTracker.Unregister(entity);
                Debug.Log($"Unregistered entity: {entity.GameObject.name}", entity.GameObject);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to unregister entity {entity.GameObject.name}: {e.Message}", entity.GameObject);
            }
        }
        
        /// <summary>
        /// Notify the framework that a behavioral entity has finished executing an action.
        /// Call this from your <see cref="IBehavioralEntity"/> implementation when an action completes,
        /// using the <paramref name="actionToken"/> received in <see cref="IBehavioralEntity.OnActionRequested"/>.
        /// </summary>
        public void CompleteCharacterAction(IBehavioralEntity entity, Int32 actionId, Int64 actionToken)
        {
            if (!handleTracker.TryGetHandle(entity, out var handle))
            {
                Debug.LogWarning($"Cannot complete action: Entity {entity.GameObject.name} not registered", entity.GameObject);
                return;
            }
            
            try
            {
                BehaviorFrameworkWrapper.CompleteCharacterAction(
                    frameworkHandle,
                    handle,
                    actionId,
                    actionToken
                );
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to complete action for {entity.GameObject.name}: {e.Message}", entity.GameObject);
            }
        }
        
        /// <summary>
        /// Trigger an interruption for specific entities
        /// </summary>
        public void TriggerInterruption(Int32 interruptionId, IAmbientEntity[] affectedEntities = null)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("Cannot trigger interruption: Framework not initialized", this);
                return;
            }
            
            try
            {
                IntPtr[] handles;
                
                if (affectedEntities != null && affectedEntities.Length > 0)
                {
                    // Get handles for specific entities
                    var validHandles = new System.Collections.Generic.List<IntPtr>();
                    foreach (var entity in affectedEntities)
                    {
                        if (handleTracker.TryGetHandle(entity, out var handle))
                        {
                            validHandles.Add(handle);
                        }
                    }
                    handles = validHandles.ToArray();
                }
                else
                {
                    // Affect all registered entities
                    // (This requires exposing all handles - we'll need to add this to EntityHandleTracker)
                    Debug.LogWarning("Triggering interruption for all entities not yet implemented");
                    return;
                }
                
                BehaviorFrameworkWrapper.ProcessInterruption(
                    frameworkHandle,
                    interruptionId,
                    handles,
                    handles.Length
                );
                
                Debug.Log($"Triggered interruption {interruptionId} for {handles.Length} entities", this);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to trigger interruption: {e.Message}", this);
            }
        }
        
        #endregion
        
        #region Framework Callbacks
        
        private Int32 OnQueryEnvironmentalCondition(Int32 conditionKey)
        {
            try
            {
                return environmentalProvider.QueryCondition(conditionKey);
            }
            catch (Exception e)
            {
                Debug.LogError($"Environmental query error for key {conditionKey}: {e.Message}", this);
                return Int32.MinValue;
            }
        }
        
        private void OnStartCharacterAction(
            IntPtr entityHandle,
            Int32 actionId,
            Int64 actionToken,
            Int64 actionDurationMs,
            IntPtr targetEntityHandle)
        {
            try
            {
                if (!handleTracker.TryGetEntity<IBehavioralEntity>(entityHandle, out var entity))
                {
                    Debug.LogWarning($"Cannot find entity for action {actionId}", this);
                    return;
                }
                
                GameObject target = null;
                if (targetEntityHandle != IntPtr.Zero)
                {
                    handleTracker.TryGetGameObject(targetEntityHandle, out target);
                }
                
                entity.OnActionRequested(actionId, actionToken, actionDurationMs, target);
            }
            catch (Exception e)
            {
                Debug.LogError($"Action callback error for action {actionId}: {e.Message}", this);
            }
        }
        
        private bool OnQueryEntityPosition(IntPtr entityHandle, Int32[] outXYZ)
        {
            if (!handleTracker.TryGetEntity<IBehavioralEntity>(entityHandle, out var entity))
            {
                Debug.LogError("Could not find the entity handle to update its position!");
                return false;
            }

            Debug.Log($"Position query for: {entity.GameObject.name}, MovementTransform null: {entity.MovementTransform == null}");
            
            var pos = entity.MovementTransform.position;
            outXYZ[0] = (Int32)pos.x;
            outXYZ[1] = (Int32)pos.y;
            outXYZ[2] = (Int32)pos.z;
            
            Debug.Log($"Updating entity position to x:{outXYZ[0]}, y:{outXYZ[1]}, z:{outXYZ[2]}");
            return true;
        }
        
        #endregion
        
        #region Abstract Methods
        
        /// <summary>
        /// Create the environmental query provider for your game.
        /// Called during framework initialization.
        /// </summary>
        /// <returns>
        /// An implementation of <see cref="IEnvironmentalQueryProvider"/> that maps
        /// integer condition keys (defined in your JSON configuration) to integer values.
        /// For example, a key representing "time of day" might return 0 for day and 1 for night.
        /// Return <see cref="Int32.MinValue"/> to indicate a condition could not be evaluated.
        /// </returns>
        protected abstract IEnvironmentalQueryProvider CreateEnvironmentalProvider();
        
        #endregion
        
        #region Virtual Hooks
        
        /// <summary>
        /// Called after framework successfully initializes.
        /// Override to perform additional setup.
        /// </summary>
        protected virtual void OnFrameworkInitialized() { }
        
        /// <summary>
        /// Called before framework shuts down.
        /// Override to perform cleanup.
        /// </summary>
        protected virtual void OnFrameworkShutdown() { }
        
        #endregion
    }
}