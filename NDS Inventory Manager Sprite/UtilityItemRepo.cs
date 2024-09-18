using Microsoft.Build.Framework.XamlTypes;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;
using static IngameScript.Program;

namespace IngameScript
{
    partial class Program
    {
        public class ItemRepo
        {
            // Main item list
            public SortedList<MyItemType, ItemDefinition> itemList = new SortedList<MyItemType, ItemDefinition>();
            private List<ItemDefinition> allItemsCache = new List<ItemDefinition>();
            private bool updateItemCache = false;

            // Mod Items
            // Craftable items with no blueprints
            public SortedList<MyItemType, ItemDefinition> modItems = new SortedList<MyItemType, ItemDefinition>();
            // Blueprints with no items
            public List<MyDefinitionId> modBlueprints = new List<MyDefinitionId>();

            // Display names
            public SortedList<MyItemType, string> itemDisplayNames = new SortedList<MyItemType, string>();
            public Dictionary<string, List<MyItemType>> displayNameLookupDictionary = new Dictionary<string, List<MyItemType>>();

            // Categories
            public SortedList<MyItemType, string> categories = new SortedList<MyItemType, string>();
            public Dictionary<string, List<MyItemType>> categoryLookupDictionary = new Dictionary<string, List<MyItemType>>();

            // Blueprints
            public SortedList<MyItemType, List<MyDefinitionId>> itemBlueprints = new SortedList<MyItemType, List<MyDefinitionId>>();
            public Dictionary<MyDefinitionId, SortedList<MyItemType, double>> blueprintLookupDictionary = new Dictionary<MyDefinitionId, SortedList<MyItemType, double>>();


            /**
             * To Do
             * 
             * Ore - Ingot Keys
             * Ingot/List<Ores>
             * Ore/List<Ingots>
             * 
            */

            public bool AddItemToDictionary(MyInventoryItem item)
            {
                return AddItemToDictionary(item.Type);
            }

            public bool AddItemToDictionary(MyItemType itemType)
            {
                if (itemType != default(MyItemType) && !itemList.ContainsKey(itemType))
                {
                    itemList[itemType] = new ItemDefinition(itemType);
                    if (CraftableItem(itemType)) modItems[itemType] = itemList[itemType];
                    updateItemCache = true;
                    return true;
                }

                return false;
            }

            public void AddBlueprint(MyDefinitionId blueprintDefinition)
            {
                if (blueprintLookupDictionary.ContainsKey(blueprintDefinition)) return;

                modBlueprints.Add(blueprintDefinition);
            }

            public void AddItemBlueprint(MyItemType itemType, string blueprintID)
            {
                double multiplier = 1;
                int index = blueprintID.IndexOf(":");
                if (index > -1)
                {
                    if (!double.TryParse(blueprintID.Substring(index + 1), out multiplier))
                        multiplier = 1;
                    blueprintID = blueprintID.Substring(0, index);
                }

                MyDefinitionId blueprintDefinition;
                if (!MyDefinitionId.TryParse(blueprintID, out blueprintDefinition)) return;

                AddItemBlueprint(itemType, blueprintDefinition, multiplier);
            }

            public void AddItemBlueprint(MyItemType itemType, MyDefinitionId blueprintDefinition, double multiplier)
            {
                if (!itemBlueprints.ContainsKey(itemType))
                    itemBlueprints[itemType] = new List<MyDefinitionId>();
                if (!itemBlueprints[itemType].Contains(blueprintDefinition))
                    itemBlueprints[itemType].Add(blueprintDefinition);

                if (!blueprintLookupDictionary.ContainsKey(blueprintDefinition))
                    blueprintLookupDictionary[blueprintDefinition] = new SortedList<MyItemType, double>();
                blueprintLookupDictionary[blueprintDefinition][itemType] = multiplier;

                modBlueprints.Remove(blueprintDefinition);
                modItems.Remove(itemType);
            }

