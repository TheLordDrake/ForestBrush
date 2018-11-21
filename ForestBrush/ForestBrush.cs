﻿using System;
using System.Collections.Generic;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using ForestBrush.GUI;
using ICities;
using UnityEngine;

namespace ForestBrush
{           
    public class ForestBrushMod : Singleton<ForestBrushMod>
    {
        private static readonly string kEmptyContainer = "EmptyContainer";

        private static readonly string kMainToolbarSeparatorTemplate = "MainToolbarSeparator";

        private static readonly string kMainToolbarButtonTemplate = "MainToolbarButtonTemplate";

        private string kToggleButton = "ForestBrushModToggle";

        bool initialized;

        ToolBase lastTool;

        internal ForestBrushPanel ForestBrushPanel { get; private set; }

        internal TreeInfo Container { get; private set; }

        UITextureAtlas atlas;

        UITextureAtlas Atlas
        {
            get
            {
                if (atlas == null)
                {
                    atlas = Resources.ResourceLoader.LoadAtlas();
                }
                return atlas;
            }
        }

        internal bool IsCurrentTreeContainer =>  Container != null && ToolsModifierControl.toolController.CurrentTool is TreeTool && ((TreeTool)ToolsModifierControl.toolController?.CurrentTool)?.m_prefab == Container;

        public Dictionary<string, TreeInfo> Trees { get; set; } = new Dictionary<string, TreeInfo>();

        public Dictionary<string, List<string>> Brushes { get; set; } = new Dictionary<string, List<string>>();

        public TreeBrushTool BrushTool { get; internal set; }

        public UIButton ToggleButton { get; private set; }

        GameObject page;

        internal void Initialize()
        {
            Container = PrefabCollection<TreeInfo>.FindLoaded("ForestBrushContainer.Forest Brush_Data");
            if (Container == null)
            {
                Debug.LogWarning("Can't find ForestBrushContainer asset. Initialization failed.");
                return;
            }

            UITabstrip tabstrip = ToolsModifierControl.mainToolbar.component as UITabstrip;

            SpawnSeparator(tabstrip);
            ToggleButton = SpawnToggleButton(tabstrip);
            SpawnSeparator(tabstrip);

            Trees = LoadAllTrees();

            List<string> brush = new List<string>();
            try
            {
                Brushes.TryGetValue(SavedSettings.SelectedBrush.value, out brush);
            }
            catch (Exception)
            {
                Debug.LogWarning("Failed Brushes TryGetValue. Line 92   ");
            }
            try
            {
                BrushTool = new TreeBrushTool(SavedSettings.SelectedBrush, SavedSettings.SelectedBrush == Constants.VanillaPack ? null : brush);
            }
            catch (Exception)
            {
                Debug.LogWarning("Failed on brushTool = new TreeBrushTool Line 100");
            }

            ForestBrushPanel = page.GetComponent<UIPanel>().AddUIComponent(typeof(ForestBrushPanel)) as ForestBrushPanel;

            ToggleButton.eventClick += OnToggleClick;

            ForestBrushPanel.eventVisibilityChanged += OnForestBrushPanelVisibilityChanged;

            LocaleManager.eventLocaleChanged += SetTutorialLocale;
            
            SetTutorialLocale();

            initialized = true;
        }

        private UIButton SpawnToggleButton(UITabstrip tabstrip)
        {
            UIButton button;

            GameObject mainToolbarButtonTemplate = UITemplateManager.GetAsGameObject(kMainToolbarButtonTemplate);
            page = UITemplateManager.GetAsGameObject(kEmptyContainer);


            button = tabstrip.AddTab(kToggleButton, mainToolbarButtonTemplate, page, new Type[0]) as UIButton;
            button.atlas = Atlas;

            button.normalFgSprite = "ForestBrushNormal";
            button.disabledFgSprite = "ForestBrushDisabled";
            button.focusedFgSprite = "ForestBrushFocused";
            button.hoveredFgSprite = "ForestBrushHovered";
            button.pressedFgSprite = "ForestBrushPressed";

            button.normalBgSprite = "ToolbarIconGroup6Normal";
            button.disabledBgSprite = "ToolbarIconGroup6Disabled";
            button.focusedBgSprite = "ToolbarIconGroup6Focused";
            button.hoveredBgSprite = "ToolbarIconGroup6Hovered";
            button.pressedBgSprite = "ToolbarIconGroup6Pressed";

            IncrementObjectIndex();

            return button;
        }

