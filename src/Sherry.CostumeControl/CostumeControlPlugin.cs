using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Logging;
using Bulbul;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sherry.CostumeControl
{
    [BepInPlugin("sherry.chillwithyou.costumecontrol", "Sherry Costume Control", "1.0.0")]
    public sealed class CostumeControlPlugin : BaseUnityPlugin
    {
        private static readonly CostumeChangeService.CostumeSkinType[] SkinTypes =
        {
            CostumeChangeService.CostumeSkinType.Default_1,
            CostumeChangeService.CostumeSkinType.Polo_1,
            CostumeChangeService.CostumeSkinType.Polo_2,
            CostumeChangeService.CostumeSkinType.Tee_1,
            CostumeChangeService.CostumeSkinType.Tee_2,
        };

        private static readonly System.Random Random = new System.Random();
        private static ManualLogSource Log = null!;
        private static CostumeChangeService? CurrentService;
        private static string ConfigFilePath = string.Empty;
        private static DateTime LastConfigWriteTimeUtc;
        private static string Mode = "fixed";
        private static CostumeChangeService.CostumeSkinType SelectedSkinType = CostumeChangeService.CostumeSkinType.Polo_2;
        private static float ToastEndTime;
        private static string ToastText = string.Empty;
        private static bool HasLoggedUpdate;
        private static bool IsWindowVisible = true;
        private static Rect WindowRect = new Rect(24f, 72f, 430f, 470f);
        private static Vector2 LogScroll;
        private static readonly string[] RecentLogs = new string[10];
        private static int RecentLogCount;
        private static GameObject? UiRoot;
        private static GameObject? PanelObject;
        private static GameObject? ToggleObject;
        private static Text? StatusText;
        private static Text? LogText;

        private void Awake()
        {
            Log = Logger;
            ConfigFilePath = Path.Combine(Paths.ConfigPath, "Sherry.CostumeControl.json");
            EnsureConfigFile();
            LoadConfig();
            new Harmony("sherry.chillwithyou.costumecontrol").PatchAll(typeof(CostumeControlPlugin).Assembly);
            AddLog("衣服控制插件已加载。F7 显示/隐藏面板。");
        }

        private void Update()
        {
            if (!HasLoggedUpdate)
            {
                HasLoggedUpdate = true;
                Log.LogInfo("衣服控制插件 Update 已启动。");
            }

            if (Input.GetKeyDown(KeyCode.F10))
            {
                ReloadConfigByUser();
            }

            if (Input.GetKeyDown(KeyCode.F7))
            {
                SetPanelVisible(!IsWindowVisible);
            }

            if (Input.GetKeyDown(KeyCode.F8))
            {
                RandomNextByUser();
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                FixCurrentByUser();
            }
        }

        private void OnGUI()
        {
            var currentEvent = Event.current;
            if (currentEvent != null && currentEvent.type == EventType.KeyDown)
            {
                if (currentEvent.keyCode == KeyCode.F7)
                {
                    SetPanelVisible(!IsWindowVisible);
                    currentEvent.Use();
                }
                else if (currentEvent.keyCode == KeyCode.F8)
                {
                    RandomNextByUser();
                    currentEvent.Use();
                }
                else if (currentEvent.keyCode == KeyCode.F9)
                {
                    FixCurrentByUser();
                    currentEvent.Use();
                }
                else if (currentEvent.keyCode == KeyCode.F10)
                {
                    ReloadConfigByUser();
                    currentEvent.Use();
                }
            }

            if (Time.realtimeSinceStartup <= ToastEndTime)
            {
                GUI.Label(new Rect(16f, 16f, 640f, 32f), ToastText);
            }

            if (IsWindowVisible)
            {
                WindowRect = GUI.Window(918230, WindowRect, DrawWindow, "Sherry 衣服控制");
            }
        }

        private static void DrawWindow(int windowId)
        {
            GUILayout.Label($"状态：{(CurrentService == null ? "未捕获衣服服务" : "已捕获衣服服务")}");
            GUILayout.Label($"模式：{Mode}");
            GUILayout.Label($"当前选择：{SelectedSkinType} ({(int)SelectedSkinType})");

            GUILayout.Space(8f);
            GUILayout.Label("选择衣服");
            GUILayout.BeginHorizontal();
            DrawSkinButton(CostumeChangeService.CostumeSkinType.Default_1);
            DrawSkinButton(CostumeChangeService.CostumeSkinType.Polo_1);
            DrawSkinButton(CostumeChangeService.CostumeSkinType.Polo_2);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawSkinButton(CostumeChangeService.CostumeSkinType.Tee_1);
            DrawSkinButton(CostumeChangeService.CostumeSkinType.Tee_2);
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("应用并固定", GUILayout.Height(32f)))
            {
                Mode = "fixed";
                SaveConfig();
                ApplySkinType(SelectedSkinType, true);
                AddLog($"已应用并固定：{SelectedSkinType} ({(int)SelectedSkinType})");
            }

            if (GUILayout.Button("随机一套", GUILayout.Height(32f)))
            {
                RandomNextByUser();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("重载配置", GUILayout.Height(28f)))
            {
                ReloadConfigByUser();
            }

            if (GUILayout.Button("隐藏面板 F7", GUILayout.Height(28f)))
            {
                SetPanelVisible(false);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.Label("日志");
            LogScroll = GUILayout.BeginScrollView(LogScroll, GUILayout.Height(150f));
            for (var index = 0; index < RecentLogCount; index++)
            {
                GUILayout.Label(RecentLogs[index]);
            }
            GUILayout.EndScrollView();

            GUILayout.Space(4f);
            GUILayout.Label("提示：如果状态一直是未捕获，先进入房间主界面再点应用。");
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        private static void DrawSkinButton(CostumeChangeService.CostumeSkinType skinType)
        {
            var label = skinType == SelectedSkinType ? $"* {skinType}" : skinType.ToString();
            if (GUILayout.Button(label, GUILayout.Height(30f)))
            {
                SelectedSkinType = skinType;
                Mode = "fixed";
                SaveConfig();
                ApplySkinType(skinType, true);
                AddLog($"已选择：{skinType} ({(int)skinType})");
            }
        }

        private static void EnsureUi()
        {
            if (UiRoot != null)
            {
                UpdateUiText();
                SetPanelVisible(IsWindowVisible);
                return;
            }

            EnsureEventSystem();

            UiRoot = new GameObject("SherryCostumeControlCanvas");
            UnityEngine.Object.DontDestroyOnLoad(UiRoot);

            var canvas = UiRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32760;
            UiRoot.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            UiRoot.AddComponent<GraphicRaycaster>();

            var panel = CreateRect("Panel", UiRoot.transform, new Vector2(24f, -72f), new Vector2(430f, 430f));
            PanelObject = panel.gameObject;
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.04f, 0.05f, 0.07f, 0.88f);

            ToggleObject = CreateButton(UiRoot.transform, new Vector2(24f, -72f), new Vector2(76f, 36f), "衣装", () =>
            {
                SetPanelVisible(true);
            });

            var title = CreateText("Title", panel, new Vector2(14f, -12f), new Vector2(390f, 28f), "Sherry 衣服控制", 20, TextAnchor.MiddleLeft);
            title.color = new Color(1f, 0.88f, 0.96f, 1f);

            StatusText = CreateText("Status", panel, new Vector2(14f, -45f), new Vector2(390f, 52f), string.Empty, 14, TextAnchor.UpperLeft);

            var y = -106f;
            CreateButton(panel, new Vector2(14f, y), new Vector2(128f, 34f), "Default_1", () => SelectSkinByUi(CostumeChangeService.CostumeSkinType.Default_1));
            CreateButton(panel, new Vector2(150f, y), new Vector2(128f, 34f), "Polo_1", () => SelectSkinByUi(CostumeChangeService.CostumeSkinType.Polo_1));
            CreateButton(panel, new Vector2(286f, y), new Vector2(128f, 34f), "Polo_2", () => SelectSkinByUi(CostumeChangeService.CostumeSkinType.Polo_2));

            y -= 42f;
            CreateButton(panel, new Vector2(14f, y), new Vector2(128f, 34f), "Tee_1", () => SelectSkinByUi(CostumeChangeService.CostumeSkinType.Tee_1));
            CreateButton(panel, new Vector2(150f, y), new Vector2(128f, 34f), "Tee_2", () => SelectSkinByUi(CostumeChangeService.CostumeSkinType.Tee_2));
            CreateButton(panel, new Vector2(286f, y), new Vector2(128f, 34f), "随机一套", RandomNextByUser);

            y -= 50f;
            CreateButton(panel, new Vector2(14f, y), new Vector2(128f, 34f), "应用并固定", () =>
            {
                Mode = "fixed";
                SaveConfig();
                ApplySkinType(SelectedSkinType, true);
                AddLog($"已应用并固定：{SelectedSkinType} ({(int)SelectedSkinType})");
            });
            CreateButton(panel, new Vector2(150f, y), new Vector2(128f, 34f), "重载配置", ReloadConfigByUser);
            CreateButton(panel, new Vector2(286f, y), new Vector2(128f, 34f), "隐藏面板", () =>
            {
                SetPanelVisible(false);
            });

            LogText = CreateText("Logs", panel, new Vector2(14f, -246f), new Vector2(400f, 160f), string.Empty, 13, TextAnchor.UpperLeft);
            UpdateUiText();
            SetPanelVisible(IsWindowVisible);
        }

        private static void SetPanelVisible(bool isVisible)
        {
            IsWindowVisible = isVisible;
            PanelObject?.SetActive(isVisible);
            ToggleObject?.SetActive(!isVisible);
            AddLog(isVisible ? "已显示衣服控制面板。" : "已隐藏面板，点击左上角“衣装”按钮可恢复。");
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("SherryCostumeControlEventSystem");
            UnityEngine.Object.DontDestroyOnLoad(eventSystemObject);
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchoredPosition, Vector2 size)
        {
            var rectObject = new GameObject(name);
            rectObject.transform.SetParent(parent, false);
            var rectTransform = rectObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
            return rectTransform;
        }

        private static Text CreateText(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, string content, int fontSize, TextAnchor alignment)
        {
            var rectTransform = CreateRect(name, parent, anchoredPosition, size);
            var text = rectTransform.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.text = content;
            return text;
        }

        private static GameObject CreateButton(Transform parent, Vector2 anchoredPosition, Vector2 size, string label, Action onClick)
        {
            var rectTransform = CreateRect(label + "Button", parent, anchoredPosition, size);
            var image = rectTransform.gameObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.2f, 0.26f, 0.96f);

            var button = rectTransform.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());

            var text = CreateText(label + "Text", rectTransform, new Vector2(0f, 0f), size, label, 14, TextAnchor.MiddleCenter);
            text.raycastTarget = false;
            return rectTransform.gameObject;
        }

        private static void SelectSkinByUi(CostumeChangeService.CostumeSkinType skinType)
        {
            SelectedSkinType = skinType;
            Mode = "fixed";
            SaveConfig();
            ApplySkinType(skinType, true);
            AddLog($"已选择：{skinType} ({(int)skinType})");
        }

        private static void UpdateUiText()
        {
            if (StatusText != null)
            {
                StatusText.text = $"服务：{(CurrentService == null ? "未捕获" : "已捕获")}\n模式：{Mode}    当前：{SelectedSkinType} ({(int)SelectedSkinType})";
            }

            if (LogText != null)
            {
                LogText.text = string.Join("\n", RecentLogs.Take(RecentLogCount));
            }
        }

        private static void RandomNextByUser()
        {
            var nextSkinType = GetRandomSkinType();
            Mode = "fixed";
            SelectedSkinType = nextSkinType;
            SaveConfig();
            ApplySkinType(nextSkinType, true);
            ShowToast($"Sherry衣服控制：随机切换到 {nextSkinType} ({(int)nextSkinType})");
            AddLog($"已随机切换衣服：{nextSkinType} ({(int)nextSkinType})");
        }

        private static void FixCurrentByUser()
        {
            Mode = "fixed";
            SaveConfig();
            SaveTodaySkinType(SelectedSkinType);
            ShowToast($"Sherry衣服控制：已固定 {SelectedSkinType} ({(int)SelectedSkinType})");
            AddLog($"已固定当前配置衣服：{SelectedSkinType} ({(int)SelectedSkinType})");
        }

        private static void ReloadConfigByUser()
        {
            LoadConfig();
            ShowToast($"Sherry衣服控制：已重载 {SelectedSkinType} ({(int)SelectedSkinType})");
            AddLog("已重载衣服配置。");
        }

        private static void ShowToast(string text)
        {
            ToastText = text;
            ToastEndTime = Time.realtimeSinceStartup + 3f;
        }

        private static void AddLog(string message)
        {
            var text = DateTime.Now.ToString("HH:mm:ss") + "  " + message;

            if (RecentLogCount < RecentLogs.Length)
            {
                RecentLogCount++;
            }

            for (var index = RecentLogCount - 1; index > 0; index--)
            {
                RecentLogs[index] = RecentLogs[index - 1];
            }

            RecentLogs[0] = text;
            Log?.LogInfo(message);
            UpdateUiText();
        }

        private static bool TryGetForcedSkinType(out CostumeChangeService.CostumeSkinType skinType)
        {
            ReloadConfigIfChanged();
            skinType = SelectedSkinType;
            return string.Equals(Mode, "fixed", StringComparison.OrdinalIgnoreCase);
        }

        private static void CaptureService(CostumeChangeService service)
        {
            if (CurrentService == null)
            {
                AddLog("已捕获衣服服务。");
            }

            CurrentService = service;
            EnsureUi();
        }

        private static void ApplySkinType(CostumeChangeService.CostumeSkinType skinType, bool save)
        {
            if (CurrentService == null)
            {
                AddLog("还没有捕获到衣服服务，进入房间主界面后再试。");
                return;
            }

            CurrentService.ChangeCostume(skinType);

            if (save)
            {
                SaveTodaySkinType(skinType);
            }
        }

        private static void SaveTodaySkinType(CostumeChangeService.CostumeSkinType skinType)
        {
            try
            {
                var latestChangeData = SaveDataManager.Instance.CostumeChangeSaveData.LatestChangeData;
                latestChangeData.SetChangedCostumeDate();
                latestChangeData.SetChangedCostumeSkinType(skinType);
                SaveDataManager.Instance.SaveCostumeChangeSaveData();
            }
            catch (Exception ex)
            {
                AddLog($"保存今日衣服失败：{ex.Message}");
            }
        }

        private static CostumeChangeService.CostumeSkinType GetRandomSkinType()
        {
            var candidates = SkinTypes.Where(skinType => skinType != SelectedSkinType).ToArray();
            return candidates[Random.Next(candidates.Length)];
        }

        private static void EnsureConfigFile()
        {
            if (File.Exists(ConfigFilePath))
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath)!);
            File.WriteAllText(ConfigFilePath, CreateConfigContent(), new UTF8Encoding(false));
        }

        private static void LoadConfig()
        {
            EnsureConfigFile();
            var text = File.ReadAllText(ConfigFilePath, Encoding.UTF8);
            Mode = ReadString(text, "mode", "fixed");
            var skinTypeValue = ReadInt(text, "skinType", (int)SelectedSkinType);

            if (Enum.IsDefined(typeof(CostumeChangeService.CostumeSkinType), skinTypeValue))
            {
                SelectedSkinType = (CostumeChangeService.CostumeSkinType)skinTypeValue;
            }
            else
            {
                AddLog($"配置中的 skinType 无效：{skinTypeValue}，已继续使用 {SelectedSkinType}。");
            }

            LastConfigWriteTimeUtc = File.GetLastWriteTimeUtc(ConfigFilePath);
        }

        private static void ReloadConfigIfChanged()
        {
            if (!File.Exists(ConfigFilePath))
            {
                EnsureConfigFile();
                LoadConfig();
                return;
            }

            var writeTimeUtc = File.GetLastWriteTimeUtc(ConfigFilePath);
            if (writeTimeUtc != LastConfigWriteTimeUtc)
            {
                LoadConfig();
            }
        }

        private static void SaveConfig()
        {
            File.WriteAllText(ConfigFilePath, CreateConfigContent(), new UTF8Encoding(false));
            LastConfigWriteTimeUtc = File.GetLastWriteTimeUtc(ConfigFilePath);
        }

        private static string CreateConfigContent()
        {
            return "{\n"
                + "  \"mode\": \"" + Mode + "\",\n"
                + "  \"skinType\": " + (int)SelectedSkinType + ",\n"
                + "  \"availableSkinTypes\": {\n"
                + "    \"Default_1\": 1,\n"
                + "    \"Polo_1\": 1001,\n"
                + "    \"Polo_2\": 1002,\n"
                + "    \"Tee_1\": 2001,\n"
                + "    \"Tee_2\": 2002\n"
                + "  },\n"
                + "  \"hotkeys\": {\n"
                + "    \"randomNext\": \"F8\",\n"
                + "    \"fixCurrent\": \"F9\",\n"
                + "    \"reloadConfig\": \"F10\"\n"
                + "  }\n"
                + "}\n";
        }

        private static string ReadString(string text, string key, string defaultValue)
        {
            var match = Regex.Match(text, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]*)\"");
            return match.Success ? match.Groups[1].Value : defaultValue;
        }

        private static int ReadInt(string text, string key, int defaultValue)
        {
            var match = Regex.Match(text, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(-?\\d+)");
            return match.Success && int.TryParse(match.Groups[1].Value, out var value) ? value : defaultValue;
        }

        [HarmonyPatch(typeof(CostumeChangeService), nameof(CostumeChangeService.ChangeTodayCostume))]
        private static class ChangeTodayCostumePatch
        {
            private static void Prefix(CostumeChangeService __instance)
            {
                CaptureService(__instance);
            }
        }

        [HarmonyPatch(typeof(CostumeChangeService), nameof(CostumeChangeService.ChangeCostume))]
        private static class ChangeCostumePatch
        {
            private static void Prefix(CostumeChangeService __instance, ref CostumeChangeService.CostumeSkinType skinType)
            {
                CaptureService(__instance);

                if (TryGetForcedSkinType(out var forcedSkinType))
                {
                    skinType = forcedSkinType;
                }
            }
        }

        [HarmonyPatch(typeof(CostumeChangeService), "TryChangeCostume")]
        private static class TryChangeCostumePatch
        {
            private static void Prefix(CostumeChangeService __instance, ref CostumeChangeService.CostumeSkinType skinType)
            {
                CaptureService(__instance);

                if (TryGetForcedSkinType(out var forcedSkinType))
                {
                    skinType = forcedSkinType;
                }
            }
        }

        [HarmonyPatch(typeof(CostumeChangeService), nameof(CostumeChangeService.LotterySkin))]
        private static class LotterySkinPatch
        {
            private static bool Prefix(CostumeChangeService __instance, ref CostumeChangeService.CostumeSkinType __result)
            {
                CaptureService(__instance);

                if (!TryGetForcedSkinType(out var skinType))
                {
                    return true;
                }

                __result = skinType;
                SaveTodaySkinType(skinType);
                return false;
            }
        }
    }
}
