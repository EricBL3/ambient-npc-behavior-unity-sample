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
using UnityEngine;
using Random = UnityEngine.Random;

public enum HotspotRole
{
    All,
}

/// <summary>
/// Defines a single interaction hotspot with position, facing direction,
/// role assignment, and an optional seat position for sitting actions.
/// </summary>
[Serializable]
public class HotspotDefinition
{
    [Tooltip("Transform defining the approach position and facing direction (Z-forward).")]
    public Transform point;

    [Tooltip("Role restriction for this hotspot.")]
    public HotspotRole role = HotspotRole.All;

    [Tooltip("Optional: exact seat position for sitting actions. " +
             "If set, NPCs navigate to 'point' then get placed at 'seatPosition'. " +
             "Leave empty for non-sitting hotspots.")]
    public Transform seatPosition;
}

/// <summary>
/// Manages interaction hotspots for a target entity (stall, bench, fountain, etc.).
/// Attach to the target GameObject and configure hotspots in the inspector.
/// </summary>
public class HotspotProvider : MonoBehaviour
{
    [Tooltip("Child Transforms that define interaction points. " +
         "Position = where the NPC stands, Forward = direction NPC faces.")]
    [SerializeField] 
    private HotspotDefinition[] hotspots;

    // Parallel arrays tracking occupation state
    [SerializeField]
    private bool[] claimed;
    [SerializeField]
    private BehavioralEntity[] claimedBy;

    private void Awake()
    {
        if (hotspots == null || hotspots.Length == 0)
        {
            Debug.LogWarning($"HotspotProvider on {gameObject.name} has no hotspots assigned.", this);
            hotspots = Array.Empty<HotspotDefinition>();
        }

        claimed = new bool[hotspots.Length];
        claimedBy = new BehavioralEntity[hotspots.Length];
    }
    
    /// <summary>
    /// Claims a random available hotspot.
    /// Returns the hotspot definition, or null if none are available.
    /// </summary>
    public HotspotDefinition TryClaim(BehavioralEntity entity)
    {
        return TryClaimWithRole(entity, HotspotRole.All);
    }

    /// <summary>
    /// Claims a random available hotspot matching the specified role.
    /// Returns the hotspot definition, or null if none are available.
    /// </summary>
    public HotspotDefinition TryClaimWithRole(BehavioralEntity entity, HotspotRole role)
    {
        // Collect available indices
        var availableCount = 0;
        for (var i = 0; i < hotspots.Length; i++)
        {
            if (!claimed[i] && hotspots[i].role == role) availableCount++;
        }

        if (availableCount == 0)
        {
            return null;
        }

        // Pick a random available hotspot
        var pick = Random.Range(0, availableCount);
        var current = 0;
        for (var i = 0; i < hotspots.Length; i++)
        {
            if (!claimed[i] && hotspots[i].role == role)
            {
                if (current == pick)
                {
                    claimed[i] = true;
                    claimedBy[i] = entity;
                    return hotspots[i];
                }
                current++;
            }
        }

        // Should not reach here
        return null;
    }

    /// <summary>
    /// Returns the closest hotspot Transform regardless of availability.
    /// Used as a fallback when all hotspots are claimed.
    /// </summary>
    public HotspotDefinition GetClosest(Vector3 fromPosition)
    {
        if (hotspots.Length == 0) return null;

        var closest = hotspots[0];
        var closestDist = Vector3.Distance(fromPosition, hotspots[0].point.position);

        for (var i = 1; i < hotspots.Length; i++)
        {
            var dist = Vector3.Distance(fromPosition, hotspots[i].point.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = hotspots[i];
            }
        }

        return closest;
    }

    /// <summary>
    /// Releases the hotspot currently held by the given entity, if any.
    /// </summary>
    public void Release(BehavioralEntity entity)
    {
        for (var i = 0; i < claimedBy.Length; i++)
        {
            if (claimedBy[i] == entity)
            {
                claimed[i] = false;
                claimedBy[i] = null;
                return;
            }
        }
    }

    /// <summary>
    /// Number of configured hotspots on this provider.
    /// </summary>
    public int HotspotCount => hotspots.Length;

    /// <summary>
    /// Number of currently available (unclaimed) hotspots.
    /// </summary>
    public int AvailableCount
    {
        get
        {
            var count = 0;
            for (var i = 0; i < claimed.Length; i++)
            {
                if (!claimed[i]) count++;
            }
            return count;
        }
    }

#region Debug Visualization

    private void OnDrawGizmosSelected()
    {
        if (hotspots == null) return;

        for (var i = 0; i < hotspots.Length; i++)
        {
            var def = hotspots[i];
            if (def?.point == null) continue;

            var pos = def.point.position;
            var forward = def.point.forward;

            // Show claimed state in play mode, neutral in edit mode
            var isClaimed = Application.isPlaying && claimed != null && i < claimed.Length && claimed[i];
            
            // Role-based coloring
            if (isClaimed)
            {
                Gizmos.color = Color.red;
            }
            else
            {
                Gizmos.color = def.role switch
                {
                    HotspotRole.All => Color.green,
                    _ => Gizmos.color
                };
            }

            // Approach position
            Gizmos.DrawSphere(pos, 0.2f);

            // Facing direction arrow
            Gizmos.DrawLine(pos, pos + forward * 0.6f);
            Gizmos.DrawSphere(pos + forward * 0.6f, 0.06f);
            
            // Seat position (if configured)
            if (def.seatPosition != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(def.seatPosition.position, 0.12f);
                Gizmos.DrawLine(pos, def.seatPosition.position);
            }
        }
    }

#endregion
}