            public bool CategorizeItem(MyItemType itemType, string category)
            {
                bool changed = false;

                // Remove old categorization
                if (categories.ContainsKey(itemType))
                {
                    string oldCategory = categories[itemType].ToLower();
                    if (categoryLookupDictionary.ContainsKey(oldCategory) &&
                        categoryLookupDictionary[oldCategory].Remove(itemType) &&
                        categoryLookupDictionary[oldCategory].Count == 0)
                        categoryLookupDictionary.Remove(oldCategory);
                }

                // Update item
                ItemDefinition itemDefinition = GetItemByType(itemType);
                if (itemDefinition != null)
                {
                    changed = !StringsMatch(itemDefinition.category, category);
                    itemDefinition.category = category;
                }

                // Categorize item
                categories[itemType] = category;
                category = category.ToLower();
                if (!categoryLookupDictionary.ContainsKey(category))
                    categoryLookupDictionary[category] = new List<MyItemType>();
                categoryLookupDictionary[category].Add(itemType);

                return changed;
            }

            public bool RenameItem(MyItemType itemType, string name)
            {
                bool changed = false;

                // Remove old display name
                if (itemDisplayNames.ContainsKey(itemType))
                {
                    string oldDisplayName = itemDisplayNames[itemType].ToLower();
                    if (displayNameLookupDictionary.ContainsKey(oldDisplayName) &&
                        displayNameLookupDictionary[oldDisplayName].Remove(itemType) &&
                        displayNameLookupDictionary[oldDisplayName].Count == 0)
                        displayNameLookupDictionary.Remove(oldDisplayName);
                }

                // Update item
                ItemDefinition item = GetItemByType(itemType);
                if (item != null)
                {
                    changed = !StringsMatch(item.displayName, name);
                    item.displayName = name;
                }

                // Name item
                itemDisplayNames[itemType] = name;
                name = RemoveSpaces(name, true);
                if (!categoryLookupDictionary.ContainsKey(name))
                    categoryLookupDictionary[name] = new List<MyItemType>();
                categoryLookupDictionary[name].Add(itemType);

                return changed;
            }

            public void AddItemCount(MyInventoryItem item)
            {
                ItemDefinition itemDefinition = GetItemByType(item.Type);
                if (itemDefinition != null) itemDefinition.AddAmount(item.Amount);
            }

            public void SetCounts(double maxMultiplier, bool useMultiplier, double negativeThreshold, double positiveThreshold, double multiplierChange, bool increaseWhenLow)
            {
                foreach (var kvp in itemList)
                    kvp.Value.SwitchCount(maxMultiplier, useMultiplier, negativeThreshold, positiveThreshold, multiplierChange, increaseWhenLow);
            }


            private bool CraftableItem(MyItemType itemType)
            {
                MyItemInfo info = itemType.GetItemInfo();
                return !info.IsOre && !info.IsIngot;
            }

            public ItemDefinition GetItemByType(MyItemType itemType)
            {
                if (itemList.ContainsKey(itemType))
                    return itemList[itemType];

                return null;
            }

            public void GetItemsByDisplay(string category, string name, List<ItemDefinition> itemDefinitions)
            {
                itemDefinitions.AddRange(itemList
                                        .Where(item =>
                                            MatchCategory(category, item.Value.TypeID, item.Value.category) &&
                                            MatchName(name, item.Value.SubtypeID, item.Value.displayName))
                                        .Select(item => item.Value));
            }

            public void GetItemsBySearchObject(List<ItemSearchObject> searchObjects, List<ItemDefinition> itemDefinitions)
            {
                itemDefinitions.AddRange(itemList
                                        .Where(item => FilterItem(searchObjects, item.Value))
                                        .Select(item => item.Value));
            }

            private bool FilterItem(List<ItemSearchObject> searchObjects, ItemDefinition itemDefinition)
            {
                foreach (ItemSearchObject itemSearchObject in searchObjects)
                    if ((IsWildCard(itemSearchObject.Category) || StringsMatch(itemSearchObject.Category, itemDefinition.category) &&
                        MatchName(itemSearchObject.DisplayName, itemDefinition.SubtypeID, itemDefinition.displayName)))
                        return true;

                return false;
            }

