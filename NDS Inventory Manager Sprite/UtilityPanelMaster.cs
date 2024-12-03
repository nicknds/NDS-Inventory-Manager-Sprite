using System.Collections.Generic;
using VRageMath;
using VRage.Game.GUI.TextPanel;
using System;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program
    {
        public class PanelMaster2
        {
            #region Constants

            public static Program parent;

            Color defPanelForegroundColor = new Color(0.7019608f, 0.9294118f, 1f, 1f);

            #endregion


            #region Short Links

            PanelFunctionIdentifier selfContainedIdentifier;
            bool PauseTickRun => parent.PauseTickRun;
            bool IsStateRunning => parent.stateRecords.IsActive($"{selfContainedIdentifier}");
            bool RunStateManager => parent.StateManager($"{selfContainedIdentifier}");
            bool IsInitialized => parent.stateRecords.IsInitialized($"{selfContainedIdentifier}");
            SortedList<string, LongListPlus> typedIndexes => parent.typedIndexes;
            Dictionary<long, BlockDefinition> managedBlocks => parent.managedBlocks;

            IEnumerator<FunctionState> SetEnumerator { set { parent.stateRecords[$"{selfContainedIdentifier}"].enumerator = value; } }

            #endregion


            #region Variables

            Dictionary<string, GraphicDocument> cachedDocuments = new Dictionary<string, GraphicDocument>();

            PanelClass tempManagerPanelDefinition, tempSettingsPanelDefinition;

            List<string> fontList = NewStringList;

            DateTime NextStorageTime = DateTime.MinValue;

            Dictionary<string, double> StorageCurrentValues = new Dictionary<string, double>(),
                                       StorageMaxValues = new Dictionary<string, double>();

            #endregion


            public PanelMaster2()
            {
                PanelSettings.master = this;
            }


            #region State Functions

            /// <summary>
            /// Processes the given panel producing output
            /// </summary>
            /// <param name="panelDefinition">The panel class to process</param>
            /// <returns>True when the current panel completes</returns>
            public bool PanelManager(PanelClass panelDefinition)
            {
                selfContainedIdentifier = PanelFunctionIdentifier.Panel_Manager;

                if (!IsInitialized)
                    SetEnumerator = PanelManagerState();

                if (!IsStateRunning)
                    tempManagerPanelDefinition = panelDefinition;

                return RunStateManager;
            }

            /// <summary>
            /// Processes the given panel producing output
            /// </summary>
            /// <returns></returns>
            IEnumerator<FunctionState> PanelManagerState()
            {
                bool cacheSkip;
                while (true)
                {
                    // Check for previously generated documents
                    cacheSkip = false;
                    if (cachedDocuments.ContainsKey(tempManagerPanelDefinition.DocumentKey))
                    {
                        if (!parent.scanning && Now >= cachedDocuments[tempManagerPanelDefinition.DocumentKey].expirationTime)
                            cachedDocuments.Remove(tempManagerPanelDefinition.DocumentKey);
                        else cacheSkip = true;
                    }

                    // Generate document if needed
                    // Span documents do not need generation
                    if (!cacheSkip)
                    {
                        switch (tempManagerPanelDefinition.PanelSettings.Type)
                        {
                            case PanelType.Item:
                                while (!ItemPanel()) yield return stateActive;
                                break;
                            case PanelType.Output:
                                while (!OutputPanel()) yield return stateActive;
                                break;
                            case PanelType.Status:
                                while (!StatusPanel()) yield return stateActive;
                                break;
                            case PanelType.Cargo:
                                while (!CargoPanel()) yield return stateActive;
                                break;
                            default:
                                break;
                        }

                        if (cachedDocuments.ContainsKey(tempManagerPanelDefinition.DocumentKey))
                            cachedDocuments[tempManagerPanelDefinition.DocumentKey].Generated();
                    }

                    // Convert documents to sprite/text here
                    if (cachedDocuments.ContainsKey(tempManagerPanelDefinition.DocumentKey))
                        switch (tempManagerPanelDefinition.Surface.ContentType)
                        {
                            case ContentType.SCRIPT:
                                while (!SpriteProcessor()) yield return stateActive;
                                break;
                            case ContentType.TEXT_AND_IMAGE:
                                while (!TextProcessor()) yield return stateActive;
                                break;
                            default:
                                break;
                        }

                    // Set update time
                    tempManagerPanelDefinition.NextUpdateTime = Now.AddSeconds(tempManagerPanelDefinition.PanelSettings.UpdateDelay);

                    yield return stateContinue;
                }
            }

            /// <summary>
            /// Turns documents into sprite displays
            /// </summary>
            /// <returns>True when the display is complete</returns>
            bool SpriteProcessor()
            {
                selfContainedIdentifier = PanelFunctionIdentifier.Sprite_Processor;

                if (!IsInitialized)
                    SetEnumerator = SpriteProcessorState();

                return RunStateManager;
            }

            IEnumerator<FunctionState> SpriteProcessorState()
            {
                GraphicDocument document, spannedDocument;
                int maxRows, displayedRows;
                Vector2 positionOffset, surfaceSize, objectSize, objectOffset;
                IMyTextSurface surface;
                bool span, multipleColumns;
                List<MySprite> spriteList = new List<MySprite>();
                while (true)
                {
                    surface = tempManagerPanelDefinition.Surface;
                    document = cachedDocuments[tempManagerPanelDefinition.DocumentKey];
                    surfaceSize = surface.SurfaceSize;
                    positionOffset = (surface.TextureSize - surfaceSize) + (surfaceSize * (surface.TextPadding / 200f));
                    positionOffset.Y *= (float)tempManagerPanelDefinition.PanelSettings.OffsetMultiplier;
                    surfaceSize *= 1f - (surface.TextPadding / 100f);
                    maxRows = tempManagerPanelDefinition.PanelSettings.Rows;
                    span = TextHasLength(tempManagerPanelDefinition.PanelSettings.SpanChildID);
                    spannedDocument = null;
                    spriteList.Clear();
                    multipleColumns = !document.GraphicObjects.ContainsKey(centerAlignment) &&
                                      document.GraphicObjects.ContainsKey(leftAlignment) &&
                                      document.GraphicObjects.ContainsKey(rightAlignment) &&
                                      maxRows != 0;

                    foreach (KeyValuePair<TextAlignment, List<GraphicObject>> pair in document.GraphicObjects)
                    {
                        if (PauseTickRun) yield return stateActive;

                        displayedRows = pair.Key == leftAlignment || maxRows < 0 ? pair.Value.Count : maxRows;

                        for (int i = 0; i < pair.Value.Count; i++)
                        {
                            if (PauseTickRun) yield return stateActive;

                            // Copy excess elements to spanned document
                            if (span && i >= displayedRows)
                            {
                                if (spannedDocument == null)
                                    spannedDocument = new GraphicDocument();
                                if (!spannedDocument.GraphicObjects.ContainsKey(pair.Key))
                                    spannedDocument.GraphicObjects[centerAlignment] = new List<GraphicObject>();
                                spannedDocument.GraphicObjects[centerAlignment].Add(pair.Value[i]);
                            }
                            else if (i < displayedRows)
                            {
                                objectSize = new Vector2(multipleColumns ? (surfaceSize.X / 2f) - 1f : surfaceSize.X, surfaceSize.Y / (float)displayedRows);
                                objectOffset = new Vector2(pair.Key == TextAlignment.RIGHT ? (surfaceSize.X / 2f) + 1f : 0f, (float)i * objectSize.Y);
                                foreach (GraphicElement graphicElement in pair.Value[i].Elements)
                                {
                                    if (PauseTickRun) yield return stateActive;
                                    spriteList.Add(ProcessSprite(
                                        graphicElement,
                                        positionOffset + objectOffset + new Vector2(objectSize.X * graphicElement.StartPointHorizontal, objectSize.Y * graphicElement.StartPointVertical),
                                        new Vector2(objectSize.X * graphicElement.WidthPercentage, objectSize.Y * graphicElement.HeightPercentage),
                                        tempManagerPanelDefinition.PanelSettings.Font,
                                        surface
                                        ));
                                }
                            }
                        }
                    }

                    MySpriteDrawFrame frame = surface.DrawFrame();
                    frame.AddRange(spriteList);
                    frame.Dispose();

                    if (spannedDocument != null)
                    {
                        spannedDocument.Generated();
                        cachedDocuments[tempManagerPanelDefinition.PanelSettings.SpanChildID] = spannedDocument;
                    }

                    yield return stateContinue;
                }
            }

            /// <summary>
            /// Turns documents into text displays
            /// </summary>
            /// <returns>True when the display is complete</returns>
            bool TextProcessor()
            {
                selfContainedIdentifier = PanelFunctionIdentifier.Text_Processor;

                if (!IsInitialized)
                    SetEnumerator = TextProcessorState();

                return RunStateManager;
            }

            IEnumerator<FunctionState> TextProcessorState()
            {
                GraphicDocument document, spannedDocument;
                int maxRows, displayedRows, leftMax, rightMax, centerMax;
                float currentStartPointVertical;
                Vector2 surfaceSize, buildSize;
                IMyTextSurface surface;
                bool span;
                List<string> leftColumn = NewStringList, rightColumn = NewStringList, centerColumn = NewStringList;
                StringBuilder builder = NewBuilder;
                while (true)
                {
                    surface = tempManagerPanelDefinition.Surface;
                    document = cachedDocuments[tempManagerPanelDefinition.DocumentKey];
                    surfaceSize = surface.SurfaceSize * (1f - (surface.TextPadding / 50f));
                    maxRows = tempManagerPanelDefinition.PanelSettings.Rows;
                    span = TextHasLength(tempManagerPanelDefinition.PanelSettings.SpanChildID);
                    spannedDocument = null;
                    leftColumn.Clear();
                    rightColumn.Clear();
                    centerColumn.Clear();
                    leftMax = rightMax = centerMax = 0;

                    foreach (KeyValuePair<TextAlignment, List<GraphicObject>> pair in document.GraphicObjects)
                    {
                        if (PauseTickRun) yield return stateActive;

                        displayedRows = pair.Key == leftAlignment || maxRows < 0 ? pair.Value.Count : maxRows;

                        for (int i = 0; i < pair.Value.Count; i++)
                        {
                            currentStartPointVertical = 0f;

                            // Copy excess elements to spanned document
                            if (span && i >= displayedRows)
                            {
                                if (spannedDocument == null)
                                    spannedDocument = new GraphicDocument();
                                if (!spannedDocument.GraphicObjects.ContainsKey(pair.Key))
                                    spannedDocument.GraphicObjects[centerAlignment] = new List<GraphicObject>();
                                spannedDocument.GraphicObjects[centerAlignment].Add(pair.Value[i]);
                            }
                            else if (i < displayedRows)
                            {
                                foreach (GraphicElement graphicElement in pair.Value[i].Elements)
                                {
                                    if (graphicElement.Texture) continue;
                                    if (PauseTickRun) yield return stateActive;
                                    if (graphicElement.StartPointVertical > currentStartPointVertical && BuilderHasLength(builder))
                                    {
                                        switch (pair.Key)
                                        {
                                            case leftAlignment:
                                                leftColumn.Add(builder.ToString());
                                                break;
                                            case centerAlignment:
                                                centerColumn.Add(builder.ToString());
                                                break;
                                            default:
                                                rightColumn.Add(builder.ToString());
                                                break;
                                        }
                                        builder.Clear();
                                        currentStartPointVertical = graphicElement.StartPointVertical;
                                    }
                                    builder.Append($"{(BuilderHasLength(builder) ? " " : "")}{graphicElement.Text}");
                                }
                                if (BuilderHasLength(builder))
                                {
                                    switch (pair.Key)
                                    {
                                        case leftAlignment:
                                            leftColumn.Add(builder.ToString());
                                            break;
                                        case centerAlignment:
                                            centerColumn.Add(builder.ToString());
                                            break;
                                        default:
                                            rightColumn.Add(builder.ToString());
                                            break;
                                    }
                                    builder.Clear();
                                }
                            }
                        }
                    }

                    foreach (string line in rightColumn)
                    {
                        if (PauseTickRun) yield return stateActive;
                        rightMax = Math.Max(rightMax, line.Length);
                    }
                    foreach (string line in leftColumn)
                    {
                        if (PauseTickRun) yield return stateActive;
                        leftMax = Math.Max(leftMax, line.Length);
                    }

                    for (int i = 0; i < rightColumn.Count || i < leftColumn.Count; i++)
                    {
                        if (PauseTickRun) yield return stateActive;
                        centerColumn.Add($"{(i < leftColumn.Count ? leftColumn[i] : "").PadRight(leftMax)} {(i < rightColumn.Count ? rightColumn[i] : "").PadRight(rightMax)}");
                    }
                    for (int i = 0; i < centerColumn.Count; i++)
                    {
                        if (PauseTickRun) yield return stateActive;
                        centerMax = Math.Max(centerMax, centerColumn[i].Length);
                    }
                    for (int i = 0; i < centerColumn.Count; i++)
                    {
                        if (PauseTickRun) yield return stateActive;
                        if (i + 1 < centerColumn.Count)
                            BuilderAppendLine(builder, centerColumn[i].PadRight(centerMax));
                        else
                            builder.Append(centerColumn[i].PadRight(centerMax));
                    }

                    surface.WriteText(builder);
                    builder.Clear();

                    if (centerColumn.Count > 0)
                    {
                        builder.Append(centerColumn[0].Replace(" ", "_"));
                        buildSize = surface.MeasureStringInPixels(builder, surface.Font, 1f) / (float)tempManagerPanelDefinition.PanelSettings.TextMultiplier;
                        buildSize = new Vector2(buildSize.X, buildSize.Y * (float)centerColumn.Count);
                        surface.FontSize = Math.Max(0.1f, Math.Min(10f, Math.Min(surfaceSize.X / buildSize.X, surfaceSize.Y / buildSize.Y)));
                        builder.Clear();
                    }

                    if (spannedDocument != null)
                    {
                        spannedDocument.Generated();
                        cachedDocuments[tempManagerPanelDefinition.PanelSettings.SpanChildID] = spannedDocument;
                    }

                    yield return stateContinue;
                }
            }

            /// <summary>
            /// Processes an item panel to produce a document
            /// </summary>
            /// <returns>True when the document is complete</returns>
            bool ItemPanel()
            {
                selfContainedIdentifier = PanelFunctionIdentifier.Item_Panel;

                if (!IsInitialized)
                    SetEnumerator = ItemPanelState();

                return RunStateManager;
            }

            IEnumerator<FunctionState> ItemPanelState()
            {
                List<ItemDefinition> items = NewItemDefinitionList;
                bool belowQuota, hasActivity;
                double minValue, maxValue;
                while (true)
                {
                    // Populate temporary list
                    PopulateClassList(items, tempManagerPanelDefinition.PanelSettings.Items.ItemList.Values.Select(b => b.ItemReference));

                    // Filters
                    belowQuota = tempManagerPanelDefinition.PanelSettings.Options.Contains(PanelOptions.BelowQuota);
                    hasActivity = tempManagerPanelDefinition.PanelSettings.Options.Contains(PanelOptions.HasActivity);
                    minValue = tempManagerPanelDefinition.PanelSettings.MinimumItemValue;
                    maxValue = tempManagerPanelDefinition.PanelSettings.MaximumItemValue;
                    if (maxValue <= zero) maxValue = double.MaxValue;
                    for (int i = 0; i < items.Count; i += 0)
                    {
                        if (PauseTickRun) yield return stateActive;
                        if ((belowQuota && items[i].amount >= items[i].currentQuota) ||
                            items[i].amount < minValue ||
                            items[i].amount > maxValue ||
                            (hasActivity && items[i].amountDifference == zero))
                            items.RemoveAtFast(i);
                        else
                            i++;
                    }

                    // Order items by setting
                    switch (tempManagerPanelDefinition.PanelSettings.SortType)
                    {
                        case PanelItemSorting.Alphabetical:
                            items = items.OrderBy(b => b.displayName).ToList();
                            break;
                        case PanelItemSorting.AscendingAmount:
                            items = items.OrderBy(b => b.amount).ToList();
                            break;
                        case PanelItemSorting.DescendingAmount:
                            items = items.OrderByDescending(b => b.amount).ToList();
                            break;
                        case PanelItemSorting.AscendingPercent:
                            items = items.OrderBy(b => b.Percentage).ToList();
                            break;
                        case PanelItemSorting.DescendingPercent:
                            items = items.OrderByDescending(b => b.Percentage).ToList();
                            break;
                    }

                    // Generate document
                    GraphicDocument document = new GraphicDocument();
                    document.GraphicObjects[centerAlignment] = new List<GraphicObject>();
                    foreach (ItemDefinition item in items)
                    {
                        if (PauseTickRun) yield return stateActive;

                        switch (tempManagerPanelDefinition.PanelSettings.DisplayType)
                        {
                            case DisplayType.Standard:
                                document.GraphicObjects[centerAlignment].Add(GenerateStandardItem(item));
                                break;
                            case DisplayType.CompactAmount:
                                document.GraphicObjects[centerAlignment].Add(GenerateCompactAmountItem(item));
                                break;
                            case DisplayType.CompactPercent:
                                document.GraphicObjects[centerAlignment].Add(GenerateCompactPercent(item));
                                break;
                            case DisplayType.Detailed:
                                document.GraphicObjects[centerAlignment].Add(GenerateDetailedItem(item));
                                break;
                        }
                    }

                    if (document.GraphicObjects[centerAlignment].Count == 0)
                    {
                        document.GraphicObjects.Clear();
                        document.GraphicObjects[leftAlignment] = new List<GraphicObject> { GenerateText(tempManagerPanelDefinition.PanelSettings.Items.Count == 0 ? "No Items. Add Items or Categories" : $"Filtered 0/{tempManagerPanelDefinition.PanelSettings.Items.Count} Items") };
                    }

                    // Cache document
                    cachedDocuments[tempManagerPanelDefinition.DocumentKey] = document;

                    yield return stateContinue;
                }
            }

            /// <summary>
            /// Processes an output panel to produce a document
            /// </summary>
            /// <returns>True when the document is complete</returns>
            bool OutputPanel()
            {
                selfContainedIdentifier = PanelFunctionIdentifier.Output_Panel;

                if (!IsInitialized)
                    SetEnumerator = OutputPanelState();

                return RunStateManager;
            }

            IEnumerator<FunctionState> OutputPanelState()
            {
                List<string> suffixes;
                List<OutputObject> tempOutputList = new List<OutputObject>();
                while (true)
                {
                    suffixes = tempManagerPanelDefinition.PanelSettings.Suffixes;
                    GraphicDocument document = new GraphicDocument();
                    document.GraphicObjects[leftAlignment] = new List<GraphicObject>();
                    document.GraphicObjects[rightAlignment] = new List<GraphicObject>();

                    document.GraphicObjects[leftAlignment].Add(GenerateText($"NDS Inventory Manager v{buildVersion}"));
                    document.GraphicObjects[leftAlignment].Add(GenerateText($"{parent.currentMajorFunction}".Replace("_", " ")));
                    document.GraphicObjects[leftAlignment].Add(GenerateText($"Runtime:    {parent.ShortMSTime(torchAverage)}"));
                    document.GraphicObjects[leftAlignment].Add(GeneratePerformance);
                    document.GraphicObjects[leftAlignment].Add(GenerateText($"Blocks:     {ShortNumber2(parent.managedBlocks.Count, suffixes)}"));
                    document.GraphicObjects[leftAlignment].Add(GenerateText($"Storages:   {ShortNumber2(typedIndexes[setKeyIndexStorage].Count, suffixes)}"));
                    document.GraphicObjects[leftAlignment].Add(GenerateText($"Assemblers: {ShortNumber2(typedIndexes[setKeyIndexAssemblers].Count, suffixes)}"));
                    document.GraphicObjects[leftAlignment].Add(GenerateText($"H2/O2 Gens: {ShortNumber2(typedIndexes[setKeyIndexGasGenerators].Count, suffixes)}"));
                    document.GraphicObjects[leftAlignment].Add(GenerateText($"Refineries: {ShortNumber2(typedIndexes[setKeyIndexRefinery].Count, suffixes)}"));
                    document.GraphicObjects[leftAlignment].Add(GenerateText($"H/O Tanks:  {ShortNumber2(typedIndexes[setKeyIndexHydrogenTank].Count, suffixes)}/{ShortNumber2(typedIndexes[setKeyIndexOxygenTank].Count, suffixes)}"));
                    document.GraphicObjects[leftAlignment].Add(GenerateText($"Weapons:    {ShortNumber2(typedIndexes[setKeyIndexGun].Count, suffixes)}"));
                    document.GraphicObjects[leftAlignment].Add(GenerateText($"Reactors:   {ShortNumber2(typedIndexes[setKeyIndexReactor].Count, suffixes)}"));
                    document.GraphicObjects[leftAlignment].Add(GenerateText(parent.errorFilter ? $"Errors:     {ShortNumber2(parent.currentErrorCount, suffixes, 0, 6)} of {ShortNumber2(parent.totalErrorCount, suffixes, 0, 6)}" : $"Status:  {ShortNumber2(parent.scriptHealth, suffixes, 3, 6)}%"));

                    PopulateClassList(tempOutputList, parent.errorFilter ? parent.outputErrorList : parent.outputList);
                    foreach (OutputObject outputObject in tempOutputList)
                    {
                        if (PauseTickRun) yield return stateActive;
                        document.GraphicObjects[rightAlignment].Add(GenerateText(outputObject.Output));
                    }

                    // Cache document
                    cachedDocuments[tempManagerPanelDefinition.DocumentKey] = document;

                    yield return stateContinue;
                }
            }

            /// <summary>
            /// Processes a status panel to produce a document
            /// </summary>
            /// <returns>True when the document is complete</returns>
            bool StatusPanel()
            {
                selfContainedIdentifier = PanelFunctionIdentifier.Status_Panel;

                if (!IsInitialized)
                    SetEnumerator = StatusPanelState();

                return RunStateManager;
            }

            IEnumerator<FunctionState> StatusPanelState()
            {
                int assembling, disassembling, idle, disabled, nameLength, decimals;
                List<long> tempIndices = NewLongList;
                List<string> suffixes;
                SortedList<string, int> assemblyList = new SortedList<string, int>(),
                                        disassemblyList = new SortedList<string, int>();
                while (true)
                {
                    // Set panel settings
                    suffixes = tempManagerPanelDefinition.PanelSettings.Suffixes;
                    nameLength = tempManagerPanelDefinition.PanelSettings.NameLength;
                    decimals = tempManagerPanelDefinition.PanelSettings.Decimals;

                    // Initialize document
                    GraphicDocument document = new GraphicDocument();
                    document.GraphicObjects[leftAlignment] = new List<GraphicObject>();
                    document.GraphicObjects[rightAlignment] = new List<GraphicObject>();

                    // Reset settings
                    assembling = disassembling = idle = disabled = 0;
                    assemblyList.Clear();
                    disassemblyList.Clear();
                    // Check assemblers
                    PopulateStructList(tempIndices, typedIndexes[setKeyIndexAssemblers]);
                    foreach (long index in tempIndices)
                    {
                        if (PauseTickRun) yield return stateActive;
                        BlockStatus(index, ref assembling, ref disassembling, ref idle, ref disabled, assemblyList, disassemblyList);
                    }
                    // Generate assembler output
                    document.GraphicObjects[leftAlignment].Add(GenerateText(BlockStatusTitle($"Assemblers x{ShortNumber2(typedIndexes[setKeyIndexAssemblers].Count, suffixes, decimals, 4, false)}", disabled).PadRight(nameLength)));
                    document.GraphicObjects[leftAlignment].Add(GenerateText($" Assembling:    {ShortNumber2(assembling, suffixes, decimals, 4)}".PadRight(nameLength)));
                    document.GraphicObjects[leftAlignment].Add(GenerateText($" Disassembling: {ShortNumber2(disassembling, suffixes, decimals, 4)}".PadRight(nameLength)));
                    document.GraphicObjects[leftAlignment].Add(GenerateText($" Idle:          {ShortNumber2(idle, suffixes, decimals, 4)}".PadRight(nameLength)));
                    // Generate assembler details
                    foreach (KeyValuePair<string, int> kvp in assemblyList)
                    {
                        if (PauseTickRun) yield return stateActive;
                        document.GraphicObjects[rightAlignment].Add(GenerateText($"Assembling x{ShortNumber2(kvp.Value, suffixes, decimals, 4, false)} {ShortenName(kvp.Key, nameLength, true)}"));
                    }
                    foreach (KeyValuePair<string, int> kvp in disassemblyList)
                    {
                        if (PauseTickRun) yield return stateActive;
                        document.GraphicObjects[rightAlignment].Add(GenerateText($"Disassembling x{ShortNumber2(kvp.Value, suffixes, decimals, 4, false)} {ShortenName(kvp.Key, nameLength, true)}"));
                    }

                    // Reset settings
                    assembling = idle = disabled = 0;
                    assemblyList.Clear();
                    // Check refineries
                    PopulateStructList(tempIndices, typedIndexes[setKeyIndexRefinery]);
                    foreach (long index in tempIndices)
                    {
                        if (PauseTickRun) yield return stateActive;
                        BlockStatus(index, ref assembling, ref disassembling, ref idle, ref disabled, assemblyList, disassemblyList);
                    }
                    // Generate refinery output
                    document.GraphicObjects[leftAlignment].Add(GenerateText(BlockStatusTitle($"Refineries x{ShortNumber2(typedIndexes[setKeyIndexRefinery].Count, suffixes, decimals, 4, false)}", disabled).PadRight(nameLength)));
                    document.GraphicObjects[leftAlignment].Add(GenerateText($" Refining:      {ShortNumber2(assembling, suffixes, decimals, 4)}".PadRight(nameLength)));
                    document.GraphicObjects[leftAlignment].Add(GenerateText($" Idle:          {ShortNumber2(idle, suffixes, decimals, 4)}".PadRight(nameLength)));
                    // Generate refinery details
                    foreach (KeyValuePair<string, int> kvp in assemblyList)
                    {
                        if (PauseTickRun) yield return stateActive;
                        document.GraphicObjects[rightAlignment].Add(GenerateText($"Refining x{ShortNumber2(kvp.Value, suffixes, decimals, 4, false)} {ShortenName(kvp.Key, nameLength, true)}"));
                    }

                    // Reset settings
                    assembling = idle = disabled = 0;
                    assemblyList.Clear();
                    // Check h2/o2 generators
                    PopulateStructList(tempIndices, typedIndexes[setKeyIndexGasGenerators]);
                    foreach (long index in tempIndices)
                    {
                        if (PauseTickRun) yield return stateActive;
                        BlockStatus(index, ref assembling, ref disassembling, ref idle, ref disabled, assemblyList, disassemblyList);
                    }
                    // Generate h2/o2 output
                    document.GraphicObjects[leftAlignment].Add(GenerateText(BlockStatusTitle($"O2/H2 Gens x{ShortNumber2(typedIndexes[setKeyIndexGasGenerators].Count, suffixes, decimals, 4, false)}", disabled).PadRight(nameLength)));
                    document.GraphicObjects[leftAlignment].Add(GenerateText($" Active:        {ShortNumber2(assembling, suffixes, decimals, 4)}".PadRight(nameLength)));
                    document.GraphicObjects[leftAlignment].Add(GenerateText($" Idle:          {ShortNumber2(idle, suffixes, decimals, 4)}".PadRight(nameLength)));
                    // Generate h2/o2 details
                    foreach (KeyValuePair<string, int> kvp in assemblyList)
                    {
                        if (PauseTickRun) yield return stateActive;
                        document.GraphicObjects[rightAlignment].Add(GenerateText($"Processing x{ShortNumber2(kvp.Value, suffixes, decimals, 4, false)} {ShortenName(kvp.Key, nameLength, true)}"));
                    }

                    // Cache document
                    cachedDocuments[tempManagerPanelDefinition.DocumentKey] = document;

                    yield return stateContinue;
                }
            }

            /// <summary>
            /// Processes a cargo panel to produce a document
            /// </summary>
            /// <returns>True when the document is complete</returns>
            bool CargoPanel()
            {
                selfContainedIdentifier = PanelFunctionIdentifier.Cargo_Panel;

                if (!IsInitialized)
                    SetEnumerator = CargoPanelState();

                return RunStateManager;
            }

            IEnumerator<FunctionState> CargoPanelState()
            {
                List<string> categories = NewStringList, keys = NewStringList;
                List<long> indexes = NewLongList;
                double current, max;
                while (true)
                {
                    GraphicDocument document = new GraphicDocument();
                    document.GraphicObjects[leftAlignment] = new List<GraphicObject>();
                    PopulateClassList(categories, tempManagerPanelDefinition.PanelSettings.Categories);

                    // Update storage capacities
                    if (Now >= NextStorageTime)
                    {
                        StorageCurrentValues.Clear();
                        StorageMaxValues.Clear();
                        PopulateClassList(keys, parent.indexesStorageLists.Keys);

                        foreach (string key in keys)
                        {
                            if (!parent.indexesStorageLists.ContainsKey(key)) continue;
                            current = max = 0;
                            PopulateStructList(indexes, parent.indexesStorageLists[key]);
                            foreach (long index in indexes)
                            {
                                if (PauseTickRun) yield return stateActive;
                                if (!parent.IsBlockBad(index))
                                {
                                    current += (double)managedBlocks[index].Block.GetInventory(0).CurrentVolume;
                                    max += (double)managedBlocks[index].Block.GetInventory(0).MaxVolume;
                                }
                            }
                            StorageCurrentValues[key] = current;
                            StorageMaxValues[key] = max;
                        }

                        NextStorageTime = Now.AddSeconds(1 + Math.Min(4, typedIndexes[setKeyIndexStorage].Count / 25));
                    }

                    // Draw total storage object
                    if (categories.Where(x => IsWildCard(x)).Count() > 0)
                    {
                        current = max = 0;
                        foreach (string key in StorageCurrentValues.Keys)
                        {
                            if (PauseTickRun) yield return stateActive;
                            current += StorageCurrentValues[key];
                            max += StorageMaxValues[key];
                        }
                        document.GraphicObjects[leftAlignment].Add(GenerateStorage("All", max == 0f ? 1f : (float)(current / max)));
                    }

                    // Draw chosen storage objects
                    foreach (string category in categories)
                        if (StorageCurrentValues.ContainsKey(category))
                            document.GraphicObjects[leftAlignment].Add(GenerateStorage(Formatted(category), StorageMaxValues[category] == 0f ? 1f : (float)(StorageCurrentValues[category] / StorageMaxValues[category])));

                    if (document.GraphicObjects[leftAlignment].Count == 0)
                        document.GraphicObjects[leftAlignment].Add(GenerateText("No Categories Chosen"));

                    // Cache document
                    cachedDocuments[tempManagerPanelDefinition.DocumentKey] = document;

                    yield return stateContinue;
                }
            }

            /// <summary>
            /// Processes the panel class, loading/saving settings
            /// </summary>
            /// <param name="panelClass"></param>
            /// <returns>True when the panel is done processing</returns>
            public bool ProcessPanelOptions(PanelClass panelClass)
            {
                selfContainedIdentifier = PanelFunctionIdentifier.Panel_Settings;

                if (!IsInitialized)
                    SetEnumerator = ProcessPanelOptionState();

                if (!IsStateRunning)
                    tempSettingsPanelDefinition = panelClass;

                return RunStateManager;
            }

            IEnumerator<FunctionState> ProcessPanelOptionState()
            {
                string dataSource, surfaceHeader, dataPrevious, dataAfter;
                List<string> dataLines = NewStringList;
                int processedSettings;
                while (true)
                {
                    dataSource = tempSettingsPanelDefinition.DataSource;
                    processedSettings = -1;
                    dataPrevious = dataAfter = "";

                    surfaceHeader = tempSettingsPanelDefinition.Provider ? $"{panelTag}:{tempSettingsPanelDefinition.SurfaceIndex}" : "";

                    if (TextHasLength(dataSource) && !StringsMatch(dataSource, tempSettingsPanelDefinition.PanelSettings.SettingBackup))
                    {
                        tempSettingsPanelDefinition.PanelSettings.SettingBackup = dataSource;

                        tempSettingsPanelDefinition.PanelSettings.Initialize();

                        if (tempSettingsPanelDefinition.Provider)
                            ParseHeaderedSettings(dataSource, surfaceHeader, dataLines, out dataPrevious, out dataAfter, true);
                        else dataLines.AddRange(SplitLines(dataSource));

                        processedSettings = 0;
                        foreach (string setting in dataLines)
                        {
                            if (PauseTickRun) yield return stateActive;

                            if (tempSettingsPanelDefinition.PanelSettings.LoadSetting(setting))
                                processedSettings++;
                        }
                        dataLines.Clear();
                    }

                    if (tempSettingsPanelDefinition.PanelSettings.Type == PanelType.Item &&
                        (tempSettingsPanelDefinition.PanelSettings.LastUpdateTime < parent.itemAddedOrChanged ||
                        (tempSettingsPanelDefinition.PanelSettings.Items.Count == 0 && (TextHasLength(tempSettingsPanelDefinition.PanelSettings.ItemSearchString) || tempSettingsPanelDefinition.PanelSettings.Categories.Count > 0))))
                    {
                        tempSettingsPanelDefinition.PanelSettings.Items.Clear();
                        string searchString = $"{tempSettingsPanelDefinition.PanelSettings.ItemSearchString}{(TextHasLength(tempSettingsPanelDefinition.PanelSettings.ItemSearchString) && tempSettingsPanelDefinition.PanelSettings.Categories.Count > 0 ? "|" : "")}{String.Join("|", tempSettingsPanelDefinition.PanelSettings.Categories.Select(c => $"{c}:*"))}";
                        while (!parent.MatchItems2(searchString, tempSettingsPanelDefinition.PanelSettings.Items)) yield return stateActive;
                        tempSettingsPanelDefinition.PanelSettings.LastUpdateTime = Now;
                    }

                    if (!TextHasLength(dataSource) || processedSettings == 0)
                    {
                        if (processedSettings == 0 && TextHasLength(dataSource) && !tempSettingsPanelDefinition.Provider)
                            dataPrevious = dataSource;

                        if (parent.GetKeyBool(setKeyAutoTagBlocks) && !ContainsString(tempSettingsPanelDefinition.Parent.Block.CustomName, parent.GetKeyString(setKeyNoTag)))
                        {
                            tempSettingsPanelDefinition.DataSource = $"{dataPrevious}{(TextHasLength(dataPrevious) ? newLine : "")}{(tempSettingsPanelDefinition.Provider ? $"{surfaceHeader}{newLine}" : "")}{tempSettingsPanelDefinition.PanelSettings}{newLine}{(tempSettingsPanelDefinition.Provider ? $"{surfaceHeader}{newLine}" : "")}{(TextHasLength(dataAfter) ? newLine : "")}{dataAfter}";

                            tempSettingsPanelDefinition.Parent.Block.CustomName = tempSettingsPanelDefinition.Parent.Block.CustomName.Replace(panelTag, panelTag.ToUpper());
                        }
                    }

                    yield return stateContinue;
                }
            }

            #endregion


            #region Return Methods


            void BlockStatus(long index, ref int assembling, ref int disassembling, ref int idle, ref int disabled, SortedList<string, int> assemblyList, SortedList<string, int> disassemblyList)
            {
                IMyTerminalBlock block = managedBlocks[index].Block;
                MyInventoryItem item;
                MyProductionItem productionItem;
                string key;
                if (parent.IsBlockBad(index) || !((IMyFunctionalBlock)block).Enabled)
                {
                    disabled++;
                    return;
                }
                if (block is IMyAssembler)
                {
                    IMyAssembler assembler = (IMyAssembler)block;
                    if (assembler.IsQueueEmpty)
                        idle++;
                    else
                    {
                        List<MyProductionItem> productionList = NewProductionList;
                        assembler.GetQueue(productionList);
                        productionItem = productionList[0];
                        key = BlueprintSubtype(productionItem);
                        if (parent.blueprintList.ContainsKey(key))
                        {
                            Blueprint blueprint = parent.blueprintList[key];
                            key = parent.ItemName(blueprint.typeID, blueprint.subtypeID);
                        }
                        if (assembler.Mode == assemblyMode)
                        {
                            assembling++;
                            if (!assemblyList.ContainsKey(key))
                                assemblyList[key] = (int)productionItem.Amount;
                            else
                                assemblyList[key] += (int)productionItem.Amount;
                        }
                        else
                        {
                            disassembling++;
                            if (!disassemblyList.ContainsKey(key))
                                disassemblyList[key] = (int)productionItem.Amount;
                            else
                                disassemblyList[key] += (int)productionItem.Amount;
                        }
                    }
                }
                else if (block is IMyRefinery)
                {
                    if (block.GetInventory(0).ItemCount == 0)
                        idle++;
                    else
                    {
                        assembling++;
                        item = (MyInventoryItem)block.GetInventory(0).GetItemAt(0);
                        key = parent.ItemName(item);
                        if (!assemblyList.ContainsKey(key))
                            assemblyList[key] = (int)(item).Amount;
                        else
                            assemblyList[key] += (int)(item).Amount;
                    }
                }
                else if (block is IMyGasGenerator)
                {
                    if (block.GetInventory(0).ItemCount > 0)
                    {
                        item = (MyInventoryItem)block.GetInventory(0).GetItemAt(0);
                        key = parent.ItemName(item);
                        if (!assemblyList.ContainsKey(key))
                            assemblyList[key] = (int)item.Amount;
                        else
                            assemblyList[key] += (int)item.Amount;
                        if (parent.IsGas(item))
                            assembling++;
                        else
                            idle++;
                    }
                    else
                        idle++;
                }
            }

            string BlockStatusTitle(string title, int disabled) => $"{title}{(disabled > 0 ? $" -{ShortNumber2(disabled, tempManagerPanelDefinition.PanelSettings.Suffixes)}" : "")}";

            GraphicObject GeneratePerformance =>
                new GraphicObject(new List<GraphicElement>
                {
                    new GraphicElement("SquareSimple", tempManagerPanelDefinition.PanelSettings.BackColor, 1, 1, 0, 0, true),
                    ProgressBarBack(1, 0.333f, 0, 0),
                    ProgressBarFront((float)parent.runtimePercentage, 1, 0.333f, 0, 0, true),
                    ProgressBarBack(1, 0.333f, 0, 0.333f),
                    ProgressBarFront((float)parent.actionPercentage, 1, 0.333f, 0, 0.333f, true),
                    ProgressBarBack(1, 0.334f, 0, 0.666f),
                    ProgressBarFront((float)parent.dynamicActionMultiplier, 1, 0.334f, 0, 0.666f)
                });

            GraphicObject GenerateStorage(string text, float percent) =>
                new GraphicObject(new List<GraphicElement>
                {
                    new GraphicElement("SquareSimple", tempManagerPanelDefinition.PanelSettings.BackColor, 1, 1, 0, 0, true),
                    new GraphicElement(text.PadRight(9), tempManagerPanelDefinition.PanelSettings.TextColor, 0.75f),
                    ProgressBarBack(0.25f, 1, 0.75f, 0), // Progress bar back @ 75%,0% x 25%*100%
                    ProgressBarFront(percent, 0.25f, 1, 0.75f, 0, true), // Progress bar front @ 75%,0% x 25%*100%
                    new GraphicElement($"{TruncateNumber(percent * 100f, 2)}%".PadLeft(6), tempManagerPanelDefinition.PanelSettings.NumberColor, 0.25f, 1, 0.75f)
                });

            GraphicObject GenerateText(string text) =>
                new GraphicObject(new List<GraphicElement>
                {
                    new GraphicElement("SquareSimple", tempManagerPanelDefinition.PanelSettings.BackColor, 1, 1, 0, 0, true),
                    new GraphicElement(text, tempManagerPanelDefinition.PanelSettings.TextColor)
                });

            GraphicObject GenerateDetailedItem(ItemDefinition item)
            {
                //Background
                //(Name) (Count/Quota) (Progress Bar Background & Progress Bar Foreground & Percentage)
                //(Assembly status) (rate)
                float percent = (float)item.Percentage / 100f;
                double displayedPercent = Math.Min(100.0, item.Percentage);
                return new GraphicObject(new List<GraphicElement>
                {
                    new GraphicElement("SquareSimple", tempManagerPanelDefinition.PanelSettings.BackColor, 1, 1, 0, 0, true), // Background @ 0%,0% x 100%*100%
                    new GraphicElement(ShortenName(item.displayName, tempManagerPanelDefinition.PanelSettings.NameLength, true), tempManagerPanelDefinition.PanelSettings.TextColor, 0.5f, 0.5f), // Name @ 50%,0% x 50%*50%
                    new GraphicElement($"{ShortNumber2(item.amount, tempManagerPanelDefinition.PanelSettings.Suffixes, tempManagerPanelDefinition.PanelSettings.Decimals, 6)}/{ShortNumber2(item.currentQuota, tempManagerPanelDefinition.PanelSettings.Suffixes, tempManagerPanelDefinition.PanelSettings.Decimals, 6, false)}", tempManagerPanelDefinition.PanelSettings.NumberColor, 0.35f, 0.5f, 0.5f), // Number @ 50%,0% x 35%*50%
                    ProgressBarBack(0.15f, 0.5f, 0.85f, 0), // Progress bar back @ 85%,0% x 15%*50%
                    ProgressBarFront(percent, 0.15f, 0.5f, 0.85f, 0), // Progress bar front @ 80%,0% x 20%*50%
                    new GraphicElement($"{TruncateNumber(displayedPercent, 2):N2}%".PadLeft(6), tempManagerPanelDefinition.PanelSettings.NumberColor, 0.15f, 0.5f, 0.85f), // Progress bar number @ 80%,0% x 20%*50%
                    new GraphicElement($"Status: {item.AssemblyStatus,-16}", tempManagerPanelDefinition.PanelSettings.TextColor, 0.7f, 0.5f, 0, 0.5f), // Status @ 0%,50% x 70%*50%
                    new GraphicElement("Rate: ", tempManagerPanelDefinition.PanelSettings.TextColor, 0.1f, 0.5f, 0.7f, 0.5f), // Rate @ 70%,50% x 10%*50%
                    new GraphicElement($"{(item.amountDifference > zero ? "+" : item.amountDifference == zero ? "+/-" : "")}{ShortNumber2(item.amountDifference, tempManagerPanelDefinition.PanelSettings.Suffixes, tempManagerPanelDefinition.PanelSettings.Decimals, 6, false)}", tempManagerPanelDefinition.PanelSettings.NumberColor, 0.2f, 0.5f, 0.8f, 0.5f) // Rate @ 80%,50% x 20%*50%
                });
            }

            GraphicObject GenerateCompactPercent(ItemDefinition item)
            {
                //Background
                //Name
                //Progress Bar Background
                //Progress Bar Foreground
                //Percentage
                float percent = (float)item.Percentage / 100f;
                double displayedPercent = Math.Min(100.0, item.Percentage);
                return new GraphicObject(new List<GraphicElement>
                {
                    ProgressBarBack(1, 1, 0, 0), // Progress bar back @ 0%,0% x 100%*100%
                    ProgressBarFront(percent, 1, 1, 0, 0), // Progress bar front @ 0%,0% x 100%*100%
                    new GraphicElement(ShortenName(item.displayName, tempManagerPanelDefinition.PanelSettings.NameLength, true), tempManagerPanelDefinition.PanelSettings.TextColor) // Name @ 0%,0% x 100%*100%
                });
            }

            GraphicObject GenerateCompactAmountItem(ItemDefinition item) =>
                new GraphicObject(new List<GraphicElement>
                {
                    new GraphicElement("SquareSimple", tempManagerPanelDefinition.PanelSettings.BackColor, 1, 1, 0, 0, true), // Background @ 0%,0% x 100%*100%
                    new GraphicElement(ShortenName(item.displayName, tempManagerPanelDefinition.PanelSettings.NameLength, true), tempManagerPanelDefinition.PanelSettings.TextColor, 0.6f), // Name @ 0%,0% x 60%*100%
                    new GraphicElement($"{ShortNumber2(item.amount, tempManagerPanelDefinition.PanelSettings.Suffixes, 2, 6)}/{ShortNumber2(item.currentQuota, tempManagerPanelDefinition.PanelSettings.Suffixes, tempManagerPanelDefinition.PanelSettings.Decimals, 6, false)}", tempManagerPanelDefinition.PanelSettings.NumberColor, 0.4f, 1, 0.6f) // Number @ 60%,0% x 40%*100%
                });

            GraphicObject GenerateStandardItem(ItemDefinition item)
            {
                //Background
                //Name
                //Count/Quota
                //Progress Bar Background
                //Progress Bar Foreground
                //Percentage
                float percent = (float)item.Percentage / 100f;
                double displayedPercent = Math.Min(100.0, item.Percentage);
                return new GraphicObject(new List<GraphicElement>
                {
                    new GraphicElement("SquareSimple", tempManagerPanelDefinition.PanelSettings.BackColor, 1, 1, 0, 0, true), // Background @ 0%,0% x 100%*100%
                    new GraphicElement(ShortenName(item.displayName, tempManagerPanelDefinition.PanelSettings.NameLength, true), tempManagerPanelDefinition.PanelSettings.TextColor, 0.5f), // Name @ 0%,0% x 50%*100%
                    new GraphicElement($"{ShortNumber2(item.amount, tempManagerPanelDefinition.PanelSettings.Suffixes, 2, 6)}/{ShortNumber2(item.currentQuota, tempManagerPanelDefinition.PanelSettings.Suffixes, tempManagerPanelDefinition.PanelSettings.Decimals, 6, false)}", tempManagerPanelDefinition.PanelSettings.NumberColor, 0.35f, 1, 0.5f), // Number @ 50%,0% x 35%*100%
                    ProgressBarBack(0.15f, 1, 0.85f, 0), // Progress bar back @ 85%,0% x 15%*100%
                    ProgressBarFront(percent, 0.15f, 1, 0.85f, 0), // Progress bar front @ 85%,0% x 15%*100%
                    new GraphicElement($"{TruncateNumber(displayedPercent, 2):N2}%".PadLeft(6), tempManagerPanelDefinition.PanelSettings.NumberColor, 0.15f, 1, 0.85f) // Progress bar number @ 85%,0% x 15%*100%
                });
            }

            GraphicElement ProgressBarBack(float width, float height, float x, float y) =>
                new GraphicElement(
                    "SquareHollow",
                    new Color(0, 0, 0, 180),
                    width,
                    height,
                    x,
                    y,
                    true);

            GraphicElement ProgressBarFront(float percent, float width, float height, float x, float y, bool invertColor = false) =>
                new GraphicElement(
                    "SquareSimple",
                    invertColor ? new Color ((int)(230.0 * Math.Min(1f, percent)), (int)(230.0 * (1f - Math.Min(1f, percent))), 0, 220) : new Color((int)(230.0 * (1f - Math.Min(1f, percent))), (int)(230.0 * Math.Min(1f, percent)), 0, 220),
                    width * Math.Min(1f, percent),
                    height,
                    x,
                    y,
                    true);

            public string GetFont(string fontType)
            {
                for (int i = 0; i < fontList.Count; i++)
                    if (StringsMatch(fontType, fontList[i]))
                        return fontList[i];
                return "Monospace";
            }

            MySprite ProcessSprite(GraphicElement graphicElement, Vector2 objectPosition, Vector2 objectSize, string font, IMyTextSurface surface)
            {
                float size = 0f;
                Vector2 objectOffset;
                if (!graphicElement.Texture)
                {
                    StringBuilder builder = new StringBuilder(graphicElement.Text);
                    Vector2 textMeasurement = surface.MeasureStringInPixels(builder, font, 1f);
                    size = Math.Min(objectSize.X / textMeasurement.X, objectSize.Y / textMeasurement.Y);
                    textMeasurement = surface.MeasureStringInPixels(builder, font, size);
                    objectOffset = new Vector2(0f, (objectSize.Y * 0.5f) - (textMeasurement.Y * 0.5f));
                }
                else objectOffset = Vector2.Zero;

                return new MySprite(
                        graphicElement.Texture ? SpriteType.TEXTURE : SpriteType.TEXT,
                        graphicElement.Text,
                        objectPosition + objectOffset + (graphicElement.Texture ? objectSize / 2f : Vector2.Zero),
                        objectSize,
                        graphicElement.ElementColor,
                        graphicElement.Texture ? null : font,
                        graphicElement.Texture ? centerAlignment : leftAlignment,
                        size
                        );
            }

            #endregion


            #region Methods

            public void CheckPanel(PanelClass panelClass)
            {
                string hashKey = panelClass.EntityFlickerID;

                if (parent.antiflickerSet.Add(hashKey))
                {
                    IMyTextSurface surface = panelClass.Surface;

                    if (surface.ContentType == ContentType.NONE)
                        surface.ContentType = ContentType.SCRIPT;

                    if (surface.Script != nothingType)
                        surface.Script = nothingType;

                    if (surface.ContentType == ContentType.SCRIPT)
                        surface.WriteText("");
                    else
                        surface.DrawFrame().Dispose();

                    if (surface.ScriptForegroundColor == defPanelForegroundColor)
                    {
                        surface.ScriptForegroundColor = Color.Black;
                        surface.ScriptBackgroundColor = new Color(73, 141, 255, 255);
                    }
                }
            }

            #endregion


            #region Classes

            public class GraphicDocument
            {
                public SortedList<TextAlignment, List<GraphicObject>> GraphicObjects = new SortedList<TextAlignment, List<GraphicObject>>();

                public DateTime expirationTime = Now;

                public void Generated()
                {
                    expirationTime = Now.AddSeconds(1);
                }
            }

            public struct GraphicObject
            {
                public List<GraphicElement> Elements;

                public GraphicObject Clone => new GraphicObject(new List<GraphicElement>(Elements));

                public GraphicObject(List<GraphicElement> elements)
                {
                    Elements = elements;
                }
            }

            public struct GraphicElement
            {
                public string Text;
                public Color ElementColor;
                public float WidthPercentage, HeightPercentage, StartPointHorizontal, StartPointVertical;
                public bool Texture;

                public GraphicElement(string text, Color color, float widthPercentage = 1f, float heightPercentage = 1f, float startPointHorizontal = 0f, float startPointVertical = 0f, bool texture = false)
                {
                    Text = text;
                    ElementColor = color;
                    WidthPercentage = widthPercentage;
                    HeightPercentage = heightPercentage;
                    StartPointHorizontal = startPointHorizontal;
                    StartPointVertical = startPointVertical;
                    Texture = texture;
                }
            }

            #endregion
        }

        public class PanelClass
        {
            public BlockDefinition Parent;

            public PanelSettings PanelSettings = new PanelSettings();

            public int SurfaceIndex = 0;

            public DateTime NextUpdateTime = Now;

            public IMyTextSurface Surface => Provider ? ((IMyTextSurfaceProvider)Parent.Block).GetSurface(SurfaceIndex) : (IMyTextPanel)Parent.Block;

            public string DocumentKey => PanelSettings.DocumentKey;

            public string EntityFlickerID => $"{Parent.Block.EntityId}{(Provider ? $":{SurfaceIndex}" : "")}";

            public bool Provider => !(Parent.Block is IMyTextPanel);

            public string DataSource
            {
                get
                {
                    return Parent.DataSource;
                }
                set
                {
                    Parent.DataSource = value;
                }
            }

            public PanelClass(BlockDefinition parent, int surfaceIndex)
            {
                Parent = parent;
                SurfaceIndex = surfaceIndex;
                PanelSettings.Initialize(this);
            }
        }

        public class PanelSettings
        {
            public static PanelMaster2 master;

            PanelClass Parent;

            public PanelType Type = PanelType.None;

            public string Font = "Monospace", SpanID, SpanChildID, ItemSearchString,
                          SettingBackup = "";

            public List<string> Categories = NewStringList,
                                Suffixes = suffixesTemplate.Split('|').ToList();

            public ItemCollection2 Items = new ItemCollection2(false);

            public DisplayType DisplayType = DisplayType.Standard;

            public PanelItemSorting SortType = PanelItemSorting.Alphabetical;

            public List<PanelOptions> Options = new List<PanelOptions>();

            public double MinimumItemValue = 0, MaximumItemValue = double.MaxValue, UpdateDelay = 1, OffsetMultiplier = 1, TextMultiplier = 1;

            public int Rows = -1, NameLength = 18, Decimals = 2;

            public Color TextColor = Color.Red, NumberColor = Color.Yellow, BackColor = Color.Black;

            public DateTime LastUpdateTime = DateTime.MinValue;

            private string documentKey = "";

            public string DocumentKey
            {
                get
                {
                    if (!TextHasLength(documentKey))
                        documentKey = Type == PanelType.Span && TextHasLength(SpanID) ? SpanID : $"{Type}:{String.Join("|", Categories)}:{String.Join("|", Suffixes)}:{Items}:{DisplayType}:{SortType}:{String.Join("|", Options)}:{MinimumItemValue}:{MaximumItemValue}:{NameLength}:{Decimals}:{TextColor}:{NumberColor}:{BackColor}";
                    return documentKey;
                }
            }

            public void Initialize(PanelClass parent = null)
            {
                if (parent != null)
                    Parent = parent;
                MinimumItemValue = 0;
                MaximumItemValue = double.MaxValue;
                ItemSearchString = SpanID = SpanChildID = documentKey = "";
                Items.Clear();
                Options.Clear();
                Categories.Clear();
            }

            public bool LoadSetting(string text)
            {
                string key, data;

                SplitData(text, out key, out data);

                key = key.ToLower();

                if (!TextHasLength(data)) return false;

                bool changed = true;

                switch (key)
                {
                    case "type":
                        Enum.TryParse<PanelType>(data, true, out Type);
                        break;
                    case "font":
                        Font = master.GetFont(data);
                        if (Parent.Surface.ContentType == ContentType.TEXT_AND_IMAGE)
                            Parent.Surface.Font = Font;
                        break;
                    case "categories":
                        PopulateClassList(Categories, data.ToLower().Split('|').OrderBy(b => b));
                        break;
                    case "items":
                        ItemSearchString += $"{(TextHasLength(ItemSearchString) ? "┤" : "")}{data}";
                        break;
                    case "item display":
                        Enum.TryParse<DisplayType>(data, true, out DisplayType);
                        break;
                    case "sorting":
                        Enum.TryParse<PanelItemSorting>(data, true, out SortType);
                        break;
                    case "options":
                        changed = false;
                        Options.Clear();
                        string[] options = data.Split('|');
                        PanelOptions panelOption;
                        foreach (string option in options)
                            if (Enum.TryParse<PanelOptions>(option, true, out panelOption))
                            {
                                Options.Add(panelOption);
                                changed = true;
                            }
                        break;
                    case "minimum value":
                        if (!double.TryParse(data, out MinimumItemValue))
                            MinimumItemValue = 0;
                        MinimumItemValue = Math.Max(0, Math.Min(MinimumItemValue, MaximumItemValue));
                        break;
                    case "maximum value":
                        if (!double.TryParse(data, out MaximumItemValue))
                            MaximumItemValue = 0;
                        MaximumItemValue = Math.Max(0, Math.Max(MaximumItemValue, MinimumItemValue));
                        break;
                    case "number suffixes":
                        PopulateClassList(Suffixes, data.Split('|'));
                        if (Suffixes.Count == 0) Suffixes = suffixesTemplate.Split('|').ToList();
                        break;
                    case "text color":
                        if (!GetColor(ref TextColor, data))
                            TextColor = Color.Red;
                        break;
                    case "number color":
                        if (!GetColor(ref NumberColor, data))
                            NumberColor = Color.Yellow;
                        break;
                    case "back color":
                        if (!GetColor(ref BackColor, data))
                            BackColor = Color.Black;
                        break;
                    case "rows":
                        if (!int.TryParse(data, out Rows))
                            Rows = -1;
                        break;
                    case "name length":
                        if (!int.TryParse(data, out NameLength))
                            NameLength = 18;
                        NameLength = Math.Max(NameLength, 3);
                        break;
                    case "decimals":
                        if (!int.TryParse(data, out Decimals))
                            Decimals = 2;
                        Decimals = Math.Max(0, Decimals);
                        break;
                    case "update delay":
                        if (!double.TryParse(data, out UpdateDelay))
                            UpdateDelay = 1;
                        UpdateDelay = Math.Max(0, UpdateDelay);
                        break;
                    case "span id":
                        SpanID = data;
                        break;
                    case "span child id":
                        SpanChildID = data;
                        break;
                    case "offset multiplier":
                        if (!double.TryParse(data, out OffsetMultiplier))
                            OffsetMultiplier = 1;
                        break;
                    case "text multiplier":
                        if (!double.TryParse(data, out TextMultiplier))
                            TextMultiplier = 1;
                        break;
                    default:
                        changed = false;
                        break;
                }
                if (changed)
                {
                    documentKey = "";
                    return true;
                }
                return false;
            }

            string ColorToString(Color color)
            {
                return $"{color.R}:{color.G}:{color.B}:{color.A}";
            }

            bool GetColor(ref Color color, string colorString)
            {
                try
                {
                    string[] colorArray = colorString.Split(':');
                    if (colorArray.Length == 4)
                        color = new Color(int.Parse(colorArray[0]), int.Parse(colorArray[1]), int.Parse(colorArray[2]), int.Parse(colorArray[3]));
                    else if (colorArray.Length == 3)
                        color = new Color(int.Parse(colorArray[0]), int.Parse(colorArray[1]), int.Parse(colorArray[2]));
                    else
                        return false;
                    return true;
                }
                catch { }
                return false;
            }

            public override string ToString()
            {
                StringBuilder builder = NewBuilder;
                BuilderAppendLine(builder, $"Type={(Type == PanelType.None ? String.Join("/", GetEnumList<PanelType>()) : $"{Type}")}");
                AppendOption(builder, $"Font={Font}", Font == "Monospace");
                AppendOption(builder, $"Categories={(Categories.Count > 0 ? String.Join("|", Categories.Select(c => Formatted(c))) : itemCategoryString)}", Categories.Count == 0);
                if (!AppendSearchString(builder, ItemSearchString, "Items"))
                    AppendOption(builder, "Items=ingot:Iron:Cobalt|ore:Iron");
                AppendOption(builder, $"Item Display={(DisplayType == DisplayType.Standard ? String.Join("/", GetEnumList<DisplayType>()) : $"{DisplayType}")}", DisplayType == DisplayType.Standard);
                AppendOption(builder, $"Sorting={(SortType == PanelItemSorting.Alphabetical ? String.Join("/", GetEnumList<PanelItemSorting>()) : $"{SortType}")}", SortType == PanelItemSorting.Alphabetical);
                AppendOption(builder, $"Options={(Options.Count == 0 ? String.Join("|", GetEnumList<PanelOptions>()) : String.Join("|", Options))}", Options.Count == 0);
                AppendOption(builder, $"Minimum Value={MinimumItemValue}", MinimumItemValue <= zero);
                AppendOption(builder, $"Maximum Value={(MaximumItemValue < double.MaxValue ? $"{MaximumItemValue}" : "0")}", MaximumItemValue == double.MaxValue);
                BuilderAppendLine(builder, $"Number Suffixes={String.Join("|", Suffixes)}");
                BuilderAppendLine(builder, $"Text Color={ColorToString(TextColor)}");
                BuilderAppendLine(builder, $"Number Color={ColorToString(NumberColor)}");
                BuilderAppendLine(builder, $"Back Color={ColorToString(BackColor)}");
                AppendOption(builder, $"Rows={Rows}", Rows < 0);
                AppendOption(builder, $"Name Length={NameLength}", NameLength == 18);
                AppendOption(builder, $"Decimals={Decimals}", Decimals == 2);
                AppendOption(builder, $"Update Delay={UpdateDelay}", UpdateDelay == 1.0);
                AppendOption(builder, $"Offset Multiplier={OffsetMultiplier}", OffsetMultiplier == 1.0);
                AppendOption(builder, $"Text Multiplier={TextMultiplier}", TextMultiplier == 1.0);
                AppendOption(builder, $"Span ID={SpanID}", !TextHasLength(SpanID));
                AppendOption(builder, $"Span Child ID={SpanChildID}", !TextHasLength(SpanChildID));

                return builder.ToString().Trim();
            }
        }
    }
}