        private void SetTutorialLocale()
        {
            Locale locale = (Locale)typeof(LocaleManager).GetField("m_Locale", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(LocaleManager.instance);
            Locale.Key key = new Locale.Key
            {
                m_Identifier = "TUTORIAL_ADVISER_TITLE",
                m_Key = kToggleButton,
            };
            if (!locale.Exists(key))
            {
                locale.AddLocalizedString(key, UserMod.Translation.GetTranslation("FOREST-BRUSH-MODNAME"));
            }
            key = new Locale.Key
            {
                m_Identifier = "TUTORIAL_ADVISER",
                m_Key = kToggleButton
            };
            if (!locale.Exists(key))
            {
                locale.AddLocalizedString(key, UserMod.Translation.GetTranslation("FOREST-BRUSH-TUTORIAL"));
            }
        }

        private void IncrementObjectIndex()
        {
            FieldInfo m_ObjectIndex = typeof(MainToolbar).GetField("m_ObjectIndex", BindingFlags.Instance | BindingFlags.NonPublic);
            m_ObjectIndex.SetValue(ToolsModifierControl.mainToolbar, (int)m_ObjectIndex.GetValue(ToolsModifierControl.mainToolbar) + 1);
        }

        protected UIComponent SpawnSeparator(UITabstrip strip)
        {
            UIComponent uicomponent;
            GameObject asGameObject = UITemplateManager.GetAsGameObject(kMainToolbarSeparatorTemplate);
            GameObject asGameObject2 = UITemplateManager.GetAsGameObject(kEmptyContainer);
            uicomponent = strip.AddTab("Separator", asGameObject, asGameObject2, new Type[0]);
            uicomponent.width *= 0.5f;
            uicomponent.isEnabled = false;
            IncrementObjectIndex();
            return uicomponent;
        }

        private void OnForestBrushPanelVisibilityChanged(UIComponent component, bool visible)
        {
            if (visible)
            {
                lastTool = ToolsModifierControl.toolController.CurrentTool;
                TreeTool tool = ToolsModifierControl.GetTool<TreeTool>();
                ToolsModifierControl.toolController.CurrentTool = tool;
                tool.m_prefab = Container;
            }
            else if (lastTool != null && lastTool.GetType() != typeof(TreeTool) && ToolsModifierControl.toolController.NextTool == null)
            {                
                lastTool.enabled = true;
            }
        }

        private void OnToggleClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            ForestBrushPanel.BringToFront();
            ForestBrushPanel.UpdatePanelPosition();
            ForestBrushPanel.isVisible = !ForestBrushPanel.isVisible;
        }

        internal void CleanUp()
        {
            if (ForestBrushPanel && initialized)
            {
                Destroy(ForestBrushPanel);
                ForestBrushPanel = null;
            } 
            initialized = false;
        }

        private Dictionary<string, TreeInfo> LoadAllTrees()
        {
            var trees = new Dictionary<string, TreeInfo>();
            var treeCount = PrefabCollection<TreeInfo>.LoadedCount();
            for (uint i = 0; i < treeCount; i++)
            {
                var tree = PrefabCollection<TreeInfo>.GetLoaded(i);
                if (tree == null || tree == Container) continue;
                if (tree.m_availableIn != ItemClass.Availability.All)
                {
                    tree.m_availableIn = ItemClass.Availability.All;
                }

                if (tree.m_Atlas == null || tree.m_Thumbnail.IsNullOrWhiteSpace()) ImageUtils.CreateThumbnailAtlas(GetName(tree), tree);

                trees.Add(tree.name, tree);
            }
            return trees;
        }

        public static string GetName(PrefabInfo prefab)
        {
            string name = prefab.name;
            if (name.EndsWith("_Data"))
            {
                name = name.Substring(0, name.LastIndexOf("_Data"));
            }
            return name;
        }

        public void OnGUI()
        {
            try
            {
                if (initialized && !UIView.HasModalInput() &&
                    (!UIView.HasInputFocus() || (UIView.activeComponent != null)))
                {
                    Event e = Event.current;

                    if (SavedSettings.ToggleTool.IsPressed(e))
                    {
                        ToggleButton.SimulateClick();
                    }

                    if (ForestBrushPanel.isVisible && UIView.activeComponent.parent is ForestBrushPanel)
                    {

                        if (SavedSettings.Search.IsPressed(e))
                        {
                            ForestBrushPanel.SearchTextField.Focus();
                        }
                        if (SavedSettings.ToggleSquare.IsPressed(e))
                        {
                            ForestBrushPanel.brushOptionsPanel.squareBrushCheckBox.SimulateClick();
                        }
                        if (SavedSettings.ToggleAutoDensity.IsPressed(e))
                        {
                            ForestBrushPanel.brushOptionsPanel.autoDensityCheckBox.SimulateClick();
                        }
                        if (SavedSettings.IncreaseSize.IsPressed(e))
                        {
                            ForestBrushPanel.brushOptionsPanel.sizeSlider.value += 5f;
                        }
                        if (SavedSettings.DecreaseSize.IsPressed(e))
                        {
                            ForestBrushPanel.brushOptionsPanel.sizeSlider.value -= 5f;
                        }
                        if (SavedSettings.IncreaseDensity.IsPressed(e))
                        {
                            ForestBrushPanel.brushOptionsPanel.densitySlider.value += 0.1f;
                        }
                        if (SavedSettings.DecreaseDensity.IsPressed(e))
                        {
                            ForestBrushPanel.brushOptionsPanel.densitySlider.value -= 0.1f;
                        }
                    }
                }
                
            }
            catch (Exception)
            {
                Debug.LogWarning("OnGUI failed.");
            }            
        }

        internal void UpdateBrushItems(TreeInfo treeInfo, bool value, bool updateAll)
        {
            BrushTool.Update(treeInfo, value, updateAll);
        }
    }
}