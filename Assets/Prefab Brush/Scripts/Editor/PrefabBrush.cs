/*
 * 		Prefab Brush+ 
 * 		Version 1.4.3
 *		Author: Archie Andrews
 *		www.archieandrews.games
 */

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace ArchieAndrews.PrefabBrush
{
    [ExecuteInEditMode]
    public class PrefabBrush : EditorWindow
    {
        private string version = "1.4.3";
        private PB_ActiveTab activeTab = PB_ActiveTab.PrefabPaint;
        private PB_ActiveTab previousTab = PB_ActiveTab.PrefabPaint;

        [SerializeField]
        public PB_SaveObject activeSave;
        public List<PB_SaveObject> saveObjects = new List<PB_SaveObject>();
        private string[] saveObjectNames;
        private int selectedSave = 0;

        //Foldout bools
        private bool showEraseSettings = true;
        private bool showHotKeySettings = true;
        private bool showFilters = true;
        private bool showMods = true;
        private bool showPrefabDisplay = true;
        private bool showPaintOptions = true;
        private bool showBrushSlider = true;
        private bool showEraseFilters = true;
        private bool showMaxBrushSizeSlider = false;
        private bool showMaxMinPaintDelta = false;
        private bool showMaxMinPrefabsPerStroke = false;
        private bool showIgnoreOptions = false;

        //Scrolls
        private Vector2 scrollPos;
        private Vector2 prefabViewScrollPos;

        //Settings variables
        private Color placeBrush = new Color(0, 1, 0, 0.1f);
        private Color eraseBrush = new Color(1, 0, 0, 0.1f);
        private Color selectedTab = Color.green;
        private Color disabledColor = new Color(1, 1, 1, .3f);
        private bool tempTab = false, tempState = false;
        private bool showToolsWarning = true;

        //On Off variables
        [SerializeField]
        private bool isOn = true;
        private bool wasOn = true;
        private Texture2D onButtonLight;
        private Texture2D offButtonLight;
        private Texture2D onButtonDark;
        private Texture2D offButtonDark;
        private Texture2D buttonIcon;
        private Texture2D icon;

        //Styles
        private GUIStyle style;
        private GUIStyle styleBold;
        private GUIStyle styleFold;

        //Scale
        private const float deleteButtonSize = 20;
        private const float toggleButtonSize = 20;
        private const int prefabIconMinSize = 64;
        private float prefabIconScaleFactor = 1;

        //Prefab mod variables
        private PB_PrefabDisplayType prefabDisplayType;
        private int roundRobinCount = 0;
        private float rotationSet = 0, scaleSet = 1;

        //Rects
        Rect dropRect;
        Rect saveDropRect;
        Rect parentRect;

        //Other
        private GameObject[] hierarchy;
        private Bounds brushBounds;
        private float paintTravelDistance = 0;
        private Vector3 rayLastFrame = Vector3.positiveInfinity;
        private bool moddingSingle = false;
        private GameObject objectToSingleMod = null;
        private float objectToSingleRotation = 0;
        private Vector3 hitNormalSinceLastMovement;
        private const int maxFails = 10;
        private LayerMask layerBeforeSingleMod;
        Event e;
        private GameObject selectedObject, clone;
        private string[] filters = new string[] { "Tag", "Layer", "Slope", "Terrain" };
        private string[] modifiers = new string[] { "Offset Center", "Offset Rotation", "Apply Parent", "Rotate To Match Surface", "Customize Rotation", "Customize Scale" };
        private bool showSaveDrop = false;
        private GUIContent labelContent;
        private const string saveLocationKey = "PB_SaveLocation";
        private List<string> saveSearchPaths = new List<string>();

        public class PathWrapper
        {
            public List<string> paths = new List<string>();
        }

        public static PB_SaveObject newSave;

        //Display the window.
        public static void ShowWindow(PB_SaveObject save)
        {
            ShowWindow();
            newSave = save;
        }

        //Display the window using the Tools menu
        [MenuItem("Tools/Prefab Brush+")]
        public static void ShowWindow()
        {
            GetWindow(typeof(PrefabBrush), false, "Prefab Brush+");
        }

#region SetUp
        void OnBecameVisible()
        {
            SetOnState(wasOn);
        }

        void OnBecameInvisible()
        {
            //Cache the on state so we can bring it back when the window is visible again
            wasOn = isOn;
            SetOnState(false);
        }

        private void HideTools(bool hide = true)
        {
            Tools.hidden = hide;

            if (activeSave == null)
                return;

            if (showToolsWarning)
            {
                string message = hide ? "Hiding Tools as prefab brush window is active. (Go to the settings tab in the Prefab Brush+ window to hide these warnings.)" : "Showing Tools again as prefab brush window is no longer active. (Go to the settings tab in the Prefab Brush+ window to hide these warnings.)";
                Debug.LogWarning(message);
            }
        }

        void OnFocus()
        {
            GetAllSaves();

#if UNITY_2018 || UNITY_2017 || UNITY_5 || UNITY_4
            SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
            SceneView.onSceneGUIDelegate += this.OnSceneGUI;
#else
            SceneView.duringSceneGui -= this.OnSceneGUI;
            SceneView.duringSceneGui += this.OnSceneGUI;
#endif
        }

        void Awake()
        {
            LoadResources();
            GetSavePaths();
            //Any issues with not being able to find brush files uncomment the line under, comment out the line above and run for a second and then recomment it again.
            //ResetStoredSavePaths();
        }

        private void ResetStoredSavePaths()
        {
            saveSearchPaths.Clear();
            saveSearchPaths.Add(GetSaveDirectory());

            StoreSavePaths();
        }

        private void LoadResources()
        {
            string[] guids;
            guids = AssetDatabase.FindAssets($"t:{nameof(PB_EditorResources)}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PB_EditorResources editorResources = AssetDatabase.LoadAssetAtPath<PB_EditorResources>(path);

                onButtonLight = editorResources.L_Button_On;
                offButtonLight = editorResources.L_Button_Off;
                onButtonDark = editorResources.D_Button_On;
                offButtonDark = editorResources.D_Button_Off;
                icon = editorResources.PB_Icon;
                break;
            }

            buttonIcon = GetButtonTexture();

            //Repaint for good mesure.
            Repaint();
        }

        private void GetSavePaths()
        {
            if (EditorPrefs.HasKey(saveLocationKey))
            {
                string jsonObject = EditorPrefs.GetString(saveLocationKey);
                saveSearchPaths = JsonUtility.FromJson<PathWrapper>(jsonObject).paths;
                saveSearchPaths[0] = GetSaveDirectory();

                CheckSavePathIntegrity();
            }
            else
            {
                saveSearchPaths.Add(GetSaveDirectory());
                StoreSavePaths();
            }
        }

        private void CheckSavePathIntegrity()
        {
            for (int i = 0; i < saveSearchPaths.Count; i++)
            {
                if (!AssetDatabase.IsValidFolder(saveSearchPaths[i]))
                {
                    saveSearchPaths.RemoveAt(i);
                }
            }
        }

        private void StoreSavePaths()
        {
            PathWrapper newWrapper = new PathWrapper();
            newWrapper.paths = saveSearchPaths;
            EditorPrefs.SetString(saveLocationKey, JsonUtility.ToJson(newWrapper));
        }
        #endregion

        #region GUI
        void OnGUI()
        {
            CheckIfDraggingSave();
            SetStyles();
            EditorGUILayout.BeginVertical();

            DrawHeader();
            EditorGUILayout.Space();
            DrawTabs();
            EditorGUILayout.Space();
            DrawUtilityButtons();

            DrawContent();

            EditorGUILayout.EndVertical();

            //Keep the active data saved over play mode
            if (activeSave != null)
                EditorUtility.SetDirty(activeSave);
        }
        
        private void SetStyles()
        {
            style = EditorStyles.label;
            styleBold = EditorStyles.boldLabel;
            styleFold = EditorStyles.foldout;
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(icon, GUILayout.Width(30), GUILayout.Height(30));
            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Prefab Brush+   v{version}", styleBold);
            GUILayout.FlexibleSpace();

            GUI.color = Color.white;
            if (GUILayout.Button("Documentation"))
                Help.BrowseURL("https://drive.google.com/file/d/1KAgStGaMbPGgChszVGRNNr9ivGjSUuYK/view?usp=sharing");

            SetTabColour(PB_ActiveTab.About);
            if (GUILayout.Button("About"))
                SetActiveTab(PB_ActiveTab.About);

            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();
            DrawSaveDropDown();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSaveDropDown()
        {
            if (saveObjects.Count <= 0)
                return;

            EditorGUI.BeginChangeCheck();

            selectedSave = EditorGUILayout.Popup("Selected Brush", selectedSave, saveObjectNames);

            if (EditorGUI.EndChangeCheck())
            {
                SelectSave(saveObjects[selectedSave]);
            }
        }

        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal();
            SetTabColour(PB_ActiveTab.PrefabPaint);
            if (GUILayout.Button("Prefab Paint Brush"))
                SetActiveTab(PB_ActiveTab.PrefabPaint);

            SetTabColour(PB_ActiveTab.PrefabErase);
            if (GUILayout.Button("Prefab Erase Brush"))
                SetActiveTab(PB_ActiveTab.PrefabErase);

            SetTabColour(PB_ActiveTab.Settings);
            if (GUILayout.Button("Settings"))
                SetActiveTab(PB_ActiveTab.Settings);

            EditorGUILayout.EndHorizontal();

            GUI.color = Color.white;
        }

        private void DrawUtilityButtons()
        {
            EditorGUILayout.BeginHorizontal();
            if (activeSave != null)
            {
                DrawOnOffButton();
            }

            if (GUILayout.Button("Select Active Save"))
            {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = saveObjects[selectedSave];
                EditorGUIUtility.PingObject(saveObjects[selectedSave]);
            }

            if (GUILayout.Button("Make New Brush"))
            {
                CreateNewSave();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawContent()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(position.height - 100));
            switch (activeTab)
            {
                case PB_ActiveTab.PrefabPaint:
                    if (activeSave != null && !showSaveDrop)
                        DrawPrefabPaintTab();
                    break;
                case PB_ActiveTab.PrefabErase:
                    if (activeSave != null && !showSaveDrop)
                        DrawEraseTab();
                    break;
                case PB_ActiveTab.Settings:
                    if (!showSaveDrop)
                        DrawSettingsTab();
                    break;
                case PB_ActiveTab.About:
                    if(!showSaveDrop)
                        DrawAboutTab();
                    break;
            }

            if (activeSave == null && activeTab != PB_ActiveTab.About && activeTab != PB_ActiveTab.Settings || showSaveDrop)
            {
                DrawDragAndDropSave();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawDragAndDropSave()
        {
            GUI.color = Color.green;
            saveDropRect = EditorGUILayout.BeginVertical("box", GUILayout.Height(position.height - 160));
            GUI.color = Color.white;
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Drag and Drop your brush file here to open it", styleBold);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.DragUpdated && saveDropRect.Contains(Event.current.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                Event.current.Use();
            }

            if (Event.current.type == EventType.DragPerform && saveDropRect.Contains(Event.current.mousePosition))
                AddSaveFromDragAndDrop(DragAndDrop.objectReferences);
        }

        #region PrefabPaintTab
        private void DrawPrefabDisplay()
        {
            EditorGUILayout.BeginHorizontal();
            labelContent = new GUIContent("Prefab Display", "Area for adding, removeing and selecting prefab to use in the brush.");
            GUILayout.Label(labelContent, styleBold);
            showPrefabDisplay = EditorGUILayout.Foldout(showPrefabDisplay, showPrefabDisplay ? "Hide" : "Show", styleFold);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GuiLine(2);

            if (showPrefabDisplay)
            {
                EditorGUILayout.BeginVertical("box");
                labelContent = new GUIContent("Prefab Display Type", "Change the interface for prefabs to use icons or lists.");
                prefabDisplayType = (PB_PrefabDisplayType)EditorGUILayout.EnumPopup(labelContent, prefabDisplayType);

                if (prefabDisplayType == PB_PrefabDisplayType.Icon)
                {
                    labelContent = new GUIContent("Icon Scale", "Change the scale of the prefab icons.");
                    prefabIconScaleFactor = EditorGUILayout.Slider(labelContent, prefabIconScaleFactor, .7f, 2);
                }

                EditorGUILayout.BeginHorizontal();

                switch (prefabDisplayType)
                {
                    case PB_PrefabDisplayType.Icon:
                        if (DragAndDrop.paths.Length > 0 || activeSave.prefabData.Count == 0)
                            DrawDragWindow("Drag And Drop Here To Add Prefabs To The List", GetPrefabIconSize() * 1.5f);
                        else
                            DrawPrefabIconWindow();
                        break;
                    case PB_PrefabDisplayType.List:
                        DrawPrefabList();
                        break;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawPrefabPaintTab()
        {
            if (isOn)
            {
                EditorGUILayout.Space();

                DrawPrefabDisplay();
                DrawPaintOptions();

                switch (activeSave.paintType)
                {
                    case PB_PaintType.Surface:
                        DrawSliders();
                        DrawFiltersSection();
                        DrawModifiers();
                        break;
                    case PB_PaintType.Physics:
                        DrawPhysicsPaintSettings();
                        DrawSliders();
                        DrawFiltersSection();
                        DrawModifiers();
                        break;
                    case PB_PaintType.Single:
                        DrawSingleModOptions();
                        break;
                }

                DrawEditHotKeyButton();
            }
            else
            {
                EditorGUILayout.HelpBox("Prefab Brush Is Off", MessageType.Warning);
            }   
        }
        
        #region PrefabDisplay
        private void DrawPrefabList()
        {
            EditorGUILayout.BeginVertical();
            for (int i = 0; i < activeSave.prefabData.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                DrawPrefabListItem(i);
                EditorGUILayout.EndHorizontal();
            }

            DrawDragWindow("Drag And Drop Here To Add Prefabs To The List", 50);

            EditorGUILayout.EndVertical();
        }

        private void DrawPrefabListItem(int i)
        {
            GUI.color = Color.white;
            activeSave.prefabData[i].selected = GUILayout.Toggle(activeSave.prefabData[i].selected, "");
            GUI.color = Color.white;
            activeSave.prefabData[i].prefab = EditorGUILayout.ObjectField(activeSave.prefabData[i].prefab, typeof(GameObject), false) as GameObject;
            GUI.color = Color.red;
            if (GUILayout.Button("X"))
                activeSave.prefabData.RemoveAt(i);
            GUI.color = Color.white;
        }

        private void DrawPrefabIconWindow()
        {
            int coloumnCount = Mathf.FloorToInt((position.width - GetPrefabIconSize()) / GetPrefabIconSize());
            int rowCount = Mathf.CeilToInt(activeSave.prefabData.Count / coloumnCount);

            float height = (rowCount >= 1) ? 2.3f : 1.3f;

            EditorGUILayout.BeginVertical(); //Begin the window with all the prefabs in it
            prefabViewScrollPos = EditorGUILayout.BeginScrollView(prefabViewScrollPos, GUILayout.Height(GetPrefabIconSize() * height)); //Start the scroll view 
            int id = 0; //This counts how many prefab icons have been built
            for (int y = 0; y <= rowCount; y++)
            {
                EditorGUILayout.BeginHorizontal();//Start a new row
                for (int x = 0; x < coloumnCount; x++)
                {
                    if (id >= activeSave.prefabData.Count) //If there are no more prefabs to add icons for then break
                        break;

                    if (activeSave.prefabData[id] != null)
                        DrawPrefabWindow(id);
                    else
                        activeSave.prefabData.RemoveAt(id);

                    id++;
                }
                GUILayout.FlexibleSpace();//Push all of the prefab icons to the left
                EditorGUILayout.EndHorizontal();//End the row
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawDragWindow(string message, float height)
        {
            GUI.color = Color.green;
            dropRect = EditorGUILayout.BeginVertical("box", GUILayout.Height(height));
            GUI.color = Color.white;
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(message, styleBold);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.DragUpdated && dropRect.Contains(Event.current.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                Event.current.Use();
            }

            if (Event.current.type == EventType.DragPerform && dropRect.Contains(Event.current.mousePosition))
                AddPrefab(DragAndDrop.objectReferences);
        }

        private void DrawPrefabWindow(int id)
        {
            if (id < activeSave.prefabData.Count) //Null check for when deleting prefabs.
            {
                GameObject prefab = activeSave.prefabData[id].prefab;
                EditorGUILayout.BeginVertical();

                if (!activeSave.prefabData[id].selected)
                    GUI.color = disabledColor;

                GUILayout.Box(AssetPreview.GetAssetPreview(prefab), GUILayout.Width(GetPrefabIconSize()), GUILayout.Height(GetPrefabIconSize()));

                GUI.color = Color.white;

                Rect prefabIconRect = GUILayoutUtility.GetLastRect();

                prefabIconRect.x = prefabIconRect.x + (prefabIconRect.width - deleteButtonSize);
                prefabIconRect.height = deleteButtonSize;
                prefabIconRect.width = deleteButtonSize;

                GUI.color = Color.red;

                if (GUI.Button(prefabIconRect, "X"))
                {
                    activeSave.prefabData.Remove(activeSave.prefabData[id]);
                    EditorGUILayout.EndVertical();
                    return;
                }

                GUI.color = Color.white;

                prefabIconRect.x = prefabIconRect.x - GetPrefabIconSize() + toggleButtonSize;
                prefabIconRect.height = toggleButtonSize;
                prefabIconRect.width = toggleButtonSize;

                activeSave.prefabData[id].selected = GUI.Toggle(prefabIconRect, activeSave.prefabData[id].selected, "");

                EditorGUILayout.EndVertical();
            }
        }
        #endregion        

        #region PaintOptions
        private void DrawPaintOptions()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            
            labelContent = new GUIContent("Paint Options", "Pick how the brush works, change the type of painting it does and how the brush detects surfaces.");
            GUILayout.Label(labelContent, styleBold);
            showPaintOptions = EditorGUILayout.Foldout(showPaintOptions, showPaintOptions ? "Hide" : "Show", styleFold);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GuiLine(2);

            if (showPaintOptions)
            {
                DrawPaintType();
                DrawIgnoreOptions();
            }
        }

        private void DrawPaintType()
        {
#if UNITY_5 || UNITY_4
        if (activeSave.paintType != PB_PaintType.Surface)
            activeSave.paintType = PB_PaintType.Surface;
#else
            EditorGUILayout.BeginVertical("box");
            labelContent = new GUIContent("Paint Type", "Select surface, single or physics brush to change how the brush places prefabs.");
            activeSave.paintType = (PB_PaintType)EditorGUILayout.EnumPopup(labelContent, activeSave.paintType);
            EditorGUILayout.EndVertical();
#endif
        }
        
        private void DrawIgnoreOptions()
        {
            EditorGUILayout.BeginVertical("box");
            showIgnoreOptions = EditorGUILayout.Foldout(showIgnoreOptions, "Ignore Options");
            if (showIgnoreOptions)
            {
                labelContent = new GUIContent("Ignore Trigger Colliders", "Don't detect trigger surfaces when painting.");
                activeSave.ignoreTriggers = EditorGUILayout.ToggleLeft(labelContent, activeSave.ignoreTriggers);

                if (activeTab != PB_ActiveTab.PrefabErase)
                {
                    labelContent = new GUIContent("Ignore Prefabs From Brush", "When enabled prefabs in the scene that match the selected Prefabs in the brush wont be treated as a surface.");
                    activeSave.ignorePaintedPrefabs = EditorGUILayout.ToggleLeft(labelContent, activeSave.ignorePaintedPrefabs);
                }
                    
                if (activeSave.ignorePaintedPrefabs)
                {
                    labelContent = new GUIContent("Include Inactive Prefabs In Checks.", "When enabled prefabs in the scene that match selected prefabs in the brush wont be treated as a surface.");
                    activeSave.includeInactivePrefabsInCheck = EditorGUILayout.ToggleLeft(labelContent, activeSave.includeInactivePrefabsInCheck);
                }
            }
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Sliders
        private void DrawSliders()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            labelContent = new GUIContent("Brush Sliders", "Sliders for changing brush size and strength.");
            GUILayout.Label(labelContent, styleBold);
            showBrushSlider = EditorGUILayout.Foldout(showBrushSlider, showBrushSlider ? "Hide" : "Show", styleFold);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GuiLine(2);

            if (showBrushSlider)
            {
                DrawBrushSizeSlider();
                DrawPrefabPerStrokeSlider();
            }
        }

        private void DrawBrushSizeSlider()
        {
            //Define radius of the brush.
            EditorGUILayout.BeginVertical("box");
            labelContent = new GUIContent("Brush Size", "The size of the circle draw to represent the brush.");
            GUILayout.Label(labelContent, styleBold);
            EditorGUILayout.BeginHorizontal();
            activeSave.brushSize = EditorGUILayout.Slider(activeSave.brushSize, activeSave.minBrushSize, activeSave.maxBrushSize);
            EditorGUILayout.EndHorizontal();
            labelContent = new GUIContent("Max Slider Size", "Change the max value for the brush size slider.");
            showMaxBrushSizeSlider = EditorGUILayout.Foldout(showMaxBrushSizeSlider, labelContent);
            if (showMaxBrushSizeSlider)
            {
                EditorGUILayout.BeginHorizontal();
                activeSave.maxBrushSize = EditorGUILayout.FloatField(activeSave.maxBrushSize);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }


            EditorGUILayout.EndVertical();
        }

        private void DrawPrefabPerStrokeSlider()
        {
            //Define the distance the brush needs to move before it paints again
            EditorGUILayout.BeginVertical("box");
            labelContent = new GUIContent("Paint Delta Distance", "The distance the brush moves before prefabs are painted again.");
            GUILayout.Label(labelContent, styleBold);
            activeSave.paintDeltaDistance = EditorGUILayout.Slider(activeSave.paintDeltaDistance, activeSave.minPaintDeltaDistance, activeSave.maxPaintDeltaDistance);

            labelContent = new GUIContent("Show Min/Max Paint Delta Distance", "Change the minimum and maximum slider values for the paint delta distance slider.");
            showMaxMinPaintDelta = EditorGUILayout.Foldout(showMaxMinPaintDelta, labelContent);

            if (showMaxMinPaintDelta)
            {
                activeSave.minPaintDeltaDistance = Mathf.Clamp(EditorGUILayout.FloatField("Min Paint Delta Distance", activeSave.minPaintDeltaDistance), 0, Mathf.Infinity);               
                activeSave.maxPaintDeltaDistance = Mathf.Clamp(EditorGUILayout.FloatField("Max Paint Delta Distance", activeSave.maxPaintDeltaDistance), activeSave.minPaintDeltaDistance + 1, Mathf.Infinity);   
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("box");
            labelContent = new GUIContent("Prefabs Per Stroke", "How many prefabs are painted when the brush completes a stroke.");
            GUILayout.Label(labelContent, style);
            activeSave.prefabsPerStroke = EditorGUILayout.IntSlider(activeSave.prefabsPerStroke, activeSave.minprefabsPerStroke, activeSave.maxprefabsPerStroke);

            labelContent = new GUIContent("Show Max/Min Prefabs Per Stroke", "The minimum maximum values for the slider prefabs per stroke.");
            showMaxMinPrefabsPerStroke = EditorGUILayout.Foldout(showMaxMinPrefabsPerStroke, labelContent);

            if (showMaxMinPrefabsPerStroke)
            {
                activeSave.minprefabsPerStroke = Mathf.Clamp(EditorGUILayout.IntField("Min Prefabs Per Stroke", activeSave.minprefabsPerStroke), 0, int.MaxValue);               
                activeSave.maxprefabsPerStroke = Mathf.Clamp(EditorGUILayout.IntField("Max Prefabs Per Stroke", activeSave.maxprefabsPerStroke), activeSave.minprefabsPerStroke + 1, int.MaxValue);  
            }

            EditorGUILayout.EndVertical();
        }
        #endregion

        #region Paint Filters
        private void DrawFiltersSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            labelContent = new GUIContent("Filters", "Filters are options that decide if a surface is paintable or not.");
            GUILayout.Label(labelContent, styleBold);
            showFilters = EditorGUILayout.Foldout(showFilters, showFilters ? "Hide" : "Show", styleFold);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GuiLine(2);
            EditorGUILayout.Space();

            if (showFilters)
            {
                labelContent = new GUIContent("Select Filters: ", "Select the desired filters from the drop down menu to enable them.");
                activeSave.filterFlags = EditorGUILayout.MaskField(labelContent, activeSave.filterFlags, filters);
                for (var i = 0; i < filters.Length; i++)
                {
                    var value = (activeSave.filterFlags & (1 << i)) != 0;

                    switch (filters[i])
                    {
                        case "Tag":
                            activeSave.checkTag = value;
                            break;
                        case "Layer":
                            activeSave.checkLayer = value;
                            break;
                        case "Slope":
                            activeSave.checkSlope = value;
                            break;
                        case "Terrain":
                            activeSave.checkTerrainLayer = value;
                            break;
                    }
                }

                if (activeSave.checkLayer)
                {
                    DrawLayerToBrush();
                    GuiLine(1);
                }

                if (activeSave.checkTag)
                {
                    DrawTagToBrush();
                    GuiLine(1);
                }

                if (activeSave.checkSlope)
                {
                    DrawSlopAngleToBrush();
                    GuiLine(1);
                }

                if (activeSave.checkTerrainLayer)
                {
                    DrawTerrainLayerList();
                    GuiLine(1);
                }
            }
        }

        private void DrawLayerToBrush()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            labelContent = new GUIContent("Filter By Layer", "Define Layers that a valid paintable surface must have. Your brush will not place prefabs on surfaces that do not match any of the selected Layers.");
            GUILayout.Label(labelContent, styleBold);
            GUILayout.FlexibleSpace();
            activeSave.requiredLayerMask = EditorGUILayout.MaskField(activeSave.requiredLayerMask, UnityEditorInternal.InternalEditorUtility.layers);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawTagToBrush()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            labelContent = new GUIContent("Filter By Tag", "Define Tags that a valid paintable surface must have. Your brush will not place prefabs on surfaces that do not match any of the selected Tags.");
            GUILayout.Label(labelContent, styleBold);
            GUILayout.FlexibleSpace();
            activeSave.requiredTagMask = EditorGUILayout.MaskField(activeSave.requiredTagMask, UnityEditorInternal.InternalEditorUtility.tags);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawSlopAngleToBrush()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("box");
            labelContent = new GUIContent("Filter By Slope Angle", "Define the minimum and maximum angle a surface must be in world space to be a valid painting surface.");
            GUILayout.Label(labelContent, styleBold);
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Minimum Angle = {Mathf.Round(activeSave.minRequiredSlope * 100f) / 100f}", style);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Maximum Angle = {Mathf.Round(activeSave.maxRequiredSlope * 100f) / 100f}", style);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.MinMaxSlider(ref activeSave.minRequiredSlope, ref activeSave.maxRequiredSlope, 0, 90);
            EditorGUILayout.EndVertical();
        }

        private void DrawTerrainLayerList()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("box");
            labelContent = new GUIContent("Filter By Terrain Layer", "Press Add to create a new field. Drag and drop terrain layers to define which terrain textures are a valid surface.");
            GUILayout.Label(labelContent, styleBold);

            labelContent = new GUIContent("Add", "Adds a new terrain layer field and strength slider to the filter.");
            if (GUILayout.Button(labelContent))
            {
                activeSave.terrainLayers.Add(new PB_TerrainLayerData());
            }

            for (int i = 0; i < activeSave.terrainLayers.Count; i++)
            {
                DrawTerrainLayerItem(activeSave.terrainLayers, i);
            }    
            EditorGUILayout.EndVertical();
        }

        private void DrawTerrainLayerItem(List<PB_TerrainLayerData> layerData, int id)
        {
            if (layerData[id] == null)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            layerData[id].terrainLayer = EditorGUILayout.ObjectField(layerData[id].terrainLayer, typeof(TerrainLayer), false) as TerrainLayer;
            if (GUILayout.Button("Remove"))
            {
                layerData.RemoveAt(id);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.EndHorizontal();
            labelContent = new GUIContent("Terrain Layer Stength", "The strength the painted terrain layer needs to meet in order to be a valid surface.");
            layerData[id].minLayerStrength = EditorGUILayout.Slider(labelContent, layerData[id].minLayerStrength, 0, 1);
            EditorGUILayout.EndVertical();
        }
        #endregion

        #region Paint Modifiers
        private void DrawModifiers()
        {
            //Start Modifiers header
            EditorGUILayout.BeginHorizontal();
            labelContent = new GUIContent("Modifiers", "Modifiers are settings that modify the prefab post paint.");
            GUILayout.Label(labelContent, styleBold);
            showMods = EditorGUILayout.Foldout(showMods, showMods ? "Hide" : "Show", styleFold);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GuiLine(2);
            //End Modifiers header

            EditorGUILayout.Space();

            if (showMods)
            {
                labelContent = new GUIContent("Select Modifiers", "Pick the desired modifiers from the drop down list to enable them.");
                activeSave.modFlags = EditorGUILayout.MaskField(labelContent, activeSave.modFlags, modifiers);
                for (var i = 0; i < modifiers.Length; i++)
                {
                    var value = (activeSave.modFlags & (1 << i)) != 0;

                    switch (modifiers[i])
                    {
                        case "Offset Center":
                            activeSave.applyOriginOffset = value;
                            break;
                        case "Offset Rotation":
                            activeSave.applyRotationOffset = value;
                            break;
                        case "Apply Parent":
                            activeSave.applyParent = value;
                            break;
                        case "Rotate To Match Surface":
                            activeSave.rotateToMatchSurface = value;
                            break;
                        case "Customize Rotation":
                            activeSave.randomizeRotation = value;
                            break;
                        case "Customize Scale":
                            activeSave.applyScale = value;
                            break;
                    }
                }

                if (activeSave.applyOriginOffset)
                {
                    DrawOffsetCenter();
                    GuiLine(1);
                }

                if (activeSave.applyRotationOffset)
                {
                    DrawOffsetRotation();
                    GuiLine(1);
                }

                if (activeSave.applyParent)
                {
                    DrawParentOptions();
                    GuiLine(1);
                }

                if (activeSave.rotateToMatchSurface)
                {
                    DrawMatchSurface();
                    GuiLine(1);
                }

                if (activeSave.randomizeRotation)
                {
                    DrawCustomRotation();
                    GuiLine(1);
                }

                if (activeSave.applyScale)
                {
                    DrawCustomScale();
                    GuiLine(1);
                }
            }
        }

        private void DrawOffsetCenter()
        {
            EditorGUILayout.BeginVertical("box");
            labelContent = new GUIContent("Offset Center Of Prefab", "Set the distance you wish to offset your painted prefabs by on each axis.");
            //GUILayout.Label(labelContent, styleBold);
            activeSave.prefabOriginOffset = EditorGUILayout.Vector3Field(labelContent, activeSave.prefabOriginOffset);
            EditorGUILayout.EndVertical();
        }

        private void DrawOffsetRotation()
        {
            EditorGUILayout.BeginVertical("box");
            labelContent = new GUIContent("Offset Rotation Of Prefab", "Set the angle you wish to offset your painted prefabs by on each axis.");
            activeSave.prefabRotationOffset = EditorGUILayout.Vector3Field(labelContent, activeSave.prefabRotationOffset);
            EditorGUILayout.EndVertical();
        }

        private void DrawParentOptions()
        {
            EditorGUILayout.BeginVertical("box");
            labelContent = new GUIContent("Parent Settings", "A range of settings that define the new parent of any prefab painted.");
            GUILayout.Label(labelContent, styleBold);
            activeSave.parentingStyle = (PB_ParentingStyle)EditorGUILayout.EnumPopup(activeSave.parentingStyle);

            switch (activeSave.parentingStyle)
            {
                case PB_ParentingStyle.Surface:
                    EditorGUILayout.HelpBox("Prefabs painted will now parent them selves to the surface they are painted on.", MessageType.Info);
                    break;
                case PB_ParentingStyle.SingleTempParent:
                    labelContent = new GUIContent("Parent GameObject", "Prefabs painted will be parented to the defined parent object.");
                    activeSave.parent = EditorGUILayout.ObjectField(labelContent, activeSave.parent, typeof(GameObject), true) as GameObject;
                    EditorGUILayout.HelpBox("The parent value will not be saved across scenes.", MessageType.Warning);
                    break;
                case PB_ParentingStyle.ClosestTempFromList:

                    EditorGUILayout.HelpBox("Prefabs painted will be parented to the closest object object in the list.", MessageType.Info);
                    EditorGUILayout.HelpBox("The parent value will not be saved across scenes.", MessageType.Warning);
                    DrawParentList();
                    break;
                case PB_ParentingStyle.TempRoundRobin:
                    EditorGUILayout.HelpBox("Prefabs painted will be parented to one of the objects in the list. The parent used will cycle through the list evenly distributing the prefabs among them.", MessageType.Info);
                    EditorGUILayout.HelpBox("The parent value will not be saved across scenes.", MessageType.Warning);
                    DrawParentList();
                    break;
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawMatchSurface()
        {
            EditorGUILayout.BeginVertical("box");
            labelContent = new GUIContent("Rotate GameObjects To Match Surface", "Rotates painted prefabs soi that their Y axis points up in relation to the surface paited on.");
            GUILayout.Label(labelContent, styleBold);
            labelContent = new GUIContent("Set Up Axis", "This setting can be used to change which direction is up for all Prefabs being painted. This can help resolve issues where Y is not the up axis for the Prefab.");
            activeSave.rotateSurfaceDirection = (PB_Direction)EditorGUILayout.EnumPopup(labelContent, activeSave.rotateSurfaceDirection);
            EditorGUILayout.EndVertical();
        }

        private void DrawCustomRotation()
        {
            EditorGUILayout.BeginVertical("box");
            labelContent = new GUIContent("Customize Rotation", "Change the rotation of the Prefab after it has been painted.");
            GUILayout.Label(labelContent, styleBold);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            GUILayout.Label("Axis");
            GUILayout.Label("X");
            GUILayout.Label("Y");
            GUILayout.Label("Z");
            EditorGUILayout.EndVertical();

            //Min
            EditorGUILayout.BeginVertical();
            labelContent = new GUIContent("Minimum Rotation", "The minimum value we want to rotate the prefab by on each axis.");
            GUILayout.Label(labelContent);
            activeSave.minXRotation = Mathf.Clamp(EditorGUILayout.FloatField(activeSave.minXRotation), -360, 360);
            activeSave.minYRotation = Mathf.Clamp(EditorGUILayout.FloatField(activeSave.minYRotation), -360, 360);
            activeSave.minZRotation = Mathf.Clamp(EditorGUILayout.FloatField(activeSave.minZRotation), -360, 360);
            EditorGUILayout.EndVertical();

            //Min
            EditorGUILayout.BeginVertical();
            labelContent = new GUIContent("Maximum Rotation", "The maximum value we want to rotate the prefab by on each axis.");
            GUILayout.Label(labelContent);
            activeSave.maxXRotation = Mathf.Clamp(EditorGUILayout.FloatField(activeSave.maxXRotation), -360, 360);
            activeSave.maxYRotation = Mathf.Clamp(EditorGUILayout.FloatField(activeSave.maxYRotation), -360, 360);
            activeSave.maxZRotation = Mathf.Clamp(EditorGUILayout.FloatField(activeSave.maxZRotation), -360, 360);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal("box");
            labelContent = new GUIContent($"Set all to {rotationSet}", $"Set all axis angles to be {rotationSet} you can change the number useing the field below.");
            if (GUILayout.Button(labelContent))
            {
                activeSave.minXRotation = rotationSet;
                activeSave.minYRotation = rotationSet;
                activeSave.minZRotation = rotationSet;
                activeSave.maxXRotation = rotationSet;
                activeSave.maxYRotation = rotationSet;
                activeSave.maxZRotation = rotationSet;
            }

            labelContent = new GUIContent("Set all to value", "The value that will be used when using the Set all to button.");
            rotationSet = EditorGUILayout.FloatField(labelContent, rotationSet);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawCustomScale()
        {
            EditorGUILayout.BeginVertical("box");
            labelContent = new GUIContent("Customize Scale", "Apply a new scale to the prefab after being painted.");
            GUILayout.Label(labelContent, styleBold);
            labelContent = new GUIContent("Scale Type", "Change the type of scaling between Set and Multiply.");
            activeSave.scaleType = (PB_ScaleType)EditorGUILayout.EnumPopup(labelContent, activeSave.scaleType);
            labelContent = new GUIContent("Scale Application", "How scale is applied, either by setting it to a specific value or multiplying exisiting values by a factor.");
            activeSave.scaleApplicationType = (PB_SaveApplicationType)EditorGUILayout.EnumPopup(labelContent, activeSave.scaleApplicationType);

            switch (activeSave.scaleType)
            {
                case PB_ScaleType.SingleValue:
                    DrawSingleValueScale();
                    break;
                case PB_ScaleType.MultiAxis:
                    DrawMultiAxisScale();
                    break;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSingleValueScale()
        {
            labelContent = new GUIContent("Minimum Scale", "Set the minimum scale factor for all 3 axis. This will apply a scale change using a random value between minimum and maximum.");
            activeSave.minScale = EditorGUILayout.FloatField(labelContent, activeSave.minScale);  
            labelContent = new GUIContent("Maximum Scale", "Set the maximum scale factor for all 3 axis. This will apply a scale change using a random value between minimum and maximum.");
            activeSave.maxScale = Mathf.Clamp(EditorGUILayout.FloatField(labelContent, activeSave.maxScale), activeSave.minScale, Mathf.Infinity);  
        }

        private void DrawMultiAxisScale()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            GUILayout.Label("Axis");
            GUILayout.Label("X");
            GUILayout.Label("Y");
            GUILayout.Label("Z");
            EditorGUILayout.EndVertical();

            //Min
            EditorGUILayout.BeginVertical();
            labelContent = new GUIContent("Minimum Scale", "Set the minimum scale factor for each individual axis.");
            GUILayout.Label(labelContent);
            activeSave.minXScale  = EditorGUILayout.FloatField(activeSave.minXScale);  
            activeSave.minYScale  = EditorGUILayout.FloatField(activeSave.minYScale);  
            activeSave.minZScale  = EditorGUILayout.FloatField(activeSave.minZScale);  
            EditorGUILayout.EndVertical();

            //Min
            EditorGUILayout.BeginVertical();
            labelContent = new GUIContent("Maximum Scale", "Set the maximum scale factor for each individual axis.");
            GUILayout.Label(labelContent);
            activeSave.maxXScale  = Mathf.Clamp(EditorGUILayout.FloatField(activeSave.maxXScale), activeSave.minXScale, Mathf.Infinity);  
            activeSave.maxYScale  = Mathf.Clamp(EditorGUILayout.FloatField(activeSave.maxYScale), activeSave.minYScale, Mathf.Infinity);
            activeSave.maxZScale  = Mathf.Clamp(EditorGUILayout.FloatField(activeSave.maxZScale), activeSave.minZScale, Mathf.Infinity);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            labelContent = new GUIContent($"Set all to {scaleSet}", $"Set all axis angles to be {scaleSet} you can change the number useing the field below.");
            if (GUILayout.Button(labelContent))
            {
                activeSave.minXScale = scaleSet;
                activeSave.minYScale = scaleSet;
                activeSave.minZScale = scaleSet;
                activeSave.maxXScale = scaleSet;
                activeSave.maxYScale = scaleSet;
                activeSave.maxZScale = scaleSet;
            }

            labelContent = new GUIContent("Set all to value", "The value that will be used when using the Set all to button.");
            scaleSet = EditorGUILayout.FloatField(labelContent, scaleSet);

            EditorGUILayout.EndHorizontal();
            DrawScaleError();
        }

        private void DrawScaleError()
        {
            if (activeSave.minXScale == 0)
                EditorGUILayout.HelpBox("Minimum Scale X is equal to 0 this can cause issues with the prefab", MessageType.Error);

            if (activeSave.minYScale == 0)
                EditorGUILayout.HelpBox("Minimum Scale Y is equal to 0 this can cause issues with the prefab", MessageType.Error);

            if (activeSave.minZScale == 0)
                EditorGUILayout.HelpBox("Minimum Scale Z is equal to 0 this can cause issues with the prefab", MessageType.Error);

            if (activeSave.maxXScale == 0)
                EditorGUILayout.HelpBox("Maximum Scale X is equal to 0 this can cause issues with the prefab", MessageType.Error);

            if (activeSave.maxYScale == 0)
                EditorGUILayout.HelpBox("Maximum Scale Y is equal to 0 this can cause issues with the prefab", MessageType.Error);

            if (activeSave.maxZScale == 0)
                EditorGUILayout.HelpBox("Maximum Scale Z is equal to 0 this can cause issues with the prefab", MessageType.Error);
        }

        private void DrawParentList()
        {
            EditorGUILayout.BeginVertical();
            for (int i = 0; i < activeSave.parentList.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                DrawParentListItem(i);
                EditorGUILayout.EndHorizontal();
            }

            GUI.color = Color.green;
            parentRect = EditorGUILayout.BeginVertical("box", GUILayout.Height(1));
            GUI.color = Color.white;
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            labelContent = new GUIContent("Drag And Drop Here To Add Parents To The List", "Select the Prefabs you wish to add to the brush in the Project view and drag them into the green space.");
            GUILayout.Label(labelContent, styleBold);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.DragUpdated && parentRect.Contains(Event.current.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                Event.current.Use();
            }

            if (Event.current.type == EventType.DragPerform && parentRect.Contains(Event.current.mousePosition))
                AddParent(DragAndDrop.objectReferences);

            EditorGUILayout.EndVertical();
        }

        private void DrawParentListItem(int i)
        {
            labelContent = new GUIContent("Parent GameObject", "Prefabs painted will be parented to the defined parent object.");
            activeSave.parentList[i] = EditorGUILayout.ObjectField(labelContent, activeSave.parentList[i], typeof(GameObject), true) as GameObject;

            GUI.color = Color.red;

            if (GUILayout.Button("X"))
                activeSave.parentList.RemoveAt(i);

            GUI.color = Color.white;
        }

        #endregion

        private void DrawPhysicsPaintSettings()
        {
            if (!showPaintOptions)
                return;

            //Define radius of the brush.
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Physics Brush", styleBold);
            labelContent = new GUIContent("Paint Height", "The height offset for the prefabs to be painted in order to simulate a drop.");
            activeSave.spawnHeight = EditorGUILayout.FloatField(labelContent, activeSave.spawnHeight);
            activeSave.addRigidbodyToPaintedPrefab = EditorGUILayout.ToggleLeft("Add Rigidbody To Prefab", activeSave.addRigidbodyToPaintedPrefab);
            labelContent = new GUIContent("Physics Iterations", "How many iterations of simulation will the brush perform on each prefab after being painted.");
            activeSave.physicsIterations = EditorGUILayout.FloatField(labelContent, activeSave.physicsIterations);
            EditorGUILayout.EndVertical();
        }

        private void DrawSingleModOptions()
        {
            if (Tools.current != Tool.Move && Tools.current != Tool.Rotate && Tools.current != Tool.Scale)
            {
                EditorGUILayout.BeginVertical("box");
                labelContent = new GUIContent("Drag Modification Type", "While still holding the mouse post paint what should moving the mouse do to modify the state of the prefab.");
                activeSave.draggingAction = (PB_DragModType)EditorGUILayout.EnumPopup(labelContent, activeSave.draggingAction);
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.BeginVertical("box");
                GUILayout.Label(Tools.current.ToString(), styleBold);
                EditorGUILayout.HelpBox("'Drag Modification Type' can be changed by switching between the transform tools in the scene view.", MessageType.Info);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.BeginVertical("box");
            labelContent = new GUIContent("Rotation Settings", "Settings for how rotation should work when using the rotation transform tool.");
            GUILayout.Label(labelContent, styleBold);
            labelContent = new GUIContent("Rotation Axis", "The axis we should rotate the prefab in world space.");
            activeSave.rotationAxis = (PB_Direction)EditorGUILayout.EnumPopup(labelContent, activeSave.rotationAxis);
            labelContent = new GUIContent("Rotation Sensitivity", "How sensitive should the mouse be to rotation.");
            activeSave.rotationSensitivity = EditorGUILayout.FloatField(labelContent, activeSave.rotationSensitivity);
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region EraseTab
        private void DrawEraseTab()
        {
            if (isOn)
            {
                EditorGUILayout.Space();
                DrawPrefabDisplay();
                EditorGUILayout.Space();

                //Erase settings header start
                EditorGUILayout.BeginHorizontal();
                labelContent = new GUIContent("Erase Settings", "Change how the erase brush detects valid prefabs and what surfaces it should erase on.");
                GUILayout.Label(labelContent, styleBold);
                showEraseSettings = EditorGUILayout.Foldout(showEraseSettings, showEraseSettings ? "Hide" : "Show", styleFold);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GuiLine(2);
                //Erase settings header end

                if (showEraseSettings)
                {
                    EditorGUILayout.BeginVertical();
                    DrawEraseBrushSizeSlider();
                    DrawEraseDetectionType();
                    DrawEraseIgnoreOptions();
                    DrawEraseType();
                    EditorGUILayout.EndVertical();
                }

                //Erase filters header start
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                labelContent = new GUIContent("Erase Filters", "Filters for detecting valid erase surfaces or erasable prefabs.");
                GUILayout.Label(labelContent, styleBold);
                showEraseFilters = EditorGUILayout.Foldout(showEraseFilters, showEraseFilters ? "Hide" : "Show", styleFold);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GuiLine(2);
                //Erase filters header end

                if (showEraseFilters)
                {
                    EditorGUILayout.BeginVertical();
                    DrawEraseFilters();
                    EditorGUILayout.EndVertical();
                }

                DrawEditHotKeyButton();
            }
            else
            {
                EditorGUILayout.HelpBox("Prefab Brush Is Off", MessageType.Warning);
            }
        }

        private void DrawEraseDetectionType()
        {
            EditorGUILayout.BeginVertical("box");
            labelContent = new GUIContent("Erase Detection Type", "Erase detection can be done 2 ways, with collision or with checking distance from brush.");
            GUILayout.Label(labelContent, styleBold);
            activeSave.eraseDetection = (PB_EraseDetectionType)EditorGUILayout.EnumPopup(activeSave.eraseDetection);

            switch (activeSave.eraseDetection)
            {
                case PB_EraseDetectionType.Collision:
                    EditorGUILayout.HelpBox("Collision is the fastest detection type. This will not work for objects with no collider component on them.", MessageType.Warning);
                    break;
                case PB_EraseDetectionType.Distance:
                    EditorGUILayout.HelpBox("Distance will work with objects that don't use colliders. Although this is very slow. Try not to use with complex scenes! Disabling as many objects as possible will help.", MessageType.Warning);
                    break;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawEraseIgnoreOptions()
        {
            EditorGUILayout.BeginVertical("box");
            labelContent = new GUIContent("Ignore Options", "Options to ignore surfaces when erasing.");
            showIgnoreOptions = EditorGUILayout.Foldout(showIgnoreOptions, labelContent);
            if (showIgnoreOptions)
            {
                labelContent = new GUIContent("Ignore Trigger Colliders", "Don't detect trigger surfaces when erasing.");
                activeSave.ignoreTriggersErase = EditorGUILayout.ToggleLeft(labelContent, activeSave.ignoreTriggersErase);
                labelContent = new GUIContent("Ignore Prefabs from Brush", "When enabled prefabs in the scene that match the selected Prefabs in the brush wont be treated as a surface.");
                activeSave.ignorePaintedPrefabsErase = EditorGUILayout.ToggleLeft(labelContent, activeSave.ignorePaintedPrefabsErase);

                if (activeSave.ignorePaintedPrefabsErase)
                {
                    labelContent = new GUIContent("Include inactive prefabs in checks", "When enabled prefabs in the scene that match selected prefabs in the brush wont be treated as a surface.");
                    activeSave.includeInactivePrefabsInCheckErase = EditorGUILayout.ToggleLeft(labelContent, activeSave.includeInactivePrefabsInCheckErase);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawEraseType()
        {
            EditorGUILayout.BeginVertical("box");
            labelContent = new GUIContent("Erase Target", "Define how prefabs are detected and removed. In brush means the prefab must match on defined in the brush. In bounds means that any prefab thats bounding box fits in the erase area will be erased.");
            activeSave.eraseType = (PB_EraseTypes)EditorGUILayout.EnumPopup(labelContent, activeSave.eraseType);
            
            if(activeSave.eraseType == PB_EraseTypes.PrefabsInBrush)
            {
                labelContent = new GUIContent("Only erase active prefabs", "If selected prefabs must be selected in the brush in order to count as erasable.");
                activeSave.mustBeSelectedInBrush = EditorGUILayout.ToggleLeft(labelContent, activeSave.mustBeSelectedInBrush);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawEraseBrushSizeSlider()
        {
            //Define radius of the eraser.
            EditorGUILayout.BeginVertical("box");
            labelContent = new GUIContent("Eraser Sliders", "The sliders related to the erase tool.");
            GUILayout.Label(labelContent, styleBold);
            labelContent = new GUIContent("Eraser Size", "The size of the erase area in the scene.");
            activeSave.eraseBrushSize = EditorGUILayout.Slider(labelContent, activeSave.eraseBrushSize, activeSave.minEraseBrushSize, activeSave.maxEraseBrushSize);
            labelContent = new GUIContent("Maximum Slider Size", "The maximum size the erase brush can go to on the slider.");
            activeSave.maxEraseBrushSize = EditorGUILayout.FloatField(labelContent, activeSave.maxEraseBrushSize);
            EditorGUILayout.EndVertical();
        }

        private void DrawEraseFilters()
        {
            EditorGUILayout.Space();
            labelContent = new GUIContent("Select Filters", "Select the desired filters for the erase brush from the drop down menu to enable them.");
            activeSave.eraseFilterFlags = EditorGUILayout.MaskField(labelContent, activeSave.eraseFilterFlags, filters);
            for (var i = 0; i < filters.Length; i++)
            {
                var value = (activeSave.eraseFilterFlags & (1 << i)) != 0;

                switch (filters[i])
                {
                    case "Tag":
                        activeSave.checkTagForErase = value;
                        break;
                    case "Layer":
                        activeSave.checkLayerForErase = value;
                        break;
                    case "Slope":
                        activeSave.checkSlopeForErase = value;
                        break;
                    case "Terrain":
                        activeSave.checkTerrainLayerForErase = value;
                        break;
                }
            }

            if (activeSave.checkTagForErase)
            {
                DrawTagToErase();
                GuiLine(1);
            }

            if (activeSave.checkLayerForErase)
            {
                DrawLayerToErase();
                GuiLine(1);
            }

            if (activeSave.checkSlopeForErase)
            {
                DrawSlopAngleToErase();
                GuiLine(1);
            }

            if (activeSave.checkTerrainLayerForErase)
            {
                DrawTerrainLayerListToErase();
                GuiLine(1);
            }
        }
        
  
        #region EraseFilters
        private void DrawTagToErase()
        {
            EditorGUILayout.Space();
            //Define the object tag that is reqired for erasing 
            EditorGUILayout.BeginVertical("box");
            labelContent = new GUIContent("Filter By Tag", "The tag that either the prefab or the surface needs to match to make the erase valid.");
            activeSave.requiredTagMaskForErase = EditorGUILayout.MaskField(labelContent, activeSave.requiredTagMaskForErase, UnityEditorInternal.InternalEditorUtility.tags);
            labelContent = new GUIContent("Erase Tag Check Type", "If you want to check the surface or the prefab for a matching tag.");
            activeSave.eraseTagCheckType = (PB_FilterCheckType)EditorGUILayout.EnumPopup(labelContent, activeSave.eraseTagCheckType);
            EditorGUILayout.EndVertical();
        }

        private void DrawLayerToErase()
        {
            EditorGUILayout.Space();
            //Define the layer for objects that need to be erased
            EditorGUILayout.BeginVertical("box");
            labelContent = new GUIContent("Filter By Layer", "The layer that either the prefab or the surface needs to match to make the erase valid.");
            activeSave.requiredLayerMaskForErase = EditorGUILayout.MaskField(labelContent, activeSave.requiredLayerMaskForErase, UnityEditorInternal.InternalEditorUtility.layers);
            labelContent = new GUIContent("Erase Layer Check Type", "If you want to check the surface or the prefab for a matching layer.");
            activeSave.eraseLayerCheckType = (PB_FilterCheckType)EditorGUILayout.EnumPopup(labelContent, activeSave.eraseLayerCheckType);
            EditorGUILayout.EndVertical();
        }
        
        private void DrawSlopAngleToErase()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("box");
            labelContent = new GUIContent("Slope Angle To Erase", "Define a range of angles to check the surface for when erasing.");
            GUILayout.Label(labelContent, style);

            if (activeSave.checkSlopeForErase)
            {
                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                labelContent = new GUIContent($"Minimum Angle = {Mathf.Round(activeSave.minRequiredSlopeForErase * 100f) / 100f}", "The minimum angle that the surface needs to match to make the erase valid.");
                GUILayout.Label(labelContent, style);
                GUILayout.FlexibleSpace();
                labelContent = new GUIContent($"Maximum Angle = {Mathf.Round(activeSave.maxRequiredSlopeForErase * 100f) / 100f}", "The maximum angle that the surface needs to match to make the erase valid.");
                GUILayout.Label(labelContent, style);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.MinMaxSlider(ref activeSave.minRequiredSlopeForErase, ref activeSave.maxRequiredSlopeForErase, 0, 90);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawTerrainLayerListToErase()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("box");

            labelContent = new GUIContent("Terrain Layer To Brush", "Press Add to create a new field. Drag and drop terrain layers to define which terrain textures are a valid surface.");
            GUILayout.Label(labelContent, styleBold);

            EditorGUILayout.BeginVertical();
            labelContent = new GUIContent("Add", "Adds a new terrain layer field and strength slider to the filter.");
            if (GUILayout.Button(labelContent))
            {
                activeSave.terrainLayersErase.Add(new PB_TerrainLayerData());
            }

            for (int i = 0; i < activeSave.terrainLayersErase.Count; i++)
            {
                DrawTerrainLayerItem(activeSave.terrainLayersErase, i);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
        }

        #endregion
      
        #endregion

        #region SettingsTab
        private void DrawSettingsTab()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            labelContent = new GUIContent("Hot Keys", "Edit the hotkeys for Prefab Brush+.");
            GUILayout.Label(labelContent, styleBold);
            showHotKeySettings = EditorGUILayout.Foldout(showHotKeySettings, showHotKeySettings ? "Hide" : "Show", styleFold);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GuiLine(2);

            if (showHotKeySettings)
            {
                EditorGUILayout.BeginVertical();
                DrawHotKeys();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.BeginHorizontal();
            labelContent = new GUIContent("Brush Folders", "Add additional folders for the tool to search when looking for brush files.");
            GUILayout.Label(labelContent, styleBold);
            EditorGUILayout.EndHorizontal();
            GuiLine(2);

            for (int i = 0; i < saveSearchPaths.Count; i++)
            {
                EditorGUILayout.BeginHorizontal("box");
                labelContent = new GUIContent(saveSearchPaths[i], "The path which is checked when looking for brush files.");
                GUILayout.Label(labelContent);
                if(i != 0)
                {
                    EditorGUI.BeginChangeCheck();

                    labelContent = new GUIContent("Select Folder", "Opens a window to select a folder in the project to look for brush files at.");
                    if (GUILayout.Button(labelContent))
                    {
                        saveSearchPaths[i] = EditorUtility.OpenFolderPanel("Select folder to search for saves.", Application.dataPath, "").Replace(Application.dataPath, "Assets"); 
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        StoreSavePaths();
                        CheckForNewSaves();
                    }

                    if (saveSearchPaths.Count > 1)
                    {
                        labelContent = new GUIContent("X", "Delete this folder from the list.");
                        if (GUILayout.Button(labelContent))
                        {
                            saveSearchPaths.RemoveAt(i);
                            StoreSavePaths();
                            CheckForNewSaves();
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            labelContent = new GUIContent("Add New Brush Folder", "Add another entry for finding brush files");
            if (GUILayout.Button(labelContent))
            {
                saveSearchPaths.Add(GetSaveDirectory());
                StoreSavePaths();
            }

            EditorGUILayout.BeginHorizontal();
            labelContent = new GUIContent("Other settings", "Settings that do not sit with other settings.");
            GUILayout.Label(labelContent, styleBold);
            EditorGUILayout.EndHorizontal();
            GuiLine(2);

            labelContent = new GUIContent("Show tools warning", "Turn on or off the warning you get when the transform Tools have been disabled or enabled during painting.");
            showToolsWarning = EditorGUILayout.ToggleLeft(labelContent, showToolsWarning);
        }
        #endregion

        #region AboutTab
        private void DrawAboutTab()
        {
            EditorGUILayout.Space();
            GUILayout.Label("About", styleBold);
            GuiLine(2);

            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Created by Archie Andrews", EditorStyles.wordWrappedLabel);
            GUILayout.Label("Released: Sep 8, 2015", EditorStyles.wordWrappedLabel);
            GUILayout.Label($"Version: {version}", EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            GUILayout.Label("Contact / Support", styleBold);
            GuiLine(2);

            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("If you have any issues or inquiries please get in contact via email.", EditorStyles.wordWrappedLabel);
            GUILayout.Label("When asking for support please include any screenshots or videos that might help me troubleshoot your issue.", EditorStyles.wordWrappedLabel);
            GUILayout.Label("Sending a screenshot of your Prefab Brush+ settings will greatly improve the chance and speed of a solution.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField("contact@archieandrews.games");
            if(GUILayout.Button("Copy email"))
            {
                EditorGUIUtility.systemCopyBuffer = "contact@archieandrews.games";
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Email Now"))
            {
                Help.BrowseURL("mailto:contact@archieandrews.games");
            }

            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("I am also always interested to see what people make using Prefab Brush+, so please feel free to email me with anything you have made using this tool :)", EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            GUILayout.Label("About Archie", styleBold);
            GuiLine(2);

            EditorGUILayout.BeginVertical("box");
            GUILayout.Label($"Hello! I hope you are enjoying Prefab Brush+ Version {version}. I built this tool roughly {System.DateTime.Now.Year - 2015} years ago and have been suprised by how many people have used it over the years.", EditorStyles.wordWrappedLabel);
            GUILayout.Label($"To new and returning customers thank you for purchasing Prefab Brush+.", EditorStyles.wordWrappedLabel);
            GUILayout.Label($"I don't really use social media these days so under this you can find links to my website, Itch.io and Github.", EditorStyles.wordWrappedLabel);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Website", styleBold);
            EditorGUILayout.TextField("https://archieandrews.games");
            if(GUILayout.Button("Copy url"))
            {
                EditorGUIUtility.systemCopyBuffer = "https://archieandrews.games";
            }

            if (GUILayout.Button("Open"))
            {
                Help.BrowseURL("https://archieandrews.games");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Itch.io", styleBold);
            EditorGUILayout.TextField("https://archieandrewsdev.itch.io");
            if(GUILayout.Button("Copy url"))
            {
                EditorGUIUtility.systemCopyBuffer = "https://archieandrewsdev.itch.io";
            }

            if (GUILayout.Button("Open"))
            {
                Help.BrowseURL("https://archieandrewsdev.itch.io");
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("GitHub", styleBold);
            EditorGUILayout.TextField("https://github.com/ArchieAndrewsDev");
            if(GUILayout.Button("Copy url"))
            {
                EditorGUIUtility.systemCopyBuffer = "https://github.com/ArchieAndrewsDev";
            }

            if (GUILayout.Button("Open"))
            {
                Help.BrowseURL("https://github.com/ArchieAndrewsDev");
            }  
            EditorGUILayout.EndHorizontal();          
            EditorGUILayout.EndVertical();
            
        }
        #endregion

        #region General
        private void DrawEditHotKeyButton()
        {
            EditorGUILayout.Space();
            labelContent = new GUIContent("Edit Hotkeys", "Short cut to take you to the hotkeys section of the settings.");
            if (GUILayout.Button(labelContent))
            {
                activeTab = PB_ActiveTab.Settings;
                showHotKeySettings = true;
            }
        }

        private void DrawOnOffButton()
        {
            labelContent = new GUIContent(buttonIcon, "Turn Prefab Brush+ on and off.");
            if (GUILayout.Button(labelContent, GUI.skin.label))
                ToggleOnState();

            Repaint();
        }

        private void DrawHotKeys()
        {
            if(activeSave == null)
            {
                GUILayout.Label("No save select for hotkeys...");
                return;
            }

            EditorGUILayout.BeginVertical("box");
            labelContent = new GUIContent("Paint HotKey", "The hotkey to swap to the paint brush tool.");
            activeSave.paintBrushHotKey = (KeyCode)EditorGUILayout.EnumPopup(labelContent, activeSave.paintBrushHotKey);
            labelContent = new GUIContent("Hold Key", "Enable this to change the hotkey's behaviour from toggle to hold.");
            activeSave.paintBrushHoldKey = EditorGUILayout.ToggleLeft(labelContent, activeSave.paintBrushHoldKey);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("box");
            labelContent = new GUIContent("Erase HotKey", "The hotkey to swap to the erase brush tool.");
            activeSave.removeBrushHotKey = (KeyCode)EditorGUILayout.EnumPopup(labelContent, activeSave.removeBrushHotKey);
            labelContent = new GUIContent("Hold Key", "Enable this to change the hotkey's behaviour from toggle to hold.");
            activeSave.removeBrushHoldKey = EditorGUILayout.ToggleLeft(labelContent, activeSave.removeBrushHoldKey);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("box");
            labelContent = new GUIContent("Toggle Prefab Brush HotKey", "The hotkey to turn Prefab Brush+ on and off.");
            activeSave.disableBrushHotKey = (KeyCode)EditorGUILayout.EnumPopup(labelContent, activeSave.disableBrushHotKey);
            labelContent = new GUIContent("Hold Key", "Enable this to change the hotkey's behaviour from toggle to hold.");
            activeSave.disableBrushHoldKey = EditorGUILayout.ToggleLeft(labelContent, activeSave.disableBrushHoldKey);
            EditorGUILayout.EndVertical();
        }
        private void DrawPaintCircle(Color circleColour, float radius)
        {
            Ray drawPointRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit drawPointHit;

            bool ignorePaintedPrefabs = activeTab == PB_ActiveTab.PrefabPaint ? activeSave.ignorePaintedPrefabs : activeSave.ignorePaintedPrefabsErase;
            bool ignoreTrigger = activeTab == PB_ActiveTab.PrefabPaint ? activeSave.ignoreTriggers : activeSave.ignoreTriggersErase;
            bool emmitPrefabs = activeTab == PB_ActiveTab.PrefabPaint ? activeSave.includeInactivePrefabsInCheck : activeSave.includeInactivePrefabsInCheckErase;

            if (GetHitPoint(drawPointRay.origin, drawPointRay.direction, ignoreTrigger, ignorePaintedPrefabs, emmitPrefabs, out drawPointHit))
            {
                if (activeSave.paintType == PB_PaintType.Surface || activeSave.paintType == PB_PaintType.Physics && activeTab == PB_ActiveTab.PrefabErase)
                {
                        Handles.color = circleColour;
                        Handles.DrawSolidDisc(drawPointHit.point, drawPointHit.normal, radius * .5f);
                }
                
                if(activeSave.paintType == PB_PaintType.Physics && activeTab != PB_ActiveTab.PrefabErase)
                {
                    Handles.color = circleColour;
                    Handles.DrawSolidDisc(drawPointHit.point + (Vector3.up * activeSave.spawnHeight), Vector3.up, radius * .5f);
                    Handles.color = Color.grey;
                    Handles.DrawWireDisc(drawPointHit.point, Vector3.up, radius * .5f);
                    Handles.DrawLine(drawPointHit.point + (Vector3.up * activeSave.spawnHeight), drawPointHit.point);
                }

                SceneView.RepaintAll();
            }
        }

        void GuiLine(int i_height = 1)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, i_height);
            rect.height = i_height;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }
        #endregion

        #endregion

        #region Logic

        public void GetAllSaves()
        {
            CheckSavePathIntegrity();
            string[] guids;
            guids = AssetDatabase.FindAssets($"t:{nameof(PB_SaveObject)}", saveSearchPaths.ToArray());
            saveObjectNames = new string[guids.Length];

            bool resetLoadedSave = DoesSelectedSaveMatch();

            saveObjects.Clear();

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                PB_SaveObject save = AssetDatabase.LoadAssetAtPath<PB_SaveObject>(path);
                saveObjects.Add(save);
                saveObjectNames[i] = save.name;

                //Auto Upgrade
                if (saveObjects[i].prefabList.Count > 0)
                    saveObjects[i].UpgradeSave();
            }

            if (!resetLoadedSave)
            {
                if(saveObjects.Contains(activeSave))
                {
                    SelectSave(activeSave);
                }
                else
                {
                    ClearActiveSave();
                }     
            }
        }
        
        public void CheckForNewSaves()
        {
            string[] guids;
            guids = AssetDatabase.FindAssets($"t:{nameof(PB_SaveObject)}");

            if (saveObjects.Count != guids.Length)
                GetAllSaves();
        }

        public void AddSaveFromDragAndDrop(Object[] objectsToAdd)
        {
            for (int i = 0; i < objectsToAdd.Length; i++)
            {
                if (objectsToAdd[i].GetType() == typeof(PB_SaveObject))
                {
                    SelectSave(objectsToAdd[i] as PB_SaveObject);
                    break;
                }
            }
        }
                
        private void AddPrefab(Object[] objectsToAdd)
        {
            for (int i = 0; i < objectsToAdd.Length; i++)
            {
                if (objectsToAdd[i].GetType() == typeof(GameObject))
                    activeSave.AddPrefab(objectsToAdd[i] as GameObject);
            }
        }

        private void AddParent(Object[] objectsToAdd)
        {
            for (int i = 0; i < objectsToAdd.Length; i++)
            {
                if (objectsToAdd[i].GetType() == typeof(GameObject))
                    activeSave.parentList.Add(objectsToAdd[i] as GameObject);
            }
        }

        private PB_PrefabData GetSaveDataFromPrefab(GameObject prefab)
        {
            if(prefab == null)
                return null;

            for (int i = 0; i < activeSave.prefabData.Count; i++)
            {
                if (activeSave.prefabData[i].prefab == null)
                    continue;

#if UNITY_2017 || UNITY_5 || UNITY_4
                if(activeSave.prefabData[i].prefab == PrefabUtility.GetPrefabParent(prefab))
                    return activeSave.prefabData[i];
#else
                
                
                GameObject prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(prefab);
                if(prefabRoot != null)
                {
                    if (activeSave.prefabData[i].prefab == PrefabUtility.GetCorrespondingObjectFromSource(prefabRoot))
                    {
                        return activeSave.prefabData[i];
                    }  
                }
#endif
            }

            return null;
        }

        private void RunPrefabPaint()
        {
            if (!IsTabActive(PB_ActiveTab.PrefabPaint) || Event.current.alt)
                return;

            //If the placment brush is selected and the mouse is being dragged across the scene view.
            bool mouseDrag = Event.current.type == EventType.MouseDrag && Event.current.button == 0;
            bool mouseDown = Event.current.type == EventType.MouseDown && Event.current.button == 0;
            bool mouseUp = Event.current.type == EventType.MouseUp && Event.current.button == 0;
            bool mouseLeaveWindow = Event.current.type == EventType.MouseLeaveWindow;

            //Create a raycast that will come from the scene camera
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit finalHit;

            //Calculate the radius of the brush size.
            //Set the radius to 0 when doing single paint
            float newBrushSize = (activeSave.paintType == PB_PaintType.Single) ? 0 : activeSave.brushSize * .5f;
            Vector3 paintOffset = new Vector3 (Random.insideUnitSphere.x* newBrushSize, 0, Random.insideUnitSphere.z* newBrushSize);
            bool didRayHit = GetHitPoint(ray.origin + paintOffset, ray.direction, activeSave.ignoreTriggers, activeSave.ignorePaintedPrefabs, activeSave.includeInactivePrefabsInCheck, out finalHit);
            int failCount = 0;

            RaycastHit brushRootHit;
            bool didBrushRootHit = GetHitPoint(ray.origin, ray.direction, activeSave.ignoreTriggers, activeSave.ignorePaintedPrefabs, activeSave.includeInactivePrefabsInCheck, out brushRootHit);

            //If we lift the mouse reset the last ray so it acts as a fresh check next mouse down.
            if (mouseDown)
            {
                rayLastFrame = Vector3.positiveInfinity;
                paintTravelDistance = 0;
            }

            //Check if the brush move distance has been met, if not return.
            if (didBrushRootHit)
                CheckPaintDistance(brushRootHit.point);
                
            if (paintTravelDistance < activeSave.paintDeltaDistance)
                return;

            paintTravelDistance = 0;

            switch (activeSave.paintType)
            {
                case PB_PaintType.Surface:
                    if (mouseDrag || mouseDown)
                        for (int i = 0; i < activeSave.prefabsPerStroke; i++)
                        {
                            //We don't want to run GetHitPoint twice in the same frame
                            if (i != 0)
                            {
                                while (!didRayHit && failCount <= maxFails)
                                {
                                    didRayHit = GetHitPoint(new Vector3(ray.origin.x + Random.insideUnitSphere.x * newBrushSize, ray.origin.y, ray.origin.z + Random.insideUnitSphere.z * newBrushSize), ray.direction, activeSave.ignoreTriggers, activeSave.ignorePaintedPrefabs, activeSave.includeInactivePrefabsInCheck, out finalHit);
                                    //After max fails we want to exit the while loop
                                    if (!didRayHit)
                                        failCount++;
                                }
                            }

                            if (didRayHit == false)
                                continue;

                            didRayHit = false; //Reset this so that on the next loop it will check for a new hit

                            RunSurfacePaint(mouseDown, finalHit, false, brushRootHit.point);
                        }
                    break;
                case PB_PaintType.Physics:
                    if (mouseDrag || mouseDown)
                        for (int i = 0; i < activeSave.prefabsPerStroke; i++)
                        {
                            //We don't want to run GetHitPoint twice in the same frame
                            if (i != 0)
                            {
                                while (!didRayHit && failCount <= maxFails)
                                {
                                    didRayHit = GetHitPoint(new Vector3(ray.origin.x + Random.insideUnitSphere.x * newBrushSize, ray.origin.y, ray.origin.z + Random.insideUnitSphere.z * newBrushSize), ray.direction, activeSave.ignoreTriggers, activeSave.ignorePaintedPrefabs, activeSave.includeInactivePrefabsInCheck, out finalHit);

                                    //After max fails we want to exit the while loop
                                    if (!didRayHit)
                                        failCount++;
                                }
                            }

                            if (didRayHit == false)
                                continue;

                            didRayHit = false; //Reset this so that on the next loop it will check for a new hit

                            RunSurfacePaint(mouseDown, finalHit, true, brushRootHit.point);
                        }
                    break;
                case PB_PaintType.Single:
                    if (mouseDown)
                        StartSinglePaint(finalHit);
                    else if (mouseUp)
                        StopSinglePaint();
                    break;
            }

            if (moddingSingle)
                RunSinglePaint(mouseDown, mouseDrag, mouseUp, mouseLeaveWindow, finalHit);
        }

        private void RunSurfacePaint(bool mouseDown, RaycastHit hit, bool physicsBrush, Vector3 brushCenter)
        {
            selectedObject = GetRandomObject(); //Assign random object

            if (selectedObject != null)
            {
                if (CanBrush(hit, activeSave.checkTag, activeSave.checkLayer, activeSave.checkSlope, activeSave.checkTerrainLayer)) //If the brush result come back as true then start brushing
                {
                    clone = PrefabUtility.InstantiatePrefab(selectedObject) as GameObject;

                    if (clone == null)
                        return;

                    //Apply prefabs mods
                    if (!physicsBrush)
                        ApplyModifications(clone, hit, false, activeSave.parentingStyle, activeSave.rotateToMatchSurface, activeSave.randomizeRotation, activeSave.applyScale, false);                                    
                    else
                        ApplyModifications(clone, hit, true, activeSave.parentingStyle, activeSave.rotateToMatchSurface, activeSave.randomizeRotation, activeSave.applyScale, true);

                    Undo.RegisterCreatedObjectUndo(clone, "brush stroke: " + clone.name);       //Store the undo variables.
                }
            }
            else
                Debug.LogError("There is no object selected in the prefab window, please drag a prefab into the area to use the placement brush. Or make sure that atleast one prefab has been selected.");
        }

        private void StartSinglePaint(RaycastHit hit)
        {
            EditorGUIUtility.SetWantsMouseJumping(1);
            selectedObject = GetRandomObject(); //Assign random object

            if (selectedObject != null)
            {
                clone = PrefabUtility.InstantiatePrefab(selectedObject) as GameObject;

                if (clone != null)
                {
                    ApplyModifications(clone, hit, false, activeSave.parentingStyle, activeSave.rotateToMatchSurface, activeSave.randomizeRotation, activeSave.applyScale, false);
                    Undo.RegisterCreatedObjectUndo(clone, "brush stroke: " + clone.name);       //Store the undo variables.

                    objectToSingleMod = clone;
                    layerBeforeSingleMod = clone.layer;
                    clone.layer = 2;
                    moddingSingle = true;
                }
                else
                    Debug.LogError("There is no object selected in the prefab window, please drag a prefab into the area to use the placment brush.");
            }
        }

        private void StopSinglePaint()
        {
            if (objectToSingleMod != null)
                objectToSingleMod.layer = layerBeforeSingleMod;

            moddingSingle = false;
            selectedObject = null;
            objectToSingleMod = null;

            EditorGUIUtility.SetWantsMouseJumping(0);
        }

        private void RunSinglePaint(bool mouseDown, bool mouseDrag, bool mouseUp, bool mouseLeaveWindow, RaycastHit hit)
        {
            if (moddingSingle && objectToSingleMod != null)
            {
                switch (activeSave.draggingAction)
                {
                    case PB_DragModType.Position:

                        if (mouseDrag)
                        {
                            objectToSingleMod.transform.position = hit.point;
                            hitNormalSinceLastMovement = hit.normal;

                            if (activeSave.rotateToMatchSurface)
                            {
                                objectToSingleMod.transform.rotation = Quaternion.FromToRotation(GetDirection(activeSave.rotateSurfaceDirection), hitNormalSinceLastMovement);
                            }
                        }

                        break;
                    case PB_DragModType.Rotation:

                        if (e.isMouse)
                            objectToSingleRotation += (activeSave.rotationSensitivity * (-e.delta.x * Time.deltaTime));

                        objectToSingleMod.transform.rotation = Quaternion.FromToRotation(GetDirection(activeSave.rotateSurfaceDirection), hitNormalSinceLastMovement);
                        objectToSingleMod.transform.rotation *= Quaternion.Euler(GetDirection(activeSave.rotationAxis) * objectToSingleRotation);
                        break;
                    case PB_DragModType.Scale:

                        if (e.isMouse)
                        {
                            Vector3 newScale = objectToSingleMod.transform.localScale + Vector3.one * (-e.delta.x * Time.deltaTime);

                            if (newScale.x > 0 && newScale.y > 0 && newScale.z > 0)
                                objectToSingleMod.transform.localScale = newScale;
                        }

                        break;
                }
            }
        }

        private void CheckPaintDistance(Vector3 hitPoint)
        {
            //If no last frame has been set then set it
            if (rayLastFrame == Vector3.positiveInfinity)
                rayLastFrame = hitPoint;

            paintTravelDistance += Vector3.Distance(hitPoint, rayLastFrame);
            rayLastFrame = hitPoint;
        }

        private bool GetHitPoint(Vector3 origin, Vector3 direction, bool ignoreTriggers, bool ignorePaintedPrefabs, bool includeInactivePrefabsInChecks, out RaycastHit hit)
        {
            List<RaycastHit> hits = new List<RaycastHit>(Physics.RaycastAll(origin, direction));
            float minDist = Mathf.Infinity;
            int idToReturn = -1;

            for (int i = 0; i < hits.Count; i++)
            {
                //Prevent the raycast from picking up the single paint object
                if (clone == hits[i].collider.gameObject)
                    continue;

                if (ignoreTriggers && hits[i].collider.isTrigger)
                    continue;

                if (ignorePaintedPrefabs)
                {
                    PB_PrefabData data = GetSaveDataFromPrefab(hits[i].collider.gameObject);
                    if (data != null)
                    {
                        if (data.selected && !includeInactivePrefabsInChecks)
                            continue;

                        if (includeInactivePrefabsInChecks)
                            continue;
                    }        
                }

                //If we get this far then it is a valid surface, we just need to check the distance;
                float curDist = Vector3.Distance(origin, hits[i].point);
                if(curDist < minDist)
                {
                    idToReturn = i;
                    minDist = curDist;
                }
            }

            if(hits.Count == 0 || idToReturn == -1)
            {
                hit = new RaycastHit();
                return false;
            }

            hit = hits[idToReturn];
            return true;
        }

        private void RunEraseBrush()
        {
            //If the placment brush is selected and the mouse is being dragged across the scene view.
            bool isMouseEventCorrect = (Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseDown) && Event.current.button == 0;
            if (isMouseEventCorrect && IsTabActive(PB_ActiveTab.PrefabErase))
            {
                //Calculate the radius of the brush size.
                float newBrushSize = activeSave.eraseBrushSize * .5f;

                //Create a raycast that will come from the top of the world down onto a random point within the brush size raduis calculated above.
                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                RaycastHit hit;

        bool eraseHitPoint = GetHitPoint(ray.origin, ray.direction, activeSave.ignorePaintedPrefabsErase, activeSave.ignoreTriggersErase, activeSave.includeInactivePrefabsInCheckErase, out hit);

        if (eraseHitPoint)
        {
            if (activeSave.eraseType == PB_EraseTypes.PrefabsInBounds)
                brushBounds = new Bounds(hit.point, Vector3.one * activeSave.eraseBrushSize);

            switch (activeSave.eraseDetection)
            {
                case PB_EraseDetectionType.Collision:
                    Collider[] cols = Physics.OverlapSphere(hit.point, newBrushSize);
                    for (int i = 0; i < cols.Length; i++)
                    {
                        if (cols[i] != null)
                        {
                            if (CanErase(hit, cols[i].gameObject, activeSave.checkTagForErase, activeSave.eraseTagCheckType, activeSave.checkLayerForErase, activeSave.eraseLayerCheckType, activeSave.checkSlopeForErase, activeSave.checkTerrainLayerForErase))
                            {
                                if (activeSave.eraseType == PB_EraseTypes.PrefabsInBrush)
                                    TryErase(PrefabUtility.GetNearestPrefabInstanceRoot(cols[i].gameObject));
                                else
                                    TryErase(cols[i].gameObject);
                            }
                               
                        }
                    }
                    break;
                case PB_EraseDetectionType.Distance:
                    for (int i = 0; i < hierarchy.Length; i++)
                    {
                        if (hierarchy[i] != null)
                        {
                            bool checkErase = CanErase(hit, hierarchy[i], activeSave.checkTagForErase, activeSave.eraseTagCheckType, activeSave.checkLayerForErase, activeSave.eraseLayerCheckType, activeSave.checkSlopeForErase, activeSave.checkTerrainLayerForErase);
                            bool checkDistance = CheckDistance(newBrushSize, hit.point, hierarchy[i].transform.position);

                            if (checkErase && checkDistance && activeSave.eraseType == PB_EraseTypes.PrefabsInBrush)
                                TryErase(PrefabUtility.GetNearestPrefabInstanceRoot(hierarchy[i]));
                            else if (checkErase && checkDistance)
                                TryErase(hierarchy[i]);
                        }
                    }
                    break;
            }
        }
            }
        }

        private void TryErase(GameObject g)
        {
            switch (activeSave.eraseType)
            {
                case PB_EraseTypes.PrefabsInBrush:
                    PB_PrefabData data = GetSaveDataFromPrefab(g);

                    if (data == null)
                        return;

                    if ((activeSave.mustBeSelectedInBrush && data.selected) || !activeSave.mustBeSelectedInBrush)
                    {
                        //Store this in a new gameobject to keep the copy in the Undo
                        GameObject go = g;
                        Undo.DestroyObjectImmediate(go);
                    }

                    break;
                case PB_EraseTypes.PrefabsInBounds:
                    Bounds ObjectBounds = GetBounds(g);
                    if (brushBounds.Contains(ObjectBounds.min) && brushBounds.Contains(ObjectBounds.max))
                    {
                        GameObject go = g;
                        Undo.DestroyObjectImmediate(go);
                    }
                    break;
            }
        }

        private bool CanBrush(RaycastHit surfaceHit, bool checkTag, bool checkLayer, bool checkSlope, bool checkTerrainLayer)
        {
            if (checkTag)
            {
                string[] tags = GetTagsFromMask(activeSave.requiredTagMask);

                bool foundTag = false;
                for (int i = 0; i < tags.Length; i++)
                {
                    if (tags[i] == surfaceHit.collider.tag)
                    {
                        foundTag = true;
                        break;
                    }
                }

                if (foundTag == false)
                    return false;
            }

            if (checkLayer)
            {
                string[] layers = GetTagsFromLayer(activeSave.requiredLayerMask);

                bool foundLayer = false;
                for (int i = 0; i < layers.Length; i++)
                {
                    if (layers[i] == LayerMask.LayerToName(surfaceHit.collider.gameObject.layer))
                    {
                        foundLayer = true;
                        break;
                    }
                }

                if (foundLayer == false)
                    return false;
            }

            if (checkSlope)
            {
                float angle = Vector3.Angle(surfaceHit.normal, Vector3.up);
                if (angle > activeSave.maxRequiredSlope || angle < activeSave.minRequiredSlope)
                    return false;
            }

            if (checkTerrainLayer)
            {
                Terrain terrain = surfaceHit.collider.GetComponent<Terrain>();

                if(terrain != null)
                {
                    float[] layers = GetTextureMix(surfaceHit.point, terrain);
                    bool validSurface = false;

                    for (int i = 0; i < layers.Length; i++)
                    {
                        if (validSurface)
                            continue;

                        if (layers[i] > 0)
                        {
                            for (int t = 0; t < activeSave.terrainLayers.Count; t++)
                            {
                                if (validSurface)
                                    continue;

                                if (activeSave.terrainLayers[t].terrainLayer == terrain.terrainData.terrainLayers[i] && layers[i] >= activeSave.terrainLayers[t].minLayerStrength)
                                    validSurface = true;
                            }
                        }
                    }

                    if (!validSurface)
                        return false;
                }
            }

            return true;
        }

        private bool CanErase(RaycastHit surfaceHit, GameObject objectToErase, bool checkTag, PB_FilterCheckType tagDetagCheckType, bool checkLayer, PB_FilterCheckType layerDetagCheckType, bool checkSlope, bool checkTerrainLayer)
        {
            if (checkTag)
            {
                string[] tags = GetTagsFromMask(activeSave.requiredTagMaskForErase);

                bool foundTag = false;
                for (int i = 0; i < tags.Length; i++) //Go through all tags
                {
                    if (tagDetagCheckType == PB_FilterCheckType.CheckPrefab)
                        if (tags[i] == objectToErase.tag)
                        {
                            foundTag = true; //Store and break
                            break;
                        }
                        
                    if(tagDetagCheckType == PB_FilterCheckType.CheckSurface)    
                        if (tags[i] == surfaceHit.collider.tag)
                        {
                            foundTag = true; //Store and break
                            break;
                        }      
                }

                if (foundTag == false) //Only if the result is false do we return so the other checks can be made
                    return foundTag;
            }

            if (checkLayer)
            {
                string[] layers = GetTagsFromLayer(activeSave.requiredLayerMaskForErase);

                bool foundLayer = false;
                for (int i = 0; i < layers.Length; i++)
                {
                    if (layerDetagCheckType == PB_FilterCheckType.CheckPrefab)
                        if (layers[i] == LayerMask.LayerToName(objectToErase.layer))
                        {
                            foundLayer = true;
                            break;
                        }

                    if (layerDetagCheckType == PB_FilterCheckType.CheckSurface)
                        if (layers[i] == LayerMask.LayerToName(surfaceHit.collider.gameObject.layer))
                        {
                            foundLayer = true;
                            break;
                        }
                }

                if (foundLayer == false)
                    return false;
            }

            if (checkSlope)
            {
                float angle = Vector3.Angle(surfaceHit.normal, Vector3.up);
                if (angle > activeSave.maxRequiredSlopeForErase || angle < activeSave.minRequiredSlopeForErase)
                    return false;
            }

            if (checkTerrainLayer)
            {
                Terrain terrain = surfaceHit.collider.GetComponent<Terrain>();

                if (terrain != null)
                {
                    float[] layers = GetTextureMix(surfaceHit.point, terrain);
                    bool validSurface = false;

                    for (int i = 0; i < layers.Length; i++)
                    {
                        if (validSurface)
                            continue;

                        if (layers[i] > 0)
                        {
                            for (int t = 0; t < activeSave.terrainLayersErase.Count; t++)
                            {
                        if (validSurface)
                            continue;

                        Debug.Log(activeSave.terrainLayersErase[t].terrainLayer);

                        if (activeSave.terrainLayersErase[t].terrainLayer == terrain.terrainData.terrainLayers[i] && layers[i] >= activeSave.terrainLayersErase[t].minLayerStrength)
                            validSurface = true;
                            }
                        }
                    }

                    if (!validSurface)
                        return false;
                }
            }
            
            return true;
        }

        private void ApplyModifications(GameObject objectToMod, RaycastHit hitRef, bool useHeightOffset, PB_ParentingStyle parentingStyle, bool rotateToMatchSurface, bool randomizeRotation, bool randomizeScale, bool simPhysics)
        {
            float x = hitRef.point.x;
            float y = hitRef.point.y;
            float z = hitRef.point.z;

            if (activeSave.applyOriginOffset)
            {
                x += activeSave.prefabOriginOffset.x;
                y += activeSave.prefabOriginOffset.y;
                z += activeSave.prefabOriginOffset.z;
            }

            y += useHeightOffset ? activeSave.spawnHeight : 0;

            objectToMod.transform.position = new Vector3(x, y, z);

            if(activeSave.applyRotationOffset)
                objectToMod.transform.eulerAngles += activeSave.prefabRotationOffset;

            if(activeSave.applyParent)
                switch (parentingStyle)
                {
                    case PB_ParentingStyle.Surface:
                        objectToMod.transform.parent = hitRef.collider.transform;
                        break;

                    case PB_ParentingStyle.SingleTempParent:

                        if (activeSave.parent != null)
                            objectToMod.transform.parent = activeSave.parent.transform;
                        else
                            Debug.LogWarning("Prefab Brush is trying to set the objects parent to null. Please check that you have defined a gameobject in the Prefab Brush window.");
                        break;

                    case PB_ParentingStyle.ClosestTempFromList:

                        float dist = Mathf.Infinity;
                        Transform newParent = null;
                        for (int i = 0; i < activeSave.parentList.Count; i++)
                        {
                            float curDist = Vector3.Distance(activeSave.parentList[i].transform.position, objectToMod.transform.position);
                            if (curDist < dist)
                            {
                                newParent = activeSave.parentList[i].transform;
                                dist = curDist;
                            }
                        }

                        objectToMod.transform.parent = newParent;
                        break;

                    case PB_ParentingStyle.TempRoundRobin:
                        if (activeSave.parentList.Count == 0)
                            return;

                        roundRobinCount = GetId(activeSave.parentList.Count, roundRobinCount, 1);
                        objectToMod.transform.parent = activeSave.parentList[roundRobinCount].transform;
                        break;
                }

            if (rotateToMatchSurface)    //If rotate to surface has been selected then set the spawn objects rotation to the bases normal.
                objectToMod.transform.rotation = Quaternion.FromToRotation(GetDirection(activeSave.rotateSurfaceDirection), hitRef.normal);

            if (randomizeRotation)     //If random rotation has been selected then apply a random rotation define by the values in the window.
                objectToMod.transform.rotation *= Quaternion.Euler(new Vector3(Random.Range(activeSave.minXRotation, activeSave.maxXRotation), Random.Range(activeSave.minYRotation, activeSave.maxYRotation), Random.Range(activeSave.minZRotation, activeSave.maxZRotation)));

            //If random scale has been selected then apply a new scale transform to each object based on a random range.
            if (randomizeScale)
            {
                Vector3 newScale = Vector3.one;

                switch (activeSave.scaleType)
                {
                    case PB_ScaleType.SingleValue:
                        newScale *= Random.Range(activeSave.minScale, activeSave.maxScale); //Create a random number between the min and max scale values. 
                        break;
                    case PB_ScaleType.MultiAxis:
                        newScale.x = Random.Range(activeSave.minXScale, activeSave.maxXScale);
                        newScale.y = Random.Range(activeSave.minYScale, activeSave.maxYScale);
                        newScale.z = Random.Range(activeSave.minZScale, activeSave.maxZScale);
                        break;
                }

                switch (activeSave.scaleApplicationType)
                {
                    case PB_SaveApplicationType.Set:
                        objectToMod.transform.localScale = newScale;
                        break;
                    case PB_SaveApplicationType.Multiply:
                        newScale.x = objectToMod.transform.localScale.x * newScale.x;
                        newScale.y = objectToMod.transform.localScale.y * newScale.y;
                        newScale.z = objectToMod.transform.localScale.z * newScale.z;
                        break;
                }

                objectToMod.transform.localScale = newScale;
            }

#if UNITY_4 || UNITY_5
//Do nothing
#else
            if (simPhysics)
            {
                Rigidbody rBody = objectToMod.GetComponent<Rigidbody>();
                bool removeBodyAtEnd = false;

                if (rBody == null && activeSave.addRigidbodyToPaintedPrefab)
                {
                    rBody = objectToMod.AddComponent<Rigidbody>();
                    removeBodyAtEnd = true;
                }
                else if (rBody == null)
                    return;

                //Thank you to @mikelo for reporting this and providing a fix.
                //And again for pointing a second mistake :D
#if UNITY_2022_3_OR_NEWER
                Physics.simulationMode = SimulationMode.Script;
#else
                Physics.autoSimulation = false;
#endif
                for (int i = 0; i < activeSave.physicsIterations; i++)
                {
                    Physics.Simulate(Time.fixedDeltaTime);
                }
#if UNITY_2022_3_OR_NEWER
                Physics.simulationMode = SimulationMode.FixedUpdate;
#else
                Physics.autoSimulation = true;
#endif

                if (removeBodyAtEnd)
                    DestroyImmediate(rBody);
            }
#endif
            }

        private GameObject GetRandomObject()
        {
            List<PB_PrefabData> activeData = activeSave.GetActivePrefabs();

            if (activeData.Count <= 0)
                return null;

            int rnd = Random.Range(0, activeData.Count);

            return activeData[rnd].prefab;
        }

        private void SetDragAction()
        {
            switch (Tools.current)
            {
                case Tool.Move:
                    activeSave.draggingAction = PB_DragModType.Position;
                    break;
                case Tool.Rotate:
                    activeSave.draggingAction = PB_DragModType.Rotation;
                    break;
                case Tool.Scale:
                    activeSave.draggingAction = PB_DragModType.Scale;
                    break;
            }
        }

        private void CheckForHotKeyInput()
        {
            if (GetHoldKeyState(Event.current.keyCode))
            {
                if (e.type == EventType.KeyDown)
                {
                    if (tempTab)
                    return;

                    previousTab = activeTab;
                    tempTab = true;

                    if (Event.current.keyCode == activeSave.paintBrushHotKey)
                        SetActiveTab(PB_ActiveTab.PrefabPaint);

                    if (Event.current.keyCode == activeSave.removeBrushHotKey)
                        SetActiveTab(PB_ActiveTab.PrefabErase);
                }
                else if (e.type == EventType.KeyUp)
                {
                    if (!tempTab)
                        return;

                    tempTab = false;
                    SetActiveTab(previousTab);
                }
            }
            else
            {
                if (e.type == EventType.KeyDown)
                {
                    if (Event.current.keyCode == activeSave.paintBrushHotKey)
                        SetActiveTab(PB_ActiveTab.PrefabPaint);

                    if (Event.current.keyCode == activeSave.removeBrushHotKey)
                        SetActiveTab(PB_ActiveTab.PrefabErase);
                }
            }
        }

        private void CheckForOnHotKey()
        {
            if (GetHoldKeyState(Event.current.keyCode))
            {
                if (e.type == EventType.KeyDown)
                {
                    if (tempState)
                        return;

                    tempState = true;

                    if (Event.current.keyCode == activeSave.disableBrushHotKey)
                        ToggleOnState();
                }
                else if (e.type == EventType.KeyUp)
                {
                    if (!tempState)
                        return;

                    tempState = false;

                    if (Event.current.keyCode == activeSave.disableBrushHotKey)
                        ToggleOnState();
                }
            }
            else
            {
                if (e.type == EventType.KeyDown)
                {
                    if (Event.current.keyCode == activeSave.disableBrushHotKey)
                        ToggleOnState();
                }
            }
        }

        private void ToggleOnState()
        {
            SetOnState(!isOn);
        }

        private void SetOnState(bool newState)
        {
            isOn = newState;
            HideTools(isOn);
            buttonIcon = GetButtonTexture();
        }

        private float[] GetTextureMix(Vector3 worldPos, Terrain terrain)
        {
            Vector3 terrainPosition = worldPos - terrain.transform.position;
            TerrainData terrainData = terrain.terrainData;

            Vector3 mapPosition = new Vector3(terrainPosition.x / terrainData.size.x, 0, terrainPosition.z / terrainData.size.z);

            var xCoord = mapPosition.x * terrainData.alphamapWidth;
            var zCoord = mapPosition.z * terrainData.alphamapHeight;
            var posX = (int)xCoord;
            var posZ = (int)zCoord;

            float[,,] mapData = terrain.terrainData.GetAlphamaps(posX, posZ, 1, 1);
            float[] layersCollection = new float[mapData.GetUpperBound(2) + 1];

            for (int i = 0; i < layersCollection.Length; i++)
            {
                layersCollection[i] = mapData[0, 0, i];
            }

            return layersCollection;
        }

        private string GetSaveDirectory()
        {
            string[] guid = AssetDatabase.FindAssets("PB_Enums");

            if (guid.Length <= 0)
                return "Assets/";

            string startingPath = AssetDatabase.GUIDToAssetPath(guid[0]);
            string currentPath = startingPath.Replace("/Scripts/Editor/PB_Enums.cs", "").Trim() + "/SaveFiles";

            return currentPath;
        }

        private void ClearActiveSave()
        {
            activeSave = null;
            selectedSave = -1;
            activeTab = PB_ActiveTab.PrefabPaint;
        }

        private void SelectSave(PB_SaveObject newSave)
        {
            activeSave = newSave;
            selectedSave = GetSaveID(activeSave);
        }

        private bool DoesSelectedSaveMatch()
        {
            if(saveObjects.Count <= selectedSave || saveObjects.Count == 0 || selectedSave == -1 || activeSave == null)
                return false;

            if (saveObjects[selectedSave] != activeSave)
                return false;

            return true;
        }

        private int GetSaveID(PB_SaveObject newSave)
        {
            for (int i = 0; i < saveObjects.Count; i++)
            {
                if (saveObjects[i] == newSave)
                    return i;
            }

            return 0;
        }

        private void CreateNewSave()
        {
            GetAllSaves();

            PB_SaveObject asset = CreateInstance<PB_SaveObject>();

            int count = 0;
            string newName = $"New Brush [{count}]";

            while (saveObjectNames.Contains(newName))
            {
                count++;
                newName = $"New Brush [{count}]";
            }

            string assetName = $"{GetSaveDirectory()}/{newName}.asset";

            AssetDatabase.CreateAsset(asset, assetName);
            AssetDatabase.SaveAssets();

            SelectSave((PB_SaveObject)AssetDatabase.LoadAssetAtPath(assetName, typeof(PB_SaveObject)));

            EditorUtility.FocusProjectWindow();
            GetAllSaves();
            selectedSave = GetSaveID(asset);
            EditorGUIUtility.PingObject(saveObjects[selectedSave]);

            Repaint();
        }

        #endregion

        #region Tools
        private void CheckIfDraggingSave()
        {
            if (DragAndDrop.paths.Length > 0)
            {
                foreach (object o in DragAndDrop.objectReferences)
                {
                    if (o is PB_SaveObject)
                    {
                        showSaveDrop = true;
                        return;
                    }
                }
            }

            showSaveDrop = false;
        }

        private bool GetHoldKeyState(KeyCode code)
        {
            if (activeSave.paintBrushHotKey == code)
                return activeSave.paintBrushHoldKey;

            if (activeSave.removeBrushHotKey == code)
                return activeSave.removeBrushHoldKey;

            if (activeSave.disableBrushHotKey == code)
                return activeSave.disableBrushHoldKey;

            return false;
        }

        private bool CheckDistance(float radius, Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a,b) <= radius;
        }

        private void SetActiveTab(PB_ActiveTab newTab)
        {
            activeTab = newTab;

            if (newTab == PB_ActiveTab.PrefabErase)
            {
#if UNITY_6000_0_OR_NEWER
                hierarchy = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
#else
                hierarchy = (GameObject[])FindObjectsOfType(typeof(GameObject));
#endif
            }
        }

        private void SetTabColour(PB_ActiveTab tabToCheck)
        {
            GUI.color = IsTabActive(tabToCheck) ? selectedTab : Color.white;
        }

        private bool IsTabActive(PB_ActiveTab tabToCheck)
        {
            return activeTab == tabToCheck;
        }

        private float GetPrefabIconSize()
        {
            return prefabIconMinSize * prefabIconScaleFactor;
        }

        private int GetId(int listSize, int curPointInList, int direction)
        {
            if ((curPointInList + direction) >= listSize)
                return 0;

            if ((curPointInList + direction) < 0)
                return listSize - 1;

            return curPointInList + direction;
        }

        private Vector3 GetDirection(PB_Direction direction)
        {
            switch (direction)
            {
                case PB_Direction.Up:
                    return Vector3.up;
                case PB_Direction.Down:
                    return -Vector3.up;
                case PB_Direction.Left:
                    return -Vector3.right;
                case PB_Direction.Right:
                    return Vector3.right;
                case PB_Direction.Forward:
                    return Vector3.forward;
                case PB_Direction.Backward:
                    return -Vector3.forward;
            }

            return Vector3.zero;
        }

        private string[] GetTagsFromMask(int original)
        {
            List<string> output = new List<string>();

            for (int i = 0; i < UnityEditorInternal.InternalEditorUtility.tags.Length; ++i)
            {
                int shifted = 1 << i;
                if ((original & shifted) == shifted)
                {
                    string variableName = UnityEditorInternal.InternalEditorUtility.tags[i];
                    if (!string.IsNullOrEmpty(variableName))
                    {
                        output.Add(variableName);
                    }
                }
            }
            return output.ToArray();
        }

        private string[] GetTagsFromLayer(int original)
        {
            List<string> output = new List<string>();

            for (int i = 0; i < UnityEditorInternal.InternalEditorUtility.layers.Length; ++i)
            {
                int shifted = 1 << i;
                if ((original & shifted) == shifted)
                {
                    string variableName = UnityEditorInternal.InternalEditorUtility.layers[i];
                    if (!string.IsNullOrEmpty(variableName))
                    {
                        output.Add(variableName);
                    }
                }
            }
            return output.ToArray();
        }

        private Bounds GetBounds(GameObject boundsObject)
        {
            Renderer childRender;
            Bounds bounds = GetBoundsFromRenderer(boundsObject);
            if (bounds.extents.x == 0)
            {
                bounds = new Bounds(boundsObject.transform.position, Vector3.zero);
                foreach (Transform child in boundsObject.transform)
                {
                    childRender = child.GetComponent<Renderer>();
                    if (childRender)
                        bounds.Encapsulate(childRender.bounds);
                    else
                        bounds.Encapsulate(GetBoundsFromRenderer(child.gameObject));
                }
            }
            return bounds;
        }

        Bounds GetBoundsFromRenderer(GameObject child)
        {
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            Renderer render = child.GetComponent<Renderer>();

            if (render != null)
                return render.bounds;

            return bounds;
        }

        private Texture2D GetButtonTexture()
        {
            if (isOn)
                return EditorGUIUtility.isProSkin ? onButtonDark : onButtonLight;
            else
                return EditorGUIUtility.isProSkin ? offButtonDark : offButtonLight;
        }
        
        #endregion

        void OnSceneGUI(SceneView sceneView)
        {
            if (newSave != null)
            {
                SelectSave(newSave);
                newSave = null;
            }

            if(activeSave == null)
            {
                return;
            }

            e = Event.current;
            CheckForOnHotKey();

            if (isOn && activeSave != null)
            {
                switch (activeTab)
                {
                    case PB_ActiveTab.PrefabPaint:
                        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                        DrawPaintCircle(placeBrush, activeSave.brushSize);
                        RunPrefabPaint();
                        break;
                    case PB_ActiveTab.PrefabErase:
                        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                        DrawPaintCircle(eraseBrush, activeSave.eraseBrushSize);
                        RunEraseBrush();
                        break;
                }

                CheckForHotKeyInput();
                SetDragAction();
            }
        }

        void OnDestroy()
        {
            //Force tools to come back if tool is closed.
            HideTools(false);

            // When the window is destroyed, remove the delegate
            // so that it will no longer do any drawing.
#if UNITY_2017 || UNITY_2018 || UNITY_5 || UNITY_4
            SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
#else
            SceneView.duringSceneGui -= this.OnSceneGUI;
#endif
        }
    }
}
