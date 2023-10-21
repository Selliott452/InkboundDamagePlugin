using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using InkboundModEnabler.Util;
using ShinyShoe;
using ShinyShoe.Ares;
using ShinyShoe.EcsEventSystem;
using ShinyShoe.SharedDataLoader;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;


namespace InkboundDamage
{
    public class Player
    {
        public int ID = -1;
        public string Name = "Unknown";
        public string ClassName = "Unknown";
        public readonly Dictionary<string, List<int>> DamageDealt = new();
        private readonly Dictionary<string, List<int>> _damageReceived = new();

        public int GetTotalDamageDealt()
        {
            return DamageDealt.Sum(entry => entry.Value.Sum());
        }

        public int GetDamageDealtBy(string key)
        {
            return DamageDealt[key].Sum();
        }

        public string GetDamagePercent(string key)
        {
            return $"({(float)GetDamageDealtBy(key) / GetTotalDamageDealt():0.0%})";
        }

        public int GetTotalDamageReceived()
        {
            return _damageReceived.Sum(entry => entry.Value.Sum());
        }

        public void AddDamageDealt(EventOnUnitDamaged ev)
        {
            var worldStateChange = ev.WorldStateChange;
            if (worldStateChange.LootableData != null)
            {
                if (!DamageDealt.Keys.ToList().Contains(worldStateChange.LootableData.Name))
                {
                    DamageDealt.Add(worldStateChange.LootableData.Name, new List<int>());
                }

                DamageDealt[worldStateChange.LootableData.Name].Add(worldStateChange.DamageAmount);
            }

            else if (worldStateChange.StatusEffectData != null)
            {
                if (!DamageDealt.Keys.ToList().Contains(worldStateChange.StatusEffectData.Name))
                {
                    DamageDealt.Add(worldStateChange.StatusEffectData.Name, new List<int>());
                }

                DamageDealt[worldStateChange.StatusEffectData.Name].Add(worldStateChange.DamageAmount);
            }

            else if (worldStateChange.AbilityData != null)
            {
                if (!DamageDealt.Keys.ToList().Contains(worldStateChange.AbilityData.Name))
                {
                    DamageDealt.Add(worldStateChange.AbilityData.Name, new List<int>());
                }

                DamageDealt[worldStateChange.AbilityData.Name].Add(worldStateChange.DamageAmount);
            }
        }

        public void AddDamageReceived(EventOnUnitDamaged ev)
        {
            var worldStateChange = ev.WorldStateChange;

            if (worldStateChange.StatusEffectData != null)
            {
                if (!_damageReceived.Keys.ToList().Contains(worldStateChange.StatusEffectData.name))
                {
                    _damageReceived.Add(worldStateChange.StatusEffectData.name, new List<int>());
                }

                _damageReceived[worldStateChange.StatusEffectData.name].Add(worldStateChange.DamageAmount);
            }

            else if (worldStateChange.AbilityData != null)
            {
                if (!_damageReceived.Keys.ToList().Contains(worldStateChange.AbilityData.name))
                {
                    _damageReceived.Add(worldStateChange.AbilityData.name, new List<int>());
                }

                _damageReceived[worldStateChange.AbilityData.name].Add(worldStateChange.DamageAmount);
            }
        }
    }

    public class GameTracker
    {
        public readonly List<Player> Players = new();
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [CosmeticPlugin]
    public class InkboundDamage : BaseUnityPlugin
    {
        private static readonly GameTracker Game = new();

        private static TextMeshProUGUI _label;
        private static Harmony HarmonyInstance => new(PluginInfo.PLUGIN_GUID);

        private static readonly ManualLogSource CustomLogger = new("Inkbound Damage");

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");

            BepInEx.Logging.Logger.Sources.Add(CustomLogger);

            HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        }

        private static string GetName(EntityHandle entityHandle, MessagingSystem.Components comps)
        {
            var entityName = comps.unitCombatDbRo.GetDisplayName(entityHandle);
            if (string.IsNullOrEmpty(entityName)
                && comps.scriptableObjectDbRo.TryGetGuid(entityHandle, out var dataId)
                && comps.assetLibrary.TryGetData(dataId, out ShinyShoe.Ares.SharedSOs.UnitData unitData))
            {
                entityName = unitData.Name;
            }

            return entityName;
        }

