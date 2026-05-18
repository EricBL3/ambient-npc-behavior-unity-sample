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
using System.Runtime.InteropServices;

namespace AmbientBehaviorFramework
{
    /// <summary>
    /// Low-level P/Invoke wrapper for the AmbientCoreFramework DLL
    /// </summary>
    public static class BehaviorFrameworkWrapper
    {
        private const string DLL_NAME = "AmbientCoreFramework";
        
        #region Callback Delegates
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate Int32 QueryEnvironmentalConditionDelegate(Int32 conditionKey);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void StartCharacterActionDelegate(
            IntPtr entityHandle, 
            Int32 actionID, 
            Int64 actionToken, 
            Int64 actionDurationMs,
            IntPtr targetEntityHandle
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool QueryEntityPositionDelegate(
            IntPtr entityHandle, 
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeConst = 3)] Int32[] outXYZ
        );
        
        #endregion
        
        #region Framework Lifecycles
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateAmbientBehaviorFramework(
            QueryEnvironmentalConditionDelegate queryCallback,
            StartCharacterActionDelegate actionCallback, 
            QueryEntityPositionDelegate queryEntityPositionCallback
        );
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool InitializeAmbientBehaviorFramework(
            IntPtr frameworkHandle, 
            string schemaFilePath,
            string sequencesFilePath, 
            string actionsFilePath,
            string environmentalConditionsFilePath, 
            string  logFilePath, 
            Int32 logLevel
        );
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ShutdownAmbientBehaviorFramework(IntPtr frameworkHandle);
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Update(
            IntPtr frameworkHandle, 
            Int32 batchSize, 
            Int64 currentTime
        );
        
        #endregion
        
        #region Entity Management
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void RegisterEntity(
            IntPtr frameworkHandle, 
            IntPtr entityHandle, 
            string configPath, 
            Int32 entityPosX, 
            Int32 entityPosY, 
            Int32 entityPosZ
        );
            
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void UnregisterEntity(
            IntPtr frameworkHandle, 
            IntPtr entityHandle
        );
        
        #endregion
        
        #region Action Management
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CompleteCharacterAction(
            IntPtr frameworkHandle, 
            IntPtr entityHandle, 
            Int32 actionID,
            Int64 actionToken
        );
        
        #endregion
        
        #region Interruptions
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ProcessInterruption(
            IntPtr frameworkHandle, 
            Int32 interruptionID, 
            IntPtr[] entityHandles,
            Int32 count
        );
        
        #endregion

        #region Profiling
        
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TracyFrameMarkWithTime(
            double engineTimeMs, 
            int engineFrame
        );
        
        #endregion
    }
   
    /// <summary>
    /// Framework logging levels
    /// </summary>
    public enum FrameworkLogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
    }
}