            private bool MatchCategory(string value, string typeID, string category)
            {
                if (IsWildCard(value)) return true;

                return StringsMatch(value, category) || StringsMatch(value, typeID);
            }

            private bool MatchName(string value, string subtypeID, string name)
            {
                if (value == "*") return true;

                if (value.StartsWith("'") && value.EndsWith("'"))
                    return StringsMatch(value.Substring(1, value.Length - 2), name);

                return StringsMatch(value, name) || StringsMatch(value, subtypeID);
            }
        }

        public struct ItemSearchObject : IComparable<ItemSearchObject>, IEquatable<ItemSearchObject>
        {
            public string Category, DisplayName;

            public string ID => $"{Category}/{DisplayName}";

            public bool DefinitionMatch;

            public ItemSearchObject(string category, string displayName, bool definitionMatch = false)
            {
                Category = category;
                DisplayName = displayName;
                DefinitionMatch = definitionMatch;
            }

            public ItemSearchObject(MyInventoryItem item) : this(item.Type) { }

            public ItemSearchObject(MyItemType itemType)
            {
                Category = itemType.TypeId;
                DisplayName = itemType.SubtypeId;
                DefinitionMatch = true;
            }

            int IComparable<ItemSearchObject>.CompareTo(ItemSearchObject other)
            {
                return String.Compare(ID, other.ID, true);
            }

            bool IEquatable<ItemSearchObject>.Equals(ItemSearchObject other)
            {
                return Equals(other);
            }

            public override int GetHashCode()
            {
                return ID.ToLower().GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (!(obj is ItemSearchObject)) return false;

                return Equals((ItemSearchObject)obj);
            }

            public bool Equals(ItemSearchObject other)
            {
                return ID.Equals(other.ID, StringComparison.OrdinalIgnoreCase);
            }
        }

        public class ItemCollection
        {
            public static Program parent;

            public SortedList<ItemSearchObject, VariableItemCount> ItemList = new SortedList<ItemSearchObject, VariableItemCount>();

            public int Count => ItemList.Count;

            private bool TrackAmounts;

            public bool IsEmpty => Count == 0;

            public ItemCollection(bool trackAmounts)
            {
                TrackAmounts = trackAmounts;
            }

            public void AddItem(string category, string name, VariableItemCount count, bool definitionMatch, bool append = true)
            {
                AddItem(new ItemSearchObject(category, name, definitionMatch), count, append);
            }

            public void AddItem(ItemSearchObject itemSearchObject, VariableItemCount count, bool append = true)
            {
                if (!ItemList.ContainsKey(itemSearchObject))
                    ItemList[itemSearchObject] = count;
                else if (TrackAmounts)
                {
                    if (append)
                        ItemList[itemSearchObject].Add(count);
                    else
                        ItemList[itemSearchObject] = count;
                }
            }

            public void AddItem(MyItemType itemType, VariableItemCount count, bool append = true)
            {
                AddItem(new ItemSearchObject(itemType.TypeId, itemType.SubtypeId, true), count, append);
            }

            public void AddItem(MyInventoryItem item, bool negativeCounting = false)
            {
                AddItem(item, new VariableItemCount((negativeCounting ? -1.0 : 1.0) * (double)item.Amount));
            }

            public void AddItem(MyInventoryItem item, VariableItemCount count)
            {
                AddItem(new ItemSearchObject(item.Type.TypeId, item.Type.SubtypeId, true), count);
            }

            public bool ItemCount(MyInventoryItem item, IMyTerminalBlock block, out double count)
            {
                ItemSearchObject itemSearchObject = new ItemSearchObject(item);
                if (ItemList.ContainsKey(itemSearchObject))
                {
                    count = parent.TranslateItemCount(item.Type, ItemList[itemSearchObject], block);
                    count = ItemList[itemSearchObject].Count;
                    return true;
                }

                count = 0;
                return false;
            }