        [HarmonyPatch(typeof(StatHudScreenVisual))]
        public static class StatHudScreenVisualPatch
        {
            [HarmonyPatch("Initialize")]
            [HarmonyPrefix]
            public static void Initialize(StatHudScreenVisual __instance,
                EntityHandle playerEntityHandle,
                UnitCombatDB.IReadonly unitCombatDbRo,
                ClientAssetDB clientAssetDb,
                EventDB eventDb
            )
            {
                if (_label != null) return;
                var testObject = new GameObject("Testing")
                {
                    transform =
                    {
                        parent = __instance.gameObject.transform,
                        localPosition = Vector3.zero,
                        position = Vector3.zero
                    }
                };


                _label = testObject.AddComponent<TextMeshProUGUI>();
                _label.text = "Waiting for combat...";
                _label.enabled = true;
                _label.transform.position = Vector3.zero;
                _label.transform.localPosition = new Vector3(-250, 280, 0);
                _label.fontSize = 20;
            }
        }


        // [HarmonyPatch(typeof(PartySystem))]
        // public static class PartySystemPatch
        // {
        //     [HarmonyPatch("HandlePartyRunStart")]
        //     [HarmonyPrefix]
        //     public static void HandlePartyRunStart()
        //     {
        //         label = null;
        //     }
        // }

        [HarmonyPatch(typeof(MessagingSystem))]
        public static class MessagingSystemPatch
        {
            [HarmonyPatch("HandleEventOnUnitDamaged")]
            [HarmonyPrefix]
            public static void HandleEventOnUnitDamaged(
                EventOnUnitDamaged ev,
                bool wasQueued,
                MessagingSystem.Components comps)
            {
                if (Game.Players.Count == 0)
                {
                    foreach (var entityHandle in comps.unitCombatDbRo.GetUnits())
                    {
                        if (!UnitCombatDBHelper.GetUnitIsPlayer(comps.unitCombatDbRo, entityHandle)) continue;
                        
                        CustomLogger.LogInfo("Player Found: " + entityHandle);

                        CharacterClassDBHelper.TryGetCharacterClassData(
                            entityHandle,
                            comps.worldStateRo.GetCharacterClassDBRo(),
                            comps.assetLibrary,
                            out var characterClassData);

                        Game.Players.Add(
                            new Player
                            {
                                ID = entityHandle.Handle,
                                Name = GetName(entityHandle, comps),
                                ClassName = characterClassData.Name
                            });
                    }
                }

                if (Game.Players.ConvertAll(player => player.ID)
                    .Contains(ev.WorldStateChange.TargetUnitHandle.Handle))
                {
                    CustomLogger.LogInfo("Found a player taking damage! " + ev.WorldStateChange.DamageAmount);
                    var currentPlayer =
                        Game.Players.Find(player => player.ID == ev.WorldStateChange.TargetUnitHandle.Handle);

                    currentPlayer.AddDamageReceived(ev);
                    UpdateLabel(currentPlayer);
                }

                if (Game.Players.ConvertAll(player => player.ID)
                    .Contains(ev.WorldStateChange.SourceEntityHandle.Handle))
                {
                    CustomLogger.LogInfo("Found a player doing damage! " + ev.WorldStateChange.DamageAmount);
                    var currentPlayer =
                        Game.Players.Find(player => player.ID == ev.WorldStateChange.SourceEntityHandle.Handle);

                    currentPlayer.AddDamageDealt(ev);
                    UpdateLabel(currentPlayer);
                }
            }
        }

        private static void UpdateLabel(Player currentPlayer)
        {
            
            var labelText = "Player: " + currentPlayer.Name + " (" + currentPlayer.ClassName + ")\n" +
                            "Total Damage: " + currentPlayer.GetTotalDamageDealt() + "\n" +
                            "Total Damage Received: " + currentPlayer.GetTotalDamageReceived();

            // labelText = currentPlayer.DamageDealt.Keys.Aggregate(labelText, (current, key) => current + ("\n" + key + ": " + currentPlayer.getDamageDealtBy(key) + " " + currentPlayer.getDamagePercent(key)));

            var sortedEntries = from entry in currentPlayer.DamageDealt
                orderby entry.Value.Sum() descending
                select entry;

            labelText = sortedEntries.Aggregate(labelText,
                (current, entry) => current + ("\n" + entry.Key + ": " +
                                               currentPlayer.GetDamageDealtBy(entry.Key) + " " +
                                               currentPlayer.GetDamagePercent(entry.Key)));

            CustomLogger.LogInfo(labelText);

            _label.text = labelText;
        }
    }
}