using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using VRage.Game.ModAPI.Ingame;
//using VRage.Game.ModAPI;
using PBUnlimiter;

namespace SpaceEngineersScriptTester
{
    class StoreDisplayer
    {
        public static void DoIt()
        {
            StoreDisplayer storeDisplayer = new StoreDisplayer();
            storeDisplayer.Main();
        }
        IMyGridTerminalSystem GridTerminalSystem;

        //--------Start--------
        List<DisplayLayout> displayLayouts = new List<DisplayLayout>()
        {
            new DisplayLayout(new string[]
            {
                //Список названий дисплеев для отображения инвентарей
                "ItemPanel1", 
                "ItemPanel2", 
                "ItemPanel3"
                //Список названий дисплеев для отображения инвентарей
            }) 
            { 
                showItems = true,
                itemsGrouping = false //нужно ли отображать содержимое каждого инвентаря отдельно. (сейчас работает только false)
            },

            //Еще разрабатывалось
            //new DisplayLayout(new string[]
            //{
            //    //Список названий дисплеев для отображения энергии и водорода
            //    "Panel1"
            //    //Список названий дисплеев для отображения энергии и водорода
            //}) 
            //{ 
            //    showHydrogen = true, 
            //    showEnergy = true 
            //}
        };

        List<Resource> LocalizedResources => GetLocalizedResources();

        void Main()
        {
            displayLayouts.ForEach(x => x.fillPanels(GridTerminalSystem));

            List<IMyTerminalBlock> tempList = new List<IMyTerminalBlock>();

            List<IMyCargoContainer> cargos = new List<IMyCargoContainer>();
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(tempList);
            tempList.ForEach(x => cargos.Add(x as IMyCargoContainer));
            List<DisplayItemRow> list = new List<DisplayItemRow>();
            foreach (IMyCargoContainer cargo in cargos)
            {                
                List<IMyInventoryItem> items = cargo.GetInventory().GetItems();
                foreach(IMyInventoryItem item in items)
                {
                    Resource resource = new Resource() { 
                        isLocalized = false, 
                        origName = item.Content.SubtypeName, 
                        unit = "ед.",
                        lowLimit = -1,
                        scale = 1,
                        type = ResourceType.UnDefined                        
                    };
                    long value = item.Amount.RawValue;
                    DisplayItemRow row = ToDisplayRow(resource, value);
                    row.cargoId = cargo.GetId();
                    row.cargoName = cargo.DisplayName;
                    list.Add(row);
                }
            }
            displayLayouts.Where(x => x.showItems).ForEach(y => DisplayIt(y, list));
        }