            public double ItemCount(MyInventoryItem item, IMyTerminalBlock block = null)
            {
                double count;
                return ItemCount(item, block, out count) ? count : 0;
            }

            public void AddCollection(ItemCollection other)
            {
                foreach (KeyValuePair<ItemSearchObject, VariableItemCount> kvp in other.ItemList)
                    AddItem(kvp.Key, kvp.Value);
            }

            public void Clear(bool manual = true, bool automatic = true, bool zeroValues = false)
            {
                if (manual && automatic) ItemList.Clear();
                else for (int i = 0; i < Count; i += 0)
                {
                    if ((manual && ItemList.Values[i].Manual) ||
                        (automatic && !ItemList.Values[i].Manual) ||
                        (zeroValues && ItemList.Values[i].Count < 0.0))
                        ItemList.RemoveAt(i);
                    else i++;
                }
            }
        }

        public class ItemDefinition
        {
            public MyItemType ItemType;
            public MyItemInfo ItemInfo;

            public MyDefinitionId BlueprintDefinition = default(MyDefinitionId);

            public string TypeID => ItemType.TypeId;
            public string SubtypeID => ItemType.SubtypeId;

            public bool IsIngot => ItemInfo.IsIngot;
            public bool IsOre => ItemInfo.IsOre;
            public bool IsTool => ItemInfo.IsTool;
            public bool IsComponent => ItemInfo.IsComponent;
            public bool IsAmmo => ItemInfo.IsAmmo;

            public Blueprint Blueprint => new Blueprint(BlueprintDefinition.SubtypeName, TypeID, SubtypeID);

            public double Amount => (double)amount;

            private MyFixedPoint amount;




            public string blueprintID = "", displayName = "", category = "";

            public double queuedAssemblyAmount = 0, queuedDisassemblyAmount = 0,
                          countAmount = 0, countAssemblyAmount = 0, countDisassemblyAmount = 0,
                          quota = 0, amountDifference = 0, blockQuota = 0, quotaMultiplier = 1,
                          assemblyMultiplier = 1, dynamicQuotaCounter = 0,
                          quotaMax = 0, differenceNeeded = 0, currentQuota = 0,
                          currentAssemblyAmount = 0, currentDisassemblyAmount = 0,
                          currentExcessAssembly = 0, currentExcessDisassembly = 0,
                          currentMax = 0, currentNeededAssembly = 0,
                          postAssemblyAmount = 0;

            public bool fuel = false, assemble = true,
                disassemble = true, refine = true, display = true,
                disassembleAll = false, gas = false;

            public List<string> oreKeys = NewStringList;

            public List<ItemCountRecord> countRecordList = new List<ItemCountRecord>();

            public DateTime countRecordTime = Now, dynamicQuotaTime = Now;

            public string FullID { get { return $"{TypeID}/{SubtypeID}"; } }

            public double Percentage { get { return currentQuota == 0 ? double.MaxValue : (Amount / currentQuota) * 100.0; } }

            public ItemDefinition(MyItemType itemType)
            {
                ItemType = itemType;
                ItemInfo = ItemType.GetItemInfo();
                amount = MyFixedPoint.Zero;
            }

            public ItemDefinition(MyInventoryItem item) : this(item.Type)
            {
                amount = item.Amount;
            }

            public void FinalizeKeys()
            {
                if (oreKeys.Count > 0)
                    oreKeys = oreKeys.Distinct().ToList();
            }

