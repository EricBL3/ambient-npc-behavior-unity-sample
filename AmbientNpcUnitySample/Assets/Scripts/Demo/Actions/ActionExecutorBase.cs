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
using UnityEngine;
using UnityEngine.AI;

public abstract class ActionExecutorBase : IActionExecutor
{
    protected BehavioralEntity entity;
    protected Animator animator;
    protected NavMeshAgent agent;
    
    protected static readonly int ActionIdParam = Animator.StringToHash("ActionId");
    protected static readonly int IsSittingParam = Animator.StringToHash("IsSitting");
    
    /// <summary>
    /// Duration in seconds for the NPC to smoothly rotate to face the hotspot direction.
    /// </summary>
    private const float FaceRotationDuration = 0.3f;
    
    private const float SeatLerpDuration = 0.25f;

    public ActionExecutorBase(BehavioralEntity entity)
    {
        this.entity = entity;
        this.animator = entity.GetAnimator();
        this.agent = entity.GetAgent();
    }
    
    public abstract IEnumerator Execute(Int32 actionId, Int64 actionToken, Int64 actionDurationMs, GameObject target);

    protected IEnumerator WaitForDuration(Int64 durationMs)
    {
        yield return new WaitForSeconds(durationMs / 1000f);
    }
    
    /// <summary>
    /// Navigates to a customer hotspot on the target entity. Claims an available hotspot,
    /// walks to it, and rotates to face the hotspot's configured direction.
    /// Falls back to the target's root position if no HotspotProvider is found.
    /// </summary>
    protected IEnumerator NavigateToHotspot(GameObject target, Int64 actionDurationMs, float timeoutMultiplier = 3f,
        HotspotRole role = HotspotRole.All)
    {
        return NavigateToHotspotInternal(target, role, actionDurationMs, timeoutMultiplier);
    }
    
    private IEnumerator NavigateToHotspotInternal(GameObject target, HotspotRole role, Int64 actionDurationMs, float timeoutMultiplier)
    {
        if (target == null || agent == null)
        {
            Debug.LogWarning($"{entity.gameObject.name}: Cannot navigate - target or agent is null");
            yield break;
        }

        // Try to find hotspot provider and claim a spot
        var hotspotProvider = target.GetComponent<HotspotProvider>();
        HotspotDefinition hotspot = null;

        if (hotspotProvider != null)
        {
            hotspot = hotspotProvider.TryClaimWithRole(entity, role);

            if (hotspot == null)
            {
                // All claimed (shouldn't happen due to framework capacity, but fallback)
                hotspot = hotspotProvider.GetClosest(entity.transform.position);
                Debug.LogWarning(
                    $"{entity.gameObject.name}: All hotspots claimed on {target.name}, using closest.");
            }
            else
            {
                // Track the claimed hotspot on the entity for later release
                entity.ClaimHotspot(hotspotProvider, hotspot);
            }
        }

        // Navigate to hotspot position, or target root if no hotspots
        Vector3 destination = hotspot?.point != null ? hotspot.point.position : target.transform.position;

        agent.SetDestination(destination);
        agent.isStopped = false;
        var timeout = (timeoutMultiplier * actionDurationMs) / 1000f;
        var elapsedTime = 0f;

        while (!IsNavigationComplete() && elapsedTime < timeout)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        agent.isStopped = true;

        // Rotate to face the hotspot's configured direction
        if (hotspot?.point != null)
        {
            yield return FaceDirection(hotspot.point.forward);
        }
    }
    
    /// <summary>
    /// After navigating to a hotspot with a seat position, disables the NavMeshAgent
    /// and collider, then lerps the NPC to the exact seat position.
    /// Call this after NavigateToHotspot completes, before playing the sit animation.
    /// </summary>
    protected IEnumerator PlaceInSeat()
    {
        var claimedHotspot = entity.CurrentHotspot;
        if (claimedHotspot?.seatPosition == null)
        {
            // No seat position configured — NPC just stays where they navigated to
            yield break;
        }

        // Disable movement systems so we can place the NPC through the furniture collider
        SetMovementEnabled(false);

        // Lerp to the exact seat position and rotation
        var agentTransform = agent.transform;
        var startPos = agentTransform.position;
        var startRot = agentTransform.rotation;
        var targetPos = claimedHotspot.seatPosition.position;
        var targetRot = claimedHotspot.seatPosition.rotation;
        var elapsed = 0f;

        while (elapsed < SeatLerpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / SeatLerpDuration);
            t = t * t * (3f - 2f * t); // Smoothstep
            agentTransform.position = Vector3.Lerp(startPos, targetPos, t);
            agentTransform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        agentTransform.position = targetPos;
        agentTransform.rotation = targetRot;
    }
    
    /// <summary>
    /// Re-enables the NavMeshAgent and collider after a sitting action completes.
    /// Call this during stand-up executors before navigation resumes.
    /// Warps the agent to the current position to prevent snapping.
    /// </summary>
    protected void RestoreFromSeat()
    {
        if (agent == null) return;

        var currentPos = agent.transform.position;
    
        SetMovementEnabled(true);

        // Warp the agent to current position so it doesn't snap back 
        // to wherever it was when we disabled it
        if (agent.isOnNavMesh)
        {
            agent.Warp(currentPos);
        }
    }

    protected IEnumerator NavigateToTarget(GameObject target, Int64 actionDurationMs, float timeoutMultiplier = 3f)
    {
        if (target == null || agent == null)
        {
            Debug.LogWarning($"{entity.gameObject.name}: Cannot navigate - target or agent is null");
            yield break;
        }
        
        agent.SetDestination(target.transform.position);
        agent.isStopped = false;
        var timeout = (timeoutMultiplier * actionDurationMs) / 1000f;
        var elapsedTime = 0f;

        while (!IsNavigationComplete() && elapsedTime < timeout)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        agent.isStopped = true;
    }
    
    /// <summary>
    /// Smoothly rotates the NPC's agent transform to face the given direction.
    /// </summary>
    protected IEnumerator FaceDirection(Vector3 forward)
    {
        if (agent == null) yield break;

        // Flatten to horizontal plane
        forward.y = 0;
        if (forward.sqrMagnitude < 0.001f) yield break;

        var targetRotation = Quaternion.LookRotation(forward);
        var startRotation = agent.transform.rotation;
        var elapsed = 0f;

        while (elapsed < FaceRotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / FaceRotationDuration);
            // Smooth step for a natural-looking rotation
            t = t * t * (3f - 2f * t);
            agent.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        agent.transform.rotation = targetRotation;
    }
    
    protected bool IsNavigationComplete()
    {
        if (agent == null || !agent.isOnNavMesh)
        {
            return false;
        }

        return !agent.pathPending &&
               agent.remainingDistance <= agent.stoppingDistance &&
               (!agent.hasPath || agent.velocity.sqrMagnitude < 0.1f);
    }
    
    /// <summary>
    /// Toggles NavMeshAgent and CapsuleCollider on the agent's GameObject.
    /// Used for sitting placement where we need to move through furniture colliders.
    /// </summary>
    protected void SetMovementEnabled(bool enabled)
    {
        if (agent != null)
        {
            agent.enabled = enabled;
        }

        var capsule = agent != null 
            ? agent.GetComponent<CapsuleCollider>() 
            : null;
        if (capsule != null)
        {
            capsule.enabled = enabled;
        }
    }
    
    protected void StopMovement()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }
}