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
using System.Collections;
using System.Collections.Generic;
using AmbientBehaviorFramework;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class BehavioralEntity : BehavioralEntityBase, IAnimationEventReceiver
{
    protected static readonly int SpeedParam = Animator.StringToHash("Speed");
    protected static readonly int IdleVariantParam = Animator.StringToHash("IdleVariant");

    [Header("Character Components")]
    protected Animator animator;
    protected NavMeshAgent agent;
    
    [Header("Action Execution")]
    protected Dictionary<Int32, IActionExecutor> actionExecutors;
    private Coroutine currentActionCoroutine;
    
    protected GameObject currentActionTarget = null;
    
    protected bool IsWaitingForAnimation = false;
    
    // Hotspot tracking
    [SerializeField]
    private HotspotProvider currentHotspotProvider;
    [SerializeField]
    private HotspotDefinition currentHotspot;
    
    protected virtual void Awake()
    {
        SelectRandomNpcVariant();
        
        actionExecutors = new Dictionary<Int32, IActionExecutor>();
        InitializeActionExecutors();
        
        IsWaitingForAnimation = false;
    }
    
    protected virtual void Update()
    {
        if (IsInLocomotionState())
        {
            SyncAnimatorSpeedParam();
        }
    }
    
    #region Framework Manager
    
    protected override BehaviorFrameworkManagerBase GetFrameworkManager()
    {
        return AmbientBehaviorManager.Instance;
    }
    
    #endregion
    
    #region Action Executor Registration
    
    protected virtual void InitializeActionExecutors()
    {
        //TODO: Add action executors
    }

    protected void RegisterActionExecutor(Int32 actionId, IActionExecutor executor)
    {
        actionExecutors[actionId] = executor;
    }
    
    #endregion
    
    #region Framework Callback Implementation
    
    /// <summary>
    /// Framework requests action execution - route to executor system
    /// </summary>
    protected override void OnActionRequested(Int32 actionId, Int64 actionToken, Int64 actionDurationMs, GameObject target)
    {
        // Stop any previous action
        if (currentActionCoroutine != null)
        {
            StopCoroutine(currentActionCoroutine);
            currentActionCoroutine = null;
        }
        
        // Store action target
        currentActionTarget = target;
        
        // Execute action if executor exists
        if (actionExecutors.TryGetValue(actionId, out var executor))
        {
            currentActionCoroutine = StartCoroutine(
                executor.Execute(actionId, actionToken, actionDurationMs, target)
            );
        }
        else
        {
            Debug.LogWarning($"No executor found for action ID: {actionId}");
            // Auto-complete to prevent framework timeout
            currentActionCoroutine = StartCoroutine(AutoCompleteAction(actionDurationMs));
        }
    }
    
    #endregion
    
    #region Hotspot Management

    /// <summary>
    /// Records that this entity has claimed a hotspot. Automatically releases
    /// any previously held hotspot to prevent leaks during interruptions.
    /// Called by ActionExecutorBase.NavigateToHotspot().
    /// </summary>
    public void ClaimHotspot(HotspotProvider provider, HotspotDefinition hotspot)
    {
        // Release previous hotspot if we had one (handles interruption safety)
        ReleaseCurrentHotspot();
    
        currentHotspotProvider = provider;
        currentHotspot = hotspot;
    }

    /// <summary>
    /// Releases the currently held hotspot, making it available for other NPCs.
    /// Call from action executors that end an entity's occupation of a location
    /// Safe to call even if no hotspot is currently held.
    /// </summary>
    public void ReleaseCurrentHotspot()
    {
        if (currentHotspotProvider != null)
        {
            currentHotspotProvider.Release(this);
            currentHotspotProvider = null;
            currentHotspot = null;
        }
    }
    
    /// <summary>
    /// The currently claimed hotspot definition, or null if none.
    /// Used by ActionExecutorBase to access seat position data.
    /// </summary>
    public HotspotDefinition CurrentHotspot => currentHotspot;

    /// <summary>
    /// Whether this entity currently holds a claimed hotspot.
    /// </summary>
    public bool HasClaimedHotspot => currentHotspotProvider != null;

    #endregion
    
    #region Helper Methods for Actions
    
    public Animator GetAnimator() => animator;
    public NavMeshAgent GetAgent() => agent;

    public void OnAnimationComplete()
    {
        OnAnimationFinished();
    }
    
    public IEnumerator WaitForAnimationToComplete()
    {
        IsWaitingForAnimation = true;

        while (IsWaitingForAnimation)
        {
            yield return null;
        }
    }
    
    #endregion
    
    #region Private Helpers

    private IEnumerator AutoCompleteAction(Int64 actionDurationMs)
    {
        yield return new WaitForSeconds(actionDurationMs / 1000f);
        CompleteCurrentAction();
    }
    
    private void SelectRandomNpcVariant()
    {
        var randomIndex = Random.Range(0, transform.childCount);
        var selected = transform.GetChild(randomIndex);

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);

            if (child != selected)
            {
                Destroy(child.gameObject);
            }
        }
        
        selected.gameObject.SetActive(true);
        animator = selected.GetComponent<Animator>();
        agent = selected.GetComponent<NavMeshAgent>();
        movementTransform = selected.gameObject.transform;
        
        animator?.SetFloat(SpeedParam, 0);
        animator?.SetFloat(IdleVariantParam, Random.Range(0f, 2.99f));
    }

    private bool IsInLocomotionState()
    {
        if (!animator)
        {
            return false;
        }

        var animatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return animatorStateInfo.IsName("Locomotion_Walk");
    }

    private void SyncAnimatorSpeedParam()
    {
        if (agent == null) return;
        
        var normalizedSpeed = agent.velocity.magnitude / agent.speed;
        animator.SetFloat(SpeedParam, normalizedSpeed);
                
        // randomize idle variant
        if (normalizedSpeed < 0.1f && Random.value < 0.001f)
        {
            animator.SetFloat(IdleVariantParam, Random.Range(0f, 2.99f));
        }
    }
    
    private void OnAnimationFinished()
    {
        if (IsWaitingForAnimation)
        {
            IsWaitingForAnimation = false;
        }
    }
    
    #endregion
}