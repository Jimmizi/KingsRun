using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventData
{
    /* EVENT TYPES :
     * damage
     * delay
     * loadroom
     * conversation
     * choice
     * inventory
     */
    
    /// <summary>
    /// What type of event is this, what will it do?
    /// </summary>
    public string Type = "damage|delay|loadroom|conversation|choice|inventory|event";

    #region Damage Event

    /// <summary>
    /// The index of the party member to damage for a damage event type. If left as -1, will damage a random member.
    /// </summary>
    public int DamagePartyMemberIndex = -1;

    /// <summary>
    /// Amount to default the party member for.
    /// </summary>
    public int DamageAmount = 0;

    #endregion

    #region Delay Event

    /// <summary>
    /// Length of time this event will delay whatever event is schedule next after this
    /// </summary>
    public float DelayTime = 1.0f;

    #endregion

    #region Load Room Event

    /// <summary>
    /// Name of the unity scene file we will additively load
    /// </summary>
    public string SceneName = "nameOfSceneToLoad";

    #endregion

    #region Conversation Event

    /// <summary>
    /// The conversation file to open next
    /// </summary>
    public string OpenConversationFile = "<filename> (no .json at end)";

    #endregion

    #region Choice Event

    /// <summary>
    /// The choice file to open next
    /// </summary>
    public string OpenChoiceFile = "<filename> (no .json at end)";

    #endregion

    #region Inventory Event

    /// <summary>
    /// The type of action happening to the party inventory
    /// </summary>
    public string InventoryAction = "add|remove";

    /// <summary>
    /// The name of the item being modified
    /// </summary>
    public string InventoryItemName = "";

    #endregion

    #region Event Event

    /// <summary>
    /// List of events to fire. Only the first choice or conversation event is accepted if there are duplicates.
    /// </summary>
    public List<string> EventsToFire = new List<string>();

    #endregion
}
