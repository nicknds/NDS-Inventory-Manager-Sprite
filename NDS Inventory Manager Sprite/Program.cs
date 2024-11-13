using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        #region mdk preserve
        double OverheatAverage = 0.085, ActionLimiterMultiplier = 0.05, RunTimeLimiter = 0.075;
        int EchoDelay = 6, IdleTicks = 0;
        bool UseVanillaLibrary = true;
        #endregion


        #region Variables

        #region Short Links

        const string trueString = "true", falseString = "false";
        const bool shortTrue = true, shortFalse = false;

        IMyGridTerminalSystem gtSystem;

        const MyAssemblerMode assemblyMode = MyAssemblerMode.Assembly, disassemblyMode = MyAssemblerMode.Disassembly;

        const FunctionState stateActive = FunctionState.Active, stateContinue = FunctionState.Continue, stateUninitialized = FunctionState.Uninitialized, stateError = FunctionState.Error;

        const TextAlignment leftAlignment = TextAlignment.LEFT, centerAlignment = TextAlignment.CENTER;
        bool PauseTickRun => UnavailableActions();
        bool IsStateRunning => StateRunning(selfContainedIdentifier);
        bool RunStateManager => StateManager(selfContainedIdentifier);

        double overheatAverage { get { return OverheatAverage; } set { OverheatAverage = value; } }
        double actionLimiterMultiplier { get { return ActionLimiterMultiplier; } set { ActionLimiterMultiplier = value; } }
        double runTimeLimiter { get { return RunTimeLimiter; } set { RunTimeLimiter = value; } }

        int echoDelay => EchoDelay;

        static List<long> NewLongList => new List<long>();
        static LongListPlus NewLongListPlus => new LongListPlus();
        static List<MyInventoryItem> NewItemList => new List<MyInventoryItem>();
        static List<ItemDefinition> NewItemDefinitionList => new List<ItemDefinition>();
        static List<string> NewStringList => new List<string>();
        static List<MyProductionItem> NewProductionList => new List<MyProductionItem>();
        static ItemCollection NewCollection => new ItemCollection();
        static StringBuilder NewBuilder => new StringBuilder();
        static SortedList<long, double> NewSortedListLongDouble => new SortedList<long, double>();
        static HashSet<long> NewHashSetLong => new HashSet<long>();
        static HashSet<string> NewHashSetString => new HashSet<string>();
        static DateTime Now => DateTime.Now;
        List<ItemDefinition> GetAllItems
        {
            get
            {
                if (itemListAllItems.Count > 0) return itemListAllItems;
                itemListMain.Values.ToList().ForEach(b => itemListAllItems.AddRange(b.Values));
                return itemListAllItems;
            }
        }

        #endregion

        #region Script Settings

        SortedList<string, SortedList<string, string>> settingDictionaryStrings = new SortedList<string, SortedList<string, string>>
        {
            { "1/2 Global Tags", new SortedList<string, string>
                {
                    { setKeyExclusion, "exclude" }, { setKeyIncludeGrid, "includeGrid" }, { setKeyCrossGrid, "crossGrid" },
                    { setKeyPanel, "[nds]" }, { setKeyGlobalFilter, "" }, { setKeyExcludeGrid, "excludeGrid" }, { setKeyOptionBlockFilter, "" }
                }
            },
            { "2/2 Default Categories", new SortedList<string, string>
                {
                    { setKeyIngot, "ingot" }, { setKeyOre, "ore" }, { setKeyComponent, "component" },
                    { setKeyTool, "tool" }, { setKeyAmmo, "ammo" }
                }
            }
        };

        SortedList<string, SortedList<string, double>> settingDictionaryDoubles = new SortedList<string, SortedList<string, double>>
        {
            { "1/4 Delays", new SortedList<string, double>
                {
                    { setKeyDelayScan, 10 }, { setKeyDelayProcessLimits, 20 }, { setKeyDelaySorting, 7.5 },
                    { setKeyDelayDistribution, 20 }, { setKeyDelaySpreading, 15 }, { setKeyDelayQueueAssembly, 5 },
                    { setKeyDelayQueueDisassembly, 10 }, { setKeyDelayRemoveExcessAssembly, 20 }, { setKeyDelayRemoveExcessDisassembly, 20 },
                    { setKeyDelaySortBlueprints, 12.5 }, { setKeyDelaySortCargoPriority, 90 }, { setKeyDelaySpreadBlueprints, 20 },
                    { setKeyDelayLoadouts, 15}, { setKeyDelayFillingBottles, 30 }, { setKeyDelayLogic, 10 },
                    { setKeyDelayIdleAssemblerCheck, 15 }, { setKeyDelayResetIdleAssembler, 45 }, { setKeyDelayFindModItems, 5 },
                    { setKeyDelaySortRefinery, 6 }, { setKeyDelayOrderCargo, 15 }
                }
            },
            { "2/4 Performance", new SortedList<string, double>
                {
                    { setKeyActionLimiterMultiplier, 0.35 }, { setKeyRunTimeLimiter, 0.45 },
                    { setKeyOverheatAverage, 0.6 }
                }
            },
            { "3/4 Defaults", new SortedList<string, double>
                {
                    { setKeyIcePerGenerator, 5000 }, { setKeyFuelPerReactor, 25 }, { setKeyAmmoPerGun, 40 },
                    { setKeyCanvasPerParachute, 4 }
                }
            },
            { "4/4 Adjustments", new SortedList<string, double>
                {
                    { setKeyBalanceRange, 0.05 }, { setKeyAllowedExcessPercent, 0.1 }, { setKeyDynamicQuotaPercentageIncrement, 0.05 },
                    { setKeyDynamicuotaMaxMultiplier, 2.5 }, { setKeyDynamicQuotaNegativeThreshold, 3 }, { setKeyDynamicQuotaPositiveThreshold, 9 },
                    { setKeyOreMinimum, 0.5 }
                }
            }
        };

        SortedList<string, SortedList<string, bool>> settingDictionaryBools = new SortedList<string, SortedList<string, bool>>
        {
            { "1/3 Basic", new SortedList<string, bool>
                {
                    { setKeyToggleCountItems, true }, { setKeyToggleCountBlueprints, true }, { setKeyToggleSortItems, true },
                    { setKeyToggleQueueAssembly, true}, { setKeyToggleQueueDisassembly, true }, { setKeyToggleDistribution, true },
                    { setKeyToggleAutoLoadSettings, true }
                }
            },
            { "2/3 Advanced", new SortedList<string, bool>
                {
                    { setKeyToggleProcessLimits, true }, { setKeyToggleSpreadRefieries, true },
                    { setKeyToggleSpreadReactors, true }, { setKeyToggleSpreadGuns, true }, { setKeyToggleSpreadGasGenerators, true },
                    { setKeyToggleSpreadGravelSifters, true }, { setKeyToggleSpreadParachutes, true }, { setKeyToggleRemoveExcessAssembly, true },
                    { setKeyToggleRemoveExcessDisassembly, true }, { setKeyToggleSortBlueprints, true }, { setKeyToggleSortCargoPriority, true},
                    { setKeyToggleSpreadBlueprints, true }, { setKeyToggleDoLoadouts, true }, { setKeyToggleLogic, true },
                    { setKeyToggleResetIdleAssemblers, true }, { setKeyToggleFindModItems, true }, { setKeyToggleToggleSortRefineries, true},
                    { setKeyToggleOrderCargo, true }
                }
            },
            { "3/3 Settings", new SortedList<string, bool>
                {
                    { setKeyAutoConveyorRefineries, false }, { setKeyAutoConveyorReactors, false }, { setKeyAutoConveyorGasGenerators, false },
                    { setKeyAutoConveyorGuns, false }, { setKeyToggleDynamicQuota, true }, { setKeyDynamicQuotaIncreaseWhenLow, true },
                    { setKeySameGridOnly, false }, { setKeySurvivalKitAssembly, false }, { setKeyAddLoadoutsToQuota, true },
                    { setKeyControlConveyors, true }, { setKeyAutoTagBlocks, true }, { setKeyUseAcceptanceFilter, true }
                }
            }
        };

        SortedList<string, int> settingsInts = new SortedList<string, int>()
        {
            { setKeyUpdateFrequency, 1 }, { setKeyOutputLimit, 15 },
            { setKeySurvivalKitQueuedIngots, 0 }, { setKeyAutoMergeLengthTolerance, 6 },
            { setKeyPrioritizedOreCount, 0 }
        };

        SortedList<string, List<string>> settingsListsStrings = new SortedList<string, List<string>>()
        {
            {
                setKeyExcludedDefinitions, new List<string>()
                {
                    "LargeBlockBed", "LargeBlockLockerRoom", "LargeBlockLockerRoomCorner", "LargeBlockLockers", "PassengerSeatSmall", "PassengerSeatLarge", "LargeInteriorTurret"
                }
            },
            {
                setKeyGravelSifterKeys,
                new List<string>()
                {
                    "gravelrefinery", "gravelseparator", "gravelsifter"
                }
            },
            {
                setKeyDefaultSuffixes,
                new List<string>()
                {
                "K", "M", "B", "T"
                }
            }
        };

        SortedList<string, SortedList<string, ItemDefinition>> itemListMain = new SortedList<string, SortedList<string, ItemDefinition>>();

        #endregion

        #region Lists

        SortedList<string, LongListPlus> indexesStorageLists = new SortedList<string, LongListPlus>();

        SortedList<string, List<PotentialAssembler>> potentialAssemblerList = new SortedList<string, List<PotentialAssembler>>();

        SortedList<string, string>
            modItemDictionary = new SortedList<string, string>(),
            oreKeyedItemDictionary = new SortedList<string, string>();

        SortedList<string, LongListPlus> typedIndexes = new SortedList<string, LongListPlus>
        {
            { setKeyIndexAssemblers, NewLongListPlus },
            { setKeyIndexGasGenerators, NewLongListPlus },
            { setKeyIndexGravelSifters, NewLongListPlus },
            { setKeyIndexGun, NewLongListPlus },
            { setKeyIndexHydrogenTank, NewLongListPlus },
            { setKeyIndexOxygenTank, NewLongListPlus },
            { setKeyIndexParachute, NewLongListPlus },
            { setKeyIndexReactor, NewLongListPlus },
            { setKeyIndexRefinery, NewLongListPlus },
            { setKeyIndexStorage, NewLongListPlus },
            { setKeyIndexSortable, NewLongListPlus },
            { setKeyIndexLoadout, NewLongListPlus },
            { setKeyIndexLogic, NewLongListPlus },
            { setKeyIndexPanel, NewLongListPlus},
            { setKeyIndexInventory, NewLongListPlus },
            { setKeyIndexLimit, NewLongListPlus }
        };

        SortedList<FunctionIdentifier, TimeSpan> delaySpans = new SortedList<FunctionIdentifier, TimeSpan>();

        SortedList<FunctionIdentifier, StateRecord> stateRecords = new SortedList<FunctionIdentifier, StateRecord>();

        SortedList<long, double> tempDistributeItemIndexes;

        List<MyItemType> acceptedItemList = new List<MyItemType>();

        Dictionary<long, BlockDefinition> managedBlocks = new Dictionary<long, BlockDefinition>(1500);

        Dictionary<string, Blueprint>
            blueprintList = new Dictionary<string, Blueprint>();

        Dictionary<string, string>
            gunAmmoDictionary = new Dictionary<string, string>(),
            itemCategoryDictionary = new Dictionary<string, string>();

        Dictionary<string, HashSet<string>> itemAcceptanceDictionary = new Dictionary<string, HashSet<string>>();

        HashSet<string> antiflickerSet = NewHashSetString,
                        priorityCategories = NewHashSetString,
                        priorityTypes = NewHashSetString,
                        assemblyNeededByMachine = NewHashSetString,
                        clearedSettingLists = NewHashSetString;

        HashSet<long> uniqueIndexSet = NewHashSetLong,
                      clonedEntityIDs = NewHashSetLong,
                      excludedIDs = NewHashSetLong,
                      accessibleIDs = NewHashSetLong,
                      includedIDs = NewHashSetLong,
                      setRemoveIDs = NewHashSetLong;

        List<char> spacerChars = new List<char> { '.', '-', '`', '-' };

        HashSet<IMyCubeGrid> gridList = new HashSet<IMyCubeGrid>(), excludedGridList = new HashSet<IMyCubeGrid>();

        List<LogicComparison> tempLogicComparisons;

        List<OutputObject> outputList = new List<OutputObject>(), outputErrorList = new List<OutputObject>();

        List<Blueprint> tempAddAssemblyNeededList;

        List<string>
            fullExclude = new List<string> { setKeySameGridOnly, setKeySurvivalKitAssembly },
            itemCategoryList = NewStringList, modBlueprintList = NewStringList,
            tagTempStringList = NewStringList,
            processBlockStorageList = NewStringList, processBlockOptionList = NewStringList;

        List<long>
            tempBalanceItemIndexes, tempOrderByPriorityIndexes, tempDistributeBlueprintIndexes,
            tempSortCargoListIndexes, tempCountItemsInListIndexes;

        List<SortableObject> sortableListMain = new List<SortableObject>(),
                             sortableListAlternate = new List<SortableObject>(),
                             tempOrderInventoryList;

        List<IMyBlockGroup> groupList = new List<IMyBlockGroup>();

        List<IMyTerminalBlock> groupBlocks = new List<IMyTerminalBlock>(), scannedBlocks = new List<IMyTerminalBlock>(1500);

        List<ItemDefinition> itemListAllItems = NewItemDefinitionList;

        List<MyInventoryItem> countByListA = NewItemList, amountContainedListA = NewItemList, mainFunctionItemList = NewItemList, tempStorageItemList;

        List<MyProductionItem> blueprintListMain = NewProductionList;

        #endregion

        #region Constants


        enum EchoMode
        {
            Main,
            MergeMenu,
            MergeHelp,
        };

        public enum DisplayType
        {
            Detailed,
            CompactAmount,
            Standard,
            CompactPercent
        };

        public enum PanelItemSorting
        {
            Alphabetical,
            AscendingAmount,
            DescendingAmount,
            AscendingPercent,
            DescendingPercent
        };

        public enum PanelType
        {
            Item,
            Cargo,
            Output,
            Status,
            Span,
            None
        };

        public enum FunctionState
        {
            Active,
            Continue,
            Uninitialized,
            Error
        };

        enum FunctionIdentifier
        {
            #region mdk preserve
            Script, Main_Control, Main_Output, Main_Sprites, Processing_Block_Options, Status_Panel,
            Measuring_Capacities, Output_Panel, Counting_Listed_Items, Distribution, Distributing_Item, Counting_Item_In_Inventory,
            Processing_Limits, Sorting, Storing_Item, Counting_Blueprints, Counting_Items, Scanning,
            Generating_Sprites, Item_Panel, Order_Inventory, Processing_Tags, Transferring_Item,
            Spreading_Items, Cargo_Panel, Distributing_Blueprint, Removing_Excess_Assembly, Generating_Block_Options, Setting_Block_Quotas,
            Save, Queue_Assembly, Queue_Disassembly, Inserting_Blueprint, Process_Panel_Options, Removing_Blueprint,
            Removing_Excess_Disassembly, Order_Blocks_By_Priority, Cargo_Priority_Loop, Sorting_Cargo_Priority, Sort_Blueprints, Spread_Blueprints,
            Load, Loadouts, Sort_Refineries, Custom_Logic, Matching_Items, Process_Logic,
            Checking_Idle_Assemblers, Find_Mod_Items, Process_Setting, Main_Panel, Assembly_Reserve, Processing_Item_Setting,
            Order_Storage, Idle
            #endregion
        };

        IEqualityComparer<string> stringComparer = StringComparer.OrdinalIgnoreCase;

        #region Constant Strings

        const string
            componentType = "MyObjectBuilder_Component",
            oreType = "MyObjectBuilder_Ore",
            ingotType = "MyObjectBuilder_Ingot",
            toolType = "MyObjectBuilder_PhysicalGunObject",
            hydBottleType = "MyObjectBuilder_GasContainerObject",
            oxyBottleType = "MyObjectBuilder_OxygenContainerObject",
            ammoType = "MyObjectBuilder_AmmoMagazine",
            dataPadType = "MyObjectBuilder_Datapad",
            consumableType = "MyObjectBuilder_ConsumableItem",
            physicalObjectType = "MyObjectBuilder_PhysicalObject",
            nothingType = "None",
            stoneType = "Stone",
            canvasType = "Canvas",
            blueprintPrefix = "MyObjectBuilder_BlueprintDefinition",
            setKeyExclusion = "exclusionKeyword", //modifier tags
            setKeyIncludeGrid = "includeGridKeyword",
            setKeyExcludeGrid = "excludeGridKeyword",
            setKeyCrossGrid = "crossGridControlKeyword",
            setKeyGlobalFilter = "globalFilterKeyword",
            setKeyOptionBlockFilter = "optionHeader",
            setKeyIngot = "itemIngotKeyword", //control keys
            setKeyOre = "itemOreKeyword",
            setKeyComponent = "itemComponentKeyword",
            setKeyTool = "itemToolKeyword",
            setKeyAmmo = "itemAmmoKeyword",
            setKeyPanel = "panelKeyword",
            setKeyDelayScan = "delayScan", //delays
            setKeyDelayProcessLimits = "delayProcessLimits",
            setKeyDelaySorting = "delaySortItems",
            setKeyDelayDistribution = "delayDistributeItems",
            setKeyDelaySpreading = "delaySpreadItems",
            setKeyDelayQueueAssembly = "delayQueueAssembly",
            setKeyDelayQueueDisassembly = "delayQueueDisassembly",
            setKeyDelayRemoveExcessAssembly = "delayRemoveExcessAssembly",
            setKeyDelayRemoveExcessDisassembly = "delayRemoveExcessDisassembly",
            setKeyDelaySortBlueprints = "delaySortBlueprints",
            setKeyDelaySortCargoPriority = "delaySortCargoPriority",
            setKeyDelaySpreadBlueprints = "delaySpreadBlueprints",
            setKeyDelayLoadouts = "delayLoadouts",
            setKeyDelayFillingBottles = "delayFillingBottles",
            setKeyDelayLogic = "delayLogic",
            setKeyDelayIdleAssemblerCheck = "delayCheckIdleAssemblers",
            setKeyDelayResetIdleAssembler = "delayResetIdleAssembler",
            setKeyDelayFindModItems = "delayFindModItems",
            setKeyDelaySortRefinery = "delaySortRefinery",
            setKeyDelayOrderCargo = "delayOrderCargo",
            setKeyActionLimiterMultiplier = "actionLimiterMultiplier", //performance
            setKeyRunTimeLimiter = "runTimeLimiter",
            setKeyOverheatAverage = "overheatAverage",
            setKeyIcePerGenerator = "icePerO2/H2Generator", //default fill amounts
            setKeyFuelPerReactor = "fuelPerReactor",
            setKeyAmmoPerGun = "ammoPerGun",
            setKeyCanvasPerParachute = "canvasPerParachute",
            setKeyBalanceRange = "balanceRange", //adjustments
            setKeyAllowedExcessPercent = "allowedExcessPercent",
            setKeyDynamicQuotaPercentageIncrement = "dynamicQuotaPercentageIncrement",
            setKeyDynamicuotaMaxMultiplier = "dynamicQuotaMaxMultiplier",
            setKeyDynamicQuotaNegativeThreshold = "dynamicQuotaNegativeThreshold",
            setKeyDynamicQuotaPositiveThreshold = "dynamicQuotaPositiveThreshold",
            setKeyUpdateFrequency = "updateFrequency",
            setKeyOutputLimit = "outputLimit",
            setKeySurvivalKitQueuedIngots = "survivalKitQueuedIngots",
            setKeyAutoMergeLengthTolerance = "autoMergeLengthTolerance",
            setKeyPrioritizedOreCount = "prioritizedOreCount",
            setKeyOreMinimum = "oreMinimum",
            setKeyToggleCountItems = "countItems", //basic toggles
            setKeyToggleCountBlueprints = "countBlueprints",
            setKeyToggleSortItems = "sortItems",
            setKeyToggleQueueAssembly = "queueAssembly",
            setKeyToggleQueueDisassembly = "queueDisassembly",
            setKeyToggleDistribution = "distributeItems",
            setKeyToggleAutoLoadSettings = "autoLoadSettings",
            setKeyToggleProcessLimits = "processLimits",//advanced toggles
            setKeyToggleSpreadRefieries = "spreadRefineries",
            setKeyToggleSpreadReactors = "spreadReactors",
            setKeyToggleSpreadGuns = "spreadGuns",
            setKeyToggleSpreadGasGenerators = "spreadH2/O2Gens",
            setKeyToggleSpreadGravelSifters = "spreadGravelSifters",
            setKeyToggleSpreadParachutes = "spreadParachutes",
            setKeyToggleRemoveExcessAssembly = "removeExcessAssembly",
            setKeyToggleRemoveExcessDisassembly = "removeExcessDisassembly",
            setKeyToggleSortBlueprints = "sortBlueprints",
            setKeyToggleSortCargoPriority = "sortCargoPriority",
            setKeyToggleSpreadBlueprints = "spreadBlueprints",
            setKeyToggleDoLoadouts = "doLoadouts",
            setKeyToggleLogic = "triggerLogic",
            setKeyToggleResetIdleAssemblers = "resetIdleAssemblers",
            setKeyToggleFindModItems = "findModItems",
            setKeyToggleToggleSortRefineries = "sortRefineries",
            setKeyToggleOrderCargo = "orderCargo",
            setKeyAutoConveyorRefineries = "useConveyorRefineries", //settings
            setKeyAutoConveyorReactors = "useConveyorReactors",
            setKeyAutoConveyorGasGenerators = "useConveyorH2/O2Gens",
            setKeyAutoConveyorGuns = "useConveyorGuns",
            setKeyToggleDynamicQuota = "dynamicQuota",
            setKeyDynamicQuotaIncreaseWhenLow = "dynamicQuotaIncreaseWhenLow",
            setKeySameGridOnly = "sameGridOnly",
            setKeySurvivalKitAssembly = "survivalKitAssembly",
            setKeyAddLoadoutsToQuota = "addLoadoutsToQuota",
            setKeyControlConveyors = "controlConveyors",
            setKeyAutoTagBlocks = "autoTagBlocks",
            setKeyUseAcceptanceFilter = "useAcceptanceFilter",
            setKeyExcludedDefinitions = "excludedDefinitions", //setting lists
            setKeyGravelSifterKeys = "gravelSifterKeys",
            setKeyDefaultSuffixes = "numberSuffixes",
            setKeyIndexAssemblers = "asm", //Block index list keys
            setKeyIndexGasGenerators = "gas",
            setKeyIndexGravelSifters = "sft",
            setKeyIndexGun = "gun",
            setKeyIndexHydrogenTank = "htk",
            setKeyIndexOxygenTank = "otk",
            setKeyIndexParachute = "prt",
            setKeyIndexReactor = "rcr",
            setKeyIndexRefinery = "rfy",
            setKeyIndexStorage = "Storage",
            setKeyIndexSortable = "srt",
            setKeyIndexLoadout = "ldt",
            setKeyIndexLogic = "lgc",
            setKeyIndexPanel = "pnl",
            setKeyIndexInventory = "inv",
            setKeyIndexLimit = "lmt",
            setKeyBlockToggles = "Toggles", //Block setting list categories
            setKeyBlockInputToggles = "Input Toggles",
            setKeyBlockOutputToggles = "Output Toggles",
            setKeyBlockAssemblerToggles = "Assembler Toggles",
            setKeyBlockStorageKey = "storage",
            setKeyBlockAutoConveyorKey = "autoconveyor",
            setKeyBlockAssemblyOnlyKey = "assemblyonly",
            setKeyBlockDisassemblyOnlyKey = "disassemblyonly",
            setKeyBlockKeepInputKey = "keepinput",
            setKeyBlockKeepOutputKey = "keepoutput",
            setKeyBlockRemoveInputKey = "removeinput",
            setKeyBlockRemoveOutputKey = "removeoutput",
            setKeyBlockNoIdleResetKey = "noidlereset",
            setKeyBlockNoSortingKey = "nosorting",
            setKeyBlockNoSpreadKey = "nospreading",
            setKeyBlockNoCountKey = "nocounting",
            setKeyBlockUniqueBlueprinteOnlyKey = "uniqueblueprintsonly",
            setKeyBlockGunOverrideKey = "gunoverride",
            setKeyBlockLoadoutNoCount = "nocountloadout",
            setKeyBlockNoAutolimit = "noautolimit";

        #endregion

        #endregion

        #region Global

        static TimeSpan scriptSpan = TimeSpan.Zero;

        static string
            crossGridKeyword, includeGridKeyword,
            excludeGridKeyword, exclusionKeyword,
            ingotKeyword, oreKeyword,
            componentKeyword, ammoKeyword,
            toolKeyword, globalFilterKeyword,
            panelTag, optionBlockFilter;

        static double settingVersion = 5.28, buildVersion = 279, torchAverage = 0, tickWeight = 0.005;

        #endregion

        #region Values

        bool
            booted = false, saving, loading, fillingBottles = false,
            reset = false, autoLoadSettings = true, correctScript = false, correctVersion = false,
            scanning = false, allowEcho = true,
            prioritySystemActivated = false, errorFilter = false, useDynamicQuota, increaseDynamicQuotaWhenLow, update = false,
            tempMatchingItemAppend, tempMatchingItemAcceptZero, tempOrderByPriority, tempDistributeBlueprintCount,
            tempInsertBlueprintCount, tempGetTagsAcceptZero, useAcceptanceFilter = true;

        FunctionIdentifier selfContainedIdentifier, currentFunction = FunctionIdentifier.Idle, currentMajorFunction = FunctionIdentifier.Idle;

        MyAssemblerMode tempDistributeBlueprintMode, tempInsertBlueprintMode, tempRemoveBlueprintMode;

        EchoMode echoMode = EchoMode.Main;

        int echoTicks = 10, activeOres = 0, overheatTicks = 0,
            currentErrorCount = 0, totalErrorCount = 0, checkTicks = 0, mergeLengthTolerance = 0, updateFrequency = 1,
            outputLimit = 15, tempStorageInventoryIndex, tempStoragePriorityMax,
            tempStorageIndexStart, spacerIndex = 0, currentIdleTicks = 0;

        double transferredAmount = 0, countedAmount = 0,
            transferAmount, dynamicQuotaMultiplierIncrement, dynamicQuotaMaxMultiplier, dynamicQuotaPositiveThreshold,
            dynamicQuotaNegativeThreshold, balanceRange = 0.05,
            allowedExcessPercent = 0,
            delayResetIdleAssembler = 45,
            scriptHealth = 100, tempTransferAmount, tempStorageMax, tempDistributeItemMax,
            tempDistributeBlueprintAmount, tempInsertBlueprintAmount, tempRemoveBlueprintAmount, oreMinimum = 0.5;

        long tempStorageBlockIndex;

        string
            scriptName = "NDS Inventory Manager",
            settingBackup = "", mergeItem = "",
            stoneOreToIngotBasicID = PositionPrefix("0010", "StoneOreToIngotBasic"),
            lastString = "", itemCategoryString,
            tempItemSetting, tempScriptSetting,
            tempMatchingItemCategory, tempMatchingItemName,
            tempMatchingItemAmount, tempProcessLogicData,
            tempCountItemsInListTypeID, tempCountItemsInListSubtypeID,
            tempAmountContainedTypeID, tempAmountContainedSubtypeID,
            tempGetTagsText;

        string echoSpacer => ColoredEcho("".PadRight(4, spacerChars[spacerIndex]), 3);

        static string newLine;

        TimeSpan fillBottleSpan = TimeSpan.Zero;

        DateTime tickStartTime = Now, lastActionClearTime = Now, itemAddedOrChanged = Now;

        #endregion

        #region Classes/Structs

        PanelMasterClass panelMaster = new PanelMasterClass();

        ItemCollection itemCollectionMain = NewCollection,
                       itemCollectionAlternate = NewCollection, itemCollectionProcessTotalLoadout = NewCollection,
                       tempMatchingItemsCollection, tempSetBlockQuotaCollection,
                       tempCountItemsInListCollection,
                       tempGetTagsCollection;

        BlockDefinition mainBlockDefinition, alternateBlockDefinition, storageDefinitionA, tempBlockOptionDefinition,
                        tempDistributeItemBlockDefinition, tempInsertBlueprintBlockDefinition, tempGenerateBlockOptionBlockDefinition;

        Blueprint tempDistributeBlueprint, tempRemoveBlueprint;

        MyInventoryItem tempTransferInventoryItem, tempDistributeItem;

        MyDefinitionId tempInsertBlueprintID;

        IMyInventory tempTransferOriginInventory, tempOrderInventory, tempAmountContainedInventory;

        #endregion

        #endregion


        #region Main


        Program()
        {
            gtSystem = GridTerminalSystem;
            ItemCollection.parent = PanelMasterClass.parent = LogicComparison.parent = this;
            newLine = Environment.NewLine;

            SetConstants();

            saving = !(loading = TextHasLength(Me.CustomData));

            FillDict();

            foreach (FunctionIdentifier identifier in Enum.GetValues(typeof(FunctionIdentifier)))
                if (identifier != FunctionIdentifier.Idle)
                {
                    stateRecords[identifier] = new StateRecord();
                    InitializeStateV2(identifier);
                }

            booted = true;
        }

        void FillDict()
        {
            if (!UseVanillaLibrary || booted) return;
            AddItemDef("Bulletproof Glass", "BulletproofGlass", componentType, "BulletproofGlass");
            AddItemDef(canvasType, canvasType, componentType, PositionPrefix("0030", canvasType));
            AddItemDef("Computer", "Computer", componentType, "ComputerComponent");
            AddItemDef("Construction Comp", "Construction", componentType, "ConstructionComponent");
            AddItemDef("Detector Component", "Detector", componentType, "DetectorComponent");
            AddItemDef("Display", "Display", componentType, "Display");
            AddItemDef("Explosives", "Explosives", componentType, "ExplosivesComponent");
            AddItemDef("Girder", "Girder", componentType, "GirderComponent");
            AddItemDef("Gravity Gen. Comp", "GravityGenerator", componentType, "GravityGeneratorComponent");
            AddItemDef("Interior Plate", "InteriorPlate", componentType, "InteriorPlate");
            AddItemDef("Large Steel Tube", "LargeTube", componentType, "LargeTube");
            AddItemDef("Medical Component", "Medical", componentType, "MedicalComponent");
            AddItemDef("Metal Grid", "MetalGrid", componentType, "MetalGrid");
            AddItemDef("Motor", "Motor", componentType, "MotorComponent");
            AddItemDef("Power Cell", "PowerCell", componentType, "PowerCell");
            AddItemDef("Radio Comm. Comp", "RadioCommunication", componentType, "RadioCommunicationComponent");
            AddItemDef("Reactor Component", "Reactor", componentType, "ReactorComponent");
            AddItemDef("Small Steel Tube", "SmallTube", componentType, "SmallTube");
            AddItemDef("Solar Cell", "SolarCell", componentType, "SolarCell");
            AddItemDef("Steel Plate", "SteelPlate", componentType, "SteelPlate");
            AddItemDef("Superconductor", "Superconductor", componentType, "Superconductor");
            AddItemDef("Thruster Component", "Thrust", componentType, "ThrustComponent");
            AddItemDef("Zone Chip", "ZoneChip", componentType, nothingType, false);
            AddItemDef("MR-20", "AutomaticRifleItem", toolType, PositionPrefix("0040", "AutomaticRifle"));
            AddItemDef("MR-8P", "PreciseAutomaticRifleItem", toolType, PositionPrefix("0060", "PreciseAutomaticRifle"));
            AddItemDef("MR-50A", "RapidFireAutomaticRifleItem", toolType, PositionPrefix("0050", "RapidFireAutomaticRifle"));
            AddItemDef("MR-30E", "UltimateAutomaticRifleItem", toolType, PositionPrefix("0070", "UltimateAutomaticRifle"));
            AddItemDef("Welder 1", "WelderItem", toolType, PositionPrefix("0090", "Welder"));
            AddItemDef("Welder 2", "Welder2Item", toolType, PositionPrefix("0100", "Welder2"));
            AddItemDef("Welder 3", "Welder3Item", toolType, PositionPrefix("0110", "Welder3"));
            AddItemDef("Welder 4", "Welder4Item", toolType, PositionPrefix("0120", "Welder4"));
            AddItemDef("Grinder 1", "AngleGrinderItem", toolType, PositionPrefix("0010", "AngleGrinder"));
            AddItemDef("Grinder 2", "AngleGrinder2Item", toolType, PositionPrefix("0020", "AngleGrinder2"));
            AddItemDef("Grinder 3", "AngleGrinder3Item", toolType, PositionPrefix("0030", "AngleGrinder3"));
            AddItemDef("Grinder 4", "AngleGrinder4Item", toolType, PositionPrefix("0040", "AngleGrinder4"));
            AddItemDef("Drill 1", "HandDrillItem", toolType, PositionPrefix("0050", "HandDrill"));
            AddItemDef("Drill 2", "HandDrill2Item", toolType, PositionPrefix("0060", "HandDrill2"));
            AddItemDef("Drill 3", "HandDrill3Item", toolType, PositionPrefix("0070", "HandDrill3"));
            AddItemDef("Drill 4", "HandDrill4Item", toolType, PositionPrefix("0080", "HandDrill4"));
            AddItemDef("Datapad", "Datapad", dataPadType, "Datapad", false);
            AddItemDef("Powerkit", "Powerkit", consumableType, nothingType, false);
            AddItemDef("Medkit", "Medkit", consumableType, nothingType, false);
            AddItemDef("Clang Cola", "ClangCola", consumableType, nothingType, false);
            AddItemDef("Cosmic Coffee", "CosmicCoffee", consumableType, nothingType, false);
            AddItemDef("SpaceCredit", "SpaceCredit", physicalObjectType, nothingType, false);
            AddItemDef("Oxygen Bottle", "OxygenBottle", oxyBottleType, PositionPrefix("0010", "OxygenBottle"));
            AddItemDef("Hydrogen Bottle", "HydrogenBottle", hydBottleType, PositionPrefix("0020", "HydrogenBottle"));
            AddItemDef("NATO 25x184mm", "NATO_25x184mm", ammoType, PositionPrefix("0080", "NATO_25x184mmMagazine"));
            AddItemDef("Missile 200mm", "Missile200mm", ammoType, PositionPrefix("0100", "Missile200mm"));
            AddItemDef("Cobalt Ore", "Cobalt", oreType);
            AddItemDef("Gold Ore", "Gold", oreType);
            AddItemDef("Ice", "Ice", oreType);
            AddItemDef("Iron Ore", "Iron", oreType);
            AddItemDef("Magnesium Ore", "Magnesium", oreType);
            AddItemDef("Nickel Ore", "Nickel", oreType);
            AddItemDef("Platinum Ore", "Platinum", oreType);
            AddItemDef("Scrap Ore", "Scrap", oreType, "", false);
            AddItemDef("Silicon Ore", "Silicon", oreType);
            AddItemDef("Silver Ore", "Silver", oreType);
            AddItemDef(stoneType, stoneType, oreType);
            AddItemDef("Uranium Ore", "Uranium", oreType);
            AddItemDef("Cobalt Ingot", "Cobalt", ingotType);
            AddItemDef("Gold Ingot", "Gold", ingotType);
            AddItemDef("Gravel", stoneType, ingotType);
            AddItemDef("Iron Ingot", "Iron", ingotType, "", true, new List<string>() { "Scrap", stoneType });
            AddItemDef("Magnesium Powder", "Magnesium", ingotType);
            AddItemDef("Nickel Ingot", "Nickel", ingotType, "", true, new List<string>() { stoneType });
            AddItemDef("Platinum Ingot", "Platinum", ingotType, "");
            AddItemDef("Silicon Wafer", "Silicon", ingotType, "", true, new List<string>() { stoneType });
            AddItemDef("Silver Ingot", "Silver", ingotType);
            AddItemDef("Uranium Ingot", "Uranium", ingotType);
            AddItemDef("MR-20 Magazine", "AutomaticRifleGun_Mag_20rd", ammoType, PositionPrefix("0040", "AutomaticRifleGun_Mag_20rd"));
            AddItemDef("S-10E Magazine", "ElitePistolMagazine", ammoType, PositionPrefix("0030", "ElitePistolMagazine"));
            AddItemDef("S-20A Magazine", "FullAutoPistolMagazine", ammoType, PositionPrefix("0020", "FullAutoPistolMagazine"));
            AddItemDef("MR-8P Magazine", "PreciseAutomaticRifleGun_Mag_5rd", ammoType, PositionPrefix("0060", "PreciseAutomaticRifleGun_Mag_5rd"));
            AddItemDef("MR-50A Magazine", "RapidFireAutomaticRifleGun_Mag_50rd", ammoType, PositionPrefix("0050", "RapidFireAutomaticRifleGun_Mag_50rd"));
            AddItemDef("S-10 Magazine", "SemiAutoPistolMagazine", ammoType, PositionPrefix("0010", "SemiAutoPistolMagazine"));
            AddItemDef("MR-30E Magazine", "UltimateAutomaticRifleGun_Mag_30rd", ammoType, PositionPrefix("0070", "UltimateAutomaticRifleGun_Mag_30rd"));
            AddItemDef("Artillery Shell", "LargeCalibreAmmo", ammoType, PositionPrefix("0120", "LargeCalibreAmmo"));
            AddItemDef("Assault Cannon Shell", "MediumCalibreAmmo", ammoType, PositionPrefix("0110", "MediumCalibreAmmo"));
            AddItemDef("Autocannon Mag", "AutocannonClip", ammoType, PositionPrefix("0090", "AutocannonClip"));
            AddItemDef("Large Railgun Sabot", "LargeRailgunAmmo", ammoType, PositionPrefix("0140", "LargeRailgunAmmo"));
            AddItemDef("Small Railgun Sabot", "SmallRailgunAmmo", ammoType, PositionPrefix("0130", "SmallRailgunAmmo"));
            AddItemDef("PRO-1", "AdvancedHandHeldLauncherItem", toolType, PositionPrefix("0090", "AdvancedHandHeldLauncher"));
            AddItemDef("RO-1", "BasicHandHeldLauncherItem", toolType, PositionPrefix("0080", "BasicHandHeldLauncher"));
            AddItemDef("S-10E", "ElitePistolItem", toolType, PositionPrefix("0030", "EliteAutoPistol"));
            AddItemDef("S-20A", "FullAutoPistolItem", toolType, PositionPrefix("0020", "FullAutoPistol"));
            AddItemDef("S-10", "SemiAutoPistolItem", toolType, PositionPrefix("0010", "SemiAutoPistol"));
        }

        void SetConstants()
        {
            //Strings
            exclusionKeyword = GetKeyString(setKeyExclusion);
            ingotKeyword = GetKeyString(setKeyIngot);
            oreKeyword = GetKeyString(setKeyOre);
            componentKeyword = GetKeyString(setKeyComponent);
            ammoKeyword = GetKeyString(setKeyAmmo);
            toolKeyword = GetKeyString(setKeyTool);
            globalFilterKeyword = GetKeyString(setKeyGlobalFilter);
            panelTag = GetKeyString(setKeyPanel);
            crossGridKeyword = GetKeyString(setKeyCrossGrid);
            includeGridKeyword = GetKeyString(setKeyIncludeGrid);
            excludeGridKeyword = GetKeyString(setKeyExcludeGrid);
            optionBlockFilter = GetKeyString(setKeyOptionBlockFilter);

            //Lists
            if (itemCategoryList.Count == 0)
                itemCategoryList.AddRange(new List<string> { ingotKeyword, oreKeyword, componentKeyword, toolKeyword, ammoKeyword });

            //Bools
            autoLoadSettings = GetKeyBool(setKeyToggleAutoLoadSettings);
            useDynamicQuota = GetKeyBool(setKeyToggleDynamicQuota);
            increaseDynamicQuotaWhenLow = GetKeyBool(setKeyDynamicQuotaIncreaseWhenLow);
            useAcceptanceFilter = GetKeyBool(setKeyUseAcceptanceFilter);

            //Doubles
            dynamicQuotaMultiplierIncrement = GetKeyDouble(setKeyDynamicQuotaPercentageIncrement);
            dynamicQuotaMaxMultiplier = GetKeyDouble(setKeyDynamicuotaMaxMultiplier);
            dynamicQuotaPositiveThreshold = GetKeyDouble(setKeyDynamicQuotaPositiveThreshold);
            dynamicQuotaNegativeThreshold = GetKeyDouble(setKeyDynamicQuotaNegativeThreshold);
            allowedExcessPercent = GetKeyDouble(setKeyAllowedExcessPercent);
            balanceRange = GetKeyDouble(setKeyBalanceRange);
            delayResetIdleAssembler = GetKeyDouble(setKeyDelayResetIdleAssembler);
            oreMinimum = GetKeyDouble(setKeyOreMinimum);

            //Ints
            updateFrequency = settingsInts[setKeyUpdateFrequency];
            updateFrequency = updateFrequency == 1 || updateFrequency == 10 || updateFrequency == 100 ? updateFrequency : 1;

            //Presets
            itemCategoryString = $"All|{String.Join("|", itemCategoryList.Select(b => Formatted(b)))}";

            panelMaster.presetPanelOptions = PanelMasterClass.PresetPanelOption(itemCategoryString);

            ResetRuntimes();
            outputLimit = settingsInts[setKeyOutputLimit];
            mergeLengthTolerance = settingsInts[setKeyAutoMergeLengthTolerance];
        }

        void SetPostLoad()
        {
            actionLimiterMultiplier = GetKeyDouble(setKeyActionLimiterMultiplier);
            runTimeLimiter = GetKeyDouble(setKeyRunTimeLimiter);
            overheatAverage = GetKeyDouble(setKeyOverheatAverage);
        }

        void Main(string argument)
        {
            if ((!reset && !update) || saving)
            {
                tickStartTime = Now;
                itemListAllItems.Clear();

                bool handledCommand = false;

                scriptSpan += Runtime.TimeSinceLastRun;
                torchAverage = TorchAverage(torchAverage, Runtime.LastRunTimeMs);

                if (echoMode != EchoMode.MergeMenu && TextHasLength(argument))
                    try
                    {
                        handledCommand = true;
                        Commands(argument);
                    }
                    catch
                    {
                        Output($"Error running command: {argument}");
                    }
                if (IdleTicks <= 0 || currentIdleTicks >= IdleTicks)
                {
                    currentIdleTicks = 0;
                    if (overheatAverage <= 0 || torchAverage < overheatAverage)
                    {
                        if (autoLoadSettings && !reset)
                        {
                            checkTicks++;
                            if (checkTicks >= 600)
                            {
                                if (!StringsMatch(Me.CustomData.Trim(), settingBackup))
                                {
                                    loading = true;
                                    checkTicks = -300;
                                }
                                else checkTicks = 0;
                            }
                        }

                        overheatTicks = 0;
                        StateManager(FunctionIdentifier.Script);
                    }
                    else overheatTicks++;
                }
                else currentIdleTicks++;

                echoTicks += updateFrequency;
                if (echoMode == EchoMode.MergeMenu)
                    MergeCommand(!handledCommand ? argument : "");
                if (allowEcho && echoTicks >= echoDelay + (overheatTicks > 0 ? overheatTicks / 10 : 0))
                    try
                    {
                        echoTicks = 0;
                        switch (echoMode)
                        {
                            case EchoMode.Main:
                                MainEcho();
                                break;
                            case EchoMode.MergeHelp:
                                MergeHelp();
                                break;
                            case EchoMode.MergeMenu:
                                MergingMenu();
                                break;
                        }
                        PadEcho();
                    }
                    catch { Output("Error caught in echo"); }
            }
            else
            {
                if (reset)
                    Echo("Please recompile to complete reset!");
                if (update)
                    Echo("Remove any settings you want to update/reset and recompile");
            }
        }

        void MergeCommand(string argument)
        {
            if (TextHasLength(argument))
            {
                string subArg = RemoveSpaces(argument, true);
                if (subArg == "merge")
                {
                    mergeItem = "";
                    echoMode = EchoMode.Main;
                    SetLastString("Closed Merge Menu");
                    return;
                }
                int index;
                if (int.TryParse(argument, out index))
                {
                    index--;
                    if (!TextHasLength(mergeItem))
                    {
                        if (index < modItemDictionary.Count)
                            mergeItem = modItemDictionary.Keys[index];
                    }
                    else if (index < modBlueprintList.Count)
                    {
                        UpdateItemDef(mergeItem, modBlueprintList[index]);
                        mergeItem = "";
                        echoMode = modItemDictionary.Count > 0 && modBlueprintList.Count > 0 ? EchoMode.MergeMenu : EchoMode.Main;
                        saving = true;
                    }
                }
            }
        }

        void MergingMenu()
        {
            Echo("--Merging Menu--");
            Echo("--Enter 'merge' to cancel--");
            if (!TextHasLength(mergeItem))
            {
                Echo("Choose Item");
                for (int i = 0, max = modItemDictionary.Count; i < max; i++)
                    Echo($"{i + 1} : {modItemDictionary.Values[i]}");

                if (modItemDictionary.Count == 0)
                    echoMode = EchoMode.Main;
            }
            else
            {
                Echo($"Choose Blueprint For {mergeItem}");
                for (int i = 0, max = modBlueprintList.Count; i < max; i++)
                    Echo($"{i + 1} : {modBlueprintList[i]}");

                if (modBlueprintList.Count == 0)
                {
                    echoMode = EchoMode.Main;
                    mergeItem = "";
                }
            }
        }

        void MergeHelp()
        {
            Echo("--Merge Help List--");
            Echo("--Enter 'merge?' to hide--");
            Echo("--Enter 'merge' to begin merge--");
            for (int i = 0; i < modItemDictionary.Count; i++)
                Echo($"ITM: {modItemDictionary.Values[i]}");

            for (int i = 0; i < modBlueprintList.Count; i++)
                Echo($"BPT: {modBlueprintList[i]}");
        }

        void MainEcho()
        {
            spacerIndex = (spacerIndex + 1) % spacerChars.Count;
            Echo($"Main: {ColoredEcho(currentMajorFunction.ToString().Replace("_", " "))}");
            Echo($"Current: {ColoredEcho(currentFunction.ToString().Replace("_", " "))}");
            Echo($"Last: {Round(Runtime.LastRunTimeMs, 4)}");
            Echo($"Avg: {Round(torchAverage, 4)}");
            Echo($"Blocks: {managedBlocks.Count}");
            Echo($"Panels: {typedIndexes[setKeyIndexPanel].Count}");

            OptionalEcho($"Mod Items: {modItemDictionary.Count}", modItemDictionary.Count > 0);
            OptionalEcho($"Mod Blueprints: {modBlueprintList.Count}", modBlueprintList.Count > 0);
            OptionalEcho("-Enter 'merge' to begin merge", modItemDictionary.Count > 0 && modBlueprintList.Count > 0);

            OptionalEcho($"{ColoredEcho($"Overheat x{overheatTicks}", 1)}", overheatTicks > 0);

            OptionalEcho($"Last: {ColoredEcho(lastString)}", TextHasLength(lastString));

            if (TextHasLength(lastString) && Now >= lastActionClearTime)
                lastString = "";

            Echo($"Uptime: {scriptSpan:c}");
        }

        void Commands(string argument)
        {
            string arg = argument.ToLower(), subArg, key, name, data;
            bool handled = true;
            SetLastString($"Running argument: {argument}");
            subArg = RemoveSpaces(arg);
            SplitData(arg, out key, out data, ' ');
            string value;
            switch (subArg)
            {
                case "save":
                    if (!loading)
                    {
                        SetLastString("Started save process");
                        saving = true;
                    }
                    else
                        SetLastString("Load process is active, please wait to save!");
                    break;
                case "load":
                    if (!saving)
                    {
                        SetLastString("Started load process");
                        loading = true;
                    }
                    else
                        SetLastString("Save process is active, please wait to load!");
                    break;
                case "clearqueue":
                    typedIndexes[setKeyIndexAssemblers].ForEach(index =>
                    {
                        if (IsBlockOk(index))
                            ((IMyAssembler)managedBlocks[index].block).ClearQueue();
                    });
                    SetLastString("Assembler queues cleared");
                    break;
                case "reset":
                    if (!saving && !loading)
                    {
                        Me.CustomData = "";
                        reset = true;
                        saving = true;
                        SetLastString("Save and reset process started");
                    }
                    break;
                case "update":
                    if (!saving && !loading)
                    {
                        saving = true;
                        update = true;
                        SetLastString("Save and update process started");
                    }
                    break;
                case "clearfunctions":
                    if (!saving && !loading)
                    {
                        ClearFunctions();
                        SetLastString("Active processes stopped");
                    }
                    break;
                case "merge?":
                    echoMode = (echoMode == EchoMode.Main && modItemDictionary.Count + modBlueprintList.Count > 0) ? EchoMode.MergeHelp : EchoMode.Main;
                    SetLastString(echoMode == EchoMode.MergeHelp ? "Opened Merge Help List" : "Closed Merge Help List");
                    break;
                case "merge":
                    echoMode = (echoMode == EchoMode.Main && modItemDictionary.Count > 0 && modBlueprintList.Count > 0) ? EchoMode.MergeMenu : EchoMode.Main;
                    SetLastString(echoMode == EchoMode.MergeMenu ? "Opened Merge Menu" : "Closed Merge Menu");
                    break;
                case "scan":
                    if (!saving && !loading)
                    {
                        delaySpans.Clear();
                        ClearFunctions();
                        SetLastString("Functions and delays reset");
                    }
                    break;
                case "echo":
                    allowEcho = !allowEcho;
                    Echo($"Echo Allowed: {allowEcho}");
                    break;
                case "error":
                    errorFilter = !errorFilter;
                    SetLastString(errorFilter ? "Error filter enabled, use 'error' to disable" : "Error filter disabled");
                    break;
                case "full":
                    if (!saving && !loading)
                    {
                        for (int i = 0; i < settingDictionaryBools.Count; i++)
                            for (int x = 0; x < settingDictionaryBools.Values[i].Count; x++)
                            {
                                value = settingDictionaryBools.Values[i].Keys[x];
                                SetKeyBool(value, i < 2 || (!LeadsString(value, "useconveyor") && !fullExclude.Contains(value)));
                            }

                        SetLastString("All functions");
                        saving = true;
                    }
                    break;
                case "basic":
                    if (!saving && !loading)
                    {
                        for (int i = 0; i < settingDictionaryBools.Count; i++)
                            for (int x = 0; x < settingDictionaryBools.Values[i].Count; x++)
                                SetKeyBool(settingDictionaryBools.Values[i].Keys[x], i == 0);

                        SetLastString("Basic functions only");
                        saving = true;
                    }
                    break;
                case "monitor":
                    if (!saving && !loading)
                    {
                        for (int i = 0; i < settingDictionaryBools.Count; i++)
                            for (int x = 0; x < settingDictionaryBools.Values[i].Count; x++)
                            {
                                value = settingDictionaryBools.Values[i].Keys[x];
                                SetKeyBool(value, value == setKeyToggleAutoLoadSettings || LeadsString(value, "useconveyor") || LeadsString(value, "count"));
                            }
                        SetLastString("Monitoring only");
                        saving = true;
                    }
                    break;
                default:
                    if (!TextHasLength(key))
                        SetLastString($"Unhandled command: {argument}");
                    handled = false;
                    break;
            }

            if (!handled && TextHasLength(key))
                switch (key)
                {
                    case "set":
                        if (!saving && !loading)
                        {
                            if (SplitData(data, out name, out data, ' ', false))
                            {
                                SetItemQuotaMain(name, data);
                                saving = true;
                            }
                        }
                        break;
                    default:
                        SetLastString($"Unhandled command: {argument}");
                        break;
                }
        }


        #endregion


        #region State Functions

        IEnumerator<FunctionState> OrderCargoState()
        {
            IMyInventory inventory;
            yield return stateContinue;

            while (true)
            {
                foreach (long index in typedIndexes[setKeyIndexStorage])
                {
                    if (PauseTickRun) yield return stateActive;
                    if (!IsBlockOk(index)) continue;

                    inventory = managedBlocks[index].Input;
                    mainFunctionItemList.Clear();
                    inventory.GetItems(mainFunctionItemList);

                    sortableListMain.Clear();
                    foreach (MyInventoryItem item in mainFunctionItemList)
                    {
                        if (PauseTickRun) yield return stateActive;
                        sortableListMain.Add(new SortableObject { text = item.Type.ToString(), key = $"{GetItemCategory(item)}|{ItemName(item)}" });
                    }
                    sortableListMain = sortableListMain.OrderBy(x => x.key).ToList();

                    while (!OrderInventory(sortableListMain, inventory))
                        yield return stateActive;
                }

                yield return stateContinue;
            }
        }

        bool OrderInventory(List<SortableObject> expectedOrder, IMyInventory inventory)
        {
            if (expectedOrder.Count <= 1) return true;

            selfContainedIdentifier = FunctionIdentifier.Order_Inventory;

            if (!IsStateRunning)
            {
                tempOrderInventoryList = expectedOrder;
                tempOrderInventory = inventory;
            }

            return RunStateManager;
        }

        IEnumerator<FunctionState> OrderInventoryState()
        {
            yield return stateContinue;

            while (true)
            {
                for (int x = 0; x < tempOrderInventoryList.Count; x++)
                    for (int z = x; z < tempOrderInventory.ItemCount; z++)
                    {
                        if (PauseTickRun) yield return stateActive;

                        try
                        {
                            MyInventoryItem item = (MyInventoryItem)tempOrderInventory.GetItemAt(z);
                            if (item.Type.ToString() == tempOrderInventoryList[x].text)
                            {
                                if (x != z)
                                    tempOrderInventory.TransferItemFrom(tempOrderInventory, z, x, false, item.Amount);
                                break;
                            }
                        }
                        catch { }
                    }

                yield return stateContinue;
            }
        }

        bool ProcessItemSetting(string setting)
        {
            selfContainedIdentifier = FunctionIdentifier.Processing_Item_Setting;

            if (!IsStateRunning)
                tempItemSetting = setting;

            return RunStateManager;
        }

        IEnumerator<FunctionState> ProcessItemSettingState()
        {
            double quota, dataNumber, quotaMaxAmount;
            bool acquiredDefinition, dataBool;
            int index;
            string subSetting, key, data, typeID, subtypeID;
            string[] subSettingArray;
            yield return stateContinue;

            while (true)
            {
                subSetting = tempItemSetting.Replace("||", "~");
                typeID = subtypeID = "";
                subSettingArray = subSetting.Split('~');

                ItemDefinition definition = new ItemDefinition();
                acquiredDefinition = false;
                for (int i = 0; i < subSettingArray.Length; i++)
                {
                    if (PauseTickRun) yield return stateActive;

                    if (SplitData(subSettingArray[i], out key, out data))
                    {
                        key = key.ToLower();
                        if (key == "type")
                            typeID = data;
                        else if (key == "subtype")
                            subtypeID = data;
                    }
                    if (TextHasLength(typeID) && TextHasLength(subtypeID)) break;
                }

                if (TextHasLength(typeID) && TextHasLength(subtypeID))
                {
                    AddItemDef(subtypeID, subtypeID, typeID, "");
                    acquiredDefinition = GetDefinition(out definition, $"{typeID}/{subtypeID}");
                }

                if (acquiredDefinition)
                {
                    for (int i = 0; i < subSettingArray.Length; i++)
                    {
                        if (PauseTickRun) yield return stateActive;

                        if (SplitData(subSettingArray[i], out key, out data))
                        {
                            dataBool = StringsMatch(data, trueString);
                            switch (RemoveSpaces(key, true).Trim())
                            {
                                case "name":
                                    if (!StringsMatch(definition.displayName, data))
                                        itemAddedOrChanged = Now;
                                    definition.displayName = data;
                                    break;
                                case "quota":
                                    index = data.IndexOf("<");
                                    if (index > 0)
                                    {
                                        if (double.TryParse(data.Substring(0, index), out quota) &&
                                            double.TryParse(data.Substring(index + 1), out quotaMaxAmount))
                                        {
                                            definition.quota = quota;
                                            if (quotaMaxAmount < quota)
                                                quotaMaxAmount = quota;

                                            if (quotaMaxAmount < 0)
                                                quotaMaxAmount = 0;

                                            definition.quotaMax = quotaMaxAmount;
                                        }
                                    }
                                    else if (double.TryParse(data, out quota))
                                    {
                                        definition.quota = quota;
                                        if (quota < 0)
                                            quota = 0;

                                        definition.quotaMax = quota;
                                    }
                                    break;
                                case "category":
                                    data = data.ToLower();
                                    if (!StringsMatch(definition.category, data))
                                        itemAddedOrChanged = Now;
                                    definition.category = data;
                                    AddCategory(data);
                                    break;
                                case "blueprint":
                                    if (IsBlueprint(definition.blueprintID))
                                        blueprintList.Remove(definition.blueprintID);

                                    definition.blueprintID = data;
                                    if (TextHasLength(data))
                                        modItemDictionary.Remove(definition.FullID);

                                    break;
                                case "assemblymultiplier":
                                    if (double.TryParse(data, out dataNumber))
                                        definition.assemblyMultiplier = dataNumber;

                                    break;
                                case "assemble":
                                    definition.assemble = dataBool;
                                    break;
                                case "disassemble":
                                    definition.disassemble = dataBool;
                                    break;
                                case "refine":
                                    definition.refine = dataBool;
                                    break;
                                case "display":
                                    definition.display = dataBool;
                                    break;
                                case "orekeys":
                                    string[] oreKeys = data.Substring(1, data.Length - 2).Split('|');
                                    if (oreKeys.Length > 0)
                                    {
                                        definition.oreKeys.Clear();
                                        definition.oreKeys.AddRange(oreKeys);
                                    }
                                    if (definition.oreKeys.Count == 0 && IsIngot(definition.typeID))
                                        definition.oreKeys.Add(subtypeID);

                                    break;
                                case "fuel":
                                    definition.fuel = dataBool;
                                    break;
                                case "gas":
                                    definition.gas = dataBool;
                                    break;
                            }
                        }
                    }

                    if (IsBlueprint(definition.blueprintID))
                        blueprintList[definition.blueprintID] = ItemToBlueprint(definition);

                    itemCategoryDictionary[definition.FullID] = definition.category;
                    FinalizeKeys(definition);
                    CheckModdedItem(definition);
                }

                yield return stateContinue;
            }
        }

        bool Transfer(ref double transferAmount, IMyInventory originInventory, BlockDefinition destinationBlock, MyInventoryItem item)
        {
            selfContainedIdentifier = FunctionIdentifier.Transferring_Item;

            if (!IsStateRunning)
            {
                tempTransferAmount = transferAmount;
                tempTransferOriginInventory = originInventory;
                alternateBlockDefinition = destinationBlock;
                tempTransferInventoryItem = item;
            }

            bool done = RunStateManager;

            if (done)
            {
                transferAmount -= transferredAmount;
                transferredAmount = 0;
                return true;
            }

            return false;
        }

        IEnumerator<FunctionState> TransferState()
        {
            IMyInventory destinationInventory;
            yield return stateContinue;

            while (true)
            {
                destinationInventory = alternateBlockDefinition.Input;
                if (destinationInventory != tempTransferOriginInventory)
                {
                    bool isLimited, stopFunc = false;
                    double itemLimit = alternateBlockDefinition.Settings.limits.ItemCount(out isLimited, tempTransferInventoryItem, alternateBlockDefinition.block), contained = 0, volumeLimit = GetCurrentVolumeLimit(tempTransferInventoryItem, alternateBlockDefinition.block), currentTransferAmount = tempTransferAmount;

                    if (isLimited)
                    {
                        if (itemLimit <= 0)
                            stopFunc = true;
                        else
                        {
                            while (!AmountContained(ref contained, tempTransferInventoryItem, alternateBlockDefinition.Input))
                                yield return stateActive;

                            if (contained >= itemLimit)
                                stopFunc = true;
                            else if (currentTransferAmount + contained > itemLimit)
                                currentTransferAmount = itemLimit - contained;
                        }
                    }
                    if (!stopFunc)
                    {
                        if (currentTransferAmount > volumeLimit)
                            currentTransferAmount = volumeLimit;

                        if (!FractionalItem(tempTransferInventoryItem))
                            currentTransferAmount = Math.Floor(currentTransferAmount);

                        if (currentTransferAmount > 0.0 && destinationInventory.TransferItemFrom(tempTransferOriginInventory, tempTransferInventoryItem, (MyFixedPoint)currentTransferAmount))
                        {
                            if (currentTransferAmount >= 0.01)
                                Output($"Moved {ShortNumber2(currentTransferAmount),-6} {ShortenName(ItemName(tempTransferInventoryItem), 12),-12} to {ShortenName(alternateBlockDefinition.block.CustomName, 12),-12}");

                            transferredAmount = currentTransferAmount;
                        }
                    }
                }
                yield return stateContinue;
            }
        }

        bool ProcessSetting(string setting)
        {
            selfContainedIdentifier = FunctionIdentifier.Process_Setting;

            if (!IsStateRunning)
                tempScriptSetting = setting;

            return RunStateManager;
        }

        IEnumerator<FunctionState> ProcessSettingState()
        {
            int index;
            string stringValue, key;
            yield return stateContinue;

            while (true)
            {
                index = tempScriptSetting.IndexOf("=");
                if (index != -1)
                {
                    key = tempScriptSetting.Substring(0, index).Trim();
                    if (StringsMatch(key, "name"))
                    {
                        while (!ProcessItemSetting(tempScriptSetting))
                            yield return stateActive;
                    }
                    else
                    {
                        stringValue = tempScriptSetting.Substring(index + 1).Trim();
                        double doubleValue;
                        bool boolValue = !StringsMatch(stringValue, falseString);
                        if (!double.TryParse(stringValue, out doubleValue))
                            doubleValue = 0;

                        if (key == "script")
                        {
                            if (stringValue == scriptName)
                                correctScript = true;
                        }
                        else if (key == "version")
                        {
                            if (doubleValue == settingVersion)
                                correctVersion = true;
                        }
                        else if (settingsInts.ContainsKey(key))
                            settingsInts[key] = (int)doubleValue;
                        else if (!SetKeyString(key, stringValue) && !SetKeyDouble(key, doubleValue) && !SetKeyBool(key, boolValue) && settingsListsStrings.ContainsKey(key) && LeadsString(stringValue, "[") && EndsString(stringValue, "]"))
                        {
                            stringValue = stringValue.Substring(1, stringValue.Length - 2);
                            string[] valueArray = stringValue.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                            if (clearedSettingLists.Add(key))
                                settingsListsStrings[key].Clear();
                            for (int i = 0; i < valueArray.Length; i++)
                            {
                                if (PauseTickRun)
                                    yield return stateActive;

                                settingsListsStrings[key].Add(valueArray[i]);
                            }
                        }
                    }
                }
                yield return stateContinue;
            }
        }

        IEnumerator<FunctionState> FindModItemState()
        {
            yield return stateContinue;

            while (true)
            {
                foreach (long index in typedIndexes[setKeyIndexAssemblers])
                {
                    if (PauseTickRun) yield return stateActive;
                    if (!IsBlockOk(index)) continue;


                    blueprintListMain.Clear();
                    ((IMyAssembler)managedBlocks[index].block).GetQueue(blueprintListMain);
                    for (int y = 0; y < blueprintListMain.Count; y++)
                    {
                        if (PauseTickRun) yield return stateActive;

                        if (UnknownBlueprint(blueprintListMain[y]))
                            AddModBlueprint(blueprintListMain[y]);
                    }
                }
                foreach (long index in typedIndexes[setKeyIndexInventory])
                {
                    if (PauseTickRun) yield return stateActive;
                    if (!IsBlockOk(index)) continue;


                    for (int inv = 0; inv < managedBlocks[index].block.InventoryCount; inv++)
                    {
                        mainFunctionItemList.Clear();
                        managedBlocks[index].Input.GetItems(mainFunctionItemList);
                        for (int y = 0; y < mainFunctionItemList.Count; y++)
                        {
                            if (PauseTickRun) yield return stateActive;

                            if (UnknownItem(mainFunctionItemList[y]))
                            {
                                Output($"Unknown Item: {ShortenName(mainFunctionItemList[y].Type.SubtypeId, 14)}, found in: {ShortenName(managedBlocks[index].block.CustomName, 14)}");
                                AddModItem(mainFunctionItemList[y]);
                            }
                        }
                    }
                }

                yield return stateContinue;
            }
        }

        IEnumerator<FunctionState> IdleAssemblerState()
        {
            IMyAssembler assembler;
            MonitoredAssembler monitoredAssembler;
            SortedList<string, double> productionComparison = new SortedList<string, double>();
            List<MyProductionItem> productionList = NewProductionList;
            bool changed;
            yield return stateContinue;

            while (true)
            {
                foreach (long index in typedIndexes[setKeyIndexAssemblers])
                {
                    if (PauseTickRun) yield return stateActive;
                    if (!IsBlockOk(index) || managedBlocks[index].Settings.GetOption(setKeyBlockNoIdleResetKey))
                        continue;

                    monitoredAssembler = managedBlocks[index].monitoredAssembler;
                    assembler = (IMyAssembler)managedBlocks[index].block;

                    if (!monitoredAssembler.CheckNow) continue;

                    changed = monitoredAssembler.HasChanged();

                    if (!changed)
                    {
                        assembler.GetQueue(productionList);
                        foreach (MyProductionItem myProductionItem in productionList)
                        {
                            if (PauseTickRun) yield return stateActive;
                            if (!productionComparison.ContainsKey(BlueprintSubtype(myProductionItem)))
                                productionComparison[BlueprintSubtype(myProductionItem)] = (double)myProductionItem.Amount;
                            else
                                productionComparison[BlueprintSubtype(myProductionItem)] += (double)myProductionItem.Amount;
                        }
                        productionList.Clear();
                        if (!(changed = productionComparison.Count != monitoredAssembler.productionComparison.Count))
                        {
                            foreach (KeyValuePair<string, double> kvp in productionComparison)
                            {
                                if (PauseTickRun) yield return stateActive;
                                if (!monitoredAssembler.productionComparison.ContainsKey(kvp.Key) ||
                                    monitoredAssembler.productionComparison[kvp.Key] != kvp.Value)
                                {
                                    changed = true;
                                    break;
                                }
                                else
                                    monitoredAssembler.productionComparison.Remove(kvp.Key);
                            }
                            changed = changed || monitoredAssembler.productionComparison.Count > 0;
                        }
                        monitoredAssembler.productionComparison.Clear();
                        foreach (KeyValuePair<string, double> kvp in productionComparison)
                            monitoredAssembler.productionComparison[kvp.Key] = kvp.Value;
                        productionComparison.Clear();
                    }

                    if (!changed)
                    {
                        if (monitoredAssembler.stalling)
                        {
                            assembler.ClearQueue();

                            for (int i = 0; i <= 1; i++)
                            {
                                mainFunctionItemList.Clear();
                                assembler.GetInventory(i).GetItems(mainFunctionItemList);
                                while (!PutInStorage(mainFunctionItemList, index, i)) yield return stateActive;
                            }

                            Output($"Reset Idle Assembler: {ShortenName(assembler.CustomName, 12)}");
                        }
                        else
                            monitoredAssembler.stalling = true;
                    }
                    else
                        monitoredAssembler.stalling = false;

                    if (assembler.IsQueueEmpty)
                        monitoredAssembler.Reset();

                    monitoredAssembler.SetNextCheck(delayResetIdleAssembler / 2.0);
                }

                yield return stateContinue;
            }
        }

        bool MatchItems(ItemCollection collection, string category, string name, bool append = true, string amount = "0", bool acceptZero = true)
        {
            selfContainedIdentifier = FunctionIdentifier.Matching_Items;

            if (!IsStateRunning)
            {
                tempMatchingItemsCollection = collection;
                tempMatchingItemCategory = category;
                tempMatchingItemName = name;
                tempMatchingItemAppend = append;
                tempMatchingItemAmount = amount;
                tempMatchingItemAcceptZero = acceptZero;
            }

            return RunStateManager;
        }

        IEnumerator<FunctionState> MatchingItemState()
        {
            List<ItemDefinition> itemList = NewItemDefinitionList;
            ItemDefinition definition;
            yield return stateContinue;

            while (true)
            {
                if (TextHasLength(tempMatchingItemName))
                {
                    PopulateItemList(itemList);
                    bool match, percentage;
                    double calcedAmount;
                    for (int i = 0; i < itemList.Count; i++)
                    {
                        if (PauseTickRun) yield return stateActive;

                        if (IsWildCard(tempMatchingItemCategory) || tempMatchingItemCategory == itemList[i].category)
                        {
                            definition = itemList[i];
                            match = tempMatchingItemName.Length > 2 && LeadsString(tempMatchingItemName, "'") && EndsString(tempMatchingItemName, "'");

                            if (IsWildCard(tempMatchingItemName) || (match && StringsMatch(RemoveSpaces(definition.displayName), tempMatchingItemName.Substring(1, tempMatchingItemName.Length - 2))) || (!match && LeadsString(definition.displayName, tempMatchingItemName)))
                            {
                                if ((percentage = EndsString(tempMatchingItemAmount, "%")) && double.TryParse(tempMatchingItemAmount.Substring(0, tempMatchingItemAmount.Length - 1), out calcedAmount))
                                    calcedAmount /= 100.0;
                                else if (!double.TryParse(tempMatchingItemAmount, out calcedAmount))
                                    calcedAmount = 0;
                                tempMatchingItemsCollection.AddItem(definition.typeID, definition.subtypeID, new VariableItemCount(calcedAmount, percentage, true), tempMatchingItemAppend);
                            }
                        }
                    }
                    if (!tempMatchingItemAcceptZero)
                        for (int i = 0; i < tempMatchingItemsCollection.itemList.Count; i += 0)
                        {
                            if (PauseTickRun) yield return stateActive;

                            if (tempMatchingItemsCollection.itemList.Values[i].count <= 0)
                                tempMatchingItemsCollection.itemList.RemoveAt(i);
                            else
                                i++;
                        }
                }

                yield return stateContinue;
            }
        }

        bool ProcessTimer(List<LogicComparison> logicComparisons, string data)
        {
            selfContainedIdentifier = FunctionIdentifier.Process_Logic;

            if (!IsStateRunning)
            {
                tempLogicComparisons = logicComparisons;
                tempProcessLogicData = data;
            }

            return RunStateManager;
        }

        IEnumerator<FunctionState> ProcessTimerState()
        {
            string customData, typeID, subtypeID, comparison, substring;
            string[] logicSetArray;
            int tempIndex, collectionCount, comparisonCollectionCount;
            yield return stateContinue;

            while (true)
            {
                customData = RemoveSpaces(tempProcessLogicData);
                comparison = ">";
                typeID = subtypeID = substring = "";

                if (PauseTickRun) yield return stateActive;

                logicSetArray = customData.Split('|');

                if (logicSetArray.Length > 0)
                    for (int x = 0; x < logicSetArray.Length; x++)
                    {
                        if (PauseTickRun) yield return stateActive;

                        try
                        {
                            substring = logicSetArray[x];
                            tempIndex = substring.IndexOf(":");
                            typeID = substring.Substring(0, tempIndex);
                            substring = substring.Substring(tempIndex + 1);

                            comparison =
                                substring.Contains(">=") ? ">=" :
                                substring.Contains("<=") ? "<=" :
                                substring.Contains("<") ? "<" :
                                substring.Contains("=") ? "=" : ">";

                            tempIndex = substring.IndexOf(comparison);
                            subtypeID = substring.Substring(0, tempIndex);
                            substring = substring.Substring(tempIndex + comparison.Length);
                        }
                        catch { }
                        itemCollectionMain.Clear();
                        while (!MatchItems(itemCollectionMain, typeID, subtypeID))
                            yield return stateActive;

                        collectionCount = itemCollectionMain.ItemTypeCount;
                        tempIndex = substring.IndexOf(":");
                        if (tempIndex > 0)
                        {
                            itemCollectionAlternate.Clear();

                            typeID = substring.Substring(0, tempIndex);
                            subtypeID = substring.Substring(tempIndex + 1);

                            while (!MatchItems(itemCollectionAlternate, typeID, subtypeID))
                                yield return stateActive;

                            comparisonCollectionCount = itemCollectionAlternate.ItemTypeCount;
                            for (int i = 0; i < collectionCount; i++)
                                for (int y = 0; y < comparisonCollectionCount; y++)
                                {
                                    if (PauseTickRun) yield return stateActive;

                                    tempLogicComparisons.Add(new LogicComparison { typeID = itemCollectionMain.ItemIDByIndex(i), compareAgainst = itemCollectionAlternate.ItemIDByIndex(y), comparison = comparison });
                                }
                        }
                        else
                            for (int i = 0; i < collectionCount; i++)
                            {
                                if (PauseTickRun) yield return stateActive;

                                tempLogicComparisons.Add(new LogicComparison { typeID = itemCollectionMain.ItemIDByIndex(i), compareAgainst = substring, comparison = comparison });

                            }
                    }

                yield return stateContinue;
            }
        }

        IEnumerator<FunctionState> LoadoutState()
        {
            IMyInventory loadoutInventory, sourceInventory;
            int itemCount;
            ItemDefinition definition;
            double addAmount;
            bool excessFound;
            yield return stateContinue;

            while (true)
            {
                foreach (long index in typedIndexes[setKeyIndexLoadout])
                {
                    if (PauseTickRun) yield return stateActive;

                    if (!IsBlockOk(index)) continue;

                    mainBlockDefinition = managedBlocks[index];
                    loadoutInventory = mainBlockDefinition.Input;
                    itemCollectionMain.Clear();
                    itemCollectionAlternate.Clear();
                    itemCollectionAlternate.AddCollection(mainBlockDefinition.Settings.loadout, mainBlockDefinition.block);
                    while (!CountItemsInList(itemCollectionMain, new List<long> { index }))
                        yield return stateActive;

                    itemCount = itemCollectionMain.ItemTypeCount;
                    for (int x = 0; x < itemCount; x++)
                    {
                        if (PauseTickRun) yield return stateActive;

                        definition = itemCollectionMain.ItemByIndex(x);
                        if (TextHasLength(definition.subtypeID) && itemCollectionAlternate.itemList.ContainsKey(definition.FullID))
                            itemCollectionAlternate.AddItem(definition.typeID, definition.subtypeID, new VariableItemCount(-definition.amount));
                    }
                    excessFound = false;
                    itemCount = itemCollectionAlternate.ItemTypeCount;
                    for (int x = 0; x < itemCount && !excessFound; x++)
                    {
                        if (PauseTickRun) yield return stateActive;

                        definition = itemCollectionAlternate.ItemByIndex(x);
                        excessFound = definition.amount <= -0.01;
                    }
                    if (excessFound)
                    {
                        mainFunctionItemList.Clear();
                        loadoutInventory.GetItems(mainFunctionItemList);
                        for (int x = 0; x < mainFunctionItemList.Count; x++)
                        {
                            addAmount = itemCollectionAlternate.ItemCount(mainFunctionItemList[x]);
                            if (addAmount <= 0 - 0.01)
                            {
                                addAmount *= -1.0;
                                addAmount = Math.Min(addAmount, (double)mainFunctionItemList[x].Amount);
                                while (!PutInStorage(new List<MyInventoryItem> { mainFunctionItemList[x] }, index, 0, addAmount))
                                    yield return stateActive;
                            }
                        }
                    }
                    foreach (long storageIndex in typedIndexes[setKeyIndexStorage])
                    {
                        if (PauseTickRun) yield return stateActive;

                        if (itemCollectionAlternate.IsEmpty)
                            break;
                        if (!IsBlockOk(storageIndex))
                            continue;


                        mainBlockDefinition = managedBlocks[storageIndex];
                        sourceInventory = mainBlockDefinition.Input;
                        mainFunctionItemList.Clear();
                        sourceInventory.GetItems(mainFunctionItemList);
                        for (int y = 0; y < mainFunctionItemList.Count && !itemCollectionAlternate.IsEmpty; y++)
                        {
                            if (PauseTickRun) yield return stateActive;

                            if (itemCollectionAlternate.ItemCount(mainFunctionItemList[y]) > 0)
                            {
                                addAmount = Math.Min((double)mainFunctionItemList[y].Amount, itemCollectionAlternate.ItemCount(mainFunctionItemList[y], null));

                                if (!FractionalItem(mainFunctionItemList[y]))
                                    addAmount = Math.Floor(addAmount);

                                if (addAmount > 0 && loadoutInventory.TransferItemFrom(sourceInventory, mainFunctionItemList[y], (MyFixedPoint)addAmount))
                                    itemCollectionAlternate.AddItem(mainFunctionItemList[y].Type.TypeId, mainFunctionItemList[y].Type.SubtypeId, new VariableItemCount(-addAmount));
                            }
                        }
                    }
                }

                yield return stateContinue;
            }
        }

        IEnumerator<FunctionState> SortRefineryStateV2()
        {
            IMyInventory inventory;
            double minPercent, maxPercent;
            SortedList<MyItemType, double> orePriorities = new SortedList<MyItemType, double>();
            yield return stateContinue;

            while (true)
            {
                foreach (ItemDefinition itemDef in itemListMain[oreType].Values)
                {
                    if (PauseTickRun) yield return stateActive;
                    if (itemDef.amount >= oreMinimum)
                        orePriorities[itemDef.ItemType] = LeastKeyedOrePercentage(itemDef.subtypeID);
                }
                foreach (long index in typedIndexes[setKeyIndexRefinery])
                {
                    if (PauseTickRun) yield return stateActive;

                    if (!IsBlockOk(index)) continue;

                    minPercent = double.MaxValue;
                    maxPercent = double.MinValue;

                    mainBlockDefinition = managedBlocks[index];
                    inventory = mainBlockDefinition.Input;

                    mainBlockDefinition.Settings.limits.Clear(false, true);

                    //Sort Ores inside of refinery
                    if (inventory.ItemCount > 1)
                    {
                        sortableListMain.Clear();
                        mainFunctionItemList.Clear();
                        inventory.GetItems(mainFunctionItemList);
                        for (int x = 0; x < mainFunctionItemList.Count; x++)
                        {
                            if (PauseTickRun) yield return stateActive;
                            if ((double)mainFunctionItemList[x].Amount >= oreMinimum)
                            {
                                if (!orePriorities.ContainsKey(mainFunctionItemList[x].Type))
                                    orePriorities[mainFunctionItemList[x].Type] = LeastKeyedOrePercentage(mainFunctionItemList[x]);
                                sortableListMain.Add(new SortableObject { amount = orePriorities[mainFunctionItemList[x].Type], text = mainFunctionItemList[x].Type.ToString() });
                                minPercent = Math.Min(minPercent, sortableListMain[sortableListMain.Count - 1].amount);
                                maxPercent = Math.Max(maxPercent, sortableListMain[sortableListMain.Count - 1].amount);
                            }
                            else while (!PutInStorage(new List<MyInventoryItem> { mainFunctionItemList[x] }, index, 0)) yield return stateActive;
                        }
                        if (minPercent < maxPercent)
                        {
                            sortableListMain = sortableListMain.OrderBy(z => z.amount).ToList();
                            while (!OrderInventory(sortableListMain, inventory))
                                yield return stateActive;
                        }
                    }
                    //Set automatic limits
                    if (!mainBlockDefinition.IsClone && !mainBlockDefinition.Settings.GetOption(setKeyBlockNoAutolimit) && activeOres > 1)
                    {
                        sortableListMain.Clear();
                        foreach (KeyValuePair<MyItemType, double> pair in orePriorities)
                            if (AcceptsItem(mainBlockDefinition, pair.Key.TypeId, pair.Key.SubtypeId))
                                sortableListMain.Add(new SortableObject { amount = pair.Value, text = pair.Key.SubtypeId });
                        sortableListMain = sortableListMain.OrderBy(x => x.amount).ToList();

                        double maxShares = 0, currentShares;
                        int prioritizedOres = settingsInts[setKeyPrioritizedOreCount];

                        for (int z = 1; z <= sortableListMain.Count; z++)
                            maxShares += z;

                        maxShares += prioritizedOres * 10;

                        for (int x = 0; x < sortableListMain.Count; x++)
                        {
                            if (PauseTickRun) yield return stateActive;
                            currentShares = (sortableListMain.Count - (x + 1)) + 1;

                            if (x < prioritizedOres)
                                currentShares += 10;

                            mainBlockDefinition.Settings.limits.AddItem(oreType, sortableListMain[x].text, new VariableItemCount(currentShares / maxShares, true));
                        }
                    }

                    if (mainBlockDefinition.Settings.limits.ItemTypeCount > 0)
                        typedIndexes[setKeyIndexLimit].Add(index);
                    else
                        typedIndexes[setKeyIndexLimit].Remove(index);
                }

                while (!OrderListByPriority(typedIndexes[setKeyIndexLimit], priorityTypes.Contains(setKeyIndexLimit))) yield return stateActive;
                orePriorities.Clear();
                yield return stateContinue;
            }
        }

        IEnumerator<FunctionState> LogicState()
        {
            ItemDefinition definition;
            yield return stateContinue;

            while (true)
            {
                bool andComparison, pass = true;
                foreach (long index in typedIndexes[setKeyIndexLogic])
                {
                    if (PauseTickRun) yield return stateActive;

                    alternateBlockDefinition = managedBlocks[index];
                    if (!(alternateBlockDefinition.block is IMyTimerBlock) || ((IMyFunctionalBlock)alternateBlockDefinition.block).Enabled)
                    {
                        andComparison = alternateBlockDefinition.Settings.andComparison;
                        for (int x = 0; x < alternateBlockDefinition.Settings.logicComparisons.Count; x++)
                        {
                            if (PauseTickRun)
                                yield return stateActive;

                            if (GetDefinition(out definition, alternateBlockDefinition.Settings.logicComparisons[x].typeID))
                                pass = LogicPass(definition, alternateBlockDefinition.Settings.logicComparisons[x].comparison, alternateBlockDefinition.Settings.logicComparisons[x].compareAgainst);

                            if ((!pass && andComparison) || (pass && !andComparison))
                                break;
                        }
                        if (alternateBlockDefinition.block is IMyTimerBlock)
                        {
                            if (pass)
                                ((IMyTimerBlock)alternateBlockDefinition.block).Trigger();
                        }
                        else
                            ((IMyFunctionalBlock)alternateBlockDefinition.block).Enabled = pass;
                    }
                }

                yield return stateContinue;
            }
        }

        IEnumerator<FunctionState> ScriptState()
        {
            yield return stateContinue;

            while (true)
            {
                while (!LoadData()) yield return stateActive;

                while (!SaveData()) yield return stateActive;

                StateManager(FunctionIdentifier.Main_Control);

                yield return stateActive;

                if (!scanning && !loading) StateManager(FunctionIdentifier.Main_Output);

                yield return stateContinue;
            }
        }

        IEnumerator<FunctionState> ControlState()
        {
            FunctionIdentifier key;
            yield return stateContinue;

            while (true)
            {
                if (currentErrorCount >= 10)
                {
                    currentErrorCount = 0;
                    foreach (FunctionIdentifier identifier in stateRecords.Keys)
                    {
                        if (PauseTickRun) yield return stateActive;

                        if (identifier != FunctionIdentifier.Script && identifier != FunctionIdentifier.Main_Control && identifier != FunctionIdentifier.Idle && StateInitialized(identifier))
                            StateDisposal(identifier);
                    }
                }

                key = FunctionIdentifier.Scanning;
                if (FunctionDelay(key))
                {
                    scanning = true;
                    currentMajorFunction = key;
                    while (!StateManager(key)) yield return stateActive;

                    scanning = false;
                    delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelayScan));
                }

                if (GetKeyBool(setKeyToggleCountItems))
                {
                    currentMajorFunction = FunctionIdentifier.Counting_Items;
                    while (!StateManager(currentMajorFunction)) yield return stateActive;
                    yield return stateActive;
                }

                if (GetKeyBool(setKeyToggleCountBlueprints))
                {
                    currentMajorFunction = FunctionIdentifier.Counting_Blueprints;
                    while (!StateManager(currentMajorFunction)) yield return stateActive;
                }

                if (GetKeyBool(setKeyToggleSortBlueprints))
                {
                    key = FunctionIdentifier.Sort_Blueprints;
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;
                        while (!StateManager(key)) yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelaySortBlueprints));
                    }
                }

                if (GetKeyBool(setKeyToggleQueueAssembly) && GetKeyBool(setKeyToggleCountBlueprints))
                {
                    key = FunctionIdentifier.Queue_Assembly;
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;
                        while (!StateManager(key)) yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelayQueueAssembly));
                    }
                }

                if (GetKeyBool(setKeyToggleQueueDisassembly) && GetKeyBool(setKeyToggleCountBlueprints))
                {
                    key = FunctionIdentifier.Queue_Disassembly;
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;
                        while (!StateManager(key)) yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelayQueueDisassembly));
                    }
                }

                if (GetKeyBool(setKeyToggleRemoveExcessAssembly) && GetKeyBool(setKeyToggleCountBlueprints))
                {
                    key = FunctionIdentifier.Removing_Excess_Assembly;
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;
                        while (!StateManager(key)) yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelayRemoveExcessAssembly));
                    }
                }

                if (GetKeyBool(setKeyToggleRemoveExcessDisassembly) && GetKeyBool(setKeyToggleCountBlueprints))
                {
                    key = FunctionIdentifier.Removing_Excess_Disassembly;
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;
                        while (!StateManager(key)) yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelayRemoveExcessDisassembly));
                    }
                }

                if (activeOres > 0 && GetKeyBool(setKeyToggleToggleSortRefineries))
                {
                    key = FunctionIdentifier.Sort_Refineries;
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;
                        while (!StateManager(key)) yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelaySortRefinery));
                    }
                }

                if (typedIndexes[setKeyIndexLimit].Count > 0 && GetKeyBool(setKeyToggleProcessLimits))
                {
                    key = FunctionIdentifier.Processing_Limits;
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;
                        while (!StateManager(key)) yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelayProcessLimits));
                    }
                }

                if (GetKeyBool(setKeyToggleSortItems))
                {
                    key = FunctionIdentifier.Sorting;
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;
                        bool useBottles = GetKeyDouble(setKeyDelayFillingBottles) > 0 && SpanElapsed(fillBottleSpan);
                        if (useBottles)
                            fillingBottles = !fillingBottles;

                        while (!StateManager(key)) yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelaySorting));
                        if (useBottles)
                            fillBottleSpan = SpanDelay(fillingBottles ? 7.5 : GetKeyDouble(setKeyDelayFillingBottles));
                    }
                }

                if (GetKeyBool(setKeyToggleDistribution))
                {
                    key = FunctionIdentifier.Distribution;
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;

                        while (!StateManager(key)) yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelayDistribution));
                    }
                }

                key = FunctionIdentifier.Spreading_Items;
                if (FunctionDelay(key))
                {
                    if (typedIndexes[setKeyIndexRefinery].Count > 1 && GetKeyBool(setKeyToggleSpreadRefieries))
                    {
                        currentMajorFunction = key;
                        while (!BalanceItems(typedIndexes[setKeyIndexRefinery]))
                            yield return stateActive;
                    }
                    if (typedIndexes[setKeyIndexReactor].Count > 1 && GetKeyBool(setKeyToggleSpreadReactors))
                    {
                        currentMajorFunction = key;
                        while (!BalanceItems(typedIndexes[setKeyIndexReactor]))
                            yield return stateActive;
                    }
                    if (typedIndexes[setKeyIndexGun].Count > 1 && GetKeyBool(setKeyToggleSpreadGuns))
                    {
                        currentMajorFunction = key;
                        while (!BalanceItems(typedIndexes[setKeyIndexGun]))
                            yield return stateActive;
                    }
                    if (typedIndexes[setKeyIndexGasGenerators].Count > 1 && GetKeyBool(setKeyToggleSpreadGasGenerators))
                    {
                        currentMajorFunction = key;
                        while (!BalanceItems(typedIndexes[setKeyIndexGasGenerators]))
                            yield return stateActive;
                    }
                    if (typedIndexes[setKeyIndexGravelSifters].Count > 1 && GetKeyBool(setKeyToggleSpreadGravelSifters))
                    {
                        currentMajorFunction = key;
                        while (!BalanceItems(typedIndexes[setKeyIndexGravelSifters]))
                            yield return stateActive;
                    }
                    if (typedIndexes[setKeyIndexParachute].Count > 1 && GetKeyBool(setKeyToggleSpreadParachutes))
                    {
                        currentMajorFunction = key;
                        while (!BalanceItems(typedIndexes[setKeyIndexParachute]))
                            yield return stateActive;
                    }
                    delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelaySpreading));
                }

                if (prioritySystemActivated && GetKeyBool(setKeyToggleSortCargoPriority))
                {
                    key = FunctionIdentifier.Cargo_Priority_Loop;
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;

                        while (!StateManager(key)) yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelaySortCargoPriority));
                    }
                }

                if (GetKeyBool(setKeyToggleOrderCargo))
                {
                    key = FunctionIdentifier.Order_Storage;
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;

                        while (!StateManager(key)) yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelayOrderCargo));
                    }
                }

                if (typedIndexes[setKeyIndexAssemblers].Count > 1 && GetKeyBool(setKeyToggleSpreadBlueprints))
                {
                    key = FunctionIdentifier.Spread_Blueprints;
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;

                        while (!StateManager(key)) yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelaySpreadBlueprints));
                    }
                }

                if (typedIndexes[setKeyIndexLoadout].Count > 0 && GetKeyBool(setKeyToggleDoLoadouts))
                {
                    key = FunctionIdentifier.Loadouts;
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;

                        while (!StateManager(key)) yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelayLoadouts));
                    }
                }

                if (typedIndexes[setKeyIndexLogic].Count > 0 && GetKeyBool(setKeyToggleLogic))
                {
                    key = FunctionIdentifier.Custom_Logic;
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;

                        while (!StateManager(key)) yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelayLogic));
                    }
                }

                if (typedIndexes[setKeyIndexAssemblers].Count > 0 && GetKeyBool(setKeyToggleResetIdleAssemblers))
                {
                    key = FunctionIdentifier.Checking_Idle_Assemblers;
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;

                        while (!StateManager(key)) yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelayIdleAssemblerCheck));
                    }
                }

                if (GetKeyBool(setKeyToggleFindModItems))
                {
                    key = FunctionIdentifier.Find_Mod_Items;
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;
                        while (!StateManager(key))
                            yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelayFindModItems));
                    }
                }

                yield return stateContinue;
            }
        }

        IEnumerator<FunctionState> OutputState()
        {
            List<long> panelIndexes = NewLongList;
            yield return stateContinue;

            while (true)
            {
                panelIndexes.Clear();
                panelIndexes.AddRange(typedIndexes[setKeyIndexPanel]);
                foreach (long index in panelIndexes)
                {
                    if (!IsBlockOk(index)) continue;
                    if (PauseTickRun) yield return stateActive;
                    foreach (PanelMasterClass.PanelDefinition panel in managedBlocks[index].panelDefinitionList.Values)
                        while (!panelMaster.TotalPanelV2(panel)) yield return stateActive;
                }

                yield return stateContinue;
            }
        }

        bool OrderListByPriority(List<long> indexList, bool order)
        {
            if (indexList.Count < 2) return true;

            selfContainedIdentifier = FunctionIdentifier.Order_Blocks_By_Priority;

            if (!IsStateRunning)
            {
                tempOrderByPriorityIndexes = indexList;
                tempOrderByPriority = order;
            }

            return RunStateManager;
        }

        IEnumerator<FunctionState> OrderListByPriorityState()
        {
            List<long> orderedList = NewLongList;
            IOrderedEnumerable<long> sortableObjects;
            yield return stateContinue;

            while (true)
            {
                for (int i = 0; i < tempOrderByPriorityIndexes.Count; i += 0)
                {
                    if (PauseTickRun) yield return stateActive;
                    if (!uniqueIndexSet.Add(tempOrderByPriorityIndexes[i]))
                        tempOrderByPriorityIndexes.RemoveAt(i);
                    else i++;
                }
                uniqueIndexSet.Clear();
                if (tempOrderByPriority && prioritySystemActivated)
                {
                    orderedList.Clear();
                    orderedList.AddRange(tempOrderByPriorityIndexes);
                    sortableObjects = orderedList.OrderByDescending(x => managedBlocks[x].Settings.priority);
                    tempOrderByPriorityIndexes.Clear();
                    foreach (long index in sortableObjects)
                    {
                        if (PauseTickRun) yield return stateActive;
                        tempOrderByPriorityIndexes.Add(index);
                    }
                }
                yield return stateContinue;
            }
        }

        bool SetBlockQuotas(ItemCollection collection)
        {
            selfContainedIdentifier = FunctionIdentifier.Setting_Block_Quotas;

            if (!IsStateRunning)
                tempSetBlockQuotaCollection = collection;

            return RunStateManager;
        }

        IEnumerator<FunctionState> SetBlockQuotaState()
        {
            List<ItemDefinition> itemList = NewItemDefinitionList;
            bool append;
            yield return stateContinue;

            while (true)
            {
                PopulateItemList(itemList);
                append = tempSetBlockQuotaCollection.ItemTypeCount > 0;

                foreach (ItemDefinition def in itemList)
                {
                    if (PauseTickRun) yield return stateActive;

                    def.blockQuota = append ? tempSetBlockQuotaCollection.ItemCount(def.typeID, def.subtypeID, null) : 0;
                }

                yield return stateContinue;
            }
        }

        bool SaveData()
        {
            if (!saving)
                return true;

            currentMajorFunction = selfContainedIdentifier = FunctionIdentifier.Save;

            if (!IsStateRunning)
                SetLastString("Saving Data");

            return RunStateManager;
        }

        IEnumerator<FunctionState> SaveState()
        {
            StringBuilder builder = NewBuilder;
            string currentCategory;
            SortedList<string, SortedList<string, ItemDefinition>> categoryAndNameSorter = new SortedList<string, SortedList<string, ItemDefinition>>();
            Dictionary<string, int> duplicateDictionary = new Dictionary<string, int>();
            List<ItemDefinition> itemList = NewItemDefinitionList;
            yield return stateContinue;

            while (true)
            {
                builder.Clear();
                categoryAndNameSorter.Clear();
                duplicateDictionary.Clear();
                PopulateItemList(itemList);
                foreach (ItemDefinition definition in itemList)
                {
                    if (PauseTickRun) yield return stateActive;

                    currentCategory = Formatted(definition.category);
                    if (!categoryAndNameSorter.ContainsKey(currentCategory))
                        categoryAndNameSorter[currentCategory] = new SortedList<string, ItemDefinition>();

                    categoryAndNameSorter[currentCategory][$"{definition.displayName}{(duplicateDictionary.ContainsKey(definition.displayName) ? $" {duplicateDictionary[definition.displayName]}" : "")}"] = definition;

                    if (!duplicateDictionary.ContainsKey(definition.displayName))
                        duplicateDictionary[definition.displayName] = 1;
                    else
                        duplicateDictionary[definition.displayName] = duplicateDictionary[definition.displayName] + 1;
                }

                SaveSettingDictionaryMulti<ItemDefinition>(categoryAndNameSorter, builder, "Items - ");
                if (PauseTickRun) yield return stateActive;


                if (!reset)
                {
                    SaveSettingDictionaryMulti<bool>(settingDictionaryBools, builder, "Switches - ", true);
                    if (PauseTickRun) yield return stateActive;

                    SaveSettingDictionaryMulti<double>(settingDictionaryDoubles, builder, "Numbers - ", true);
                    if (PauseTickRun) yield return stateActive;

                    SaveSettingDictionarySingle<int>(settingsInts, builder, "", true);
                    if (PauseTickRun) yield return stateActive;

                    SaveSettingDictionaryMulti<string>(settingDictionaryStrings, builder, "Text - ", true);
                    if (PauseTickRun) yield return stateActive;

                    AppendHeader(builder, "Lists");
                    foreach (KeyValuePair<string, List<string>> kvp in settingsListsStrings)
                    {
                        if (PauseTickRun)
                            yield return stateActive;

                        BuilderAppendLine(builder, $"{kvp.Key}=[{String.Join("|", kvp.Value)}]");
                        BuilderAppendLine(builder);
                    }

                    BuilderAppendLine(builder);
                    BuilderAppendLine(builder, $"script={scriptName}");

                    BuilderAppendLine(builder, update ? "version=-1" : $"version={settingVersion}");
                }

                Me.CustomData = builder.ToString().Trim();
                settingBackup = Me.CustomData;
                correctScript = true;
                correctVersion = true;
                saving = false;
                SetLastString("Save Data Complete");
                yield return stateContinue;
            }
        }

        void SaveSettingDictionaryMulti<T>(SortedList<string, SortedList<string, T>> list, StringBuilder builder, string header = "", bool prefixKey = false)
        {
            foreach (KeyValuePair<string, SortedList<string, T>> kvp in list)
            {
                if (header.Length > 0)
                    AppendHeader(builder, $"{header}{kvp.Key}");
                SaveSettingDictionarySingle<T>(kvp.Value, builder, "", prefixKey);
            }
        }

        void SaveSettingDictionarySingle<T>(SortedList<string, T> list, StringBuilder builder, string header = "", bool prefixKey = false)
        {
            if (header.Length > 0)
                AppendHeader(builder, $"{header}");
            foreach (KeyValuePair<string, T> kvp in list)
                BuilderAppendLine(builder, $"{(prefixKey ? $"{kvp.Key}=" : "")}{kvp.Value}");
        }

        bool LoadData()
        {
            if (!loading)
                return true;

            currentMajorFunction = selfContainedIdentifier = FunctionIdentifier.Load;

            if (!IsStateRunning)
                SetLastString("Loading Data");

            return RunStateManager;
        }

        IEnumerator<FunctionState> LoadState()
        {
            string[] settingArray;
            List<string> settingList = NewStringList;
            string excludedDefTemp;
            yield return stateContinue;

            while (true)
            {
                if (TextHasLength(Me.CustomData))
                {
                    correctScript = false;
                    correctVersion = false;
                    itemCategoryList.Clear();
                    yield return stateActive;
                    settingArray = SplitLines(Me.CustomData);
                    settingList.Clear();
                    for (int i = 0; i < settingArray.Length; i++)
                    {
                        if (PauseTickRun) yield return stateActive;

                        if (LeadsString(settingArray[i], "^") && settingList.Count > 0)
                            settingList[settingList.Count - 1] += $"||{settingArray[i].Substring(1).Trim()}";
                        else
                            settingList.Add(settingArray[i].Trim());
                    }
                    foreach (string setting in settingList)
                    {
                        if (PauseTickRun) yield return stateActive;

                        while (!ProcessSetting(setting))
                            yield return stateActive;
                    }
                    clearedSettingLists.Clear();
                    for (int i = 0; i < settingsListsStrings[setKeyExcludedDefinitions].Count; i++)
                    {
                        if (PauseTickRun) yield return stateActive;

                        excludedDefTemp = settingsListsStrings[setKeyExcludedDefinitions][i];
                        if (excludedDefTemp.Contains("/"))
                            settingsListsStrings[setKeyExcludedDefinitions][i] = excludedDefTemp.Substring(excludedDefTemp.IndexOf("/") + 1);
                    }
                    if (!correctVersion || !correctScript)
                        saving = true;
                }
                else saving = true;

                if (PauseTickRun) yield return stateActive;

                SetConstants();
                if (PauseTickRun) yield return stateActive;

                SetPostLoad();
                if (PauseTickRun) yield return stateActive;

                settingBackup = Me.CustomData.Trim();
                SetLastString("Load Data Complete");
                loading = false;
                yield return stateContinue;
            }
        }

        IEnumerator<FunctionState> QueueAssemblyState()
        {
            double queueAmount;
            List<Blueprint> tempBlueprintList = new List<Blueprint>();

            yield return stateContinue;

            while (true)
            {
                tempBlueprintList.AddRange(blueprintList.Values);
                foreach (Blueprint blueprint in tempBlueprintList)
                {
                    if (PauseTickRun) yield return stateActive;

                    queueAmount = AssemblyAmount(blueprint);
                    if (queueAmount > 0)
                        while (!DistributeBlueprint(blueprint, queueAmount, typedIndexes[setKeyIndexAssemblers]))
                            yield return stateActive;
                }
                tempBlueprintList.Clear();
                int queuedIngots = settingsInts[setKeySurvivalKitQueuedIngots];
                if (queuedIngots > 0)
                {
                    Blueprint blueprint = new Blueprint { blueprintID = stoneOreToIngotBasicID };
                    while (!DistributeBlueprint(blueprint, queuedIngots, typedIndexes[setKeyIndexAssemblers], assemblyMode, false))
                        yield return stateActive;
                }
                yield return stateContinue;
            }
        }

        IEnumerator<FunctionState> QueueDisassemblyState()
        {
            double queueAmount;
            List<Blueprint> tempBlueprintList = new List<Blueprint>();

            yield return stateContinue;

            while (true)
            {
                tempBlueprintList.AddRange(blueprintList.Values);
                foreach (Blueprint blueprint in tempBlueprintList)
                {
                    if (PauseTickRun) yield return stateActive;

                    queueAmount = AssemblyAmount(blueprint, true);
                    if (queueAmount > 0)
                        while (!DistributeBlueprint(blueprint, queueAmount, typedIndexes[setKeyIndexAssemblers], disassemblyMode))
                            yield return stateActive;
                }
                tempBlueprintList.Clear();
                yield return stateContinue;
            }
        }

        bool DistributeBlueprint(Blueprint blueprint, double amount, List<long> assemblerIndexList, MyAssemblerMode mode = assemblyMode, bool count = true)
        {
            selfContainedIdentifier = FunctionIdentifier.Distributing_Blueprint;

            if (!IsStateRunning)
            {
                tempDistributeBlueprint = blueprint;
                tempDistributeBlueprintAmount = amount;
                tempDistributeBlueprintIndexes = assemblerIndexList;
                tempDistributeBlueprintMode = mode;
                tempDistributeBlueprintCount = count;
            }

            return RunStateManager;
        }

        IEnumerator<FunctionState> DistributeBlueprintState()
        {
            double amount, multiplier;
            string blocksubtype;
            MyDefinitionId blueprintID;
            IMyTerminalBlock block;
            List<long> indexList = NewLongList;
            yield return stateContinue;

            while (true)
            {
                amount = tempDistributeBlueprintAmount;
                if (tempDistributeBlueprintMode == disassemblyMode)
                {
                    multiplier = tempDistributeBlueprint.multiplier;
                    amount = Math.Floor(tempDistributeBlueprintAmount / multiplier);
                }
                else multiplier = 1;

                if (MyDefinitionId.TryParse(MakeBlueprint(tempDistributeBlueprint), out blueprintID))
                {
                    potentialAssemblerList.Clear();

                    foreach (long index in tempDistributeBlueprintIndexes)
                    {
                        if (PauseTickRun) yield return stateActive;

                        if (!IsBlockOk(index)) continue;

                        if (UsableAssembler(managedBlocks[index], blueprintID, tempDistributeBlueprintMode))
                        {
                            block = managedBlocks[index].block;
                            blocksubtype = BlockSubtype(block);
                            if (!potentialAssemblerList.ContainsKey(blocksubtype))
                                potentialAssemblerList[blocksubtype] = new List<PotentialAssembler>();

                            potentialAssemblerList[blocksubtype].Add(new PotentialAssembler { index = index, empty = ((IMyAssembler)block).IsQueueEmpty, specific = managedBlocks[index].Settings.GetOption(setKeyBlockUniqueBlueprinteOnlyKey) });
                        }
                    }

                    indexList.Clear();
                    foreach (KeyValuePair<string, List<PotentialAssembler>> kvpA in potentialAssemblerList)
                    {
                        if (PauseTickRun) yield return stateActive;

                        foreach (PotentialAssembler potentialAssembler in kvpA.Value)
                        {
                            if (PauseTickRun) yield return stateActive;

                            if (!potentialAssembler.specific || potentialAssemblerList.Count == 1)
                            {
                                if (tempDistributeBlueprintMode == disassemblyMode && assemblyNeededByMachine.Contains(kvpA.Key) && indexList.Count > 0)
                                    continue;

                                if (potentialAssembler.empty)
                                    indexList.Insert(0, potentialAssembler.index);
                                else
                                    indexList.Add(potentialAssembler.index);
                            }
                        }
                    }
                    potentialAssemblerList.Clear();

                    if (indexList.Count > 0)
                    {
                        int splitAmount, excessAmount, currentAmount;
                        splitAmount = Math.DivRem((int)amount, indexList.Count, out excessAmount);
                        if (tempDistributeBlueprint.blueprintID == stoneOreToIngotBasicID)
                        {
                            excessAmount = 0;
                            splitAmount = (int)amount;
                        }

                        for (int i = 0; i < indexList.Count && i < amount; i++)
                        {
                            currentAmount = splitAmount;
                            if (i < excessAmount)
                                currentAmount++;

                            if (currentAmount > 0)
                                while (!InsertBlueprint(blueprintID, currentAmount * multiplier, managedBlocks[indexList[i]], tempDistributeBlueprintMode, tempDistributeBlueprintCount))
                                    yield return stateActive;
                        }
                    }
                }

                yield return stateContinue;
            }
        }

        bool InsertBlueprint(MyDefinitionId blueprintID, double amount, BlockDefinition managedBlock, MyAssemblerMode mode, bool count = true)
        {
            selfContainedIdentifier = FunctionIdentifier.Inserting_Blueprint;

            if (!IsStateRunning)
            {
                tempInsertBlueprintID = blueprintID;
                tempInsertBlueprintAmount = amount;
                tempInsertBlueprintBlockDefinition = managedBlock;
                tempInsertBlueprintMode = mode;
                tempInsertBlueprintCount = count;
            }

            return RunStateManager;
        }

        IEnumerator<FunctionState> InsertBlueprintState()
        {
            double currentAmount, currentPercent, nextPercent;
            bool contains, inserted;
            IMyAssembler assembler;
            yield return stateContinue;

            while (true)
            {
                currentAmount = Math.Floor(tempInsertBlueprintAmount);
                assembler = (IMyAssembler)tempInsertBlueprintBlockDefinition.block;
                assembler.Mode = tempInsertBlueprintMode;
                if (assembler.Mode != tempInsertBlueprintMode)
                    assembler.Mode = tempInsertBlueprintMode;

                if (assembler.Mode == tempInsertBlueprintMode)
                {
                    if (assembler.IsQueueEmpty)
                        assembler.AddQueueItem(tempInsertBlueprintID, currentAmount);
                    else
                    {
                        inserted = false;
                        currentPercent = tempInsertBlueprintMode == assemblyMode ? BlueprintPercentage(tempInsertBlueprintID) : 0;

                        blueprintListMain.Clear();
                        assembler.GetQueue(blueprintListMain);
                        contains = false;
                        for (int i = 0; !contains && i < blueprintListMain.Count; i++)
                        {
                            if (PauseTickRun) yield return stateActive;

                            if (BlueprintSubtype(blueprintListMain[i]) == tempInsertBlueprintID.SubtypeName)
                            {
                                contains = true;
                                if (tempInsertBlueprintID.SubtypeName == stoneOreToIngotBasicID)
                                    currentAmount = Math.Floor(currentAmount - (double)blueprintListMain[i].Amount);
                            }
                        }
                        if (currentAmount > 0 && (contains || (tempInsertBlueprintMode == assemblyMode && !tempInsertBlueprintBlockDefinition.Settings.GetOption(setKeyBlockNoSortingKey))))
                            for (int i = 0; i < blueprintListMain.Count; i++)
                            {
                                if (PauseTickRun) yield return stateActive;

                                if (!contains && tempInsertBlueprintMode == assemblyMode)
                                    nextPercent = BlueprintPercentage(blueprintListMain[i].BlueprintId);
                                else
                                    nextPercent = 0;

                                if ((!contains && currentPercent <= nextPercent) || BlueprintSubtype(blueprintListMain[i]) == tempInsertBlueprintID.SubtypeName)
                                {
                                    assembler.InsertQueueItem(i, tempInsertBlueprintID, currentAmount);
                                    inserted = true;
                                    break;
                                }
                            }

                        if (!inserted && currentAmount > 0)
                            assembler.AddQueueItem(tempInsertBlueprintID, currentAmount);
                    }
                    if (tempInsertBlueprintCount)
                        AddBlueprintAmount(tempInsertBlueprintID.SubtypeName, tempInsertBlueprintMode == assemblyMode, currentAmount, true);
                }

                yield return stateContinue;
            }
        }

        IEnumerator<FunctionState> RemoveExcessAssemblyState()
        {
            ItemDefinition definition;
            double excessQueued;
            yield return stateContinue;

            while (true)
            {
                foreach (KeyValuePair<string, Blueprint> kvp in blueprintList)
                {
                    if (PauseTickRun) yield return stateActive;

                    if (GetDefinition(out definition, $"{kvp.Value.typeID}/{kvp.Value.subtypeID}"))
                    {
                        excessQueued = Math.Floor(definition.currentExcessAssembly);
                        if (excessQueued > 0)
                            while (!RemoveBlueprint(kvp.Value, excessQueued))
                                yield return stateActive;
                    }
                }
                yield return stateContinue;
            }
        }

        bool RemoveBlueprint(Blueprint blueprint, double amount, MyAssemblerMode mode = assemblyMode)
        {
            selfContainedIdentifier = FunctionIdentifier.Removing_Blueprint;

            if (!IsStateRunning)
            {
                tempRemoveBlueprint = blueprint;
                tempRemoveBlueprintAmount = amount;
                tempRemoveBlueprintMode = mode;
            }

            return RunStateManager;
        }

        IEnumerator<FunctionState> RemoveBlueprintState()
        {
            double removalAmount, toBeRemovedAmount;
            IMyAssembler assembler;
            yield return stateContinue;

            while (true)
            {
                toBeRemovedAmount = tempRemoveBlueprintAmount;

                foreach (long index in typedIndexes[setKeyIndexAssemblers])
                {
                    if (toBeRemovedAmount <= 0) break;
                    if (PauseTickRun) yield return stateActive;
                    if (!IsBlockOk(index)) continue;

                    assembler = (IMyAssembler)managedBlocks[index].block;

                    if (assembler.Mode == tempRemoveBlueprintMode && !assembler.IsQueueEmpty)
                    {
                        blueprintListMain.Clear();
                        assembler.GetQueue(blueprintListMain);
                        for (int x = blueprintListMain.Count - 1; x >= 0 && toBeRemovedAmount > 0; x--)
                        {
                            if (PauseTickRun) yield return stateActive;

                            if (BlueprintSubtype(blueprintListMain[x]) == tempRemoveBlueprint.blueprintID)
                            {
                                removalAmount = (double)blueprintListMain[x].Amount;
                                if (removalAmount > toBeRemovedAmount)
                                    removalAmount = toBeRemovedAmount;

                                assembler.RemoveQueueItem(x, (MyFixedPoint)removalAmount);
                                AddBlueprintAmount(tempRemoveBlueprint.blueprintID, tempRemoveBlueprintMode == assemblyMode, -removalAmount, true);
                                toBeRemovedAmount -= removalAmount;
                            }
                        }
                    }
                }

                yield return stateContinue;
            }
        }

        IEnumerator<FunctionState> RemoveExcessDisassemblyState()
        {
            ItemDefinition definition;
            double excessQueued;
            yield return stateContinue;

            while (true)
            {
                foreach (KeyValuePair<string, Blueprint> kvp in blueprintList)
                {
                    if (PauseTickRun) yield return stateActive;

                    if (GetDefinition(out definition, $"{kvp.Value.typeID}/{kvp.Value.subtypeID}"))
                    {
                        excessQueued = Math.Floor(definition.currentExcessDisassembly);
                        if (excessQueued > 0)
                            while (!RemoveBlueprint(kvp.Value, excessQueued, disassemblyMode))
                                yield return stateActive;
                    }
                }
                yield return stateContinue;
            }
        }

        IEnumerator<FunctionState> SortCargoPriorityState()
        {
            yield return stateContinue;

            while (true)
            {
                foreach (KeyValuePair<string, LongListPlus> kvp in indexesStorageLists)
                {
                    if (PauseTickRun) yield return stateActive;

                    if (kvp.Value.Count > 1 && priorityCategories.Contains(kvp.Key))
                        while (!SortCargoList(kvp.Value))
                            yield return stateActive;
                }
                yield return stateContinue;
            }
        }

        bool SortCargoList(List<long> indexList)
        {
            selfContainedIdentifier = FunctionIdentifier.Sorting_Cargo_Priority;

            if (!IsStateRunning)
                tempSortCargoListIndexes = indexList;

            return RunStateManager;
        }

        IEnumerator<FunctionState> SortCargoListState()
        {
            int storageStartIndex;
            yield return stateContinue;

            while (true)
            {
                storageStartIndex = 0;
                while (storageStartIndex < tempSortCargoListIndexes.Count)
                {
                    if (PauseTickRun) yield return stateActive;

                    if (CurrentVolumePercentage(tempSortCargoListIndexes[storageStartIndex]) >= 0.985)
                        storageStartIndex++;
                    else
                        break;
                }

                for (int i = storageStartIndex + 1; i < tempSortCargoListIndexes.Count; i++)
                {
                    if (PauseTickRun) yield return stateActive;

                    if (!IsBlockOk(tempSortCargoListIndexes[i])) continue;

                    if (CurrentVolumePercentage(tempSortCargoListIndexes[storageStartIndex]) >= 0.985)
                        storageStartIndex++;

                    if (i > storageStartIndex)
                    {
                        mainFunctionItemList.Clear();
                        mainBlockDefinition = managedBlocks[tempSortCargoListIndexes[i]];
                        mainBlockDefinition.Input.GetItems(mainFunctionItemList);

                        for (int x = 0; x < mainFunctionItemList.Count; x += 0)
                        {
                            if (PauseTickRun) yield return stateActive;

                            if (mainBlockDefinition.Settings.loadout.ItemCount(mainFunctionItemList[x], mainBlockDefinition.block) > 0)
                                mainFunctionItemList.RemoveAtFast(x);
                            else
                                x++;
                        }

                        while (!PutInStorage(mainFunctionItemList, tempSortCargoListIndexes[i], 0, -1, i, storageStartIndex))
                            yield return stateActive;
                    }
                }

                yield return stateContinue;
            }
        }

        IEnumerator<FunctionState> SortBlueprintState()
        {
            IMyAssembler assembler;
            int tempAmount;
            double blueprintPercent, minPercent = 0, maxPercent = 0;
            BlockDefinition managedBlock;
            yield return stateContinue;

            while (true)
            {
                sortableListMain.Clear();
                foreach (long index in typedIndexes[setKeyIndexAssemblers])
                {
                    if (PauseTickRun) yield return stateActive;
                    managedBlock = managedBlocks[index];
                    if (!IsBlockOk(index) || managedBlock.Settings.GetOption(setKeyBlockNoSortingKey))
                        continue;

                    assembler = (IMyAssembler)managedBlock.block;
                    if (!assembler.IsQueueEmpty && assembler.Enabled && assembler.Mode == assemblyMode)
                    {
                        blueprintListMain.Clear();
                        assembler.GetQueue(blueprintListMain);
                        if (blueprintListMain.Count > 1)
                        {
                            sortableListMain.Clear();
                            for (int x = 0; x < blueprintListMain.Count; x++)
                            {
                                if (PauseTickRun) yield return stateActive;

                                tempAmount = (int)blueprintListMain[x].Amount;
                                if (x == 0 && assembler.CurrentProgress >= 0.1f)
                                {
                                    if (tempAmount > 10)
                                        tempAmount -= 3;
                                    else
                                        tempAmount = 0;
                                }
                                if (tempAmount > 0)
                                {
                                    blueprintPercent = BlueprintPercentage(blueprintListMain[x].BlueprintId);

                                    minPercent = x == 0 ? blueprintPercent : Math.Min(minPercent, blueprintPercent);

                                    maxPercent = x == 0 ? blueprintPercent : Math.Max(maxPercent, blueprintPercent);

                                    sortableListMain.Add(new SortableObject { amount = blueprintPercent, key = BlueprintSubtype(blueprintListMain[x]) });
                                }
                            }
                            if (minPercent == maxPercent)
                                continue;

                            sortableListMain = sortableListMain.OrderBy(x => x.amount).ToList();

                            for (int s = 0; s < sortableListMain.Count; s++)
                            {
                                if (PauseTickRun) yield return stateActive;
                                for (int a = 0; a < blueprintListMain.Count; a++)
                                {
                                    if (PauseTickRun) yield return stateActive;
                                    if (BlueprintSubtype(blueprintListMain[a]) == sortableListMain[s].key)
                                    {
                                        if (a != s)
                                            assembler.MoveQueueItemRequest(blueprintListMain[a].ItemId, s);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                yield return stateContinue;
            }
        }

        IEnumerator<FunctionState> SpreadBlueprintStateV2()
        {
            SortedList<MyAssemblerMode, SortedList<string, BlueprintSpreadInformation>> blueprintInformation = new SortedList<MyAssemblerMode, SortedList<string, BlueprintSpreadInformation>>();
            List<MyProductionItem> currentProductionList = NewProductionList;
            IMyAssembler currentAssembler;
            string key;
            MyAssemblerMode currentMode;
            double moveAmount, averageAmount, minimalRange = balanceRange;
            List<long> indexList = NewLongList;
            long currentEntityID;
            yield return stateContinue;

            while (true)
            {
                //Count blueprints, both total counts and individual counts
                foreach (long originIndex in typedIndexes[setKeyIndexAssemblers])
                {
                    if (PauseTickRun) yield return stateActive;

                    if (!IsBlockOk(originIndex)) continue;

                    currentAssembler = (IMyAssembler)managedBlocks[originIndex].block;

                    if (currentAssembler.IsQueueEmpty) continue;

                    currentAssembler.GetQueue(currentProductionList);
                    currentEntityID = currentAssembler.EntityId;
                    currentMode = currentAssembler.Mode;

                    foreach (MyProductionItem productionItem in currentProductionList)
                    {
                        if (PauseTickRun) yield return stateActive;
                        key = BlueprintSubtype(productionItem);

                        if (!blueprintInformation.ContainsKey(currentMode))
                            blueprintInformation[currentMode] = new SortedList<string, BlueprintSpreadInformation>();

                        if (!blueprintInformation[currentMode].ContainsKey(key))
                            blueprintInformation[currentMode][key] = new BlueprintSpreadInformation();

                        blueprintInformation[currentMode][key].AddCount(currentEntityID, (double)productionItem.Amount);
                    }
                    currentProductionList.Clear();
                }


                //Determine which assemblers can use the blueprints known to be in queue
                foreach (long originIndex in typedIndexes[setKeyIndexAssemblers])
                {
                    if (PauseTickRun) yield return stateActive;

                    if (!IsBlockOk(originIndex))
                        continue;

                    currentAssembler = (IMyAssembler)managedBlocks[originIndex].block;
                    currentEntityID = currentAssembler.EntityId;

                    foreach (KeyValuePair<MyAssemblerMode, SortedList<string, BlueprintSpreadInformation>> modePair in blueprintInformation)
                        foreach (KeyValuePair<string, BlueprintSpreadInformation> blueprintPair in modePair.Value)
                        {
                            if (PauseTickRun) yield return stateActive;
                            if (UsableAssembler(managedBlocks[originIndex], MyDefinitionId.Parse($"{blueprintPrefix}/{blueprintPair.Key}"), modePair.Key))
                                blueprintPair.Value.acceptingIndexList.Add(originIndex);
                        }
                }


                //Spread blueprints from each assembler
                foreach (long index in typedIndexes[setKeyIndexAssemblers])
                {
                    if (PauseTickRun) yield return stateActive;

                    if (!IsBlockOk(index)) continue;

                    currentAssembler = (IMyAssembler)managedBlocks[index].block;

                    if (currentAssembler.IsQueueEmpty) continue;

                    currentAssembler.GetQueue(currentProductionList);
                    currentMode = currentAssembler.Mode;

                    for (int i = 0; i < currentProductionList.Count; i++)
                    {
                        if (PauseTickRun) yield return stateActive;
                        key = BlueprintSubtype(currentProductionList[i]);
                        if (!blueprintInformation.ContainsKey(currentMode) || !blueprintInformation[currentMode].ContainsKey(key) || blueprintInformation[currentMode][key].acceptingIndexList.Count == 1)
                            continue;

                        averageAmount = Math.Floor(blueprintInformation[currentMode][key].totalCount / (double)blueprintInformation[currentMode][key].acceptingIndexList.Count);
                        if (averageAmount <= 0)
                            continue;
                        minimalRange = Math.Max(minimalRange, 1.0 / averageAmount);
                        if (OverRange((double)currentProductionList[i].Amount, averageAmount, minimalRange))
                        {
                            moveAmount = Math.Floor((double)currentProductionList[i].Amount - averageAmount);
                            if (moveAmount <= 0)
                                continue;
                            currentAssembler.RemoveQueueItem(i, moveAmount);
                            indexList.AddRange(blueprintInformation[currentMode][key].acceptingIndexList);
                            for (int z = 0; z < indexList.Count; z += 0)
                            {
                                if (PauseTickRun) yield return stateActive;
                                if (blueprintInformation[currentMode][key].individualCounts.ContainsKey(indexList[z]) && !UnderRange(blueprintInformation[currentMode][key].individualCounts[indexList[z]], averageAmount, minimalRange))
                                    indexList.RemoveAt(z);
                                else
                                    z++;
                            }
                            if (indexList.Count > 0)
                                while (!DistributeBlueprint(new Blueprint { amount = moveAmount, blueprintID = BlueprintSubtype(currentProductionList[i]) }, moveAmount, indexList, currentMode, false)) yield return stateActive;
                            indexList.Clear();
                        }
                    }
                    currentProductionList.Clear();
                }

                blueprintInformation.Clear();

                yield return stateContinue;
            }
        }

        bool BalanceItems(List<long> indexList)
        {
            selfContainedIdentifier = FunctionIdentifier.Spreading_Items;

            if (!IsStateRunning)
                tempBalanceItemIndexes = indexList;

            return RunStateManager;
        }

        IEnumerator<FunctionState> BalanceState2()
        {
            SortedList<MyItemType, SortedList<long, double>> countList = new SortedList<MyItemType, SortedList<long, double>>();
            yield return stateContinue;

            while (true)
            {
                countList.Clear();
                // Initial Count
                foreach (long index in tempBalanceItemIndexes)
                {
                    if (PauseTickRun) yield return stateActive;
                    if (!IsBlockOk(index) || managedBlocks[index].Settings.GetOption(setKeyBlockNoSpreadKey)) continue;

                    mainFunctionItemList.Clear();
                    managedBlocks[index].Input.GetItems(mainFunctionItemList);

                    foreach (MyInventoryItem item in mainFunctionItemList)
                    {
                        if (PauseTickRun) yield return stateActive;

                        try
                        {
                            if (!countList.ContainsKey(item.Type)) countList[item.Type] = new SortedList<long, double>();
                        }
                        catch { continue; }

                        if (!countList[item.Type].ContainsKey(index)) countList[item.Type][index] = (double)item.Amount;
                        else countList[item.Type][index] += (double)item.Amount;
                    }
                }
                // Include Blocks with 0
                foreach (KeyValuePair<MyItemType, SortedList<long, double>> kvp in countList)
                    foreach (long index in tempBalanceItemIndexes)
                    {
                        if (PauseTickRun) yield return stateActive;

                        if (IsBlockOk(index) && !managedBlocks[index].Settings.GetOption(setKeyBlockNoSpreadKey) && !kvp.Value.ContainsKey(index) && AcceptsItem(managedBlocks[index], kvp.Key.TypeId, kvp.Key.SubtypeId)) kvp.Value[index] = 0;
                    }
                // Loop through each item
                foreach (KeyValuePair<MyItemType, SortedList<long, double>> kvpA in countList)
                {
                    double min = double.MaxValue, max = double.MinValue, average = 0, excess, transferAmount;
                    // List and sort by count in descending order
                    // Note mininum, maximum, and total values
                    foreach (KeyValuePair<long, double> kvpB in kvpA.Value)
                    {
                        if (PauseTickRun) yield return stateActive;

                        sortableListMain.Add(new SortableObject { amount = kvpB.Value, numberLong = kvpB.Key });
                        min = Math.Min(min, kvpB.Value);
                        max = Math.Max(max, kvpB.Value);
                        average += kvpB.Value;
                    }
                    sortableListMain = sortableListMain.OrderByDescending(b => b.amount).ToList();

                    // Convert the total value to the average desired among all blocks
                    average /= (double)sortableListMain.Count;

                    int averageIndex = -1, // Last index of a count above average
                        belowAverageCount = 0; // Count of items below average

                    // If a block is below average and another is above average
                    if (!FractionalItem(kvpA.Key.TypeId, kvpA.Key.SubtypeId) && max - max >= 2 || FractionalItem(kvpA.Key.TypeId, kvpA.Key.SubtypeId) && (min < average * 0.95 && max > average * 1.05 || min <= average * 0.25))
                    {
                        // Remove blocks close enough to average
                        for (int i = 0; i < sortableListMain.Count; i += 0)
                        {
                            if (PauseTickRun) yield return stateActive;
                            if (sortableListMain[i].amount >= average * 0.95 && sortableListMain[i].amount <= average * 1.001)
                                sortableListMain.RemoveAt(i);
                            else
                            {
                                if (sortableListMain[i].amount > average)
                                {
                                    averageIndex = i;
                                    i++;
                                }
                                else
                                {
                                    sortableListAlternate.Add(sortableListMain[i]);
                                    sortableListMain.RemoveAt(i);
                                }
                            }
                        }
                        if (sortableListMain.Count > 0 && sortableListAlternate.Count > 0)
                            for (int i = 0; i < sortableListMain.Count; i++)
                            {
                                if (PauseTickRun) yield return stateActive;
                                if (!IsBlockOk(sortableListMain[i].numberLong)) continue;

                                mainFunctionItemList.Clear();
                                managedBlocks[sortableListMain[i].numberLong].Input.GetItems(mainFunctionItemList, b => b.Type == kvpA.Key);
                                sortableListMain[i].amount = mainFunctionItemList.Count > 0 ? (double)mainFunctionItemList[0].Amount : 0;
                                excess = mainFunctionItemList.Count > 0 ? (double)mainFunctionItemList[0].Amount - average : 0;
                                if (excess > 0.0)
                                    for (int x = sortableListAlternate.Count - 1; x >= 0; x--)
                                    {
                                        if (PauseTickRun) yield return stateActive;
                                        if (!IsBlockOk(sortableListAlternate[x].numberLong)) continue;
                                        transferAmount = average - sortableListAlternate[x].amount;
                                        if (transferAmount > excess) transferAmount = excess;
                                        if (!FractionalItem(kvpA.Key.TypeId, kvpA.Key.SubtypeId))
                                        {
                                            transferAmount = Math.Floor(transferAmount);
                                            if (transferAmount < 1) break;
                                        }
                                        if (transferAmount > 0.0 && managedBlocks[sortableListAlternate[x].numberLong].Input.TransferItemFrom(managedBlocks[sortableListMain[i].numberLong].Input, mainFunctionItemList[0], (MyFixedPoint)transferAmount))
                                        {
                                            sortableListAlternate[x].amount += transferAmount;
                                            sortableListMain[i].amount -= transferAmount;
                                            excess -= transferAmount;
                                            if (sortableListAlternate[x].amount >= average)
                                            {
                                                sortableListAlternate.RemoveAt(x);
                                                belowAverageCount--;
                                            }
                                        }
                                    }
                            }
                    }
                    sortableListMain.Clear();
                    sortableListAlternate.Clear();
                }

                yield return stateContinue;
            }
        }

        bool CountItemsInList(ItemCollection count, List<long> indexes, string typeID = "", string subtypeID = "")
        {
            selfContainedIdentifier = FunctionIdentifier.Counting_Listed_Items;

            if (!IsStateRunning)
            {
                tempCountItemsInListCollection = count;
                tempCountItemsInListIndexes = indexes;
                tempCountItemsInListTypeID = typeID;
                tempCountItemsInListSubtypeID = subtypeID;
            }

            return RunStateManager;
        }

        IEnumerator<FunctionState> CountListState()
        {
            yield return stateContinue;

            while (true)
            {
                foreach (long index in tempCountItemsInListIndexes)
                {
                    if (PauseTickRun) yield return stateActive;

                    if (!IsBlockOk(index)) continue;

                    countByListA.Clear();
                    managedBlocks[index].Input.GetItems(countByListA);
                    for (int x = 0; x < countByListA.Count; x++)
                    {
                        if (PauseTickRun) yield return stateActive;

                        if ((!TextHasLength(tempCountItemsInListTypeID) || countByListA[x].Type.TypeId == tempCountItemsInListTypeID) && (!TextHasLength(tempCountItemsInListSubtypeID) || countByListA[x].Type.SubtypeId == tempCountItemsInListSubtypeID))
                            tempCountItemsInListCollection.AddItem(countByListA[x]);
                    }
                }
                yield return stateContinue;
            }
        }

        IEnumerator<FunctionState> DistributeState()
        {
            string subtypeID;
            SortedList<long, double> acceptingIndexes = NewSortedListLongDouble;
            List<long> tempIndexes = NewLongList;
            yield return stateContinue;

            while (true)
            {
                foreach (long index in typedIndexes[setKeyIndexStorage])
                {
                    if (PauseTickRun) yield return stateActive;

                    if (!IsBlockOk(index)) continue;

                    mainBlockDefinition = managedBlocks[index];
                    IMyInventory inventory = mainBlockDefinition.Input;
                    if (inventory.ItemCount == 0)
                        continue;

                    mainFunctionItemList.Clear();
                    inventory.GetItems(mainFunctionItemList);

                    for (int x = 0; x < mainFunctionItemList.Count; x++)
                    {
                        if (PauseTickRun) yield return stateActive;

                        if (Distributable(mainFunctionItemList[x], mainBlockDefinition))
                        {
                            subtypeID = mainFunctionItemList[x].Type.SubtypeId;
                            acceptingIndexes.Clear();
                            tempIndexes.Clear();

                            if (IsOre(mainFunctionItemList[x]))
                            {
                                if (RefinedOre(mainFunctionItemList[x]))
                                    tempIndexes.AddRange(typedIndexes[setKeyIndexRefinery]);
                            }
                            else if (IsAmmo(mainFunctionItemList[x]))
                                tempIndexes.AddRange(typedIndexes[setKeyIndexGun]);
                            else if (IsIngot(mainFunctionItemList[x]) && subtypeID == stoneType)
                                tempIndexes.AddRange(typedIndexes[setKeyIndexGravelSifters]);
                            else if (IsComponent(mainFunctionItemList[x]) && subtypeID == canvasType)
                                tempIndexes.AddRange(typedIndexes[setKeyIndexParachute]);

                            if (IsFuel(mainFunctionItemList[x]))
                                tempIndexes.AddRange(typedIndexes[setKeyIndexReactor]);

                            if (IsGas(mainFunctionItemList[x]))
                                tempIndexes.AddRange(typedIndexes[setKeyIndexGasGenerators]);

                            if (tempIndexes.Count > 0)
                            {
                                foreach (long subIndex in tempIndexes)
                                {
                                    if (PauseTickRun) yield return stateActive;
                                    if (!AcceptsItem(managedBlocks[subIndex], mainFunctionItemList[x]))
                                        continue;
                                    acceptingIndexes[subIndex] = -1;
                                }

                                while (!DistributeItem(mainFunctionItemList[x], mainBlockDefinition, acceptingIndexes))
                                    yield return stateActive;
                            }
                        }
                    }
                }

                acceptingIndexes.Clear();
                yield return stateContinue;
            }
        }

        bool DistributeItem(MyInventoryItem item, BlockDefinition block, SortedList<long, double> acceptingIndexes, double specifixMax = -1)
        {
            selfContainedIdentifier = FunctionIdentifier.Distributing_Item;

            if (!IsStateRunning)
            {
                tempDistributeItem = item;
                tempDistributeItemBlockDefinition = block;
                tempDistributeItemIndexes = acceptingIndexes;
                tempDistributeItemMax = specifixMax;
            }

            return RunStateManager;
        }

        IEnumerator<FunctionState> DistributeItemState()
        {
            SortedList<long, double> tempSortedIndexList = NewSortedListLongDouble;
            List<long> tempIndexList = NewLongList;
            double contained, totalAmount, splitAmount, originalSplitAmount,
                    maxAmount, balanceRange, balancedShare, itemLimit, remainder;
            bool fractional, foundLimit;
            long key;
            int indexCount;
            yield return stateContinue;

            while (true)
            {
                tempSortedIndexList.Clear();
                tempIndexList.Clear();
                foreach (KeyValuePair<long, double> kvp in tempDistributeItemIndexes)
                    tempSortedIndexList[kvp.Key] = kvp.Value;

                remainder = 0;

                for (int i = 0; i < tempSortedIndexList.Count; i += 0)
                {
                    if (PauseTickRun) yield return stateActive;

                    if (IsBlockOk(tempSortedIndexList.Keys[i]) && CurrentVolumePercentage(tempSortedIndexList.Keys[i]) < 0.99 && AcceptsItem(managedBlocks[tempSortedIndexList.Keys[i]], tempDistributeItem))
                    {
                        tempIndexList.Add(tempSortedIndexList.Keys[i]);
                        i++;
                    }
                    else
                        tempSortedIndexList.Remove(tempSortedIndexList.Keys[i]);
                }

                if (tempSortedIndexList.Count > 0)
                {
                    itemCollectionMain.Clear();

                    while (!CountItemsInList(itemCollectionMain, tempIndexList, tempDistributeItem.Type.TypeId, tempDistributeItem.Type.SubtypeId))
                        yield return stateActive;

                    tempIndexList.Clear();

                    totalAmount = (double)tempDistributeItem.Amount;
                    balanceRange = GetKeyDouble(setKeyBalanceRange);
                    if (tempDistributeItemMax > 0 && totalAmount > tempDistributeItemMax)
                        totalAmount = tempDistributeItemMax;

                    fractional = FractionalItem(tempDistributeItem);

                    itemCollectionMain.AddItem(tempDistributeItem.Type.TypeId, tempDistributeItem.Type.SubtypeId, new VariableItemCount(totalAmount));

                    if (itemCollectionMain.ItemTypeCount > 0 && tempSortedIndexList.Count > 0)
                    {
                        balancedShare = itemCollectionMain.ItemCount(tempDistributeItem) / tempSortedIndexList.Count;
                        for (int i = 0; i < tempSortedIndexList.Count; i += 0)
                        {
                            if (PauseTickRun) yield return stateActive;

                            contained = tempSortedIndexList.Values[i];
                            if (contained == -1)
                            {
                                contained = 0;
                                while (!AmountContained(ref contained, tempDistributeItem, managedBlocks[tempSortedIndexList.Keys[i]].Input))
                                    yield return stateActive;

                                tempSortedIndexList[tempSortedIndexList.Keys[i]] = contained;
                            }
                            if (contained > balancedShare + (balancedShare * balanceRange))
                                tempSortedIndexList.RemoveAt(i);
                            else
                                i++;
                        }
                    }

                    if (tempSortedIndexList.Count > 0)
                    {
                        indexCount = 0;
                        foreach (KeyValuePair<long, double> kvp in tempSortedIndexList)
                        {
                            maxAmount = DefaultMax(tempDistributeItem, managedBlocks[kvp.Key]);
                            itemLimit = managedBlocks[kvp.Key].Settings.limits.ItemCount(out foundLimit, tempDistributeItem, managedBlocks[kvp.Key].block);
                            if (foundLimit) maxAmount = itemLimit;

                            foundLimit = maxAmount < double.MaxValue;

                            splitAmount = totalAmount / ((double)tempSortedIndexList.Count - indexCount);
                            contained = kvp.Value;
                            key = kvp.Key;
                            if (PauseTickRun) yield return stateActive;

                            if (contained == -1)
                            {
                                contained = 0;
                                if (foundLimit)
                                    while (!AmountContained(ref contained, tempDistributeItem, managedBlocks[kvp.Key].Input))
                                        yield return stateActive;
                            }
                            if (splitAmount + contained > maxAmount)
                                splitAmount = maxAmount - contained;

                            if (!fractional)
                            {
                                remainder += splitAmount - Math.Floor(splitAmount);
                                splitAmount = Math.Floor(splitAmount);
                                if (remainder >= 1 && (contained + splitAmount + 1 <= maxAmount))
                                {
                                    splitAmount++;
                                    remainder--;
                                }
                            }
                            if (indexCount + 1 == tempSortedIndexList.Count && splitAmount + remainder <= maxAmount)
                                splitAmount += remainder;

                            originalSplitAmount = splitAmount;
                            while (!Transfer(ref splitAmount, tempDistributeItemBlockDefinition.Input, managedBlocks[kvp.Key], tempDistributeItem))
                                yield return stateActive;

                            if (splitAmount > 0)
                                totalAmount -= originalSplitAmount - splitAmount;
                            indexCount++;
                        }
                    }
                }
                yield return stateContinue;
            }
        }

        bool AmountContained(ref double amount, MyInventoryItem item, IMyInventory inventory)
        {
            return AmountContained(ref amount, item.Type.TypeId, item.Type.SubtypeId, inventory);
        }

        bool AmountContained(ref double amount, string typeID, string subtypeID, IMyInventory inventory)
        {
            selfContainedIdentifier = FunctionIdentifier.Counting_Item_In_Inventory;

            if (!IsStateRunning)
            {
                tempAmountContainedTypeID = typeID;
                tempAmountContainedSubtypeID = subtypeID;
                tempAmountContainedInventory = inventory;
            }

            if (RunStateManager)
            {
                amount = countedAmount;
                countedAmount = 0;
                return true;
            }

            return false;
        }

        IEnumerator<FunctionState> AmountContainedState()
        {
            yield return stateContinue;

            while (true)
            {
                amountContainedListA.Clear();
                tempAmountContainedInventory.GetItems(amountContainedListA);
                for (int i = 0; i < amountContainedListA.Count; i++)
                {
                    if (PauseTickRun) yield return stateActive;

                    if (amountContainedListA[i].Type.TypeId == tempAmountContainedTypeID && amountContainedListA[i].Type.SubtypeId == tempAmountContainedSubtypeID)
                        countedAmount += (double)amountContainedListA[i].Amount;
                }
                yield return stateContinue;
            }
        }

        IEnumerator<FunctionState> ProcessLimitsState()
        {
            double limit, excess;
            yield return stateContinue;

            while (true)
            {
                limit = 0;
                foreach (long index in typedIndexes[setKeyIndexLimit])
                {
                    if (PauseTickRun) yield return stateActive;

                    if (!IsBlockOk(index)) continue;

                    mainBlockDefinition = managedBlocks[index];

                    mainFunctionItemList.Clear();
                    mainBlockDefinition.Input.GetItems(mainFunctionItemList);
                    if (PauseTickRun) yield return stateActive;

                    for (int x = 0; x < mainFunctionItemList.Count; x++)
                    {
                        if (PauseTickRun) yield return stateActive;

                        if (GetSetLimit(mainBlockDefinition, ref limit, mainFunctionItemList[x]))
                        {
                            excess = (double)mainFunctionItemList[x].Amount - limit;
                            if (excess > 0)
                                while (!PutInStorage(new List<MyInventoryItem> { mainFunctionItemList[x] }, index, 0, excess)) yield return stateActive;
                        }
                    }
                }

                yield return stateContinue;
            }
        }

        IEnumerator<FunctionState> SortState()
        {
            IMyTerminalBlock block;
            yield return stateContinue;

            while (true)
            {
                foreach (long index in typedIndexes[setKeyIndexSortable])
                {
                    if (PauseTickRun) yield return stateActive;
                    if (!IsBlockOk(index)) continue;

                    mainBlockDefinition = managedBlocks[index];
                    block = mainBlockDefinition.block;

                    if (mainBlockDefinition.Settings.GetOption(setKeyBlockRemoveInputKey) || (!mainBlockDefinition.Settings.manual && Sortable(mainBlockDefinition, 0)))
                    {
                        mainFunctionItemList.Clear();
                        mainBlockDefinition.Input.GetItems(mainFunctionItemList);
                        if (!mainBlockDefinition.Settings.GetOption(setKeyBlockRemoveInputKey))
                            for (int x = 0; x < mainFunctionItemList.Count; x += 0)
                            {
                                if (PauseTickRun) yield return stateActive;

                                if (!Sortable(mainFunctionItemList[x], mainBlockDefinition))
                                    mainFunctionItemList.RemoveAtFast(x);
                                else x++;
                            }
                        while (!PutInStorage(mainFunctionItemList, index, 0)) yield return stateActive;
                    }
                    if (block.InventoryCount > 1 && (mainBlockDefinition.Settings.GetOption(setKeyBlockRemoveOutputKey) || (!mainBlockDefinition.Settings.manual && Sortable(mainBlockDefinition, 1))))
                    {
                        mainFunctionItemList.Clear();
                        block.GetInventory(1).GetItems(mainFunctionItemList);
                        if (!mainBlockDefinition.Settings.GetOption(setKeyBlockRemoveOutputKey))
                            for (int x = 0; x < mainFunctionItemList.Count; x += 0)
                            {
                                if (PauseTickRun) yield return stateActive;
                                if (!Sortable(mainFunctionItemList[x], mainBlockDefinition, 1))
                                    mainFunctionItemList.RemoveAtFast(x);
                                else x++;
                            }

                        while (!PutInStorage(mainFunctionItemList, index, 1))
                            yield return stateActive;
                    }
                }

                yield return stateContinue;
            }
        }

        bool PutInStorage(List<MyInventoryItem> items, long blockIndex, int inventoryIndex, double max = -1, int priorityMax = -1, int storageIndexStart = 0)
        {
            selfContainedIdentifier = FunctionIdentifier.Storing_Item;

            if (!IsStateRunning)
            {
                if (items.Count == 0) return true;
                tempStorageItemList = items;
                tempStorageBlockIndex = blockIndex;
                tempStorageInventoryIndex = inventoryIndex;
                tempStorageMax = max;
                tempStoragePriorityMax = priorityMax;
                tempStorageIndexStart = storageIndexStart;
            }

            return RunStateManager;
        }

        IEnumerator<FunctionState> PutInStorageState()
        {
            IMyTerminalBlock destBlock, origBlock;
            IMyInventory originInventory;
            List<long> tempIndexList = NewLongList;
            string itemID, typeID, typeKey;
            bool bottleException;
            long storageIndex;
            yield return stateContinue;

            while (true)
            {
                storageDefinitionA = managedBlocks[tempStorageBlockIndex];
                origBlock = storageDefinitionA.block;

                foreach (MyInventoryItem item in tempStorageItemList)
                {
                    tempIndexList.Clear();
                    itemID = item.Type.ToString();
                    typeID = item.Type.TypeId;
                    transferAmount = (double)item.Amount;

                    if (tempStorageMax > 0 && transferAmount > tempStorageMax)
                        transferAmount = tempStorageMax;

                    originInventory = origBlock.GetInventory(tempStorageInventoryIndex);
                    bottleException = fillingBottles && IsBottle(item) && !(origBlock is IMyGasGenerator || origBlock is IMyGasTank);

                    if (transferAmount > 0)
                    {
                        if (bottleException)
                        {
                            tempIndexList.AddRange(typedIndexes[item.Type.TypeId == oxyBottleType ? setKeyIndexOxygenTank : setKeyIndexHydrogenTank]);

                            tempIndexList.AddRange(typedIndexes[setKeyIndexGasGenerators]);
                        }
                        else
                        {
                            typeKey = GetItemCategory(itemID).ToLower();

                            if (indexesStorageLists.ContainsKey(typeKey))
                                tempIndexList.AddRange(indexesStorageLists[typeKey]);

                            if (tempIndexList.Count == 0)
                                tempIndexList.AddRange(typedIndexes[setKeyIndexStorage]);
                        }

                        for (int i = tempStorageIndexStart; i < tempIndexList.Count && (tempStoragePriorityMax <= 0 || i < tempStoragePriorityMax); i++)
                        {
                            if (PauseTickRun) yield return stateActive;

                            storageIndex = tempIndexList[i];

                            if (!IsBlockOk(storageIndex) || CurrentVolumePercentage(storageIndex) >= 0.99 || !AcceptsItem(managedBlocks[storageIndex], item))
                                continue;

                            destBlock = managedBlocks[storageIndex].block;
                            if (storageDefinitionA.block != destBlock)
                            {
                                while (!Transfer(ref transferAmount, originInventory, managedBlocks[storageIndex], item))
                                    yield return stateActive;

                                if (transferAmount <= 0)
                                    break;
                            }
                        }
                    }
                }

                yield return stateContinue;
            }
        }

        IEnumerator<FunctionState> CountBlueprintState()
        {
            IMyAssembler assembler;
            bool assembly, queueAssembly = GetKeyBool(setKeyToggleQueueAssembly);
            List<Blueprint> blueprints = new List<Blueprint>();
            List<ItemDefinition> itemList = NewItemDefinitionList;
            Blueprint blueprint;
            yield return stateContinue;

            while (true)
            {
                blueprints.Clear();
                foreach (long index in typedIndexes[setKeyIndexAssemblers])
                {
                    if (PauseTickRun) yield return stateActive;

                    if (!IsBlockOk(index))
                        continue;

                    mainBlockDefinition = managedBlocks[index];
                    assembler = (IMyAssembler)mainBlockDefinition.block;
                    blueprintListMain.Clear();
                    assembler.GetQueue(blueprintListMain);
                    if (blueprintListMain.Count == 0)
                        assembler.Mode = assemblyMode;

                    assembly = ((IMyAssembler)mainBlockDefinition.block).Mode == assemblyMode;
                    for (int x = 0; x < blueprintListMain.Count; x++)
                    {
                        if (PauseTickRun) yield return stateActive;

                        AddBlueprintAmount(blueprintListMain[x], assembly);
                    }
                }

                PopulateItemList(itemList);
                foreach (ItemDefinition item in itemList)
                {
                    if (PauseTickRun) yield return stateActive;

                    item.SwitchAssemblyCount();
                    item.SetDifferenceNeeded(allowedExcessPercent);
                    if (queueAssembly)
                    {
                        blueprint = new Blueprint { blueprintID = item.blueprintID, amount = item.currentNeededAssembly };
                        if (blueprint.amount >= 1)
                            blueprints.Add(blueprint.Clone());
                    }
                }

                assemblyNeededByMachine.Clear();
                if (queueAssembly)
                    while (!AddAssemblyNeeded(blueprints))
                        yield return stateActive;
                yield return stateContinue;
            }
        }

        bool AddAssemblyNeeded(List<Blueprint> blueprints)
        {
            selfContainedIdentifier = FunctionIdentifier.Assembly_Reserve;

            if (!IsStateRunning)
                tempAddAssemblyNeededList = blueprints;

            return RunStateManager;
        }

        IEnumerator<FunctionState> AddAssemblyState()
        {
            MyDefinitionId blueprintID;
            IMyAssembler assembler;
            string key;
            yield return stateContinue;

            while (true)
            {
                foreach (long index in typedIndexes[setKeyIndexAssemblers])
                {
                    if (PauseTickRun) yield return stateActive;

                    if (!IsBlockOk(index)) continue;

                    assembler = (IMyAssembler)managedBlocks[index].block;
                    if (assemblyNeededByMachine.Contains(BlockSubtype(assembler))) continue;
                    foreach (Blueprint blueprint in tempAddAssemblyNeededList)
                    {
                        if (PauseTickRun) yield return stateActive;
                        blueprintID = MyDefinitionId.Parse(MakeBlueprint(blueprint));
                        if (assembler.CanUseBlueprint(blueprintID))
                        {
                            key = BlockSubtype(assembler);

                            assemblyNeededByMachine.Add(BlockSubtype(assembler));
                            break;
                        }
                    }
                }

                yield return stateContinue;
            }
        }

        IEnumerator<FunctionState> CountState()
        {
            IMyTerminalBlock block;
            List<ItemDefinition> itemList = NewItemDefinitionList;
            yield return stateContinue;

            while (true)
            {
                activeOres = 0;
                foreach (long index in typedIndexes[setKeyIndexInventory])
                {
                    if (PauseTickRun) yield return stateActive;

                    if (!IsBlockOk(index) || managedBlocks[index].Settings.GetOption(setKeyBlockNoCountKey))
                        continue;

                    block = managedBlocks[index].block;

                    for (int inv = 0; inv < block.InventoryCount; inv++)
                    {
                        if (PauseTickRun) yield return stateActive;

                        mainFunctionItemList.Clear();
                        block.GetInventory(inv).GetItems(mainFunctionItemList);

                        for (int x = 0; x < mainFunctionItemList.Count; x++)
                        {
                            if (PauseTickRun) yield return stateActive;
                            AddAmount(mainFunctionItemList[x]);
                        }
                    }

                }

                PopulateItemList(itemList);
                foreach (ItemDefinition item in itemList)
                {
                    if (PauseTickRun) yield return stateActive;

                    item.SwitchCount(dynamicQuotaMaxMultiplier, useDynamicQuota && IsBlueprint(item.blueprintID), dynamicQuotaNegativeThreshold, dynamicQuotaPositiveThreshold, dynamicQuotaMultiplierIncrement, increaseDynamicQuotaWhenLow);
                    if (IsOre(item.typeID) && item.subtypeID != "Ice" && item.amount >= 0.5 && item.refine)
                        activeOres++;
                }

                yield return stateContinue;
            }
        }

        IEnumerator<FunctionState> ScanState()
        {
            long currentEntityID;
            IMyTextSurfaceProvider provider;
            IMyTerminalBlock currentBlock;
            BlockDefinition currentDefinition;
            bool currentPriority, emptyLoadout, isClone;
            string blockDef, typeID, subtypeID;
            IMyCubeGrid currentGrid;
            yield return stateContinue;

            while (true)
            {
                // ----------------------------------------------------
                // ------------- Set Variables -------------
                // ----------------------------------------------------
                //Clear indexes
                for (int i = 0; i < typedIndexes.Count; i++)
                {
                    if (PauseTickRun) yield return stateActive;
                    typedIndexes.Values[i].Clear();
                }
                for (int i = 0; i < indexesStorageLists.Count; i++)
                {
                    if (PauseTickRun) yield return stateActive;
                    indexesStorageLists.Values[i].Clear();
                }

                //Clear cached information
                priorityCategories.Clear();
                priorityTypes.Clear();
                prioritySystemActivated = false;

                //Get scan variables
                bool sameGridOnly = GetKeyBool(setKeySameGridOnly),
                     addLoadoutsToQuota = GetKeyBool(setKeyAddLoadoutsToQuota),
                     conveyorControl = GetKeyBool(setKeyControlConveyors);

                // ----------------------------------------------------
                // ------------- Scan Blocks -------------
                // ----------------------------------------------------

                //Get exclusion groups
                gtSystem.GetBlockGroups(groupList, b => LeadsString(b.Name, $"nds {exclusionKeyword}"));
                //Record excluded IDs in removal list
                foreach (IMyBlockGroup group in groupList)
                {
                    if (PauseTickRun) yield return stateActive;
                    group.GetBlocks(groupBlocks);
                    foreach (IMyTerminalBlock block in groupBlocks)
                    {
                        if (PauseTickRun) yield return stateActive;
                        excludedIDs.Add(block.EntityId);
                    }
                    groupBlocks.Clear();
                }
                groupList.Clear();
                //excludedIDs

                //Scan accessible blocks
                gtSystem.GetBlocksOfType<IMyTerminalBlock>(scannedBlocks);
                //excludedIDs, scannedBlocks (all)

                //Remove globally filtered blocked from scan list
                for (int i = 0; i < scannedBlocks.Count; i += 0)
                {
                    if (PauseTickRun) yield return stateActive;
                    if (!ScanFilter(scannedBlocks[i], excludedIDs))
                        scannedBlocks.RemoveAtFast(i);
                    else
                        i++;
                }
                //excludedIDs, scannedBlocks (filtered)

                //Record accessible IDs
                foreach (IMyTerminalBlock block in scannedBlocks)
                {
                    if (PauseTickRun) yield return stateActive;
                    accessibleIDs.Add(block.EntityId);
                }
                //excludedIDs, scannedBlocks (filtered), accessibleIDs

                //Add scanned blocks to managed list
                foreach (IMyTerminalBlock block in scannedBlocks)
                {
                    if (PauseTickRun) yield return stateActive;
                    if (!managedBlocks.ContainsKey(block.EntityId))
                        managedBlocks[block.EntityId] = new BlockDefinition(block);
                }
                scannedBlocks.Clear();
                //excludedIDs, accessibleIDs, managedBlocks (all)

                // ----------------------------------------------------
                // ------------- Load Block Custom Data -------------
                // ----------------------------------------------------

                //Process block options
                foreach (KeyValuePair<long, BlockDefinition> kvp in managedBlocks)
                {
                    if (PauseTickRun) yield return stateActive;

                    if (!kvp.Value.IsClone) while (!ProcessBlockOptions(kvp.Value)) yield return stateActive;
                    else if (!TextHasLength(kvp.Value.DataSource)) setRemoveIDs.Add(kvp.Key);

                    if (kvp.Value.Settings.GetOption(crossGridKeyword))
                        includedIDs.Add(kvp.Value.block.EntityId);

                    if (kvp.Value.Settings.GetOption(excludeGridKeyword))
                        excludedGridList.Add(kvp.Value.block.CubeGrid);

                    if (kvp.Value.Settings.GetOption(includeGridKeyword))
                        gridList.Add(kvp.Value.block.CubeGrid);

                }
                //excludedIDs, excludedGridList, accessibleIDs, managedBlocks (all-processed), gridList

                //Process clones separating from cloning
                foreach (long index in setRemoveIDs)
                {
                    if (PauseTickRun) yield return stateActive;
                    if (!clonedEntityIDs.Contains(index) && managedBlocks.ContainsKey(index))
                        managedBlocks[index].SetClone(null);
                }

                setRemoveIDs.Clear();
                clonedEntityIDs.Clear();

                //Get included group blocks by ID
                if (sameGridOnly)
                {
                    gridList.Add(Me.CubeGrid);
                    gtSystem.GetBlockGroups(groupList, b => LeadsString(b.Name, $"nds {crossGridKeyword}"));
                    foreach (IMyBlockGroup group in groupList)
                    {
                        if (PauseTickRun)
                            yield return stateActive;

                        group.GetBlocks(groupBlocks);
                        foreach (IMyTerminalBlock block in groupBlocks)
                        {
                            if (PauseTickRun) yield return stateActive;

                            includedIDs.Add(block.EntityId);
                        }
                        groupBlocks.Clear();
                    }
                    groupList.Clear();
                }
                //excludedIDs, accessibleIDs, managedBlocks (all-processed), gridList, includedIDs

                // ----------------------------------------------------
                // ------------- Filter Blocks -------------
                // ----------------------------------------------------

                //Queue managed blocks for removal if not accessible or excluded in some way
                foreach (KeyValuePair<long, BlockDefinition> kvp in managedBlocks)
                {
                    if (PauseTickRun) yield return stateActive;
                    currentEntityID = kvp.Value.block.EntityId;
                    currentGrid = kvp.Value.block.CubeGrid;
                    if (!accessibleIDs.Contains(currentEntityID) ||
                        kvp.Value.Settings.GetOption(exclusionKeyword) ||
                        excludedIDs.Contains(currentEntityID) ||
                        excludedGridList.Contains(currentGrid) ||
                        (sameGridOnly && !kvp.Value.Settings.GetOption(crossGridKeyword) && !gridList.Contains(currentGrid) && !includedIDs.Contains(currentEntityID)))
                        setRemoveIDs.Add(kvp.Key);
                }
                excludedIDs.Clear();
                accessibleIDs.Clear();
                gridList.Clear();
                excludedGridList.Clear();
                includedIDs.Clear();
                //managedBlocks (all-processed), setRemoveIDs

                //Remove blocks queued for removal
                foreach (long index in setRemoveIDs)
                {
                    if (PauseTickRun) yield return stateActive;
                    managedBlocks.Remove(index);
                }
                setRemoveIDs.Clear();
                //managedBlocks (filtered-processed)

                //Clear unused item category indexes
                for (int i = 0; i < indexesStorageLists.Count; i += 0)
                {
                    if (PauseTickRun)
                        yield return stateActive;
                    if (!itemCategoryList.Contains(indexesStorageLists.Keys[i]))
                        indexesStorageLists.RemoveAt(i);
                    else i++;
                }

                // ----------------------------------------------------
                // ------------- Process Blocks -------------
                // ----------------------------------------------------

                //Process managed blocks
                foreach (long index in managedBlocks.Keys)
                {
                    if (PauseTickRun) yield return stateActive;

                    currentDefinition = managedBlocks[index];
                    isClone = currentDefinition.IsClone;
                    currentBlock = currentDefinition.block;
                    currentPriority = currentDefinition.Settings.priority != 1.0;
                    prioritySystemActivated = prioritySystemActivated || currentPriority;
                    currentDefinition.isGravelSifter = IsGravelSifter(currentBlock);

                    if (!(IsPanelProvider(currentBlock)) || currentBlock is IMyShipController) //Process non-panel blocks
                    {
                        //Index automated blocks
                        if (!currentDefinition.Settings.manual)
                        {
                            if (currentDefinition.isGravelSifter)
                            {
                                typedIndexes[setKeyIndexGravelSifters].Add(index);
                                if (currentPriority)
                                    priorityTypes.Add(setKeyIndexGravelSifters);
                            }
                            else if (IsGun(currentDefinition))
                            {
                                typedIndexes[setKeyIndexGun].Add(index);
                                if (currentPriority)
                                    priorityTypes.Add(setKeyIndexGun);
                            }
                            else if (currentBlock is IMyAssembler)
                            {
                                typedIndexes[setKeyIndexAssemblers].Add(index);
                                if (currentPriority)
                                    priorityTypes.Add(setKeyIndexAssemblers);
                                if (currentDefinition.monitoredAssembler == null)
                                    currentDefinition.monitoredAssembler = new MonitoredAssembler { assembler = (IMyAssembler)currentBlock };
                            }
                            else if (currentBlock is IMyGasGenerator)
                            {
                                typedIndexes[setKeyIndexGasGenerators].Add(index);
                                if (currentPriority)
                                    priorityTypes.Add(setKeyIndexGasGenerators);
                            }
                            else if (currentBlock is IMyGasTank)
                            {
                                if (ContainsString(BlockSubtype(currentBlock), "hydrogen"))
                                {
                                    typedIndexes[setKeyIndexHydrogenTank].Add(index);
                                    if (currentPriority)
                                        priorityTypes.Add(setKeyIndexHydrogenTank);
                                }
                                else
                                {
                                    typedIndexes[setKeyIndexOxygenTank].Add(index);
                                    if (currentPriority)
                                        priorityTypes.Add(setKeyIndexOxygenTank);
                                }
                            }
                            else if (currentBlock is IMyParachute)
                            {
                                typedIndexes[setKeyIndexParachute].Add(index);
                                if (currentPriority)
                                    priorityTypes.Add(setKeyIndexParachute);
                            }
                            else if (currentBlock is IMyReactor)
                            {
                                typedIndexes[setKeyIndexReactor].Add(index);
                                if (currentPriority)
                                    priorityTypes.Add(setKeyIndexReactor);
                            }
                            else if (currentBlock is IMyRefinery)
                            {
                                typedIndexes[setKeyIndexRefinery].Add(index);
                                if (currentPriority)
                                    priorityTypes.Add(setKeyIndexRefinery);
                            }
                            if (currentDefinition.Settings.GetOption(setKeyBlockStorageKey))
                            {
                                typedIndexes[setKeyIndexStorage].Add(index);
                                if (currentPriority)
                                    priorityCategories.Add(setKeyIndexStorage);
                                if (currentDefinition.Settings.storageCategories.Contains("all", stringComparer) || currentDefinition.Settings.storageCategories.Contains("*"))
                                    for (int x = 0; x < indexesStorageLists.Count; x++)
                                    {
                                        if (PauseTickRun) yield return stateActive;
                                        indexesStorageLists.Values[x].Add(index);
                                        if (currentPriority)
                                            priorityCategories.Add(indexesStorageLists.Keys[x]);
                                    }
                                else
                                    foreach (string storageType in currentDefinition.Settings.storageCategories)
                                    {
                                        if (PauseTickRun) yield return stateActive;
                                        if (indexesStorageLists.ContainsKey(storageType))
                                        {
                                            indexesStorageLists[storageType].Add(index);
                                            if (currentPriority)
                                                priorityCategories.Add(storageType);
                                        }
                                    }
                            }
                        }
                        //Index blocks with inventories
                        if (currentDefinition.HasInventory)
                        {
                            typedIndexes[setKeyIndexInventory].Add(index);
                            if (currentPriority)
                                priorityTypes.Add(setKeyIndexInventory);
                            emptyLoadout = currentDefinition.Settings.loadout.ItemTypeCount == 0 && !currentDefinition.Settings.manual;
                            if (IsGun(currentDefinition))
                            {
                                blockDef = BlockSubtype(currentBlock);
                                if (currentDefinition.Input.ItemCount > 0)
                                    gunAmmoDictionary[blockDef] = $"{((MyInventoryItem)currentDefinition.Input.GetItemAt(0)).Type}";
                                if (!isClone && emptyLoadout && gunAmmoDictionary.ContainsKey(blockDef))
                                {
                                    SplitID(gunAmmoDictionary[blockDef], out typeID, out subtypeID);
                                    currentDefinition.Settings.loadout.AddItem(typeID, subtypeID, new VariableItemCount(DefaultMax(typeID, subtypeID, currentDefinition)));
                                }
                            }
                            if (!isClone && currentBlock is IMyParachute && emptyLoadout)
                                currentDefinition.Settings.loadout.AddItem(componentType, canvasType, new VariableItemCount(DefaultMax(componentType, canvasType, currentDefinition)));

                            if (currentDefinition.Settings.loadout.ItemTypeCount > 0)
                            {
                                typedIndexes[setKeyIndexLoadout].Add(index);
                                if (addLoadoutsToQuota && !currentDefinition.Settings.GetOption(setKeyBlockLoadoutNoCount))
                                    itemCollectionProcessTotalLoadout.AddCollection(currentDefinition.Settings.loadout, currentDefinition.block);
                                if (currentPriority)
                                    priorityTypes.Add(setKeyIndexLoadout);
                            }
                            if (currentDefinition.Settings.limits.ItemTypeCount > 0)
                            {
                                if (currentPriority)
                                    priorityTypes.Add(setKeyIndexLimit);
                                typedIndexes[setKeyIndexLimit].Add(index);
                            }
                            if ((!currentDefinition.Settings.manual || currentDefinition.Settings.GetOption(setKeyBlockRemoveInputKey) || currentDefinition.Settings.GetOption(setKeyBlockRemoveOutputKey)) && !(currentDefinition.Settings.GetOption(setKeyBlockKeepInputKey) && currentDefinition.Settings.GetOption(setKeyBlockKeepOutputKey)))
                                for (int i = 0; i < currentDefinition.block.InventoryCount; i++)
                                    if (Sortable(currentDefinition, i))
                                    {
                                        typedIndexes[setKeyIndexSortable].Add(index);
                                        if (currentPriority)
                                            priorityTypes.Add(setKeyIndexSortable);
                                        break;
                                    }
                            if (conveyorControl)
                                ConveyorControl(currentDefinition);
                        }
                    }
                    if (currentBlock is IMyTextPanel || (IsPanelProvider(currentBlock) && ContainsString(currentBlock.CustomName, panelTag)))
                    {
                        provider = (IMyTextSurfaceProvider)currentBlock;
                        for (int s = 0; s < provider.SurfaceCount; s++)
                        {
                            if (PauseTickRun) yield return stateActive;

                            while (!panelMaster.ProcessPanelOptions(currentDefinition, s)) yield return stateActive;

                            if (IsPanel(currentDefinition, s))
                            {
                                typedIndexes[setKeyIndexPanel].Add(index);
                                panelMaster.CheckPanel(currentDefinition, s);
                            }
                        }
                    }

                    if (currentDefinition.Settings.logicComparisons.Count > 0)
                        typedIndexes[setKeyIndexLogic].Add(currentBlock.EntityId);

                    if (useAcceptanceFilter && currentBlock.HasInventory && !itemAcceptanceDictionary.ContainsKey(BlockSubtype(currentBlock)))
                    {
                        itemAcceptanceDictionary[BlockSubtype(currentBlock)] = new HashSet<string>();
                        acceptedItemList.Clear();
                        currentBlock.GetInventory(0).GetAcceptedItems(acceptedItemList);
                        foreach (MyItemType itemType in acceptedItemList)
                        {
                            if (PauseTickRun) yield return stateActive;
                            itemAcceptanceDictionary[BlockSubtype(currentBlock)].Add(itemType.ToString());
                        }
                    }

                }
                panelMaster.updateTime = Now;

                // ----------------------------------------------------
                // ------------- Final Organization -------------
                // ----------------------------------------------------

                //Order blocks by priority and remove duplicates
                foreach (KeyValuePair<string, LongListPlus> kvp in typedIndexes)
                    while (!OrderListByPriority(kvp.Value, priorityTypes.Contains(kvp.Key))) yield return stateActive;
                foreach (KeyValuePair<string, LongListPlus> kvp in indexesStorageLists)
                    while (!OrderListByPriority(kvp.Value, priorityCategories.Contains(kvp.Key))) yield return stateActive;

                while (!SetBlockQuotas(itemCollectionProcessTotalLoadout)) yield return stateActive;
                itemCollectionProcessTotalLoadout.Clear();

                //Find spanned panels
                foreach (long index in typedIndexes[setKeyIndexPanel])
                {
                    if (PauseTickRun) yield return stateActive;
                    foreach (PanelMasterClass.PanelDefinition panelDefinition in managedBlocks[index].panelDefinitionList.Values)
                    {
                        panelDefinition.spannedPanelList.Clear();
                        if (panelDefinition.span)
                            foreach (long counterIndex in typedIndexes[setKeyIndexPanel])
                            {
                                if (PauseTickRun) yield return stateActive;
                                foreach (PanelMasterClass.PanelDefinition opposingPanel in managedBlocks[counterIndex].panelDefinitionList.Values)
                                {
                                    if (PauseTickRun) yield return stateActive;
                                    if (opposingPanel.panelType == PanelType.Span && StringsMatch(panelDefinition.childSpanKey, opposingPanel.spanKey) && index != counterIndex || panelDefinition.surfaceIndex != opposingPanel.surfaceIndex)
                                        panelDefinition.spannedPanelList.Add(new SpanKey { index = counterIndex, surfaceIndex = opposingPanel.surfaceIndex });
                                }
                            }
                    }
                }
                yield return stateContinue;
            }
        }

        bool GetTags(ItemCollection quota, string text, bool acceptZero = true)
        {
            selfContainedIdentifier = FunctionIdentifier.Processing_Tags;

            if (!IsStateRunning)
            {
                if (!TextHasLength(text)) return true;
                tempGetTagsCollection = quota;
                tempGetTagsText = RemoveSpaces(text, true);
                tempGetTagsAcceptZero = acceptZero;
            }

            return RunStateManager;
        }

        IEnumerator<FunctionState> GetTagState()
        {
            Dictionary<char, char> limiters = new Dictionary<char, char> { { '{', '}' }, { '[', ']' }, { '<', '>' } };
            string typeID, amountString, text;
            string[] subGroups, groupTags;
            char keyChar;
            double parsedAmount;
            bool haveType;
            yield return stateContinue;

            while (true)
            {
                amountString = "0";
                text = tempGetTagsText;
                keyChar = tempGetTagsText[0];
                if (limiters.ContainsKey(keyChar) && tempGetTagsText[tempGetTagsText.Length - 1] == limiters[keyChar])
                    text = tempGetTagsText.Substring(1, tempGetTagsText.Length - 2);
                subGroups = text.ToLower().Split('|');
                typeID = "*";

                for (int y = 0; y < subGroups.Length; y++)
                {
                    if (PauseTickRun) yield return stateActive;

                    groupTags = subGroups[y].Split(':');
                    tagTempStringList.Clear();
                    haveType = false;
                    for (int z = 0; z < groupTags.Length; z++)
                    {
                        if (PauseTickRun)
                            yield return stateActive;

                        if (z == 0 && (EndsString(groupTags[z], "%") || double.TryParse(groupTags[z], out parsedAmount)))
                        {
                            amountString = groupTags[z];
                            continue;
                        }
                        if (z < 2 && !haveType && IsCategory(groupTags[z]))
                        {
                            typeID = groupTags[z];
                            haveType = true;
                        }
                        else
                            tagTempStringList.Add(groupTags[z]);
                    }
                    if (haveType && groupTags.Length == 1 && IsWildCard(typeID))
                        tagTempStringList.Add("*");

                    for (int z = 0; z < tagTempStringList.Count; z++)
                        while (!MatchItems(tempGetTagsCollection, typeID, tagTempStringList[z], false, amountString, tempGetTagsAcceptZero))
                            yield return stateActive;
                }
                yield return stateContinue;
            }
        }

        bool ProcessBlockOptions(BlockDefinition managedBlock)
        {
            selfContainedIdentifier = FunctionIdentifier.Processing_Block_Options;

            if (!IsStateRunning)
                tempBlockOptionDefinition = managedBlock;

            return RunStateManager;
        }

        IEnumerator<FunctionState> ProcessBlockOptionState()
        {
            string dataSource, key, data;
            IMyTerminalBlock block;
            string[] dataLines;
            List<IMyTerminalBlock> groupBlocks = new List<IMyTerminalBlock>();
            yield return stateContinue;

            while (true)
            {
                dataSource = tempBlockOptionDefinition.DataSource;
                block = tempBlockOptionDefinition.block;
                if (TextHasLength(dataSource) && TextHasLength(optionBlockFilter) && tempBlockOptionDefinition.headerIndex == 0)
                    OptionHeaderIndex(out tempBlockOptionDefinition.headerIndex, SplitLines(dataSource), optionBlockFilter);
                if (!TextHasLength(dataSource) || (TextHasLength(optionBlockFilter) && tempBlockOptionDefinition.headerIndex == 0))
                {
                    if (!(tempBlockOptionDefinition.block is IMyTextPanel) && GetKeyBool(setKeyAutoTagBlocks))
                        while (!GenerateBlockOptions(tempBlockOptionDefinition)) yield return stateActive;
                }
                if (!StringsMatch(dataSource, tempBlockOptionDefinition.settingBackup))
                {
                    tempBlockOptionDefinition.Settings.Initialize();
                    tempBlockOptionDefinition.cloneGroup = "";
                    dataLines = SplitLines(dataSource);
                    processBlockStorageList.Clear();
                    processBlockOptionList.Clear();
                    bool dataBool, storageSet = false, hasInventory = tempBlockOptionDefinition.HasInventory;
                    double dataDouble;

                    OptionHeaderIndex(out tempBlockOptionDefinition.headerIndex, dataLines, optionBlockFilter);

                    for (int i = tempBlockOptionDefinition.headerIndex; i < dataLines.Length; i++)
                    {
                        if (PauseTickRun) yield return stateActive;
                        if (tempBlockOptionDefinition.headerIndex > 0 && StringsMatch(dataLines[i], optionBlockFilter)) break;
                        if (!dataLines[i].StartsWith("//") && SplitData(dataLines[i], out key, out data))
                        {
                            data = data.Trim();
                            dataBool = StringsMatch(data, trueString);
                            double.TryParse(data, out dataDouble);
                            switch (key.ToLower())
                            {
                                case "automatic":
                                    tempBlockOptionDefinition.Settings.manual = !dataBool;
                                    break;
                                case "options":
                                    processBlockOptionList = RemoveSpaces(data, true).Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                                    break;
                                case "storage":
                                    if (hasInventory)
                                        processBlockStorageList = data.ToLower().Split('|').ToList();
                                    break;
                                case "loadout":
                                    if (hasInventory)
                                        tempBlockOptionDefinition.Settings.loadoutSearchString = $"{(tempBlockOptionDefinition.Settings.loadoutSearchString.Length > 0 ? $"{tempBlockOptionDefinition.Settings.loadoutSearchString}|" : "")}{data}";
                                    break;
                                case "limit":
                                    if (hasInventory)
                                        tempBlockOptionDefinition.Settings.limitSearchString = $"{(tempBlockOptionDefinition.Settings.limitSearchString.Length > 0 ? $"{tempBlockOptionDefinition.Settings.limitSearchString}|" : "")}{data}";
                                    break;
                                case "logicand":
                                    while (!ProcessTimer(tempBlockOptionDefinition.Settings.logicComparisons, data)) yield return stateActive;
                                    tempBlockOptionDefinition.Settings.andComparison = tempBlockOptionDefinition.Settings.logicComparisons.Count > 0;
                                    break;
                                case "logicor":
                                    while (!ProcessTimer(tempBlockOptionDefinition.Settings.logicComparisons, data)) yield return stateActive;
                                    break;
                                case "priority":
                                    tempBlockOptionDefinition.Settings.priority = dataDouble;
                                    break;
                                case "clone group":
                                    tempBlockOptionDefinition.cloneGroup = data;
                                    break;
                            }
                        }
                    }
                    foreach (string option in processBlockOptionList)
                    {
                        if (PauseTickRun) yield return stateActive;
                        tempBlockOptionDefinition.Settings.SetOption(option);
                    }
                    if (processBlockStorageList.Count > 0)
                    {
                        foreach (string category in processBlockStorageList)
                        {
                            if (PauseTickRun) yield return stateActive;
                            if (IsCategory(category))
                                tempBlockOptionDefinition.Settings.storageCategories.Add(category);
                        }
                        tempBlockOptionDefinition.Settings.SetOption(setKeyBlockStorageKey, tempBlockOptionDefinition.Settings.storageCategories.Count > 0);
                        storageSet = true;
                    }

                    if (processBlockOptionList.Count == 0 && !storageSet && !tempBlockOptionDefinition.Settings.manual && tempBlockOptionDefinition.block is IMyCargoContainer && !IsGun(tempBlockOptionDefinition))
                        tempBlockOptionDefinition.Settings.SetOption(setKeyBlockStorageKey);

                    if (tempBlockOptionDefinition.Settings.GetOption(setKeyBlockStorageKey) &&
                        tempBlockOptionDefinition.Settings.storageCategories.Count == 0)
                        tempBlockOptionDefinition.Settings.storageCategories.Add("all");

                    tempBlockOptionDefinition.settingBackup = dataSource;
                }
                // Update
                if (tempBlockOptionDefinition.Settings.updateTime < itemAddedOrChanged ||
                    (tempBlockOptionDefinition.Settings.loadout.ItemTypeCount == 0 && tempBlockOptionDefinition.Settings.loadoutSearchString.Length > 0) ||
                    (tempBlockOptionDefinition.Settings.limits.ItemTypeCount == 0 && tempBlockOptionDefinition.Settings.limitSearchString.Length > 0))
                {

                    tempBlockOptionDefinition.Settings.limits.Clear();
                    tempBlockOptionDefinition.Settings.loadout.Clear();
                    while (!GetTags(tempBlockOptionDefinition.Settings.loadout, tempBlockOptionDefinition.Settings.loadoutSearchString, false)) yield return stateActive;
                    while (!GetTags(tempBlockOptionDefinition.Settings.limits, tempBlockOptionDefinition.Settings.limitSearchString)) yield return stateActive;
                    tempBlockOptionDefinition.Settings.updateTime = Now;
                }
                if (TextHasLength(tempBlockOptionDefinition.cloneGroup))
                {
                    IMyBlockGroup blockGroup = gtSystem.GetBlockGroupWithName(tempBlockOptionDefinition.cloneGroup);
                    if (blockGroup != null)
                    {
                        groupBlocks.Clear();
                        blockGroup.GetBlocks(groupBlocks);

                        foreach (IMyTerminalBlock cBlock in groupBlocks)
                        {
                            if (PauseTickRun) yield return stateActive;
                            if (tempBlockOptionDefinition.block != cBlock && managedBlocks.ContainsKey(cBlock.EntityId))
                            {
                                clonedEntityIDs.Add(cBlock.EntityId);
                                managedBlocks[cBlock.EntityId].SetClone(tempBlockOptionDefinition);
                                if (!TextHasLength(cBlock.CustomData))
                                    cBlock.CustomData = $"Cloning: {tempBlockOptionDefinition.block.CustomName}";
                            }
                        }
                    }
                }

                yield return stateContinue;
            }
        }

        bool GenerateBlockOptions(BlockDefinition blockDefinition)
        {
            selfContainedIdentifier = FunctionIdentifier.Generating_Block_Options;

            if (!IsStateRunning)
                tempGenerateBlockOptionBlockDefinition = blockDefinition;

            return RunStateManager;
        }

        IEnumerator<FunctionState> GenerateBlockOptionState()
        {
            IMyTerminalBlock block;
            BlockSettings Settings;
            StringBuilder builder = NewBuilder;
            List<string> values = new List<string>();
            yield return stateContinue;

            while (true)
            {
                block = tempGenerateBlockOptionBlockDefinition.block;
                Settings = tempGenerateBlockOptionBlockDefinition.Settings;

                builder.Clear();

                // Preserve when empty
                if (TextHasLength(optionBlockFilter) && TextHasLength(tempGenerateBlockOptionBlockDefinition.DataSource))
                {
                    BuilderAppendLine(builder, tempGenerateBlockOptionBlockDefinition.DataSource);
                    BuilderAppendLine(builder);
                }
                if (TextHasLength(optionBlockFilter))
                    BuilderAppendLine(builder, optionBlockFilter);

                // Automatic=
                BuilderAppendLine(builder, $"Automatic={!Settings.manual}");

                // Options=
                foreach (KeyValuePair<string, SortedList<string, bool>> l in Settings.settingDictionaryBools)
                    values.AddRange(l.Value.Keys.Where(k => l.Value[k]));
                if (PauseTickRun) yield return stateActive;
                BuilderAppendLine(builder, $"Options={String.Join("|", values.Select(i => Formatted(i)))}");
                values.Clear();

                // Optional options
                // Basic
                values.AddRange(Settings.settingDictionaryBools[setKeyBlockToggles].Keys.Where(k => !Settings.settingDictionaryBools[setKeyBlockToggles][k]));
                // Input
                if (block.HasInventory)
                    values.AddRange(Settings.settingDictionaryBools[setKeyBlockInputToggles].Keys.Where(k => !Settings.settingDictionaryBools[setKeyBlockInputToggles][k]));
                // Output
                if (block.InventoryCount > 1)
                    values.AddRange(Settings.settingDictionaryBools[setKeyBlockOutputToggles].Keys.Where(k => !Settings.settingDictionaryBools[setKeyBlockOutputToggles][k]));
                // Assembler
                if (block is IMyAssembler)
                    values.AddRange(Settings.settingDictionaryBools[setKeyBlockAssemblerToggles].Keys.Where(k => !Settings.settingDictionaryBools[setKeyBlockAssemblerToggles][k]));
                if (PauseTickRun) yield return stateActive;
                AppendOption(builder, String.Join("|", values.Select(i => Formatted(i))));
                values.Clear();

                // Priority
                AppendOption(builder, $"Priority={Settings.priority}", Settings.priority == 1.0);

                // Clone group
                AppendOption(builder, $"Clone Group={tempGenerateBlockOptionBlockDefinition.cloneGroup}", !TextHasLength(tempGenerateBlockOptionBlockDefinition.cloneGroup));

                // Storage
                if (block.HasInventory)
                {
                    values.AddRange(Settings.storageCategories.Count > 0 ? Settings.storageCategories : itemCategoryList);
                    if (Settings.storageCategories.Count == 0)
                        values.Insert(0, "all");
                    AppendOption(builder, $"Storage={String.Join("|", values.Select(i => Formatted(i)))}", Settings.storageCategories.Count == 0);
                    values.Clear();
                }
                if (PauseTickRun) yield return stateActive;

                // Loadout
                AppendOption(builder, $"Loadout={(Settings.loadout.ItemTypeCount > 0 ? $"{Settings.loadout}" : "20%:ingot:iron:silicon:nickel|50:ore:ice")}", Settings.loadout.ItemTypeCount == 0);
                if (PauseTickRun) yield return stateActive;

                // Limit
                AppendOption(builder, $"Limit={(Settings.limits.ItemTypeCount > 0 ? $"{Settings.limits}" : "25:ingot:*|10%:ore:*")}", Settings.limits.ItemTypeCount == 0);
                if (PauseTickRun) yield return stateActive;

                // Logic
                if (block is IMyFunctionalBlock)
                {
                    if (Settings.logicComparisons.Count > 0)
                    {
                        BuilderAppendLine(builder, $"{(tempGenerateBlockOptionBlockDefinition.Settings.andComparison ? "LogicAnd=" : "LogicOr=")}{String.Join("|", Settings.logicComparisons)}");
                    }
                    else
                    {
                        AppendOption(builder, "LogicAnd=ingot:iron<100 | ore:iron>=quota*0.1");
                        AppendOption(builder, "LogicOr=ingot:iron<quota*0.95 | ingot:silicon<quota*0.95");
                    }
                }
                // Definition
                AppendOption(builder, $"Definition={block.BlockDefinition}");

                // Header
                if (TextHasLength(optionBlockFilter))
                    BuilderAppendLine(builder, optionBlockFilter);

                tempGenerateBlockOptionBlockDefinition.DataSource = builder.ToString().TrimEnd();

                yield return stateContinue;
            }
        }


        #endregion


        #region Return Methods

        string ColoredEcho(string text, int color = 2)
        {
            if (text.Length < 2) return text;

            if (color == 1)
                return $"[Color=#FFFF0000]{text}[/Color]";
            if (color == 2)
                return $"[Color=#FF00FF00]{text}[/Color]";
            if (color == 3)
                return $"[Color=#FF0000FF]{text}[/Color]";

            return text;
        }

        static string[] SplitLines(string data)
        {
            return data.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        }

        static void OptionHeaderIndex(out int startIndex, string[] lines, string key)
        {
            startIndex = 0;
            if (!TextHasLength(key)) return;

            for (int i = 0; i < lines.Length; i++)
                if (StringsMatch(lines[i], key))
                {
                    startIndex = i + 1;
                    return;
                }
            return;
        }

        TimeSpan SpanDelay(double seconds = 0)
        {
            return scriptSpan + TimeSpan.FromSeconds(seconds);
        }

        bool SpanElapsed(TimeSpan span)
        {
            return scriptSpan >= span;
        }

        bool IsGravelSifter(IMyTerminalBlock block)
        {
            return settingsListsStrings[setKeyGravelSifterKeys].Contains(BlockSubtype(block).ToLower());
        }

        static string PositionPrefix(string prefix, string input)
        {
            return $"Position{prefix}_{input}";
        }

        bool IsPanelProvider(IMyTerminalBlock block)
        {
            return block is IMyTextSurfaceProvider && (block is IMyTextPanel || block is IMyCockpit || block is IMyButtonPanel);
        }

        bool ScanFilter(IMyTerminalBlock block, HashSet<long> excludedIds)
        {
            return (block.IsFunctional && GlobalFilter(block) && PassPanel(block) &&
                (block.InventoryCount > 0 || IsPanelProvider(block) || block is IMyProgrammableBlock || block is IMyTimerBlock || TextHasLength(block.CustomData)) &&
                !excludedIds.Contains(block.EntityId));
        }

        string GetItemCategory(string itemID)
        {
            if (itemCategoryDictionary.ContainsKey(itemID))
                return itemCategoryDictionary[itemID];

            string typeID, subtypeID;
            SplitID(itemID, out typeID, out subtypeID);

            return
                IsAmmo(typeID) ? ammoKeyword :
                IsComponent(typeID) ? componentKeyword :
                IsIngot(typeID) ? ingotKeyword :
                IsOre(typeID) ? oreKeyword :
                IsTool(typeID) ? toolKeyword : typeID;
        }

        string GetItemCategory(MyInventoryItem item)
        {
            return GetItemCategory($"{item.Type}");
        }

        static bool IsBlueprint(string data)
        {
            return TextHasLength(data) && !StringsMatch(data, "none");
        }

        bool HasBlueprintMatch(MyInventoryItem item, ref string matchingKey)
        {
            if (mergeLengthTolerance < 0)
                return false;

            string itemSubtype = AutoMatchNormalize(item.Type.SubtypeId), blueprintSubtype;
            for (int i = 0; i < modBlueprintList.Count; i++)
            {
                blueprintSubtype = AutoMatchNormalize(modBlueprintList[i]);
                if (Math.Abs(blueprintSubtype.Length - itemSubtype.Length) > mergeLengthTolerance)
                    continue;

                if (LeadsString(itemSubtype, blueprintSubtype) || EndsString(itemSubtype, blueprintSubtype) || LeadsString(blueprintSubtype, itemSubtype) || EndsString(blueprintSubtype, itemSubtype))
                {
                    matchingKey = modBlueprintList[i];
                    return true;
                }
            }
            return false;
        }

        bool HasItemMatch(MyProductionItem blueprint, ref string matchingKey)
        {
            if (mergeLengthTolerance < 0)
                return false;

            string itemSubtype, blueprintSubtype = AutoMatchNormalize(BlueprintSubtype(blueprint)), subtypeID, typeID;
            for (int i = 0; i < modItemDictionary.Count; i++)
            {
                SplitID(modItemDictionary.Keys[i], out typeID, out subtypeID);
                itemSubtype = AutoMatchNormalize(subtypeID);
                if (Math.Abs(blueprintSubtype.Length - itemSubtype.Length) > mergeLengthTolerance)
                    continue;

                if (LeadsString(itemSubtype, blueprintSubtype) || EndsString(itemSubtype, blueprintSubtype) || LeadsString(blueprintSubtype, itemSubtype) || EndsString(blueprintSubtype, itemSubtype))
                {
                    matchingKey = modItemDictionary.Keys[i];
                    return true;
                }
            }
            return false;
        }

        string AutoMatchNormalize(string source)
        {
            return source.ToLower().Replace("component", "").Replace("magazine", "").Replace("blueprint", "").Replace("tier", "t").Replace("hydrogen", "hydro").Replace("thruster", "thrust");
        }

        static string Formatted(string text)
        {
            return text.Length <= 1 ? text.ToUpper() : String.Join(" ", text.Split(' ').Select(x => x.Length <= 1 ? x.ToUpper() : $"{x.Substring(0, 1).ToUpper()}{x.Substring(1)}"));
        }

        bool ContainsString(string whole, string key)
        {
            if (whole.Length >= key.Length && TextHasLength(key))
                return whole.ToLower().Contains(key.ToLower());

            return false;
        }

        string ShortMSTime(double milliseconds)
        {
            return $"{(milliseconds >= 1000.0 ? $"{ShortNumber2(milliseconds / 1000.0)}s" : $"{ShortNumber2(milliseconds)}ms")}";
        }

        static string BlockSubtype(IMyTerminalBlock block)
        {
            return block.BlockDefinition.SubtypeName;
        }

        static string BlueprintSubtype(MyProductionItem blueprint)
        {
            return blueprint.BlueprintId.SubtypeName;
        }

        bool StateInitialized(FunctionIdentifier key)
        {
            return stateRecords[key].lastStatus != stateUninitialized;
        }

        bool StateRunning(FunctionIdentifier key)
        {
            return stateRecords[key].lastStatus == stateActive;
        }

        bool GlobalFilter(IMyTerminalBlock block)
        {
            return (!TextHasLength(globalFilterKeyword) || ContainsString(block.CustomName, globalFilterKeyword)) && !settingsListsStrings[setKeyExcludedDefinitions].Contains(BlockSubtype(block));
        }

        static bool SplitData(string text, out string key, out string data, char splitter = '=', bool firstIndex = true)
        {
            int index = firstIndex ? text.IndexOf(splitter) : text.LastIndexOf(splitter);
            if (index != -1)
            {
                key = text.Substring(0, index);
                data = text.Substring(index + 1);
                return true;
            }
            key = data = "";
            return TextHasLength(data) && TextHasLength(key);
        }

        bool AcceptsItem(BlockDefinition managedBlock, string typeID, string subtypeID)
        {
            IMyTerminalBlock block = managedBlock.block;

            if (useAcceptanceFilter && itemAcceptanceDictionary.ContainsKey(BlockSubtype(block)) && (itemAcceptanceDictionary[BlockSubtype(block)].Count == 0 || !itemAcceptanceDictionary[BlockSubtype(block)].Contains($"{typeID}/{subtypeID}")))
            {
                Output($"{ShortenName(block.CustomName)} does not accept {ItemName(typeID, subtypeID)}");
                return false;
            }

            bool limited;

            double limit = managedBlock.Settings.limits.ItemCount(out limited, typeID, subtypeID, block);

            if (limited && limit <= 0.0)
                return false;

            if (IsAmmo(typeID) && gunAmmoDictionary.ContainsKey(BlockSubtype(block)))
                return gunAmmoDictionary[BlockSubtype(block)] == subtypeID;

            return true;
        }

        bool AcceptsItem(BlockDefinition managedBlock, MyInventoryItem item)
        {
            return AcceptsItem(managedBlock, item.Type.TypeId, item.Type.SubtypeId);
        }

        bool GetSetLimit(BlockDefinition managedBlock, ref double foundMax, MyInventoryItem item)
        {
            bool hasLimit;
            double tempMax = managedBlock.Settings.limits.ItemCount(out hasLimit, item, managedBlock.block);
            if (!hasLimit)
                tempMax = managedBlock.Settings.loadout.ItemCount(out hasLimit, item, managedBlock.block);

            if (hasLimit)
            {
                foundMax = tempMax;
                return true;
            }
            return false;
        }

        string GetKeyString(string key, bool lower = true)
        {
            foreach (KeyValuePair<string, SortedList<string, string>> kvp in settingDictionaryStrings)
                if (kvp.Value.ContainsKey(key))
                    return lower ? kvp.Value[key].ToLower() : kvp.Value[key];

            return "-0.1";
        }

        double GetKeyDouble(string key)
        {
            foreach (KeyValuePair<string, SortedList<string, double>> kvp in settingDictionaryDoubles)
                if (kvp.Value.ContainsKey(key))
                    return kvp.Value[key];

            return -0.1;
        }

        bool GetKeyBool(string key)
        {
            foreach (KeyValuePair<string, SortedList<string, bool>> kvp in settingDictionaryBools)
                if (kvp.Value.ContainsKey(key))
                    return kvp.Value[key];

            return false;
        }

        bool SetKeyString(string key, string data)
        {
            foreach (KeyValuePair<string, SortedList<string, string>> kvp in settingDictionaryStrings)
                if (kvp.Value.ContainsKey(key))
                {
                    kvp.Value[key] = data;
                    return true;
                }

            return false;
        }

        bool SetKeyDouble(string key, double data)
        {
            foreach (KeyValuePair<string, SortedList<string, double>> kvp in settingDictionaryDoubles)
                if (kvp.Value.ContainsKey(key))
                {
                    kvp.Value[key] = data;
                    return true;
                }

            return false;
        }

        bool SetKeyBool(string key, bool data)
        {
            foreach (KeyValuePair<string, SortedList<string, bool>> kvp in settingDictionaryBools)
                if (kvp.Value.ContainsKey(key))
                {
                    kvp.Value[key] = data;
                    return true;
                }

            return false;
        }

        static bool StringsMatch(string a, string b)
        {
            return a.Length == b.Length && string.Compare(a, b, true) == 0;
        }

        double LeastKeyedOrePercentage(string subtypeID)
        {
            double leastFound = 0.5, keyOffset;
            ItemDefinition definition;
            bool first = true;
            foreach (KeyValuePair<string, string> kvp in oreKeyedItemDictionary)
                if (GetDefinition(out definition, kvp.Key) && (keyOffset = definition.oreKeys.IndexOf(subtypeID)) != -1)
                {
                    leastFound = first ? definition.Percentage + (keyOffset * 0.00001) : Math.Min(leastFound, definition.Percentage + (keyOffset * 0.00001));
                    first = false;
                }
            return leastFound;
        }

        double LeastKeyedOrePercentage(MyInventoryItem item)
        {
            return LeastKeyedOrePercentage(item.Type.SubtypeId);
        }

        Blueprint ItemToBlueprint(ItemDefinition definition)
        {
            return new Blueprint { blueprintID = definition.blueprintID, subtypeID = definition.subtypeID, typeID = definition.typeID, multiplier = definition.assemblyMultiplier };
        }

        bool UnknownItem(MyInventoryItem item)
        {
            ItemDefinition definition;
            return !modItemDictionary.ContainsKey(item.Type.ToString()) && !GetDefinition(out definition, item.Type.ToString());
        }

        bool UnknownBlueprint(MyProductionItem blueprint)
        {
            if (BlueprintSubtype(blueprint) == stoneOreToIngotBasicID)
                return false;

            return !modBlueprintList.Contains(BlueprintSubtype(blueprint)) && !blueprintList.ContainsKey(BlueprintSubtype(blueprint));
        }

        bool LogicPass(ItemDefinition definition, string comparison, string compare)
        {
            string compareAgainst = compare.ToLower();
            double comparisonNumber = 0, changeNumber;
            if (LeadsString(compareAgainst, "quota"))
            {
                comparisonNumber = definition.currentQuota;
                int indexP = compareAgainst.IndexOf("+"), indexM = compareAgainst.IndexOf("*");
                if (indexP > 0)
                {
                    if (!double.TryParse(compareAgainst.Substring(indexP + 1), out changeNumber))
                        comparisonNumber += changeNumber;
                }
                else if (indexM > 0 && !double.TryParse(compareAgainst.Substring(indexM + 1), out changeNumber))
                    comparisonNumber *= changeNumber;
            }
            else if (ContainsString(compare, "/"))
            {
                ItemDefinition comparisonDefinition;
                if (GetDefinition(out comparisonDefinition, compare))
                    comparisonNumber = comparisonDefinition.amount;
            }
            else if (!double.TryParse(compareAgainst, out comparisonNumber))
                return false;

            return
                comparison == ">=" ? definition.amount >= comparisonNumber :
                comparison == "<=" ? definition.amount <= comparisonNumber :
                comparison == ">" ? definition.amount > comparisonNumber :
                comparison == "<" ? definition.amount < comparisonNumber :
                comparison == "=" && definition.amount == comparisonNumber;
        }

        bool PassPanel(IMyTerminalBlock block)
        {
            return block.InventoryCount > 0 || block is IMyTimerBlock || (IsPanelProvider(block) && (block is IMyProgrammableBlock || ContainsString(block.CustomName, panelTag)));
        }

        bool IsPanel(BlockDefinition managedBlock, int surfaceIndex = 0)
        {
            return managedBlock.panelDefinitionList.ContainsKey(surfaceIndex) && managedBlock.panelDefinitionList[surfaceIndex].panelType != PanelType.None;
        }

        static string ShortenName(string name, int length = 20, bool pad = false)
        {
            string shortName = name.Length <= length ? name : $"{name.Substring(0, (int)Math.Ceiling((length - 1.0) / 2.0))}.{name.Substring(name.Length - (int)Math.Floor((length - 1.0) / 2.0))}";

            if (pad)
                shortName = shortName.PadRight(length);

            return shortName;
        }

        bool IsBlockOk(long index)
        {
            return managedBlocks.ContainsKey(index) &&
                   managedBlocks[index].block != null &&
                   gtSystem.CanAccess(managedBlocks[index].block) &&
                   managedBlocks[index].block.IsFunctional;
        }

        bool GetDefinition(out ItemDefinition definition, string itemID)
        {
            string typeID, subtypeID;
            SplitID(itemID, out typeID, out subtypeID);
            if (itemListMain.ContainsKey(typeID) && itemListMain[typeID].TryGetValue(subtypeID, out definition))
                return true;

            definition = null;
            return false;
        }

        static string TruncateNumber(double number, int decimalPlaces)
        {
            return $"{Math.Floor(number * (Math.Pow(10.0, decimalPlaces))) / (Math.Pow(10.0, decimalPlaces))}";
        }

        bool IsGas(MyInventoryItem item)
        {
            return IsGas(item.Type.TypeId, item.Type.SubtypeId);
        }

        bool IsGas(string typeID, string subtypeID)
        {
            ItemDefinition definition;
            if (GetDefinition(out definition, $"{typeID}/{subtypeID}"))
                return definition.gas;

            return IsIce(typeID, subtypeID);
        }

        static bool IsIce(string typeID, string subtypeID)
        {
            return IsOre(typeID) && subtypeID == "Ice";
        }

        bool UsableAssembler(BlockDefinition block, MyDefinitionId blueprintID, MyAssemblerMode mode)
        {
            IMyAssembler assembler = (IMyAssembler)block.block;

            if (!assembler.Enabled)
                return false;

            if (!GetKeyBool(setKeySurvivalKitAssembly) && blueprintID.SubtypeName != stoneOreToIngotBasicID && ContainsString(BlockSubtype(assembler), "survival"))
                return false;

            if ((block.Settings.GetOption(setKeyBlockAssemblyOnlyKey) && mode == disassemblyMode) || (block.Settings.GetOption(setKeyBlockDisassemblyOnlyKey) && mode == assemblyMode))
                return false;

            return assembler.CanUseBlueprint(blueprintID) && (assembler.IsQueueEmpty || assembler.Mode == mode);
        }

        string MakeBlueprint(Blueprint blueprint)
        {
            return $"{blueprintPrefix}/{blueprint.blueprintID}";
        }

        double AssemblyAmount(Blueprint blueprint, bool disassembly = false)
        {
            ItemDefinition definition;
            if (GetDefinition(out definition, $"{blueprint.typeID}/{blueprint.subtypeID}"))
                return disassembly && definition.differenceNeeded < 0 ? -definition.differenceNeeded : !disassembly && definition.differenceNeeded > 0 ? definition.differenceNeeded : 0;
            return 0;
        }

        double BlueprintPercentage(MyDefinitionId blueprintID)
        {
            if (blueprintID.SubtypeName == stoneOreToIngotBasicID)
                return double.MaxValue;

            string typeID, subtypeID;
            if (GetItemFromBlueprint(blueprintID.SubtypeName, out typeID, out subtypeID))
                return ItemPercentage(typeID, subtypeID);

            return -1;
        }

        double ItemPercentage(string typeID, string subtypeID)
        {
            ItemDefinition definition;
            if (GetDefinition(out definition, $"{typeID}/{subtypeID}"))
                return definition.Percentage;

            return 100;
        }

        bool FunctionDelay(FunctionIdentifier functionKey)
        {
            if (!delaySpans.ContainsKey(functionKey))
            {
                delaySpans[functionKey] = TimeSpan.Zero;
                return true;
            }
            return SpanElapsed(delaySpans[functionKey]);
        }

        double AvailableVolume(IMyTerminalBlock block)
        {
            return (double)block.GetInventory(0).MaxVolume - (double)block.GetInventory(0).CurrentVolume;
        }

        double CurrentVolumePercentage(long index)
        {
            return (double)managedBlocks[index].Input.CurrentVolume / (double)managedBlocks[index].Input.MaxVolume;
        }

        bool OverRange(double amount, double goal, double multiplier)
        {
            return amount > goal + (goal * multiplier);
        }

        bool UnderRange(double amount, double goal, double multiplier)
        {
            return amount < goal - (goal * multiplier);
        }

        bool Distributable(MyInventoryItem item, BlockDefinition block)
        {
            if (LoadoutHome(block, item))
                return false;

            string subtypeID = item.Type.SubtypeId;

            return (IsOre(item) && RefinedOre(item) && (double)item.Amount >= oreMinimum) || IsAmmo(item) || IsFuel(item) || (typedIndexes[setKeyIndexGravelSifters].Count > 0 && IsIngot(item) && subtypeID == stoneType) ||
                   (IsComponent(item) && subtypeID == canvasType) || IsGas(item);
        }

        bool RefinedOre(MyInventoryItem item)
        {
            ItemDefinition def;
            if (GetDefinition(out def, item.Type.ToString()))
                return def.refine;

            return true;
        }

        double DefaultMax(MyInventoryItem item, BlockDefinition block)
        {
            return DefaultMax(item.Type.TypeId, item.Type.SubtypeId, block);
        }

        double DefaultMax(string typeID, string subtypeID, BlockDefinition blockDef)
        {
            IMyTerminalBlock block = blockDef.block;
            if (block is IMyGasGenerator)
            {
                if (IsGas(typeID, subtypeID))
                    return PercentageMax((float)GetKeyDouble(setKeyIcePerGenerator), typeID, subtypeID, block);
            }
            else if (block is IMyRefinery)
            {
                if (activeOres > 0)
                    return ((double)blockDef.Input.MaxVolume / activeOres) / ItemVolume(typeID, subtypeID);
            }
            else
                return
                    IsGun(blockDef) ? Math.Ceiling(PercentageMax((float)GetKeyDouble(setKeyAmmoPerGun), typeID, subtypeID, block)) :
                    block is IMyReactor ? PercentageMax((float)GetKeyDouble(setKeyFuelPerReactor), typeID, subtypeID, block) :
                    block is IMyParachute ? Math.Ceiling(PercentageMax((float)GetKeyDouble(setKeyCanvasPerParachute), typeID, subtypeID, block)) : double.MaxValue;

            return double.MaxValue;
        }

        static double PercentageMax(float value, string typeID, string subtypeID, IMyTerminalBlock block)
        {
            float calcedValue = value;
            if (value <= 1f)
            {
                calcedValue = ((float)block.GetInventory(0).MaxVolume / ItemVolume(typeID, subtypeID)) * value;
                if (!FractionalItem(typeID, subtypeID))
                    calcedValue = (float)Math.Floor(calcedValue);
            }

            return calcedValue;
        }

        static float ItemVolume(string typeID, string subtypeID)
        {
            string key = $"{typeID}/{subtypeID}";

            try
            {
                MyItemType type = MyItemType.Parse(key);
                if (type.GetItemInfo().Volume > 0f)
                    return type.GetItemInfo().Volume;
            }
            catch { }

            return 0.17f;
        }

        bool IsFuel(MyInventoryItem item)
        {
            ItemDefinition definition;
            return GetDefinition(out definition, item.Type.ToString()) && definition.fuel;
        }

        string ItemName(MyInventoryItem item)
        {
            return ItemName(item.Type.TypeId, item.Type.SubtypeId);
        }

        string ItemName(string typeID, string subtypeID)
        {
            ItemDefinition definition;
            if (GetDefinition(out definition, $"{typeID}/{subtypeID}"))
                return definition.displayName;

            return subtypeID;
        }

        double GetCurrentVolumeLimit(MyInventoryItem item, IMyTerminalBlock block)
        {
            return AvailableVolume(block) / ItemVolume(item);
        }

        static bool FractionalItem(string typeID, string subtypeID)
        {
            string key = $"{typeID}/{subtypeID}";

            try
            {
                MyItemType type = MyItemType.Parse(key);
                if (type.GetItemInfo().Volume > 0f)
                    return type.GetItemInfo().UsesFractions;
            }
            catch { }

            return typeID == ingotType || typeID == oreType;
        }

        static bool FractionalItem(MyInventoryItem item)
        {
            return item.Type.GetItemInfo().UsesFractions;
        }

        bool Sortable(BlockDefinition blockDef, int inventoryIndex)
        {
            if ((inventoryIndex == 0 && blockDef.Settings.GetOption(setKeyBlockKeepInputKey)) || (inventoryIndex == 1 && blockDef.Settings.GetOption(setKeyBlockKeepOutputKey)))
                return false;

            IMyTerminalBlock block = blockDef.block;
            if (blockDef.isGravelSifter || block is IMyRefinery)
                return inventoryIndex == 1;

            if (block is IMyAssembler)
            {
                IMyAssembler assembler = (IMyAssembler)block;
                return assembler.IsQueueEmpty || (assembler.Mode == assemblyMode && inventoryIndex == 1) || (assembler.Mode == disassemblyMode && inventoryIndex == 0);
            }
            if (block is IMyReactor || block is IMyParachute || IsGun(blockDef) || block is IMySafeZoneBlock)
                return false;

            return true;
        }

        bool IsGun(BlockDefinition block)
        {
            return block.block is IMyUserControllableGun || block.Settings.GetOption(setKeyBlockGunOverrideKey);
        }

        bool Sortable(MyInventoryItem item, BlockDefinition blockDef, int inventoryIndex = 0)
        {
            IMyTerminalBlock block = blockDef.block;
            if (fillingBottles && IsBottle(item))
                return !(block is IMyGasGenerator || block is IMyGasTank);

            if (inventoryIndex == 0 && (LoadoutHome(blockDef, item) || IsStorage(blockDef, item) || (block is IMyGasGenerator && (IsGas(item)))))
                return false;

            if (block is IMyAssembler)
            {
                IMyAssembler assembler = (IMyAssembler)block;
                return assembler.IsQueueEmpty || (inventoryIndex == 0 && assembler.Mode == disassemblyMode) || (inventoryIndex == 1 && assembler.Mode == assemblyMode);
            }
            if (block is IMyShipWelder && ((IMyShipWelder)block).Enabled && IsComponent(item))
                return false;

            return true;
        }

        bool IsStorage(BlockDefinition block, MyInventoryItem item)
        {
            return block.Settings.storageCategories.Contains("all", stringComparer) || block.Settings.storageCategories.Contains("*") ||
                   block.Settings.storageCategories.Contains(GetItemCategory(item), stringComparer);
        }

        bool LoadoutHome(BlockDefinition block, MyInventoryItem item)
        {
            return block.Settings.loadout.ItemCount(item) > 0;
        }

        float ItemVolume(MyInventoryItem item)
        {
            return item.Type.GetItemInfo().Volume;
        }

        bool IsBottle(MyInventoryItem item)
        {
            return IsBottle(item.Type.TypeId);
        }

        bool IsBottle(string typeID)
        {
            return StringsMatch(typeID, hydBottleType) || StringsMatch(typeID, oxyBottleType);
        }

        static bool IsIngot(string typeID)
        {
            return IsWildCard(typeID) || StringsMatch(typeID, ingotType) || StringsMatch(typeID, ingotKeyword) || (typeID.Length > 1 && LeadsString(ingotKeyword, typeID));
        }

        static bool IsOre(string typeID)
        {
            return IsWildCard(typeID) || StringsMatch(typeID, oreType) || StringsMatch(typeID, oreKeyword) || (typeID.Length > 1 && LeadsString(oreKeyword, typeID));
        }

        bool IsAmmo(string typeID)
        {
            return IsWildCard(typeID) || StringsMatch(typeID, ammoType) || StringsMatch(typeID, ammoKeyword) || (typeID.Length > 1 && LeadsString(ammoKeyword, typeID));
        }

        bool GetItemFromBlueprint(string blueprintID, out string typeID, out string subtypeID)
        {
            Blueprint blueprint;
            typeID = subtypeID = "";
            if (blueprintList.TryGetValue(blueprintID, out blueprint))
            {
                typeID = blueprint.typeID;
                subtypeID = blueprint.subtypeID;
                return true;
            }
            return false;
        }

        bool IsComponent(string typeID)
        {
            return IsWildCard(typeID) || StringsMatch(typeID, componentType) || StringsMatch(typeID, componentKeyword) || (typeID.Length > 1 && LeadsString(componentKeyword, typeID));
        }

        bool IsTool(string typeID)
        {
            return IsWildCard(typeID) || StringsMatch(typeID, toolType) || IsBottle(typeID) || StringsMatch(typeID, dataPadType) || StringsMatch(typeID, consumableType) || StringsMatch(typeID, physicalObjectType) || StringsMatch(typeID, toolKeyword) ||
                (
                    typeID.Length > 1 &&
                    (
                        LeadsString(toolType, typeID) ||
                        LeadsString(dataPadType, typeID) ||
                        LeadsString(consumableType, typeID) ||
                        LeadsString(physicalObjectType, typeID) ||
                        LeadsString(toolKeyword, typeID)
                    )
                );
        }

        bool EndsString(string whole, string end)
        {
            return RemoveSpaces(whole, true).EndsWith(RemoveSpaces(end, true));
        }

        static bool LeadsString(string whole, string lead)
        {
            return RemoveSpaces(whole, true).StartsWith(RemoveSpaces(lead, true));
        }

        bool IsIngot(MyInventoryItem item)
        {
            return item.Type.GetItemInfo().IsIngot;
        }

        bool IsOre(MyInventoryItem item)
        {
            return item.Type.GetItemInfo().IsOre;
        }

        bool IsComponent(MyInventoryItem item)
        {
            return item.Type.GetItemInfo().IsComponent;
        }

        bool IsAmmo(MyInventoryItem item)
        {
            return item.Type.GetItemInfo().IsAmmo;
        }

        bool IsCategory(string typeID)
        {
            return IsWildCard(typeID) || itemCategoryList.Contains(typeID, stringComparer);
        }

        bool UnavailableActions()
        {
            double mult = 1;
            mult = Math.Max(0.00001, Math.Min(mult, overheatAverage > 0.0 ? 1.0 - (torchAverage / (overheatAverage * 1.001)) : 1.0 - (torchAverage / (runTimeLimiter * 2.0))));

            return
            Runtime.CurrentInstructionCount >= (Runtime.MaxInstructionCount * actionLimiterMultiplier * mult) ||
            (Now - tickStartTime).TotalMilliseconds >= runTimeLimiter * mult;
        }

        bool StateManager(FunctionIdentifier identifier, bool updateStatus = true, bool reportErrors = true)
        {
            if (identifier == FunctionIdentifier.Idle) return true;

            if (updateStatus) currentFunction = identifier; // Update echo status

            if (!StateInitialized(identifier)) InitializeStateV2(identifier); // Initialize enumerator


            bool endReached;
            DateTime startTime = Now, endTime;
            int currentActions = Runtime.CurrentInstructionCount;
            FunctionState currentState;
            try
            {
                endReached = !stateRecords[identifier].enumerator.MoveNext();
                currentState = stateRecords[identifier].lastStatus = stateRecords[identifier].enumerator.Current;
            }
            catch
            {
                currentState = stateRecords[identifier].lastStatus = stateError;
            }
            endTime = Now;
            if (currentState == stateError && reportErrors)
                Output($"Error in function: {identifier}{(stateRecords[identifier].errorCode.Length > 0 ? $" : {stateRecords[identifier].errorCode}" : "")}");

            stateRecords[identifier].PostRun(Runtime.CurrentInstructionCount - currentActions, Now - startTime, currentState == stateError, reportErrors, currentState != stateActive);

            if (currentState != stateActive)
            {
                scriptHealth = 0;
                foreach (StateRecord record in stateRecords.Values)
                    scriptHealth += (double)record.health;
                scriptHealth /= (double)stateRecords.Count;

                StateDisposal(identifier, currentState == stateError);

                if (identifier == selfContainedIdentifier)
                    selfContainedIdentifier = FunctionIdentifier.Idle;
                if (identifier == currentMajorFunction)
                    currentMajorFunction = FunctionIdentifier.Idle;
            }

            return currentState != stateActive;
        }

        double Round(double number, int decimals = 2)
        {
            return Math.Round(number, decimals);
        }

        static string ShortNumberAbs2(double number, List<string> suffixes, int decimals)
        {
            double divisor = 1, currentNumber = Math.Abs(number);
            int index = -1, highestIndex = -1;

            while (number >= divisor * 1000.0 && index + 1 < suffixes.Count)
            {
                divisor *= 1000.0;
                index++;
                if (TextHasLength(suffixes[index]))
                    highestIndex = index;
            }

            currentNumber /= Math.Pow(1000, highestIndex + 1);

            return highestIndex >= 0 ? $"{TruncateNumber(currentNumber, decimals)}{suffixes[highestIndex]}" : TruncateNumber(currentNumber, decimals);
        }

        string ShortNumber2(double number, int decimals = 2, int padding = 0, bool left = true)
        {
            return ShortNumber2(number, settingsListsStrings[setKeyDefaultSuffixes], decimals, padding, left);
        }

        static string ShortNumber2(double number, List<string> suffixesList, int decimals = 2, int padding = 0, bool left = true)
        {
            string prefix = number < 0 ? "-" : "";

            return left ? $"{prefix}{ShortNumberAbs2(Math.Abs(number), suffixesList, decimals)}".PadLeft(padding) : $"{prefix}{ShortNumberAbs2(Math.Abs(number), suffixesList, decimals)}".PadRight(padding);
        }

        static bool TextHasLength(string text)
        {
            return text.Length > 0;
        }

        static bool BuilderHasLength(StringBuilder builder)
        {
            return builder.Length > 0;
        }

        static string RemoveSpaces(string text, bool lower = false)
        {
            return lower ? text.Replace(" ", "").ToLower() : text.Replace(" ", "");
        }

        static bool IsWildCard(string text)
        {
            return StringsMatch(text, "all") || text == "*";
        }


        #endregion


        #region Methods

        void PopulateItemList(List<ItemDefinition> list)
        {
            list.Clear();
            list.AddRange(GetAllItems);
        }

        void OptionalEcho(string text, bool condition)
        {
            Echo($"{(condition ? text : echoSpacer)}");
        }

        void InitState(IEnumerator<FunctionState> stateFunction, FunctionIdentifier identifier)
        {
            stateRecords[identifier].enumerator = stateFunction;

            stateRecords[identifier].lastStatus = stateContinue;

            if (!booted) stateRecords[identifier].enumerator.MoveNext();
        }

        void InitializeStateV2(FunctionIdentifier identifier)
        {
            switch (identifier)
            {
                case FunctionIdentifier.Script:
                    InitState(ScriptState(), identifier);
                    break;
                case FunctionIdentifier.Main_Control:
                    InitState(ControlState(), identifier);
                    break;
                case FunctionIdentifier.Main_Output:
                    InitState(OutputState(), identifier);
                    break;
                case FunctionIdentifier.Main_Sprites:
                    InitState(panelMaster.PopulateSpriteState(), identifier);
                    break;
                case FunctionIdentifier.Processing_Block_Options:
                    InitState(ProcessBlockOptionState(), identifier);
                    break;
                case FunctionIdentifier.Status_Panel:
                    InitState(panelMaster.StatusPanelState(), identifier);
                    break;
                case FunctionIdentifier.Measuring_Capacities:
                    InitState(panelMaster.CargoCapacityState(), identifier);
                    break;
                case FunctionIdentifier.Output_Panel:
                    InitState(panelMaster.OutputPanelState(), identifier);
                    break;
                case FunctionIdentifier.Counting_Listed_Items:
                    InitState(CountListState(), identifier);
                    break;
                case FunctionIdentifier.Distribution:
                    InitState(DistributeState(), identifier);
                    break;
                case FunctionIdentifier.Distributing_Item:
                    InitState(DistributeItemState(), identifier);
                    break;
                case FunctionIdentifier.Counting_Item_In_Inventory:
                    InitState(AmountContainedState(), identifier);
                    break;
                case FunctionIdentifier.Processing_Limits:
                    InitState(ProcessLimitsState(), identifier);
                    break;
                case FunctionIdentifier.Sorting:
                    InitState(SortState(), identifier);
                    break;
                case FunctionIdentifier.Storing_Item:
                    InitState(PutInStorageState(), identifier);
                    break;
                case FunctionIdentifier.Counting_Blueprints:
                    InitState(CountBlueprintState(), identifier);
                    break;
                case FunctionIdentifier.Counting_Items:
                    InitState(CountState(), identifier);
                    break;
                case FunctionIdentifier.Scanning:
                    InitState(ScanState(), identifier);
                    break;
                case FunctionIdentifier.Generating_Sprites:
                    InitState(panelMaster.PopulateSpriteListState(), identifier);
                    break;
                case FunctionIdentifier.Item_Panel:
                    InitState(panelMaster.ItemPanelState(), identifier);
                    break;
                case FunctionIdentifier.Order_Inventory:
                    InitState(OrderInventoryState(), identifier);
                    break;
                case FunctionIdentifier.Processing_Tags:
                    InitState(GetTagState(), identifier);
                    break;
                case FunctionIdentifier.Transferring_Item:
                    InitState(TransferState(), identifier);
                    break;
                case FunctionIdentifier.Spreading_Items:
                    InitState(BalanceState2(), identifier);
                    break;
                case FunctionIdentifier.Cargo_Panel:
                    InitState(panelMaster.CargoPanelState(), identifier);
                    break;
                case FunctionIdentifier.Distributing_Blueprint:
                    InitState(DistributeBlueprintState(), identifier);
                    break;
                case FunctionIdentifier.Removing_Excess_Assembly:
                    InitState(RemoveExcessAssemblyState(), identifier);
                    break;
                case FunctionIdentifier.Generating_Block_Options:
                    InitState(GenerateBlockOptionState(), identifier);
                    break;
                case FunctionIdentifier.Setting_Block_Quotas:
                    InitState(SetBlockQuotaState(), identifier);
                    break;
                case FunctionIdentifier.Save:
                    InitState(SaveState(), identifier);
                    break;
                case FunctionIdentifier.Queue_Assembly:
                    InitState(QueueAssemblyState(), identifier);
                    break;
                case FunctionIdentifier.Queue_Disassembly:
                    InitState(QueueDisassemblyState(), identifier);
                    break;
                case FunctionIdentifier.Inserting_Blueprint:
                    InitState(InsertBlueprintState(), identifier);
                    break;
                case FunctionIdentifier.Process_Panel_Options:
                    InitState(panelMaster.ProcessPanelOptionState(), identifier);
                    break;
                case FunctionIdentifier.Removing_Blueprint:
                    InitState(RemoveBlueprintState(), identifier);
                    break;
                case FunctionIdentifier.Removing_Excess_Disassembly:
                    InitState(RemoveExcessDisassemblyState(), identifier);
                    break;
                case FunctionIdentifier.Order_Blocks_By_Priority:
                    InitState(OrderListByPriorityState(), identifier);
                    break;
                case FunctionIdentifier.Cargo_Priority_Loop:
                    InitState(SortCargoPriorityState(), identifier);
                    break;
                case FunctionIdentifier.Sorting_Cargo_Priority:
                    InitState(SortCargoListState(), identifier);
                    break;
                case FunctionIdentifier.Sort_Blueprints:
                    InitState(SortBlueprintState(), identifier);
                    break;
                case FunctionIdentifier.Spread_Blueprints:
                    InitState(SpreadBlueprintStateV2(), identifier);
                    break;
                case FunctionIdentifier.Load:
                    InitState(LoadState(), identifier);
                    break;
                case FunctionIdentifier.Loadouts:
                    InitState(LoadoutState(), identifier);
                    break;
                case FunctionIdentifier.Sort_Refineries:
                    InitState(SortRefineryStateV2(), identifier);
                    break;
                case FunctionIdentifier.Custom_Logic:
                    InitState(LogicState(), identifier);
                    break;
                case FunctionIdentifier.Matching_Items:
                    InitState(MatchingItemState(), identifier);
                    break;
                case FunctionIdentifier.Process_Logic:
                    InitState(ProcessTimerState(), identifier);
                    break;
                case FunctionIdentifier.Checking_Idle_Assemblers:
                    InitState(IdleAssemblerState(), identifier);
                    break;
                case FunctionIdentifier.Find_Mod_Items:
                    InitState(FindModItemState(), identifier);
                    break;
                case FunctionIdentifier.Process_Setting:
                    InitState(ProcessSettingState(), identifier);
                    break;
                case FunctionIdentifier.Main_Panel:
                    InitState(panelMaster.TotalPanelStateV2(), identifier);
                    break;
                case FunctionIdentifier.Assembly_Reserve:
                    InitState(AddAssemblyState(), identifier);
                    break;
                case FunctionIdentifier.Processing_Item_Setting:
                    InitState(ProcessItemSettingState(), identifier);
                    break;
                case FunctionIdentifier.Order_Storage:
                    InitState(OrderCargoState(), identifier);
                    break;
            }
        }

        static double TorchAverage(double a, double b)
        {
            return (a + (b * tickWeight)) * (1.0 - tickWeight);
        }

        static void BuilderAppendLine(StringBuilder builder, string text = "")
        {
            builder.AppendLine(text);
        }

        void PadEcho(int count = 10)
        {
            for (int i = 1; i <= count; i++)
                Echo(echoSpacer);
        }

        void FinalizeKeys(ItemDefinition definition)
        {
            definition.FinalizeKeys();
            if (definition.oreKeys.Count > 0)
                oreKeyedItemDictionary[definition.FullID] = definition.subtypeID;
            else
                oreKeyedItemDictionary.Remove(definition.FullID);
        }

        void AddCategory(string category)
        {
            if (!itemCategoryList.Contains(category))
                itemCategoryList.Add(category);
            if (!indexesStorageLists.ContainsKey(category))
                indexesStorageLists[category] = NewLongListPlus;
        }

        void AppendHeader(StringBuilder builder, string header)
        {
            string prefix = "", suffix = "", spacer = "", cap = "";
            for (int i = 0; i < header.Length; i++)
            {
                prefix += "/";
                suffix += @"\";
                spacer += " ";
                cap += "|";
            }
            BuilderAppendLine(builder);

            BuilderAppendLine(builder, $"{prefix}{spacer}{suffix}{newLine}{cap}  {header}  {cap}{newLine}{suffix}{spacer}{prefix}{newLine}");
        }

        static void SplitID(string itemID, out string typeID, out string subtypeID)
        {
            int index = itemID.IndexOf("/");
            if (index == -1)
            {
                typeID = itemID;
                subtypeID = itemID;
                return;
            }
            typeID = itemID.Substring(0, index);
            subtypeID = itemID.Substring(index + 1);
        }

        void SetLastString(string text)
        {
            lastString = text;
            lastActionClearTime = Now.AddSeconds(15);
        }

        void SetItemQuotaMain(string name, string amountString)
        {
            if (TextHasLength(name))
            {
                double amount, maxAmount;
                ParseAmountAndMax(amountString, out amount, out maxAmount);
                bool category = IsCategory(name);
                SetTypeQuota(amount, maxAmount, category ? "" : name, category ? (IsWildCard(name) ? "" : name) : "");
            }
        }

        void ParseAmountAndMax(string data, out double amount, out double maxAmount)
        {
            int index = data.IndexOf("<");
            amount = index > 0 ? double.Parse(data.Substring(0, index)) : double.Parse(data);
            maxAmount = index > 0 ? double.Parse(data.Substring(index + 1)) : amount;
        }

        void SetTypeQuota(double amount, double maxAmount, string filter = "", string category = "")
        {
            List<ItemDefinition> itemList = GetAllItems;
            string subFilter = RemoveSpaces(filter);
            bool exactMatch = subFilter.Length > 2 && LeadsString(subFilter, "'") && EndsString(subFilter, "'");
            if (exactMatch)
                subFilter = subFilter.Substring(1, subFilter.Length - 2);
            for (int i = 0; i < itemList.Count; i++)
                if ((!TextHasLength(category) || StringsMatch(category, itemList[i].category)) &&
                    (!TextHasLength(subFilter) || (!exactMatch && LeadsString(itemList[i].displayName, subFilter)) || (exactMatch && StringsMatch(RemoveSpaces(itemList[i].displayName), subFilter))))
                    SetDefinitionQuota(itemList[i], amount, maxAmount);
        }

        void SetDefinitionQuota(ItemDefinition definition, double amount, double max)
        {
            double maxAmount = max + definition.blockQuota;
            definition.quota = amount;
            definition.quotaMax = amount <= max ? max : amount;
            definition.currentMax = maxAmount >= definition.quotaMax ? maxAmount : definition.quotaMax;

            if (definition.quotaMax < 0)
                definition.quotaMax = 0;

            definition.SetCurrentQuota();
        }

        void AddModItem(MyInventoryItem item)
        {
            string subtypeID = item.Type.SubtypeId, itemID = item.Type.ToString(), blueprintMatchKey = subtypeID;
            if (AddItemDef(subtypeID, subtypeID, item.Type.TypeId, ""))
                saving = true;

            if (IsIngot(item) || IsOre(item))
                return;

            if (modBlueprintList.Contains(blueprintMatchKey) || HasBlueprintMatch(item, ref blueprintMatchKey))
            {
                if (UpdateItemDef(itemID, blueprintMatchKey))
                {
                    SetLastString($"Merged Mod Item: {ShortenName(subtypeID, 15)}");
                    saving = true;
                }
            }
            else if (!modItemDictionary.ContainsKey(itemID))
                modItemDictionary[itemID] = subtypeID;
        }

        void AddModBlueprint(MyProductionItem blueprint)
        {
            string subtypeID = BlueprintSubtype(blueprint), itemMatchKey = "";
            if (HasItemMatch(blueprint, ref itemMatchKey))
            {
                if (UpdateItemDef(itemMatchKey, subtypeID))
                {
                    SetLastString($"Merged Mod Item: {ShortenName(itemMatchKey, 15)}");
                    saving = true;
                }
            }
            else if (!modBlueprintList.Contains(subtypeID))
                modBlueprintList.Add(subtypeID);
        }

        void ClearFunctions()
        {
            foreach (FunctionIdentifier identifier in stateRecords.Keys)
                if (StateInitialized(identifier))
                    StateDisposal(identifier);
        }

        void Output(string output)
        {
            if (TextHasLength(output))
            {
                if (LeadsString(output, "Error"))
                {
                    currentErrorCount++;
                    totalErrorCount++;
                    AddOutput(output, outputErrorList);
                    SetLastString(output);
                }
                AddOutput(output, outputList, outputLimit);
            }
        }

        void AddOutput(string output, List<OutputObject> list, int limit = 30)
        {
            bool unique = true;

            for (int i = 0; i < list.Count; i++)
                if (list[i].text == output)
                {
                    list[i].count++;
                    list.Move(i, 0);
                    unique = false;
                    break;
                }

            if (unique)
                list.Insert(0, new OutputObject { text = output });

            if (list.Count > limit)
                list.RemoveRange(limit, list.Count - limit);
        }

        void ResetRuntimes()
        {
            Runtime.UpdateFrequency = UpdateFrequency.None;
            Runtime.UpdateFrequency &= ~UpdateFrequency.Update1;
            Runtime.UpdateFrequency &= ~UpdateFrequency.Update10;
            Runtime.UpdateFrequency &= ~UpdateFrequency.Update100;

            Runtime.UpdateFrequency = updateFrequency == 100 ? UpdateFrequency.Update100 : updateFrequency == 1 ? UpdateFrequency.Update1 : UpdateFrequency.Update10;
        }

        void AddBlueprintAmount(string blueprintID, bool assembly, double amount, bool current)
        {
            Blueprint blueprint;
            if (blueprintList.TryGetValue(blueprintID, out blueprint))
                AddBlueprintAmount(blueprint.typeID, blueprint.subtypeID, assembly, amount, current);
        }

        void AddBlueprintAmount(MyProductionItem item, bool assembly, bool current = false)
        {
            if (blueprintList.ContainsKey(BlueprintSubtype(item)))
            {
                Blueprint blueprint = blueprintList[BlueprintSubtype(item)];
                AddBlueprintAmount(blueprint.typeID, blueprint.subtypeID, assembly, (double)item.Amount, current);
            }
        }

        void AddBlueprintAmount(string typeID, string subtypeID, bool assembly, double amount, bool current)
        {
            ItemDefinition definition;
            if (GetDefinition(out definition, $"{typeID}/{subtypeID}"))
            {
                if (assembly)
                    definition.AddAssemblyAmount((MyFixedPoint)amount, current);
                else
                    definition.AddDisassemblyAmount((MyFixedPoint)amount, current);
            }
        }

        void AddAmount(MyInventoryItem item)
        {
            ItemDefinition definition;
            if (GetDefinition(out definition, item.Type.ToString()))
                definition.AddAmount(item.Amount);
        }

        void ConveyorControl(BlockDefinition managedBlock)
        {
            IMyTerminalBlock block = managedBlock.block;
            bool applyOverride = false, autoConveyor = managedBlock.Settings.GetOption(setKeyBlockAutoConveyorKey), tempBool;

            if (block is IMyAssembler)
                ((IMyProductionBlock)block).UseConveyorSystem = true;
            else if (block is IMyRefinery)
                ((IMyProductionBlock)block).UseConveyorSystem = autoConveyor || GetKeyBool(setKeyAutoConveyorRefineries);
            else if (block is IMyReactor)
                ((IMyReactor)block).UseConveyorSystem = autoConveyor || GetKeyBool(setKeyAutoConveyorReactors);
            else if (block is IMyGasGenerator)
                ((IMyGasGenerator)block).UseConveyorSystem = autoConveyor || GetKeyBool(setKeyAutoConveyorGasGenerators);
            else if (block is IMySmallGatlingGun)
            {
                tempBool = ((IMySmallGatlingGun)block).UseConveyorSystem;
                if (autoConveyor || GetKeyBool(setKeyAutoConveyorGuns))
                    applyOverride = !tempBool;
                else
                    applyOverride = tempBool;
            }
            else if (block is IMyLargeGatlingTurret)
            {
                tempBool = ((IMyLargeGatlingTurret)block).UseConveyorSystem;
                if (autoConveyor || GetKeyBool(setKeyAutoConveyorGuns))
                    applyOverride = !tempBool;
                else
                    applyOverride = tempBool;
            }
            else if (block is IMyLargeMissileTurret)
            {
                tempBool = ((IMyLargeMissileTurret)block).UseConveyorSystem;
                if (autoConveyor || GetKeyBool(setKeyAutoConveyorGuns))
                    applyOverride = !tempBool;
                else
                    applyOverride = tempBool;
            }
            else if (block is IMySmallMissileLauncherReload)
            {
                tempBool = ((IMySmallMissileLauncherReload)block).UseConveyorSystem;
                if (autoConveyor || GetKeyBool(setKeyAutoConveyorGuns))
                    applyOverride = !tempBool;
                else
                    applyOverride = tempBool;
            }
            else if (block is IMySmallMissileLauncher)
            {
                tempBool = ((IMySmallMissileLauncher)block).UseConveyorSystem;
                if (autoConveyor || GetKeyBool(setKeyAutoConveyorGuns))
                    applyOverride = !tempBool;
                else
                    applyOverride = tempBool;
            }
            if (applyOverride)
                block.ApplyAction("UseConveyor");
        }

        bool AddItemDef(string name, string subtypeID, string typeID, string blueprintID = nothingType, bool display = true, List<string> oreKeys = null)
        {
            if (!itemListMain.ContainsKey(typeID))
                itemListMain[typeID] = new SortedList<string, ItemDefinition>();

            if (!itemListMain[typeID].ContainsKey(subtypeID))
            {
                itemListMain[typeID][subtypeID] = new ItemDefinition
                {
                    typeID = typeID,
                    subtypeID = subtypeID,
                    displayName = name,
                    blueprintID = blueprintID,
                    display = display
                };
                InitialDefinition($"{typeID}/{subtypeID}", blueprintID, oreKeys);
                itemAddedOrChanged = Now;
                return true;
            }
            return false;
        }

        void InitialDefinition(string itemID, string blueprintID, List<string> oreKeys = null)
        {
            ItemDefinition definition;
            if (GetDefinition(out definition, itemID))
            {
                if (IsIngot(definition.typeID))
                {
                    definition.quota = 100;
                    definition.quotaMax = 100;
                    if (definition.subtypeID == "Uranium")
                        definition.fuel = true;
                }
                definition.category = GetItemCategory(definition.FullID);
                if (IsIce(definition.typeID, definition.subtypeID))
                {
                    definition.gas = true;
                    definition.refine = false;
                }
                else if (IsOre(definition.typeID))
                    definition.refine = true;

                UpdateItemDef(itemID, blueprintID, oreKeys);
            }
        }

        bool UpdateItemDef(string itemID, string blueprintID = "", List<string> oreKeys = null)
        {
            ItemDefinition definition;
            if (GetDefinition(out definition, itemID))
            {
                if (IsIngot(definition.typeID) && definition.oreKeys.Count == 0)
                    definition.oreKeys.Add(definition.subtypeID);

                if (oreKeys != null)
                    definition.oreKeys.AddRange(oreKeys);

                FinalizeKeys(definition);
                if (IsBlueprint(definition.blueprintID))
                    blueprintList.Remove(definition.blueprintID);

                definition.blueprintID = blueprintID;
                if (IsBlueprint(blueprintID))
                    blueprintList[blueprintID] = ItemToBlueprint(definition);

                string category = definition.category;
                itemCategoryDictionary[definition.FullID] = category;
                AddCategory(category);
                CheckModdedItem(definition);
                itemAddedOrChanged = Now;
                return true;
            }
            return false;
        }

        void CheckModdedItem(ItemDefinition definition)
        {
            if (!IsIngot(definition.typeID) && !IsOre(definition.typeID) && !TextHasLength(definition.blueprintID))
            {
                if (!modItemDictionary.ContainsKey(definition.FullID))
                    modItemDictionary[definition.FullID] = definition.subtypeID;
            }
            else
                modItemDictionary.Remove(definition.FullID);

            if (IsBlueprint(definition.blueprintID))
                modBlueprintList.Remove(definition.blueprintID);
        }

        void StateDisposal(FunctionIdentifier identifier, bool dispose = true)
        {
            if (dispose)
            {
                try
                {
                    stateRecords[identifier].enumerator.Dispose();
                }
                catch { }
                try
                {
                    stateRecords[identifier].enumerator = null;
                }
                catch { }
                stateRecords[identifier].lastStatus = stateUninitialized;
            }
            stateRecords[identifier].errorCode = "";
        }

        static void AppendOption(StringBuilder builder, string option, bool optional = true)
        {
            BuilderAppendLine(builder, $"{(optional ? "//" : "")}{option}");
        }


        #endregion


        #region Classes

        public class LongListPlus : List<long>
        {
            private List<long> _list = NewLongList;

            #pragma warning disable CS0108 // Member hides inherited member; missing new keyword
            public IEnumerator<long> GetEnumerator()
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
            {
                return new List<long>(_list).GetEnumerator();
            }
        }

        public class BlueprintSpreadInformation
        {
            public List<long> acceptingIndexList = NewLongList;

            public double totalCount = 0;

            public SortedList<long, double> individualCounts = NewSortedListLongDouble;

            public void AddCount(long entityID, double count)
            {
                if (!individualCounts.ContainsKey(entityID))
                    individualCounts[entityID] = 0;
                individualCounts[entityID] += count;
                totalCount += count;
            }
        }

        public class StateRecord
        {
            public IEnumerator<FunctionState> enumerator;

            public FunctionState lastStatus = stateUninitialized;

            public int currentTicks = 0, currentActions = 0, lastTicks = 0, lastActions = 0, runs = 0,
                       minTicks = 0, maxTicks = 0, minActions = 0, maxActions = 0;
            public TimeSpan
                minSpan = TimeSpan.Zero, maxSpan = TimeSpan.Zero, currentSpan = TimeSpan.Zero, lastSpan = TimeSpan.Zero;
            public double averageTime = 0, averageActions = 0;
            public decimal health = 100m;

            public string errorCode = "";

            public void PostRun(int actions, TimeSpan span, bool errorCaught, bool reportError, bool complete)
            {
                if (errorCaught)
                {
                    currentTicks = currentActions = 0;
                    currentSpan = TimeSpan.Zero;
                    if (reportError)
                        health = Math.Max(0m, health - 5m);
                    return;
                }
                currentActions += actions;
                currentSpan += span;
                currentTicks++;
                if (complete)
                {
                    if (runs == 0)
                    {
                        minTicks = maxTicks = currentTicks;
                        minActions = maxActions = currentActions;
                        minSpan = maxSpan = currentSpan;
                    }
                    else
                    {
                        minTicks = Math.Min(minTicks, currentTicks);
                        maxTicks = Math.Max(maxTicks, currentTicks);
                        minActions = Math.Min(minActions, currentActions);
                        maxActions = Math.Max(maxActions, currentActions);
                        minSpan = currentSpan < minSpan ? currentSpan : minSpan;
                        maxSpan = currentSpan > maxSpan ? currentSpan : maxSpan;
                    }
                    lastActions = currentActions;
                    lastSpan = currentSpan;
                    lastTicks = currentTicks;
                    averageTime = TorchAverage(averageTime, currentSpan.TotalMilliseconds);
                    averageActions = TorchAverage(averageActions, currentActions);



                    currentTicks = currentActions = 0;
                    currentSpan = TimeSpan.Zero;
                    runs++;
                    if (health < 100m)
                    {
                        health += (100m - health) / 10m;
                        if (health >= 99.999m)
                            health = 100m;
                    }
                }
            }

            public override string ToString()
            {
                return $"Ticks:{minTicks}-{maxTicks}{newLine}" +
                       $"Actions:{minActions}-{maxActions} ~{averageActions:N0}{newLine}" +
                       $"Time:{minSpan.Milliseconds:N1}-{maxSpan.Milliseconds:N1} ~{averageTime:N3}ms{Environment.NewLine}" +
                       $"Status: {health:N1}%";
            }
        }

        public class MonitoredAssembler
        {
            MyAssemblerMode mode;
            public SortedList<string, double> productionComparison = new SortedList<string, double>();
            DateTime nextCheck = Now;
            float currentProgress = 0f;
            public IMyAssembler assembler;
            public bool stalling = false;

            public bool CheckNow => Now >= nextCheck;

            public bool HasChanged()
            {
                bool changed = !assembler.Enabled || assembler.IsQueueEmpty || assembler.CurrentProgress != currentProgress || assembler.Mode != mode;

                mode = assembler.Mode;
                currentProgress = assembler.CurrentProgress;

                return changed;
            }

            public void SetNextCheck(double delay)
            {
                nextCheck = Now.AddSeconds(delay);
            }

            public void Reset()
            {
                stalling = false;
                productionComparison.Clear();
                currentProgress = 0f;
            }
        }

        public class OutputObject
        {
            public string text = "";
            public int count = 1;
            public string Output => count > 1 ? $"{text} x{count}" : text;

        }

        public class SortableObject
        {
            public double amount = 0;
            public string key = "", text = "";
            public int integer = 0;
            public long numberLong = 0;
        }

        public class Blueprint
        {
            public string blueprintID = "", typeID = "", subtypeID = "";
            public double amount = 0, multiplier = 1;

            public Blueprint Clone()
            {
                return new Blueprint { blueprintID = blueprintID, typeID = typeID, subtypeID = subtypeID, amount = amount, multiplier = multiplier };
            }
        }

        public class VariableItemCount
        {
            public double count;
            public bool percentage, manual;

            public VariableItemCount(double countA, bool percentageA = false, bool manualA = false)
            {
                count = countA;
                percentage = percentageA;
                manual = manualA;
            }

            public override string ToString()
            {
                return $"{(percentage ? $"{count * 100.0}%" : $"{count}")}";
            }
        }

        public class ItemCollection
        {
            public static Program parent;

            public SortedList<string, VariableItemCount> itemList = new SortedList<string, VariableItemCount>();

            public int ItemTypeCount => itemList.Count;

            public bool trackAmounts = true;

            public bool IsEmpty
            {
                get
                {
                    for (int i = 0; i < itemList.Count; i++)
                        if (itemList.Values[i].count > 0)
                            return false;

                    return true;
                }
            }

            public void Clear(bool manual = true, bool automatic = true)
            {
                if (manual && automatic) itemList.Clear();
                else
                    for (int i = 0; i < itemList.Count; i += 0)
                    {
                        if ((manual && itemList.Values[i].manual) || (automatic && !itemList.Values[i].manual))
                            itemList.RemoveAt(i);
                        else
                            i++;
                    }
            }

            public void AddCollection(ItemCollection collection, IMyTerminalBlock block)
            {
                string typeID, subtypeID;
                foreach (KeyValuePair<string, VariableItemCount> kvp in collection.itemList)
                {
                    SplitID(kvp.Key, out typeID, out subtypeID);
                    AddItemInternal(kvp.Key, new VariableItemCount(kvp.Value.percentage ? PercentageMax((float)kvp.Value.count, typeID, subtypeID, block) : kvp.Value.count), true);
                }
            }

            public string CountByIndex(int index)
            {
                if (index < ItemTypeCount)
                    return itemList.Values[index].ToString();

                return "0";
            }

            public ItemDefinition ItemByIndex(int index)
            {
                if (index < ItemTypeCount)
                    return GetItem(index);

                return new ItemDefinition();
            }

            ItemDefinition GetItem(int index)
            {
                string typeID, subtypeID;
                double amount;
                SplitID(itemList.Keys[index], out typeID, out subtypeID);
                amount = itemList.Values[index].count;
                return new ItemDefinition { typeID = typeID, subtypeID = subtypeID, amount = amount };
            }

            public string ItemIDByIndex(int index)
            {
                return itemList.Keys[index];
            }

            public void AddItem(string typeID, string subtypeID, VariableItemCount amount, bool append = true)
            {
                string itemID = $"{typeID}/{subtypeID}";
                AddItemInternal(itemID, amount, append);
            }

            public void AddItem(MyInventoryItem item)
            {
                AddItem(item.Type.TypeId, item.Type.SubtypeId, new VariableItemCount((double)item.Amount));
            }

            void AddItemInternal(string itemID, VariableItemCount amount, bool append)
            {
                if (!itemList.ContainsKey(itemID) || !append || amount.manual && !itemList[itemID].manual) itemList[itemID] = amount;
                else if (itemList[itemID].manual == amount.manual && itemList[itemID].percentage == amount.percentage) itemList[itemID].count += amount.count;
            }

            public double ItemCount(MyInventoryItem item, IMyTerminalBlock block = null)
            {
                bool temp;
                return ItemCount(out temp, item, block);
            }

            public double ItemCount(string typeID, string subtypeID, IMyTerminalBlock block)
            {
                bool temp;
                return ItemCount(out temp, typeID, subtypeID, block);
            }

            public double ItemCount(out bool found, MyInventoryItem item, IMyTerminalBlock block)
            {
                return ItemCount(out found, item.Type.TypeId, item.Type.SubtypeId, block);
            }

            public double ItemCount(out bool found, string typeID, string subtypeID, IMyTerminalBlock block)
            {
                string itemID = $"{typeID}/{subtypeID}";
                if (itemList.ContainsKey(itemID))
                {
                    found = true;
                    return itemList[itemID].percentage && block != null ? PercentageMax((float)itemList[itemID].count, typeID, subtypeID, block) : itemList[itemID].count;
                }

                found = false;
                return 0;
            }

            public override string ToString()
            {
                SortedList<string, SortedList<string, List<ItemDAO>>> groupedItems = new SortedList<string, SortedList<string, List<ItemDAO>>>();
                List<string> itemGroups = new List<string>();

                string typeID, subtypeID, category;
                foreach (KeyValuePair<string, VariableItemCount> kvp in itemList)
                {
                    SplitID(kvp.Key, out typeID, out subtypeID);
                    category = parent.GetItemCategory(kvp.Key);

                    ItemDAO itemDAO = new ItemDAO(category, subtypeID, kvp.Value.ToString(), parent.ItemName(typeID, subtypeID));

                    if (!groupedItems.ContainsKey(category))
                        groupedItems[category] = new SortedList<string, List<ItemDAO>>();

                    if (!groupedItems[category].ContainsKey(itemDAO.amount))
                        groupedItems[category][itemDAO.amount] = new List<ItemDAO>();

                    groupedItems[category][itemDAO.amount].Add(itemDAO);
                }

                foreach (KeyValuePair<string, SortedList<string, List<ItemDAO>>> kvpA in groupedItems)
                    foreach (KeyValuePair<string, List<ItemDAO>> kvpB in kvpA.Value)
                        itemGroups.Add($"{(trackAmounts ? $"{kvpB.Key}:" : "")}{kvpA.Key}:{String.Join(":", kvpB.Value.Select(s => $"'{s}'"))}");

                return String.Join("|", itemGroups);
            }
        }

        public class LogicComparison
        {
            public static Program parent;

            public string typeID = "", comparison = "", compareAgainst = "";

            public override string ToString()
            {
                ItemDefinition def;
                return $"{(parent.GetDefinition(out def, typeID) ? $"{def.category}:{def.displayName}" : typeID)}{comparison}{compareAgainst}";
            }
        }

        public class PotentialAssembler
        {
            public long index;
            public bool empty, specific;
        }

        public class BlockDefinition
        {
            public IMyTerminalBlock block;

            public MonitoredAssembler monitoredAssembler;

            public BlockDefinition cloneSource = null;

            public SortedList<int, PanelMasterClass.PanelDefinition> panelDefinitionList = new SortedList<int, PanelMasterClass.PanelDefinition>();
            public string settingBackup = "", cloneGroup = "";
            public int headerIndex = 0;

            private BlockSettings innerSettings = new BlockSettings();

            private bool isClone = false;
            public bool isGravelSifter = false;

            public BlockSettings Settings => isClone && cloneSource != null ? cloneSource.Settings : innerSettings;

            public bool HasInventory => block.InventoryCount > 0;

            public IMyInventory Input => block.GetInventory(0);

            public bool IsClone => isClone;

            public BlockDefinition(IMyTerminalBlock block)
            {
                this.block = block;
                Settings.Initialize();
            }

            public void SetClone(BlockDefinition definition)
            {
                isClone = definition != null;
                cloneSource = definition;
                if (!isClone) Settings.Initialize();
            }

            public string DataSource
            {
                get { return block.CustomData; }
                set { block.CustomData = value; }
            }
        }

        public class BlockSettings
        {
            public List<LogicComparison> logicComparisons = new List<LogicComparison>();

            public List<string> storageCategories = NewStringList, tempSettings;

            public SortedList<string, SortedList<string, bool>> settingDictionaryBools = new SortedList<string, SortedList<string, bool>>
            {
                { setKeyBlockToggles, new SortedList<string, bool>() },
                { setKeyBlockInputToggles, new SortedList<string, bool>
                    {
                        { setKeyBlockStorageKey, false }, { setKeyBlockAutoConveyorKey, false }, { setKeyBlockKeepInputKey, false },
                        { setKeyBlockRemoveInputKey, false },  { setKeyBlockNoSortingKey, false }, { setKeyBlockNoSpreadKey, false },
                        { setKeyBlockNoCountKey, false }, { setKeyBlockGunOverrideKey, false }, { setKeyBlockLoadoutNoCount, false },
                        { setKeyBlockNoAutolimit, false }
                    }
                },
                { setKeyBlockOutputToggles, new SortedList<string, bool>
                    {
                        { setKeyBlockKeepOutputKey, false }, { setKeyBlockRemoveOutputKey, false }
                    }
                },
                { setKeyBlockAssemblerToggles, new SortedList<string, bool>
                    {
                        { setKeyBlockAssemblyOnlyKey, false }, { setKeyBlockDisassemblyOnlyKey, false }, { setKeyBlockUniqueBlueprinteOnlyKey, false}, { setKeyBlockNoIdleResetKey, false }
                    }
                },
            };

            public bool andComparison, manual;

            public ItemCollection limits = NewCollection, loadout = NewCollection;

            public double priority;

            public string limitSearchString = "", loadoutSearchString = "";

            public DateTime updateTime = Now;

            public void Initialize()
            {
                settingDictionaryBools[setKeyBlockToggles].Clear();

                foreach (string setCategory in settingDictionaryBools.Keys)
                {
                    tempSettings = settingDictionaryBools[setCategory].Keys.ToList();
                    tempSettings.ForEach(setKey => settingDictionaryBools[setCategory][setKey] = false);
                }

                settingDictionaryBools[setKeyBlockToggles][crossGridKeyword] = false;
                settingDictionaryBools[setKeyBlockToggles][includeGridKeyword] = false;
                settingDictionaryBools[setKeyBlockToggles][exclusionKeyword] = false;
                settingDictionaryBools[setKeyBlockToggles][excludeGridKeyword] = false;

                limitSearchString = loadoutSearchString = "";

                limits.Clear();
                loadout.Clear();
                storageCategories.Clear();
                logicComparisons.Clear();
                andComparison = manual = false;
                priority = 1;
            }

            public bool GetOption(string key)
            {
                foreach (string setCategory in settingDictionaryBools.Keys)
                    if (settingDictionaryBools[setCategory].ContainsKey(key))
                        return settingDictionaryBools[setCategory][key];

                return false;
            }

            public void SetOption(string key, bool enabled = true)
            {
                foreach (string setCategory in settingDictionaryBools.Keys)
                    if (settingDictionaryBools[setCategory].ContainsKey(key))
                    {
                        settingDictionaryBools[setCategory][key] = enabled;
                        break;
                    }
            }
        }

        public class ItemCountRecord
        {
            public double count = 0, disassembling = 0;
            public DateTime countTime = Now;
        }

        public class ItemDefinition
        {
            public string typeID = "", subtypeID = "", blueprintID = "", displayName = "", category = "";

            public double amount = 0, queuedAssemblyAmount = 0, queuedDisassemblyAmount = 0,
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

            public MyItemType ItemType => new MyItemType(typeID, subtypeID);

            public string FullID => $"{typeID}/{subtypeID}";

            public double Percentage => currentQuota == 0 ? double.MaxValue : (amount / currentQuota) * 100.0;

            public void FinalizeKeys()
            {
                if (oreKeys.Count > 0)
                    oreKeys = oreKeys.Distinct().ToList();
            }

            public void SwitchCount(double maxMultiplier, bool useMultiplier, double negativeThreshold, double positiveThreshold, double multiplierChange, bool increaseWhenLow)
            {
                amount = countAmount;
                countAmount = 0;
                if (Now >= countRecordTime)
                {
                    countRecordList.Add(new ItemCountRecord { count = amount, countTime = Now, disassembling = queuedDisassemblyAmount });
                    countRecordTime = Now.AddSeconds(1.25);
                    if (countRecordList.Count > 12)
                        countRecordList.RemoveRange(0, countRecordList.Count - 12);
                }
                double averageSpanSeconds = Math.Min(20, (Now - countRecordList[0].countTime).TotalSeconds + 0.0001),
                    lastSeconds = (Now - dynamicQuotaTime).TotalSeconds, tempDifference,
                    tempQuota = 0, disassembleCount = Math.Max(countRecordList[0].disassembling, queuedDisassemblyAmount);
                if (countRecordList.Count > 1 && averageSpanSeconds == 20)
                    countRecordList.RemoveAt(0);

                bool allOut = amount < 1.0;

                amountDifference = averageSpanSeconds > 0 ? (amount - countRecordList[0].count) / averageSpanSeconds : 0;

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

                        if (amount >= tempQuota)
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
                postAssemblyAmount = amount + (currentAssemblyAmount) - (currentDisassemblyAmount);
                SetCurrentQuota();
                if (TextHasLength(blueprintID) && blueprintID != nothingType)
                {
                    currentMax =
                        currentQuota < 0 ? 0 :
                        quotaMax > currentQuota ? quotaMax :
                        currentQuota > 0 ? Math.Floor(currentQuota * (1.0 + excessAmount)) : double.MaxValue;

                    if (!disassembleAll)
                    {
                        if (currentAssemblyAmount > 0 && amount + currentAssemblyAmount > currentMax)
                            currentExcessAssembly = (amount + currentAssemblyAmount) - currentMax;

                        if (currentDisassemblyAmount > 0 && amount - currentDisassemblyAmount < currentQuota)
                            currentExcessDisassembly = currentQuota - (amount - currentDisassemblyAmount);
                    }
                    else
                    {
                        currentExcessAssembly = queuedAssemblyAmount;
                        if (currentDisassemblyAmount > 0 && currentDisassemblyAmount > amount)
                            currentExcessDisassembly = currentDisassemblyAmount - amount;
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
                       $"^Type={typeID}||Subtype={subtypeID}" +
                       $"{(IsBlueprint(blueprintID) ? $"||Blueprint={blueprintID}||Assembly Multiplier={assemblyMultiplier}||Assemble={assemble}||Disassemble={disassemble}" : StringsMatch(blueprintID, "none") ? "||Blueprint=None" : "")}" +
                       $"{(IsOre(typeID) ? $"||Refine={refine}" : "")}" +
                       $"||Fuel={fuel}||Display={display}" +
                       $"{(gas || IsIce(typeID, subtypeID) ? $"||Gas={gas}" : "")}" +
                       $"{(IsIngot(typeID) || oreKeys.Count > 0 ? $"||Ore Keys=[{String.Join("|", oreKeys)}]" : "")}{newLine}";
            }
        }

        public class SpanKey
        {
            public long index;
            public int surfaceIndex = 0;
        }

        public struct ItemDAO
        {
            public string typeID, subtypeID, amount, name;

            public ItemDAO(string typeID, string subtypeID, string amount, string name)
            {
                this.typeID = typeID;
                this.subtypeID = subtypeID;
                this.amount = amount;
                this.name = name;
            }

            public override string ToString()
            {
                return name;
            }
        }

        #endregion
    }
}