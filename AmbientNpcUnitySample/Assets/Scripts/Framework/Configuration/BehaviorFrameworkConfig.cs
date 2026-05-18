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

using System.IO;
using UnityEngine;

namespace AmbientBehaviorFramework
{
    /// <summary>
    /// Configuration asset for the Ambient Behavior Framework.
    /// Create via: Assets -> Create -> Ambient Behavior -> Framework Config
    /// </summary>
    [CreateAssetMenu(
        fileName = "BehaviorFrameworkConfig",
        menuName = "Ambient Behavior/Framework Config",
        order = 1
    )]
    public class BehaviorFrameworkConfig : ScriptableObject
    {
        [Header("Config File Paths (relative to project root)")]
        [Tooltip("Path to state schema JSON file")]
        public string schemaPath = "BehaviorFrameworkConfig/schema.json";
        
        [Tooltip("Path to sequences JSON file")]
        public string sequencesPath = "BehaviorFrameworkConfig/sequences.json";
        
        [Tooltip("Path to actions JSON file")]
        public string actionsPath = "BehaviorFrameworkConfig/actions.json";
        
        [Tooltip("Path to environmental conditions JSON file")]
        public string environmentalPath = "BehaviorFrameworkConfig/environmental.json";
        
        [Header("Entity Configuration")]
        [Tooltip("Folder containing entity config files (relative to project root)")]
        public string entityConfigFolder = "BehaviorFrameworkConfig/Entities";
        
        [Header("Logging")]
        [Tooltip("Path to framework log file (relative to project root)")]
        public string logPath = "Logs/framework.log";
        
        [Tooltip("Minimum log level to write")]
        public FrameworkLogLevel logLevel = FrameworkLogLevel.Info;
        
        [Header("Performance")]
        [Tooltip("Number of characters to update per frame (higher = more CPU, lower = slower updates, -1 = update all NPCs each frame)")]
        [Min(-1)]
        public int characterBatchSize = 10;
        
        /// <summary>
        /// Get all config paths resolved to absolute file system paths
        /// </summary>
        public ConfigPaths GetResolvedPaths()
        {
            var dataPath = Application.dataPath;
            return new ConfigPaths
            {
                schemaPath = Path.Combine(dataPath, schemaPath),
                sequencesPath = Path.Combine(dataPath, sequencesPath),
                actionsPath = Path.Combine(dataPath, actionsPath),
                environmentalPath = Path.Combine(dataPath, environmentalPath),
                logPath = Path.Combine(dataPath, logPath)
            };
        }
        
        /// <summary>
        /// Get full path for an entity config file
        /// </summary>
        /// <param name="relativeEntityPath">Relative path from entity config folder</param>
        public string GetFullEntityConfigPath(string relativeEntityPath)
        {
            return Path.Combine(
                Application.dataPath,
                entityConfigFolder,
                relativeEntityPath
            );
        }
    }
    
    /// <summary>
    /// Resolved absolute paths for framework configuration files
    /// </summary>
    public struct ConfigPaths
    {
        public string schemaPath;
        public string sequencesPath;
        public string actionsPath;
        public string environmentalPath;
        public string logPath;
    }
}