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

namespace AmbientBehaviorFramework
{
    /// <summary>
    /// Contract for providing environmental condition queries to the framework
    /// </summary>
    public interface IEnvironmentalQueryProvider
    {
        /// <summary>
        /// Query an environmental condition by its key.
        /// Return Int32.MinValue if the query fails or the condition is unknown.
        /// </summary>
        /// <param name="conditionKey">Framework-defined condition identifier</param>
        /// <returns>Current value of the condition, or int.MinValue on failure</returns>
        Int32 QueryCondition(Int32 conditionKey);
    }
}