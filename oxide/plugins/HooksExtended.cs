using System;
using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info ("HooksExtended", "Calytic @ RustServers.IO", "0.1.3", ResourceId = 2239)]
    public class HooksExtended : RustPlugin
    {
        #region Variables

        List<uint> WoodMaterial = new List<uint> () {
            3655341
        };

        List<uint> RockMaterial = new List<uint> () {
            3506021,
            3712324229
        };

        List<uint> MetalMaterial = new List<uint> ()
        {
            103787271,
            4214819287
        };

        List<uint> SnowMaterial = new List<uint> ()
        {
            3535235
        };

        List<uint> GrassMaterial = new List<uint> () {
            98615734
        };

        List<uint> SandMaterial = new List<uint> () {
            3522692
        };

        Vector3 eyesPosition = new Vector3 (0f, 0.5f, 0f);
        static int playerLayer = LayerMask.GetMask ("Player (Server)");
        static int useLayer = LayerMask.GetMask (new string [] { "Player (Server)", "Construction", "Deployed", "Tree", "Resource", "Terrain", "AI", "Clutter", "Debris", "Vehicle Movement" });
        List<BasePlayer> inputCooldown = new List<BasePlayer> ();
        Dictionary<BasePlayer, List<MonoBehaviour>> spotCooldown = new Dictionary<BasePlayer, List<MonoBehaviour>> ();
        int spottingMask = LayerMask.GetMask (new string [] { "Player (Server)", "Construction", "Deployed", "Tree", "Resource", "Terrain", "AI", "Clutter", "Debris", "Vehicle Movement" });
        public PluginSettings settings;

        [OnlinePlayers]
        Hash<BasePlayer, PlayerProfile> onlinePlayers = new Hash<BasePlayer, PlayerProfile> ();

        #endregion

        #region Boilerplate

        Dictionary<string, bool> defaultHookSettings = new Dictionary<string, bool> () {
            {"OnPlayerTick", false},
            {"OnPlayerAttack", false},
            {"OnRunPlayerMetabolism", false},
            {"OnItemDeployed", false},
            {"OnEntityTakeDamage", false},
            {"OnConsumeFuel", false},
            {"OnEntityDeath", false},
            {"OnItemAddedToContainer", false},
            {"OnItemRemovedFromContainer", false},
            {"OnPlayerInput", false},
            {"OnItemCraft", false},
            {"OnItemCraftCancelled", false},
            {"OnItemCraftFinished", false},
            {"CanCraft", false},
            {"OnEntitySpawned", false},
            {"OnItemAction", false},
            {"CanNetworkTo", false},
        };

        class PlayerProfile
        {
            public BasePlayer Player;
            public ProfileMetabolism Metabolism;

            public bool wasDucked;
            public bool wasDrowning;
            public bool wasSprinting;
            public bool wasFlying;
            public bool wasSwimming;
            public bool wasAiming;
            public bool wasReceivingSnapshot;
            public Item activeItem;
        }

        class ProfileMetabolism
        {
            public enum MetaAction
            {
                Start,
                Stop
            }

            float wetness;
            float radiation_poison;
            float radiation_level;
            float poison;
            float comfort;
            float bleeding;
            float oxygen;

            public ProfileMetabolism (PlayerMetabolism metabolism)
            {
                Set (metabolism);
            }

            void Set (PlayerMetabolism metabolism)
            {
                wetness = metabolism.wetness.value;
                radiation_poison = metabolism.radiation_poison.value;
                radiation_level = metabolism.radiation_level.value;
                poison = metabolism.poison.value;
                comfort = metabolism.comfort.value;
                bleeding = metabolism.bleeding.value;
            }

            public Dictionary<string, MetaAction> DetectChange (PlayerMetabolism metabolism)
            {
                Dictionary<string, MetaAction> actions = new Dictionary<string, MetaAction> ();

                if (metabolism.wetness.value != wetness) {
                    if (metabolism.wetness.value == metabolism.wetness.min) {
                        actions.Add ("Wetness", MetaAction.Stop);
                    } else if (wetness == metabolism.wetness.min) {
                        actions.Add ("Wetness", MetaAction.Start);
                    }
                }

                if (metabolism.poison.value != poison) {
                    if (metabolism.poison.value == metabolism.poison.min) {
                        actions.Add ("Poison", MetaAction.Stop);
                    } else if (poison == metabolism.poison.min) {
                        actions.Add ("Poison", MetaAction.Start);
                    }
                }

                if (metabolism.oxygen.value != oxygen) {
                    if (metabolism.oxygen.value == metabolism.oxygen.min) {
                        actions.Add ("Drowning", MetaAction.Stop);
                    } else if (oxygen == metabolism.oxygen.min) {
                        actions.Add ("Drowning", MetaAction.Start);
                    }
                }

                if (metabolism.radiation_level.value != radiation_level) {
                    if (metabolism.radiation_level.value == metabolism.radiation_level.min) {
                        actions.Add ("Radiation", MetaAction.Stop);
                    } else if (radiation_level == metabolism.radiation_level.min) {
                        actions.Add ("Radiation", MetaAction.Start);
                    }
                }

                if (metabolism.radiation_poison.value != radiation_poison) {
                    if (metabolism.radiation_poison.value == metabolism.radiation_poison.min) {
                        actions.Add ("RadiationPoison", MetaAction.Stop);
                    } else if (radiation_poison == metabolism.radiation_poison.min) {
                        actions.Add ("RadiationPoison", MetaAction.Start);
                    }
                }

                if (metabolism.comfort.value != comfort) {
                    if (metabolism.comfort.value == metabolism.comfort.min) {
                        actions.Add ("Comfort", MetaAction.Stop);
                    } else if (comfort == metabolism.comfort.min) {
                        actions.Add ("Comfort", MetaAction.Start);
                    }
                }

                if (metabolism.bleeding.value != bleeding) {
                    if (metabolism.bleeding.value == metabolism.bleeding.min) {
                        actions.Add ("Bleeding", MetaAction.Stop);
                    } else if (bleeding == metabolism.bleeding.min) {
                        actions.Add ("Bleeding", MetaAction.Start);
                    }
                }

                Set (metabolism);

                return actions;
            }
        }

        public class PluginSettings
        {
            public Dictionary<string, bool> HookSettings;
            public string VERSION;
        }

        Dictionary<Type, string> killableTypes = new Dictionary<Type, string> ()
        {
            {typeof(BuildingBlock),"OnStructureDeath"},
            {typeof(NPCPlayer),"OnNPCDeath"},
            {typeof(BasePlayer),"OnPlayerDeath"},
            {typeof(AutoTurret),"OnTurretDeath"},
            {typeof(FlameTurret),"OnTurretDeath"},
            {typeof(BaseHelicopter),"OnHelicopterDeath"},
            {typeof(BuildingPrivlidge),"OnCupboardDeath"},
            {typeof(BaseCorpse),"OnCorpseDeath"},
            {typeof(SleepingBag),"OnSleepingBagDeath"},
            {typeof(BaseAnimalNPC),"OnAnimalDeath"},
            {typeof(BradleyAPC),"OnBradleyAPCDeath"},
            {typeof(BaseTrap),"OnTrapDeath"},
            {typeof(GunTrap),"OnTrapDeath"},
            {typeof(StorageContainer),"OnContainerDeath"},
            {typeof(BaseBoat),"OnBoatDeath"},
            {typeof(BaseCar),"OnCarDeath"},
            {typeof(CH47Helicopter),"OnChinookDeath"},
            {typeof(BaseLadder),"OnLadderDeath"},
        };

        Dictionary<Type, string> spawnableTypes = new Dictionary<Type, string> ()
        {
            {typeof(SupplyDrop),"OnSupplyDropSpawned"},
            {typeof(BaseHelicopter),"OnHelicopterSpawned"},
            {typeof(HelicopterDebris),"OnHelicopterDebrisSpawned"},
            {typeof(BaseAnimalNPC),"OnAnimalSpawned"},
            {typeof(NPCPlayer),"OnNPCSpawned"},
            {typeof(LootContainer),"OnLootContainerSpawned"},
            {typeof(BuildingPrivlidge),"OnCupboardSpawned"},
            {typeof(BaseCorpse),"OnCorpseSpawned"},
            {typeof(SleepingBag),"OnSleepingBagSpawned"},
            {typeof(AutoTurret),"OnTurretSpawned"},
            {typeof(FlameTurret),"OnTurretSpawned"},
            {typeof(BradleyAPC),"OnBradleyAPCSpawned"},
            {typeof(BaseTrap),"OnTrapSpawned"},
            {typeof(GunTrap),"OnTrapSpawned"},
            {typeof(StorageContainer),"OnContainerSpawned"},
            {typeof(BaseBoat),"OnBoatSpawned"},
            {typeof(BaseCar),"OnCarSpawned"},
            {typeof(CH47Helicopter),"OnChinookSpawned"},
            {typeof(BaseLadder),"OnLadderSpawned"},
        };

        Dictionary<Type, string> networkableTypes = new Dictionary<Type, string> ()
        {
            {typeof(NPCPlayer),"CanNPCNetworkTo"},
            {typeof(BasePlayer),"CanPlayerNetworkTo"},
            {typeof(SupplyDrop),"CanSupplyDropNetworkTo"},
            {typeof(BaseHelicopter),"CanHelicopterNetworkTo"},
            {typeof(HelicopterDebris),"CanHelicopterDebrisNetworkTo"},
            {typeof(BaseAnimalNPC),"CanAnimalNetworkTo"},
            {typeof(LootContainer),"CanLootContainerNetworkTo"},
            {typeof(BuildingPrivlidge),"CanCupboardNetworkTo"},
            {typeof(BaseCorpse),"CanCorpseNetworkTo"},
            {typeof(SleepingBag),"CanSleepingBagNetworkTo"},
            {typeof(AutoTurret),"CanTurretNetworkTo"},
            {typeof(FlameTurret),"CanTurretNetworkTo"},
            {typeof(BradleyAPC),"CanBradleyAPCNetworkTo"},
            {typeof(BaseTrap),"CanTrapNetworkTo"},
            {typeof(GunTrap),"CanTrapNetworkTo"},
            {typeof(StorageContainer),"CanContainerNetworkTo"},
            {typeof(BaseBoat),"CanBoatNetworkTo"},
            {typeof(BaseCar),"CanCarNetworkTo"},
            {typeof(CH47Helicopter),"CanChinookNetworkTo"},
            {typeof(BaseLadder),"CanLadderNetworkTo"},
        };

        Dictionary<Type, string> damagableTypes = new Dictionary<Type, string> ()
        {
            {typeof(NPCPlayer),"OnNPCDamage"},
            {typeof(BuildingBlock),"OnStructureDamage"},
            {typeof(BasePlayer),"OnPlayerDamage"},
            {typeof(AutoTurret),"OnTurretDamage"},
            {typeof(FlameTurret),"OnTurretDamage"},
            {typeof(BaseHelicopter),"OnHelicopterDamage"},
            {typeof(BuildingPrivlidge),"OnCupboardDamage"},
            {typeof(BaseCorpse),"OnCorpseDamage"},
            {typeof(SleepingBag),"OnSleepingBagDamage"},
            {typeof(BaseAnimalNPC),"OnAnimalDamage"},
            {typeof(BradleyAPC),"OnBradleyAPCDamage"},
            {typeof(BaseTrap),"OnTrapDamage"},
            {typeof(GunTrap),"OnTrapDamage"},
            {typeof(StorageContainer),"OnContainerDamage"},
            {typeof(BaseBoat),"OnBoatDamage"},
            {typeof(BaseCar),"OnCarDamage"},
            {typeof(CH47Helicopter),"OnChinookDamage"},
            {typeof(BaseLadder),"OnLadderDamage"},
        };

        Dictionary<Type, string> attackTypes = new Dictionary<Type, string> ()
        {
            {typeof(AutoTurret),"OnTurretAttack"},
            {typeof(FlameTurret),"OnTurretAttack"},
            {typeof(BaseHelicopter),"OnHelicopterAttack"},
            {typeof(BaseAnimalNPC),"OnAnimalAttack"},
            {typeof(NPCPlayer),"OnNPCAttack"},
            {typeof(BradleyAPC),"OnBradleyAPCAttack"},
            {typeof(BaseTrap),"OnTrapAttack"},
            {typeof(GunTrap),"OnTrapAttack"},
            {typeof(CH47Helicopter),"OnChinookAttack"},
        };

        Dictionary<Type, string> deployableTypes = new Dictionary<Type, string> ()
        {
            {typeof(BuildingPrivlidge),"OnCupboardDeployed"},
            {typeof(AutoTurret),"OnTurretDeployed"},
            {typeof(FlameTurret),"OnTurretDeployed"},
            {typeof(Door),"OnDoorDeployed"},
            {typeof(Barricade),"OnBarricadeDeployed"},
            {typeof(Stocking),"OnStockingDeployed"},
            {typeof(SleepingBag),"OnSleepingBagDeployed"},
            {typeof(Signage),"OnSignDeployed"},
            {typeof(BaseTrap),"OnTrapDeployed"},
            {typeof(GunTrap),"OnTrapDeployed"},
            {typeof(StorageContainer),"OnContainerDeployed"},
            {typeof(BaseLadder),"OnLadderDeployed"},
        };

        Dictionary<Type, string> spottableTypes = new Dictionary<Type, string> ()
        {
            {typeof(NPCPlayer),"OnSpotNPC"},
            {typeof(BasePlayer),"OnSpotPlayer"},
            {typeof(BaseAnimalNPC),"OnSpotAnimal"},
            {typeof(BuildingPrivlidge),"OnSpotCupboard"},
            {typeof(AutoTurret),"OnSpotTurret"},
            {typeof(FlameTurret),"OnSpotTurret"},
            {typeof(BaseHelicopter),"OnSpotHelicopter"},
            {typeof(ResourceDispenser),"OnSpotResource"},
            {typeof(BradleyAPC),"OnSpotBradleyAPC"},
            {typeof(BaseTrap),"OnSpotTrap"},
            {typeof(GunTrap),"OnSpotTrap"},
            {typeof(StorageContainer),"OnSpotContainer"},
            {typeof(BaseBoat),"OnSpotBoat"},
            {typeof(BaseCar),"OnSpotCar"},
            {typeof(CH47Helicopter),"OnSpotChinook"},
            {typeof(BaseLadder),"OnSpotLadder"},
        };

        Dictionary<Type, string> usableTypes = new Dictionary<Type, string> ()
        {
            {typeof(NPCPlayer),"OnUseNPC"},
            {typeof(BasePlayer),"OnUsePlayer"},
            {typeof(BaseAnimalNPC),"OnUseAnimal"},
            {typeof(BuildingPrivlidge),"OnUseCupboard"},
            {typeof(AutoTurret),"OnUseTurret"},
            {typeof(FlameTurret),"OnUseTurret"},
            {typeof(BaseHelicopter),"OnUseHelicopter"},
            {typeof(ResourceDispenser),"OnUseResource"},
            {typeof(BradleyAPC),"OnUseBradleyAPC"},
            {typeof(BaseTrap),"OnUseTrap"},
            {typeof(GunTrap),"OnUseTrap"},
            {typeof(StorageContainer),"OnUseContainer"},
            {typeof(SleepingBag),"OnUseSleepingBag"},
            {typeof(BuildingBlock),"OnUseBuilding"},
            {typeof(BaseBoat),"OnUseBoat"},
            {typeof(BaseCar),"OnUseCar"},
            {typeof(CH47Helicopter),"OnUseChinook"},
            {typeof(BaseLadder),"OnUseLadder"},
        };

        Dictionary<Type, Func<object, object>> typeCasting = new Dictionary<Type, Func<object, object>> ()
        {
            {typeof(NPCPlayer), delegate(object obj) {
                return (NPCPlayer)obj;
            }},
            {typeof(BasePlayer), delegate(object obj) {
                return (BasePlayer)obj;
            }},
            {typeof(BaseAnimalNPC), delegate(object obj) {
                return (BaseAnimalNPC)obj;
            }},
            {typeof(BuildingPrivlidge), delegate(object obj) {
                return (BuildingPrivlidge)obj;
            }},
            {typeof(AutoTurret), delegate(object obj) {
                return (AutoTurret)obj;
            }},
            {typeof(FlameTurret), delegate(object obj) {
                return (FlameTurret)obj;
            }},
            {typeof(BaseHelicopter), delegate(object obj) {
                return (BaseHelicopter)obj;
            }},
            {typeof(ResourceDispenser), delegate(object obj) {
                return (ResourceDispenser)obj;
            }},
            {typeof(BradleyAPC), delegate(object obj) {
                return (BradleyAPC)obj;
            }},
            {typeof(BaseTrap), delegate(object obj) {
                return (BaseTrap)obj;
            }},
            {typeof(GunTrap), delegate(object obj) {
                return (GunTrap)obj;
            }},
            {typeof(StorageContainer), delegate(object obj) {
                return (StorageContainer)obj;
            }},
            {typeof(SleepingBag), delegate(object obj) {
                return (SleepingBag)obj;
            }},
            {typeof(BuildingBlock), delegate(object obj) {
                return (BuildingBlock)obj;
            }},
            {typeof(BaseBoat), delegate(object obj) {
                return (BaseBoat)obj;
            }},
            {typeof(BaseCar), delegate(object obj) {
                return (BaseCar)obj;
            }},
            {typeof(CH47Helicopter), delegate(object obj) {
                return (CH47Helicopter)obj;
            }},
            {typeof(BaseLadder), delegate(object obj) {
                return (BaseLadder)obj;
            }}
        };

        #endregion

        #region Initialization & Configuration

        void Init ()
        {
            UnsubscribeHooks ();
        }

        void OnServerInitialized ()
        {
            if (settings == null) {
                LoadConfigValues ();
            }

            SubscribeHooks ();
        }

        void UnsubscribeHooks ()
        {
            foreach (KeyValuePair<string, bool> kvp in defaultHookSettings) {
                Unsubscribe (kvp.Key);
            }
        }

        void SubscribeHooks ()
        {
            if (settings == null) {
                PrintError ("Settings invalid");
                return;
            }

            if (settings.HookSettings == null) {
                PrintError ("Hook Settings invalid");
                return;
            }

            foreach (KeyValuePair<string, bool> kvp in settings.HookSettings) {
                if (kvp.Value) {
                    Subscribe (kvp.Key);
                }
            }
        }

        void EnableHook (string hookName, bool save = true)
        {
            ConfigureHook (hookName, true, save);
        }

        void EnableHooks (params string [] hookNames)
        {
            foreach (string hookName in hookNames) {
                EnableHook (hookName, false);
            }

            SaveSettings ();
            UnsubscribeHooks ();
            SubscribeHooks ();
        }

        void DisableHook (string hookName, bool save = true)
        {
            ConfigureHook (hookName, false, save);
        }

        void DisableHooks (params string [] hookNames)
        {
            foreach (string hookName in hookNames) {
                DisableHook (hookName, false);
            }

            SaveSettings ();
            UnsubscribeHooks ();
            SubscribeHooks ();
        }

        void ConfigureHook (string hookName, bool setting, bool save = true)
        {
            if (settings.HookSettings.ContainsKey (hookName)) {
                settings.HookSettings [hookName] = setting;
            } else {
                settings.HookSettings.Add (hookName, setting);
            }

            if (save) {
                SaveSettings ();
                UnsubscribeHooks ();
                SubscribeHooks ();
            }
        }

        protected override void LoadDefaultConfig ()
        {
            settings = new PluginSettings () {
                HookSettings = defaultHookSettings,
                VERSION = Version.ToString ()
            };
            SaveSettings ();
        }

        protected void SaveSettings ()
        {
            Config.WriteObject (settings, true);
        }

        void LoadConfigValues ()
        {
            settings = Config.ReadObject<PluginSettings> ();

            foreach (KeyValuePair<string, bool> kvp in defaultHookSettings) {
                if (!settings.HookSettings.ContainsKey (kvp.Key)) {
                    settings.HookSettings.Add (kvp.Key, kvp.Value);
                }
            }
        }

        #endregion

        #region Console Commands
        [ConsoleCommand ("hookx")]
        void ccHooksExtendedStatus (ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null) {
                if (arg.Args == null) {

                    Puts ("Hook Configuration:");
                    foreach (KeyValuePair<string, bool> kvp in settings.HookSettings) {
                        Puts ("{0} = {1}", kvp.Key, kvp.Value);
                    }

                } else if (arg.Args.Length > 0) {
                    var command = arg.Args [0].ToLower ();
                    UnsubscribeHooks ();
                    switch (command) {
                    case "enable":
                        foreach (KeyValuePair<string, bool> kvp in defaultHookSettings) {
                            settings.HookSettings [kvp.Key] = true;
                        }
                        Puts ("All hooks enabled");
                        break;
                    case "disable":
                        foreach (KeyValuePair<string, bool> kvp in defaultHookSettings) {
                            settings.HookSettings [kvp.Key] = false;
                        }
                        Puts ("All hooks disabled");
                        break;
                    }
                    SubscribeHooks ();
                }
            }
        }
        #endregion

        #region Extended Hooks

        /// <summary>
        /// CAN TYPE NETWORK TO
        /// </summary>
        /// <returns>Whether to network to</returns>
        /// <param name="entity">Entity.</param>
        /// <param name="target">Target.</param>
        object CanNetworkTo (BaseNetworkable entity, BasePlayer target)
        {
            string hook;
            if (TryGetHook (entity.GetType (), networkableTypes, out hook)) {
                return Interface.Oxide.CallHook (hook, entity, target);
            }
            return null;
        }


        /// <summary>
        /// ON ITEM DROP
        /// ON ITEM UNWRAP
        /// ON ITEM * ACTION
        /// </summary>
        /// <returns>Returning non-null cancels action.</returns>
        /// <param name="item">Item.</param>
        /// <param name="action">Action.</param>
        object OnItemAction (Item item, string action)
        {
            return Interface.Oxide.CallHook ("OnItem" + action, item);
        }

        /// <summary>
        /// CAN CATEGORY CRAFT
        /// </summary>
        /// <returns>Craft status.</returns>
        /// <param name="itemCrafter">Item crafter.</param>
        /// <param name="bp">Bp.</param>
        /// <param name="amount">Amount.</param>
        object CanCraft (ItemCrafter itemCrafter, ItemBlueprint bp, int amount)
        {
            string category = bp.targetItem.category.ToString ();
            return Interface.Oxide.CallHook ("Can" + category + "Craft", itemCrafter, bp, amount);
        }


        /// <summary>
        /// ON CATEGORY CRAFT
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        object OnItemCraft (ItemCraftTask task)
        {
            string category = task.blueprint.targetItem.category.ToString ();

            return Interface.Oxide.CallHook ("On" + category + "Craft", task);
        }

        /// <summary>
        /// ON CATEGORY CRAFT CANCELLED
        /// </summary>
        /// <param name="task"></param>
        void OnItemCraftCancelled (ItemCraftTask task)
        {
            string category = task.blueprint.targetItem.category.ToString ();

            Interface.Oxide.CallHook ("On" + category + "CraftCancelled", task);
        }

        /// <summary>
        /// ON CATEGORY CRAFT FINISHED
        /// </summary>
        /// <param name="task"></param>
        /// <param name="item"></param>
        void OnItemCraftFinished (ItemCraftTask task, Item item)
        {
            string category = item.info.category.ToString ();

            Interface.Oxide.CallHook ("On" + category + "CraftFinished", task, item);
        }

        /// <summary>
        /// ON ACTIVATE
        /// ON DEACTIVATE
        /// ON DUCK
        /// ON STAND
        /// ON BEGIN SPRINT
        /// ON END SPRINT
        /// </summary>
        /// <param name="player"></param>
        void OnPlayerTick (BasePlayer player)
        {
            PlayerProfile profile;
            if (!TryGetPlayer (player, out profile)) {
                return;
            }

            Item item = player.GetActiveItem ();
            if (item != null && item != profile.activeItem) {
                Interface.CallHook ("OnItemActivate", player, item);
                profile.activeItem = item;
            } else if (item == null && profile.activeItem != null) {
                Interface.CallHook ("OnItemDeactivate", player, item);
                profile.activeItem = null;
            }

            if (player.modelState.aiming) {
                if (!profile.wasAiming) {
                    Interface.CallHook ("OnStartAiming", player);
                }
                profile.wasAiming = true;
            } else {
                if (profile.wasAiming) {
                    Interface.CallHook ("OnStopAiming", player);
                }
                profile.wasAiming = false;
            }

            if (player.IsReceivingSnapshot) {
                if (!profile.wasReceivingSnapshot) {
                    Interface.CallHook ("OnReceivingSnapshot", player);
                }
                profile.wasReceivingSnapshot = true;
            } else {
                if (profile.wasReceivingSnapshot) {
                    Interface.CallHook ("OnReceivedSnapshot", player);
                }
                profile.wasReceivingSnapshot = false;
            }

            if (player.IsSwimming ()) {
                if (!profile.wasSwimming) {
                    Interface.CallHook ("OnStartSwimming", player);
                }
                profile.wasSwimming = true;
            } else {
                if (profile.wasSwimming) {
                    Interface.CallHook ("OnStopSwimming", player);
                }
                profile.wasSwimming = false;
            }

            if (player.IsFlying) {
                if (!profile.wasFlying) {
                    Interface.CallHook ("OnStartFlying", player);
                }
                profile.wasFlying = true;
            } else {
                if (profile.wasFlying) {
                    Interface.CallHook ("OnStopFlying", player);
                }
                profile.wasFlying = false;
            }

            if (player.IsDucked ()) {
                if (!profile.wasDucked) {
                    Interface.CallHook ("OnPlayerDuck", player);
                }
                profile.wasDucked = true;
            } else {
                if (profile.wasDucked) {
                    Interface.CallHook ("OnPlayerStand", player);
                }
                profile.wasDucked = false;
            }

            if (player.IsRunning ()) {
                if (!profile.wasSprinting) {
                    Interface.CallHook ("OnStartSprint", player);
                }
                profile.wasSprinting = true;
            } else {
                if (profile.wasSprinting) {
                    Interface.CallHook ("OnStopSprint", player);
                }
                profile.wasSprinting = false;
            }
        }

        /// <summary>
        /// ON HIT RESOURCE
        /// ON HIT WOOD
        /// ON HIT ROCK
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="info"></param>
        void OnPlayerAttack (BasePlayer attacker, HitInfo info)
        {
            if (info.Weapon == null) {
                return;
            }

            if (info.HitEntity != null) {
                var resourceDispenser = info.HitEntity.GetComponentInParent<ResourceDispenser> ();
                if (resourceDispenser != null) {
                    Interface.CallHook ("OnHitResource", attacker, info);
                    return;
                }

                if (info.HitEntity.name.Contains ("junkpile")) {
                    Interface.CallHook ("OnHitJunk", attacker, info);
                    return;
                }
            }
            if (WoodMaterial.Contains (info.HitMaterial)) {
                Interface.CallHook ("OnHitWood", attacker, info);
            } else if (RockMaterial.Contains (info.HitMaterial)) {
                Interface.CallHook ("OnHitRock", attacker, info);
            } else if (MetalMaterial.Contains (info.HitMaterial)) {
                Interface.CallHook ("OnHitMetal", attacker, info);
            } else if (SnowMaterial.Contains (info.HitMaterial)) {
                Interface.CallHook ("OnHitSnow", attacker, info);
            } else if (GrassMaterial.Contains (info.HitMaterial)) {
                Interface.CallHook ("OnHitGrass", attacker, info);
            } else if (SandMaterial.Contains (info.HitMaterial)) {
                Interface.CallHook ("OnHitSand", attacker, info);
            }
        }

        /// <summary>
        /// ON START WETNESS
        /// ON STOP WETNESS
        /// ON START POISON
        /// ON STOP POISON
        /// ON START RADIATION
        /// ON STOP RADIATION
        /// ON START RADIATION POISON
        /// ON STOP RADIATION POISON
        /// ON START COMFORT
        /// ON STOP COMFORT
        /// ON START BLEEDING
        /// ON STOP BLEEDING
        /// </summary>
        /// <param name="metabolism"></param>
        /// <param name="source"></param>
        void OnRunPlayerMetabolism (PlayerMetabolism metabolism, BaseCombatEntity source)
        {
            if (source is BasePlayer) {
                BasePlayer player = (BasePlayer)source;
                PlayerProfile profile;
                if (onlinePlayers.TryGetValue (player, out profile)) {
                    if (profile.Metabolism == null) {
                        profile.Metabolism = new ProfileMetabolism (metabolism);
                        return;
                    }

                    Dictionary<string, ProfileMetabolism.MetaAction> changes = profile.Metabolism.DetectChange (metabolism);

                    foreach (KeyValuePair<string, ProfileMetabolism.MetaAction> kvp in changes) {
                        if (kvp.Value == ProfileMetabolism.MetaAction.Start) {
                            Interface.CallHook ("OnStart" + kvp.Key, player);
                        } else {
                            Interface.CallHook ("OnStop" + kvp.Key, player);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// ON CUPBOARD DEPLOYED
        /// ON TURRET DEPLOYED
        /// ON DOOR DEPLOYED
        /// ON SLEEPING BAG DEPLOYED
        /// ON STOCKING DEPLOYED
        /// ON BARRICADE DEPLOYED
        /// ON CONTAINER DEPLOYED
        /// ON SIGN DEPLOYED
        /// ON FURNACE DEPLOYED
        /// ON CAMPFIRE DEPLOYED
        /// ON LIGHT DEPLOYED
        /// </summary>
        /// <param name="deployer"></param>
        /// <param name="entity"></param>
        void OnItemDeployed (Deployer deployer, BaseEntity entity)
        {
            BasePlayer player = deployer.GetOwnerPlayer ();

            var type = entity.GetType ();

            string hook;
            if (TryGetHook (type, deployableTypes, out hook)) {
                Interface.CallHook (hook, player, deployer, entity);
            } else if (entity is BaseOven) {
                if (entity.name.Contains ("furnace")) {
                    Interface.Oxide.CallHook ("OnFurnaceDeployed", player, deployer, entity);
                } else if (entity.name.Contains ("campfire")) {
                    Interface.Oxide.CallHook ("OnCampfireDeployed", player, deployer, entity);
                } else if (entity is CeilingLight) {
                    Interface.Oxide.CallHook ("OnLightDeployed", player, deployer, entity);
                }
            }
        }
        /// <summary>
        /// ON ANIMAL ATTACK
        /// ON HELICOPTER ATTACK
        /// ON STRUCTURE DAMAGE
        /// ON PLAYER DAMAGE
        /// ON TURRET DAMAGE
        /// ON HELICOPTER DAMAGE
        /// ON CUPBOARD DAMAGE
        /// ON CORPSE DAMAGE
        /// ON SLEEPING BAG DAMAGE
        /// ON NPC DAMAGE
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="info"></param>
        void OnEntityTakeDamage (BaseCombatEntity entity, HitInfo info)
        {
            string hook;

            if (info.Initiator != null) {
                if (TryGetHook (info.Initiator.GetType (), attackTypes, out hook)) {
                    Interface.CallHook (hook, entity, info.Initiator, info);
                }
            }

            if (TryGetHook (entity.GetType (), damagableTypes, out hook)) {
                Interface.CallHook (hook, entity, info);
            }
        }

        /// <summary>
        /// ON ENTITY TYPE SPAWNED
        /// </summary>
        /// <param name="entity">Entity.</param>
        void OnEntitySpawned (BaseNetworkable entity)
        {
            var type = entity.GetType ();

            string hook;
            if (TryGetHook (type, spawnableTypes, out hook)) {
                Interface.CallHook (hook, entity);
            }
        }


        /// <summary>
        /// ON COOK FURNACE
        /// ON COOK FIRE
        /// ON FUEL LIGHT
        /// </summary>
        /// <param name="oven"></param>
        /// <param name="fuel"></param>
        /// <param name="burnable"></param>
        void OnConsumeFuel (BaseOven oven, Item fuel, ItemModBurnable burnable)
        {
            if (oven.name.Contains ("furnace")) {
                Interface.CallHook ("OnCookFurnace", oven, fuel, burnable);
            } else if (oven.name.Contains ("campfire")) {
                Interface.CallHook ("OnCookFire", oven, fuel, burnable);
            } else if (oven.name.Contains ("light") || oven.name.Contains ("lantern")) {
                Interface.CallHook ("OnFuelLight", oven, fuel, burnable);
            }
        }

        /// <summary>
        /// ON STRUCTURE DEATH
        /// ON PLAYER DEATH
        /// ON TURRET DEATH
        /// ON HELICOPTER DEATH
        /// ON CUPBOARD DEATH
        /// ON SLEEPING BAG DEATH
        /// ON NPC DEATH
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="info"></param>
        void OnEntityDeath (BaseCombatEntity entity, HitInfo info)
        {
            string hook;

            if (TryGetHook (entity.GetType (), killableTypes, out hook)) {
                Interface.CallHook (hook, entity, info);
            }
        }

        /// <summary>
        /// ON EQUIP
        /// </summary>
        /// <param name="container"></param>
        /// <param name="item"></param>
        void OnItemAddedToContainer (ItemContainer container, Item item)
        {
            if (!(container.playerOwner is BasePlayer)) {
                return;
            }

            if (container.playerOwner.inventory.containerWear == container) {
                Interface.CallHook ("OnEquip", container.playerOwner, item);
            } else if (container.playerOwner.inventory.containerBelt == container && item.CanBeHeld ()) {
                Interface.CallHook ("OnEquip", container.playerOwner, item);
            }
        }

        /// <summary>
        /// ON UNEQUIP
        /// </summary>
        /// <param name="container"></param>
        /// <param name="item"></param>
        void OnItemRemovedFromContainer (ItemContainer container, Item item)
        {
            if (!(container.playerOwner is BasePlayer)) {
                return;
            }

            if (container.playerOwner.inventory.containerWear == container) {
                Interface.CallHook ("OnUnequip", container.playerOwner, item);
            } else if (container.playerOwner.inventory.containerBelt == container && item.CanBeHeld ()) {
                Interface.CallHook ("OnUnequip", container.playerOwner, item);
            }
        }

        /// <summary>
        /// ON SPOT PLAYER
        /// ON SPOT NPC
        /// ON SPOT TURRET
        /// ON SPOT HELICOPTER
        /// ON SPOT RESOURCE
        /// ON USE PLAYER
        /// ON USE TERRAIN
        /// ON USE NPC
        /// ON USE BUILDING
        /// ON USE CUPBOARD
        /// ON USE SLEEPINGBAG
        /// ON USE PLANT
        /// ON USE RESOURCE
        /// </summary>
        /// <param name="player"></param>
        /// <param name="input"></param>
        void OnPlayerInput (BasePlayer player, InputState input)
        {
            if (input.IsDown (BUTTON.FIRE_SECONDARY) && !inputCooldown.Contains (player)) {
                TriggerSpotting (player, input);
            }

            if (input.WasJustPressed (BUTTON.USE)) {
                TriggerUse (player, input);
            }

            if (!player.IsFlying && !player.IsSwimming () && input.WasJustPressed (BUTTON.JUMP)) {
                Interface.Oxide.CallHook ("OnPlayerJump", player);
            }
        }

        #endregion

        #region Helpers

        void TriggerUse (BasePlayer player, InputState input)
        {
            Quaternion currentRot;
            TryGetPlayerView (player, out currentRot);
            var hitpoints = Physics.RaycastAll (new Ray (player.eyes.position, currentRot * Vector3.forward), 5f, useLayer, QueryTriggerInteraction.Collide);
            GamePhysics.Sort (hitpoints);

            object targetEntity;
            Func<object, object> func;

            for (var i = 0; i < hitpoints.Length; i++) {
                var hit = hitpoints [i];
                var target = hit.collider;
                if (target.name == "Terrain") {
                    Interface.Oxide.CallHook ("OnUseTerrain", player, target);
                    return;
                }

                if (target.name == "MeshColliderBatch") {
                    target = RaycastHitEx.GetCollider (hit);
                }

                foreach (KeyValuePair<Type, string> kvp in usableTypes) {
                    if ((targetEntity = target.GetComponentInParent (kvp.Key)) != null) {

                        if (typeCasting.TryGetValue (kvp.Key, out func)) {
                            Interface.Oxide.CallHook (kvp.Value, player, func.Invoke (targetEntity));
                        } else {
                            Interface.Oxide.CallHook (kvp.Value, player, targetEntity);
                        }

                        return;
                    }
                }
            }
        }

        void TriggerSpotting (BasePlayer player, InputState input)
        {
            Item activeItem = player.GetActiveItem ();
            if (activeItem == null) {
                return;
            }

            if (activeItem.info.category != ItemCategory.Weapon) {
                return;
            }

            inputCooldown.Add (player);

            RaycastHit hit;
            if (Physics.Raycast (player.eyes.position, Quaternion.Euler (input.current.aimAngles) * Vector3.forward, out hit, 2000, spottingMask)) {
                BaseEntity hitEntity = hit.GetEntity ();
                ResourceDispenser dispenser = hitEntity.GetComponentInParent<ResourceDispenser> ();
                if (hitEntity == null) {
                    return;
                }

                var hitEntityType = hitEntity.GetType ();

                string hook;
                if (TryGetHook (hitEntityType, spottableTypes, out hook)) {
                    SpotTarget (player, hitEntity, hitEntityType, hook);
                }
            }

            timer.Once (1, delegate () {
                inputCooldown.Remove (player);
            });
        }

        void SpotTarget (BasePlayer player, BaseEntity hitEntity, Type hitEntityType, string hook)
        {
            MonoBehaviour target = hitEntity as MonoBehaviour;
            ResourceDispenser dispenser = hitEntity.GetComponentInParent<ResourceDispenser> ();

            var distanceTo = player.Distance (hitEntity);

            List<MonoBehaviour> playerSpotCooldown;
            if (!spotCooldown.TryGetValue (player, out playerSpotCooldown)) {
                spotCooldown.Add (player, playerSpotCooldown = new List<MonoBehaviour> ());
            }

            if (!playerSpotCooldown.Contains (target)) {
                playerSpotCooldown.Add (target);

                Interface.Oxide.CallHook (hook, player, hitEntity, distanceTo);

                timer.Once (6, delegate () {
                    playerSpotCooldown.Remove (target);
                });
            }
        }

        bool TryGetPlayer (BasePlayer player, out PlayerProfile profile)
        {
            return onlinePlayers.TryGetValue (player, out profile);
        }

        bool TryGetPlayerView (BasePlayer player, out Quaternion viewAngle)
        {
            viewAngle = new Quaternion (0f, 0f, 0f, 0f);
            var input = player.serverInput;
            if (input.current == null) return false;
            viewAngle = Quaternion.Euler (input.current.aimAngles);
            return true;
        }

        bool TryGetHook (Type type, Dictionary<Type, string> types, out string hook)
        {
            hook = string.Empty;
            if (types.TryGetValue (type, out hook)) {
                if (string.IsNullOrEmpty (hook)) {
                    return false;
                }

                return true;
            }

            var found = false;
            foreach (KeyValuePair<Type, string> kvp in types) {
                if (type.IsSubclassOf (kvp.Key)) {
                    hook = kvp.Value;
                    types.Add (type, kvp.Value);
                    found = true;
                    break;
                }
            }

            if (!found) {
                types.Add (type, string.Empty);
            }

            return found;
        }

        #endregion
    }
}
