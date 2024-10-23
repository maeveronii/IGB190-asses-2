using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Easy access to all key game events.
/// </summary>
public class GameEvents
{
    /// <summary>
    /// Arg1: Casting Unit | Arg2: Ability | Arg3: Ability Target | Arg4: Ability Target Position
    /// </summary>
    public UnityEvent<Unit, Ability, Unit, Vector3> OnAbilityCastStarted = new UnityEvent<Unit, Ability, Unit, Vector3>();

    /// <summary>
    /// Arg1: Casting Unit | Arg2: Ability | Arg3: Ability Target | Arg4: Ability Target Position
    /// </summary>
    public UnityEvent<Unit, Ability, Unit, Vector3> OnAbilityCastFinished = new UnityEvent<Unit, Ability, Unit, Vector3>();

    /// <summary>
    /// Arg1: Unit With Buff | Arg2: Buff Name | Arg3: Unit Applying Buff
    /// </summary>
    public UnityEvent<Unit, string, Unit> OnUnitGainsBuff = new UnityEvent<Unit, string, Unit>();

    /// <summary>
    /// Arg1: Unit With Buff | Arg2: Buff Name | Arg3: Unit Removing Buff
    /// </summary>
    public UnityEvent<Unit, string, Unit> OnUnitLosesBuff = new UnityEvent<Unit, string, Unit>();

    /// <summary>
    /// Arg1: Unit With Buff | Arg2: Buff Name | Arg3: Unit Refreshing Buff
    /// </summary>
    public UnityEvent<Unit, string, Unit> OnUnitRefreshesBuff = new UnityEvent<Unit, string, Unit>();

    /// <summary>
    /// Arg1: Killed Unit | Arg2: Killing Unit
    /// </summary>
    public UnityEvent<Unit, Unit, IEngineHandler, bool> OnUnitKilled = new UnityEvent<Unit, Unit, IEngineHandler, bool>();

    /// <summary>
    /// Arg1: Player | Arg2: Killing Unit
    /// </summary>
    public UnityEvent<Player, Unit> OnPlayerKilled = new UnityEvent<Player, Unit>();

    /// <summary>
    /// Arg1: Player
    /// </summary>
    public UnityEvent<Player> OnPlayerExperienceGained = new UnityEvent<Player>();

    /// <summary>
    /// Arg1: Player
    /// </summary>
    public UnityEvent<Player> OnPlayerLevelUp = new UnityEvent<Player>();

    /// <summary>
    /// Arg1: Player | Arg2: Region | Arg3: Region Name
    /// </summary>
    public UnityEvent<Player, Region, string> OnPlayerEnteredRegion = new UnityEvent<Player, Region, string>();

    /// <summary>
    /// Arg1: Player | Arg2: Region | Arg3: Region Name
    /// </summary>
    public UnityEvent<Player, Region, string> OnPlayerExitedRegion = new UnityEvent<Player, Region, string>();

    /// <summary>
    /// Arg1: Damaged Unit | Arg2: Amount | Arg3: Damaging Unit | Arg4: Damage Source
    /// </summary>
    public class OnUnitDamagedInfo
    {
        public Unit damagedUnit;
        public Unit damagingUnit;
        public float damage;
        public IEngineHandler damageSource;
        public bool isCritical;

        public OnUnitDamagedInfo(Unit damagedUnit, float damage, Unit damagingUnit, IEngineHandler damageSource, bool isCritical)
        {
            this.damagedUnit = damagedUnit;
            this.damagingUnit = damagingUnit;
            this.damage = damage;
            this.damageSource = damageSource;
            this.isCritical = isCritical;
        }
    }
    //public UnityEvent<Unit, float, Unit, IEngineHandler, bool> OnUnitDamaged = new UnityEvent<Unit, float, Unit, IEngineHandler, bool>();
    public UnityEvent<OnUnitDamagedInfo> OnUnitDamaged = new UnityEvent<OnUnitDamagedInfo>();
    
    /// <summary>
    /// Arg1: Spawned Unit | Arg2: Spawning Unit
    /// </summary>
    public UnityEvent<Unit, Unit> OnUnitSpawned = new UnityEvent<Unit, Unit>();

    /// <summary>
    /// Arg1: Gold Added
    /// </summary>
    public UnityEvent<float> OnGoldAdded = new UnityEvent<float>();

    /// <summary>
    /// Arg1: Gold Removed
    /// </summary>
    public UnityEvent<float> OnGoldRemoved = new UnityEvent<float>();

    /// <summary>
    /// Arg1: Gold Picked Up
    /// </summary>
    public UnityEvent<float> OnGoldPickedUp = new UnityEvent<float>();

    /// <summary>
    /// Arg1: Amount of Health Picked Up
    /// </summary>
    public UnityEvent<float> OnHealthPickedUp = new UnityEvent<float>();

    /// <summary>
    /// Arg1: Item Equipped
    /// </summary>
    public UnityEvent<Item> OnItemEquipped = new UnityEvent<Item>();

    /// <summary>
    /// Arg1: Item Unequipped
    /// </summary>
    public UnityEvent<Item> OnItemUnequipped = new UnityEvent<Item>();

    /// <summary>
    /// Arg1: Item Picked Up
    /// </summary>
    public UnityEvent<Item> OnItemPickedUp = new UnityEvent<Item>();

    /// <summary>
    /// Arg1: Item Sold
    /// </summary>
    public UnityEvent<Item> OnItemSold = new UnityEvent<Item>();

    /// <summary>
    /// Arg1: Quest Updated
    /// </summary>
    public UnityEvent<Quest> OnQuestUpdated = new UnityEvent<Quest>();

    /// <summary>
    /// Arg1: Quest Completed
    /// </summary>
    public UnityEvent<Quest> OnQuestCompleted = new UnityEvent<Quest>();

    /// <summary>
    /// Arg1: Quest Added
    /// </summary>
    public UnityEvent<Quest> OnQuestAdded = new UnityEvent<Quest>();

    /// <summary>
    /// No Arguments.
    /// </summary>
    public UnityEvent OnGameWon = new UnityEvent();
}
