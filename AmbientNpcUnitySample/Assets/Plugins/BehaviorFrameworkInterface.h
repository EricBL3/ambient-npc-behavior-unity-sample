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

#pragma once
#include <cstdint>

#ifdef _WIN32
    #ifdef AmbientCoreFramework_EXPORTS
        #define AmbientCoreFramework_API __declspec(dllexport)
    #else
        #define AmbientCoreFramework_API __declspec(dllimport)
    #endif
#else
    #define AmbientCoreFramework_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

    /**
     * Callback type for querying environmental condition values from the game engine.
     * The framework invokes this when a cached condition value becomes stale based on
     * its configured update frequency.
     *
     * @param condition_key  Integer key identifying the condition to query.
     * @return               Current value of the condition as a 32-bit integer.
     */
    typedef int32_t (*QueryEnvironmentalConditionFn) (int32_t condition_key);

    /**
     * Callback type for requesting the game engine to execute an action on an NPC.
     * The engine must store the action_token and return it when signaling completion
     * via CompleteCharacterAction.
     *
     * @param entity_handle         Opaque handle identifying the NPC.
     * @param action_id             Identifier of the action to execute.
     * @param action_token          Unique token for this action instance. Must be returned
     *                              on completion to match the request.
     * @param action_duration_ms    Expected duration of the action in milliseconds.
     * @param target_entity_handle  Handle of the target entity, or null for actions
     *                              that do not require a target.
     */
    typedef void (*StartCharacterActionFn) (void* entity_handle, int32_t action_id, int64_t action_token,
        int64_t action_duration_ms, void* target_entity_handle);

    /**
     * Callback type for querying an entity's current position from the game engine.
     * Invoked during distance-based precondition evaluation.
     *
     * @param entity_id  Opaque handle identifying the entity.
     * @param out_xyz    Pointer to an array of three int32_t values where the engine
     *                   writes the entity's current x, y, z coordinates.
     * @return           True if the position was successfully written. On failure,
     *                   the framework uses the previously cached position.
     */
    typedef bool (*QueryEntityPositionFn) (void* entity_id, int32_t* out_xyz);

    /* ---- Framework Lifecycle ----
     *
     * Expected call order: Create -> Initialize -> (Update loop) -> Shutdown.
     * Create returns a framework handle used by all subsequent calls.
     */

    /**
     * Creates a new framework instance with the provided engine callbacks.
     *
     * @param env_callback             Callback for querying environmental conditions.
     * @param start_action_callback    Callback for requesting action execution.
     * @param query_position_callback  Callback for querying entity positions.
     * @return                         Opaque framework handle, or null on failure.
     */
    AmbientCoreFramework_API void* CreateAmbientBehaviorFramework(QueryEnvironmentalConditionFn env_callback,
        StartCharacterActionFn start_action_callback, QueryEntityPositionFn query_position_callback);

    /**
     * Initializes the framework by loading behavior definitions from JSON configuration files.
     * Must be called after Create and before any other operations.
     *
     * @param framework_handle                  Handle returned by Create.
     * @param schema_file_path                  Path to the entity schema configuration file.
     * @param sequences_file_path               Path to the action sequences configuration file.
     * @param actions_file_path                 Path to the actions configuration file.
     * @param environmental_conditions_file_path Path to the environmental conditions configuration file.
     * @param log_file_path                     Path where the framework log file will be written.
     * @param log_level                         Logging verbosity: 0 = debug, 1 = info, 2 = warning,
     *                                          3 = error.
     * @return                                  True if all configuration files were loaded successfully.
     */
    AmbientCoreFramework_API bool InitializeAmbientBehaviorFramework(void* framework_handle, const char* schema_file_path,
        const char* sequences_file_path, const char* actions_file_path,
        const char* environmental_conditions_file_path, const char*  log_file_path, int32_t log_level);

    /**
     * Shuts down the framework, processing any remaining pending entity commands
     * and releasing all resources. The framework handle is invalid after this call.
     */
    AmbientCoreFramework_API void ShutdownAmbientBehaviorFramework(void* framework_handle);

    /* ---- Runtime Operations ---- */

    /**
     * Drives the framework's update cycle. Processes pending entity commands and
     * updates NPCs in round-robin order.
     *
     * @param framework_handle  Handle returned by Create.
     * @param batch_size        Maximum number of NPCs to update per call.
     * @param current_time      Current game time in milliseconds, provided by the engine.
     */
    AmbientCoreFramework_API void Update(void* framework_handle, int32_t batch_size, int64_t current_time);

    /**
     * Triggers an interruption on a set of entities. Entities that are not behavioral
     * entities or that lack a handler for the given interruption are skipped.
     *
     * @param framework_handle  Handle returned by Create.
     * @param interruption_id   Identifier of the interruption to trigger.
     * @param entity_handles    Array of opaque entity handles to interrupt.
     * @param count             Number of handles in the array.
     */
    AmbientCoreFramework_API void ProcessInterruption(void* framework_handle, int32_t interruption_id,
        void** entity_handles, int32_t count);

    /* ---- Entity Management ----
     *
     * Registration and unregistration are queued and take effect during the next
     * Update call, ensuring entity state does not change mid-update.
     */

    /**
     * Queues an entity for registration with the framework.
     *
     * @param framework_handle  Handle returned by Create.
     * @param entity_handle     Opaque handle identifying the entity in the game engine.
     * @param config_path       Path to the JSON entity configuration file.
     * @param entity_pos_x      Initial x coordinate.
     * @param entity_pos_y      Initial y coordinate.
     * @param entity_pos_z      Initial z coordinate.
     */
    AmbientCoreFramework_API void RegisterEntity(void* framework_handle, void* entity_handle, const char* config_path,
        int32_t entity_pos_x, int32_t entity_pos_y, int32_t entity_pos_z);

    /** Queues an entity for removal from the framework.
     * @param framework_handle  Handle returned by Create.
     * @param entity_handle     Opaque handle identifying the entity in the game engine.
     */
    AmbientCoreFramework_API void UnregisterEntity(void* framework_handle, void* entity_handle);

    /**
     * Signals that the game engine has finished executing an action for an entity.
     *
     * @param framework_handle  Handle returned by Create.
     * @param entity_handle     Handle of the entity that completed the action.
     * @param action_id         Identifier of the completed action.
     * @param action_token      Token received in the StartCharacterAction callback.
     *                          Used to match this completion to the original request.
     */
    AmbientCoreFramework_API void CompleteCharacterAction(void* framework_handle, void* entity_handle, int32_t action_id,
        int64_t action_token);

    /* ---- Profiling Utility ----
     * Not part of the core framework API. Used for performance profiling with Tracy. */

    /** Sends a frame marker to the Tracy profiler with the engine's timing data. */
    AmbientCoreFramework_API void TracyFrameMarkWithTime(double engineTimeMs, int engineFrame);

#ifdef __cplusplus
}
#endif