        void DisplayIt(DisplayLayout displayLayout, List<DisplayItemRow> rows)
        {
            List<DisplayRow> finalRow = new List<DisplayRow>();
            finalRow.Add(new DisplayHeaderRow() { title = "Запасы", alingCenter = true });
            if (displayLayout.itemsGrouping)
            {
                finalRow.AddRange(rows
                    .GroupBy(x => x.resource.name)
                    .Select(g => new DisplayItemRow(g.First().resource, g.Sum(y => y.value))).ToList());
            }
            else
            {
                //throw new NotImplementedException("itemsGrouping установите в false, я недоделал");
            }
            int rowIndex = 0;
            foreach(IMyTextPanel panel in displayLayout.panels)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < displayLayout.onPanelRowsCount; i++)
                {
                    if (rows.Count > rowIndex)
                        sb.AppendLine(rows[rowIndex].ToStringAsTableRow(displayLayout));
                    else
                        return;
                    rowIndex++;
                }
                panel.WritePublicText(sb.ToString());
            }
        }

        List<Resource> GetLocalizedResources()
        {
            return new List<Resource>() 
            {
            
            };
        }
        Resource CreateLocalizedResources(string name, ResourceType type, float limit = -1, string unit = "ед.")
        {
            Resource ret = new Resource() { 
                isLocalized = true, 
                localizedName = name, 
                type = type
            };
            switch (type)
            {
                case ResourceType.Ore:
                    ret.scale = 0.0001f;
                    ret.lowLimit = 20;
                    ret.unit = "кг.";
                    break;
                case ResourceType.UnDefined:
                    ret.scale = 1;
                    ret.lowLimit = -1;
                    ret.unit = "ед.";
                    break;
                case ResourceType.Component:
                    ret.scale = 1;
                    ret.lowLimit = 10;
                    ret.unit = "шт.";
                    break;
                case ResourceType.Ingot:
                    ret.scale = 1;
                    ret.lowLimit = 50;
                    ret.unit = "слит.";
                    break;
                case ResourceType.Instument:
                    ret.scale = 1;
                    ret.lowLimit = -1;
                    ret.unit = "шт.";
                    break;
                default:
                    ret.scale = 1;
                    ret.lowLimit = -1;
                    ret.unit = "ед.";
                    break;
            }
            if (limit != -1) ret.lowLimit = limit;
            return ret;
        }

        static DisplayItemRow ToDisplayRow(Resource resource, float value)
        {
            return new DisplayItemRow(resource, value);
        }
        static string dots(int count)
        {
            if (count <= 0) return "";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                sb.Append('.');
            }
            return sb.ToString();
        }

        class DisplayLayout
        {
            public bool showItems { get; set; } = false;
            public bool itemsGrouping { get; set; } = false;
            public bool showHydrogen { get; set; } = false;
            public bool showEnergy { get; set; } = false;
            public string[] panelsNames { get; set; }
            public IMyTextPanel[] panels { get; private set; }
            public int onPanelRowsCount { get; private set; }
            public int rowLenth { get; private set; }
            public DisplayLayout(string[] textPanelsNames, int onPanelRowsCount = 0, int rowLenth = 30)
            {
                int maxRowLenth = 20;
                if (rowLenth < maxRowLenth) throw new Exception($"Слишком маленькая максимальная длинна строки! Установите значение больше {maxRowLenth}");
                this.onPanelRowsCount = onPanelRowsCount;
                this.rowLenth = rowLenth;
                this.panelsNames = textPanelsNames;
            }
            public void fillPanels(IMyGridTerminalSystem gridTerminalSystem)
            {
                List<IMyTextPanel> panelList = new List<IMyTextPanel>();
                foreach(string name in panelsNames)
                {
                    panelList.Add(gridTerminalSystem.GetBlockWithName(name) as IMyTextPanel);
                }
                panels = panelList.ToArray();
            }
        }
        abstract class DisplayRow
        {
            public abstract string ToStringAsTableRow(DisplayLayout displayLayout);
        }
        class DisplayItemRow : DisplayRow
        {
            public DisplayItemRow(Resource resource, float value)
            {
                this.resource = resource;
                this.value = value;
            }
            public Resource resource { get; set; }
            public float value { get; set; }
            public float scaledValue => value * resource.scale;
            public bool alert => scaledValue <= resource.lowLimit;
            public float cargoId { get; set; }
            public string cargoName { get; set; }

            public override string ToStringAsTableRow(DisplayLayout displayLayout)
            {
                string name = resource.name;
                string unit = resource.unit;
                string val;
                switch (resource.type)
                {
                    case ResourceType.Ore:
                        val = Math.Round(value, 2).ToString();
                        if (val.Length > 5) val = val.Substring(0, val.Length - 3);
                        break;
                    default:
                        val = value.ToString();
                        break;
                }
                if (val.Length > 5) val = "> 10к";
                string result = $"{name} {val} {unit}";
                if (result.Length > displayLayout.rowLenth)
                {
                    name = name.Substring(0, result.Length - displayLayout.rowLenth - 3) + "...";
                    result = $"{name} {val} {unit}";
                }
                else
                {
                    result = $"{name}{dots(displayLayout.rowLenth - result.Length)} {val} {unit}";
                }
                return result;
            }
        }
        class DisplayHeaderRow : DisplayRow
        {
            public string title { get; set; }
            public bool alingCenter { get; set; }

            public override string ToStringAsTableRow(DisplayLayout displayLayout)
            {
                if (alingCenter) return $"{dots((displayLayout.rowLenth - title.Length) / 2 - 1)} {title} {dots((displayLayout.rowLenth - title.Length) / 2 - 1)}";
                else return $"{title} {dots(displayLayout.rowLenth - title.Length + 1)}"; 
            }
        }

        class Resource
        {
            public ResourceType type { get; set; } = ResourceType.UnDefined;
            public string origName { private get; set; }
            public bool isLocalized { get; set; } = false;
            public string localizedName { private get; set; }
            public string name => isLocalized ? localizedName : origName;
            public string unit { get; set; }
            public float scale { get; set; }
            public float lowLimit { get; set; }
        }

        enum ResourceType
        {
            UnDefined = 0,
            Ore = 1,
            Ingot = 2,
            Component = 3,
            Instument = 4
        }


        //---------End---------



    }
}