            public void SwitchCount(double maxMultiplier, bool useMultiplier, double negativeThreshold, double positiveThreshold, double multiplierChange, bool increaseWhenLow)
            {
                Amount = countAmount;
                countAmount = 0;
                if (Now >= countRecordTime)
                {
                    countRecordList.Add(new ItemCountRecord { count = Amount, countTime = Now, disassembling = queuedDisassemblyAmount });
                    countRecordTime = Now.AddSeconds(1.25);
                    if (countRecordList.Count > 12)
                        countRecordList.RemoveRange(0, countRecordList.Count - 12);
                }
                double averageSpanSeconds = Math.Min(20, (Now - countRecordList[0].countTime).TotalSeconds + 0.0001),
                    lastSeconds = (Now - dynamicQuotaTime).TotalSeconds, tempDifference,
                    tempQuota = 0, disassembleCount = Math.Max(countRecordList[0].disassembling, queuedDisassemblyAmount);
                if (countRecordList.Count > 1 && averageSpanSeconds == 20)
                    countRecordList.RemoveAt(0);

                bool allOut = Amount < 1.0;

                amountDifference = averageSpanSeconds > 0 ? (Amount - countRecordList[0].count) / averageSpanSeconds : 0;

                tempDifference = amountDifference;

                tempQuota += (quota > 0 ? quota : 0) + (blockQuota > 0 ? blockQuota : 0);

                if (useMultiplier && tempQuota > 0)
                {
                    if (lastSeconds >= 1)
                    {
                        if (increaseWhenLow && allOut)
                            tempDifference -= tempQuota * 0.001;

                        if (!allOut && tempDifference == 0)
                            tempDifference += tempQuota * 0.1;

                        if (disassembleCount >= 1)
                            tempDifference = tempQuota * 0.005;

                        if (Amount >= tempQuota)
                            tempDifference = tempQuota * 0.15;

                        dynamicQuotaCounter += ((tempDifference / tempQuota) * averageSpanSeconds) * Math.Min(2.5, lastSeconds);
                        if (dynamicQuotaCounter <= -negativeThreshold)
                        {
                            quotaMultiplier += multiplierChange;
                            if (quotaMultiplier > maxMultiplier)
                                quotaMultiplier = maxMultiplier;

                            dynamicQuotaCounter = 0;
                        }
                        else if (dynamicQuotaCounter >= positiveThreshold)
                        {
                            quotaMultiplier -= multiplierChange;
                            if (quotaMultiplier < 1)
                                quotaMultiplier = 1;

                            dynamicQuotaCounter = 0;
                        }
                        dynamicQuotaTime = Now;
                    }
                }
                else
                {
                    quotaMultiplier = 1;
                    dynamicQuotaTime = Now;
                    dynamicQuotaCounter = positiveThreshold * 0.95;
                }
                SetCurrentQuota();
            }

            public void SwitchAssemblyCount()
            {
                queuedAssemblyAmount = Math.Floor(countAssemblyAmount);
                queuedDisassemblyAmount = Math.Floor(countDisassemblyAmount);
                countAssemblyAmount = countDisassemblyAmount = 0;
            }

            public void AddAmount(MyFixedPoint amount)
            {
                countAmount += (double)amount;
            }

            public void AddAssemblyAmount(MyFixedPoint amount, bool current)
            {
                if (current)
                    queuedAssemblyAmount += (double)amount;
                else
                    countAssemblyAmount += (double)amount;
            }

            public void AddDisassemblyAmount(MyFixedPoint amount, bool current)
            {
                if (current)
                    queuedDisassemblyAmount += (double)amount;
                else
                    countDisassemblyAmount += (double)amount;
            }

