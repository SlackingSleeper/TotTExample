using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ToolsOfTheTrade;

namespace TotTExample
{
    internal class Main :MelonMod
    {
        internal static AssetBundle assets;
        internal static readonly HarmonyLib.Harmony harmony = new("Slacking.TotTExample");
        public override void OnLateInitializeMelon()
        {
            if (Singleton<Game>.Instance == null)
            {
                LoggerInstance.Msg($"failed to initialise");
                return;
            }
            try
            {
                LoadAssets();
                InitialiseSubmodules();
            }
            catch (Exception e)
            {
                LoggerInstance.Msg($"failed to initialise {e}");
                throw;
            }
            LoggerInstance.Msg("Completed setup.");
        }
        private void InitialiseSubmodules()
        {
            example_rifle.Init();
        }
        void LoadAssets()
        {
            if (assets == null)
            {
                assets = AssetBundle.LoadFromMemory(Properties.Resources.toolsofthetrade);
                if (assets == null)
                {
                    throw new ArgumentException("failed to load AssetBundle");
                }
                else
                {
                    LoggerInstance.Msg($"Loaded {assets.GetAllAssetNames().Length} assets");
                }
            }
        }
    }
}