            public void SetDifferenceNeeded(double excessAmount)
            {
                //Set Variables
                currentNeededAssembly = differenceNeeded = currentExcessAssembly = currentExcessDisassembly = 0;
                currentAssemblyAmount = queuedAssemblyAmount * assemblyMultiplier;
                currentDisassemblyAmount = queuedDisassemblyAmount;
                postAssemblyAmount = Amount + (currentAssemblyAmount) - (currentDisassemblyAmount);
                SetCurrentQuota();
                if (TextHasLength(blueprintID) && blueprintID != nothingType)
                {
                    currentMax =
                        currentQuota < 0 ? 0 :
                        quotaMax > currentQuota ? quotaMax :
                        currentQuota > 0 ? Math.Floor(currentQuota * (1.0 + excessAmount)) : double.MaxValue;

                    if (!disassembleAll)
                    {
                        if (currentAssemblyAmount > 0 && Amount + currentAssemblyAmount > currentMax)
                            currentExcessAssembly = (Amount + currentAssemblyAmount) - currentMax;

                        if (currentDisassemblyAmount > 0 && Amount - currentDisassemblyAmount < currentQuota)
                            currentExcessDisassembly = currentQuota - (Amount - currentDisassemblyAmount);
                    }
                    else
                    {
                        currentExcessAssembly = queuedAssemblyAmount;
                        if (currentDisassemblyAmount > 0 && currentDisassemblyAmount > Amount)
                            currentExcessDisassembly = currentDisassemblyAmount - Amount;
                    }
                    if (currentExcessAssembly > 0)
                        currentExcessAssembly = Math.Floor(currentExcessAssembly / assemblyMultiplier);

                    if (currentDisassemblyAmount > 0 && currentDisassemblyAmount < assemblyMultiplier)
                        currentExcessDisassembly = currentDisassemblyAmount;

                    if (!assemble)
                        currentExcessAssembly = 0;

                    if (!disassemble)
                        currentExcessDisassembly = 0;
                    //Logic
                    if (disassembleAll)
                    {
                        if (postAssemblyAmount > 0)
                            differenceNeeded = -postAssemblyAmount;
                    }
                    else
                    {
                        if (currentQuota == 0)
                            differenceNeeded = 0;
                        else
                        {
                            if (postAssemblyAmount < currentQuota)
                                differenceNeeded = currentQuota - postAssemblyAmount;
                            else if (postAssemblyAmount > currentMax)
                                differenceNeeded = currentMax - postAssemblyAmount;
                        }
                    }

                    if (differenceNeeded > 0)
                        differenceNeeded = Math.Ceiling(differenceNeeded / assemblyMultiplier);
                    else if (differenceNeeded < 0)
                        differenceNeeded = Math.Abs(differenceNeeded) < assemblyMultiplier ? 0 : Math.Floor(Math.Abs(differenceNeeded) / assemblyMultiplier) * -assemblyMultiplier;


                    if (differenceNeeded > 0)
                        currentNeededAssembly = differenceNeeded;

                    if (differenceNeeded > 0 && (currentDisassemblyAmount > 0 || !assemble))
                        differenceNeeded = 0;

                    if (differenceNeeded < 0 && (currentAssemblyAmount > 0 || !disassemble))
                        differenceNeeded = 0;
                }
            }

            public void SetCurrentQuota()
            {
                currentQuota =
                    (quota > 0 ? quota : 0) +
                    (blockQuota > 0 ? blockQuota : 0);

                disassembleAll = currentQuota == 0 && quota < 0;
                if (!disassembleAll && currentQuota > 0)
                    currentQuota *= quotaMultiplier;

                currentQuota = Math.Floor(currentQuota);
            }

            public override string ToString()
            {
                return $"Name={displayName}||Category={Formatted(category)}||Quota={quota}{(quota >= 0 && quotaMax > quota ? $"<{quotaMax}" : "")}{newLine}" +
                       $"^Type={TypeID}||Subtype={SubtypeID}" +
                       $"{(IsBlueprint(blueprintID) ? $"||Blueprint={blueprintID}||Assembly Multiplier={assemblyMultiplier}||Assemble={assemble}||Disassemble={disassemble}" : StringsMatch(blueprintID, "none") ? "||Blueprint=None" : "")}" +
                       $"{(IsOre(TypeID) ? $"||Refine={refine}" : "")}" +
                       $"||Fuel={fuel}||Display={display}" +
                       $"{(gas || IsIce(TypeID, SubtypeID) ? $"||Gas={gas}" : "")}" +
                       $"{(IsIngot(TypeID) || oreKeys.Count > 0 ? $"||Ore Keys=[{String.Join("|", oreKeys)}]" : "")}{newLine}";
            }
        }

    }
}
