using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Game.Bootstrap;
using Game.Bootstrap.Scopes;
using Game.Core.Enums;
using Game.Core.Messages;
using Game.Gameplay.Save;
using Game.UI;
using MessagePipe;
using UnityEditor;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace Game.Editor
{
    public sealed class DebugHub : EditorWindow
    {
        private EnemyDebugPanel _enemyPanel;
        private readonly Dictionary<string, Vector2> _panelScrollPositions = new();
        private float _combatHeightRatio = 0.56f;
        private float _leftColumnRatio = 0.34f;
        private float _middleColumnRatio = 0.52f;
        private float _leftRowRatio = 0.52f;
        private float _middleRowRatio = 0.52f;
        private int _activeSplitterId;

        private string _activeCameraName = "-";
        private string _cameraConfinerBounds = "-";
        private Vector3 _cameraDamping;
        private Vector2 _cameraPanOffset;
        private Vector2 _cameraPanTarget;
        private float _cameraPanDuration = 0.35f;
        private bool _cameraPanAvailable;
        private CameraMode _cameraMode;
        private bool _cameraImpulseActive;
        private string _dialogueState = "Not Playing";
        private bool _dialogueActive;
        private MessagePipeTracer _messageTracer;
        private SaveManager _saveManager;

        [MenuItem("Window/Debug Hub")]
        public static void ShowWindow()
        {
            var window = GetWindow<DebugHub>("Debug Hub");
            window.minSize = new Vector2(1100f, 760f);
            window.Show();
        }

        [MenuItem("Window/Game/Debug Hub")]
        private static void ShowFromGameMenu()
        {
            ShowWindow();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawWorkspace();
        }

        private void OnEnable()
        {
            _enemyPanel = new EnemyDebugPanel();
            _enemyPanel.Enable();
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            _enemyPanel?.Dispose();
            _enemyPanel = null;
        }

        private void OnEditorUpdate()
        {
            _enemyPanel?.Refresh();
            RefreshCameraState();
            RefreshDialogueState();
            ResolveSaveServices();
            Repaint();
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Debug Hub", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label(Application.isPlaying ? "PLAY MODE" : "EDIT MODE", EditorStyles.miniLabel);
            }
        }

        private void DrawWorkspace()
        {
            const float splitSize = 7f;
            Rect workspace = new Rect(6f, 38f, position.width - 12f, position.height - 44f);
            float upperHeight = (workspace.height - splitSize) * _combatHeightRatio;
            Rect combatRect = new Rect(workspace.x, workspace.y, workspace.width, upperHeight);
            Rect combatSplitter = new Rect(workspace.x, combatRect.yMax, workspace.width, splitSize);
            Rect lowerRect = new Rect(workspace.x, combatSplitter.yMax, workspace.width,
                workspace.height - upperHeight - splitSize);

            DrawPanel(combatRect, "Combat", () => _enemyPanel?.DrawContents(combatRect.width - 16f));
            DrawSplitter(combatSplitter, workspace, ref _combatHeightRatio, false, 1);
            DrawLowerWorkspace(lowerRect, splitSize);
        }

        private void DrawLowerWorkspace(Rect bounds, float splitSize)
        {
            float availableWidth = bounds.width - splitSize * 2f;
            float leftWidth = availableWidth * _leftColumnRatio;
            float remainingWidth = availableWidth - leftWidth;
            float middleWidth = remainingWidth * _middleColumnRatio;
            float rightWidth = remainingWidth - middleWidth;

            Rect leftColumn = new Rect(bounds.x, bounds.y, leftWidth, bounds.height);
            Rect firstSplitter = new Rect(leftColumn.xMax, bounds.y, splitSize, bounds.height);
            Rect middleColumn = new Rect(firstSplitter.xMax, bounds.y, middleWidth, bounds.height);
            Rect secondSplitter = new Rect(middleColumn.xMax, bounds.y, splitSize, bounds.height);
            Rect rightColumn = new Rect(secondSplitter.xMax, bounds.y, rightWidth, bounds.height);

            DrawColumnStack(leftColumn, splitSize, ref _leftRowRatio, 4,
                "Game Flow", DrawFlowPanel,
                "Save", DrawSavePanel);
            DrawSplitter(firstSplitter, bounds, ref _leftColumnRatio, true, 2);
            DrawColumnStack(middleColumn, splitSize, ref _middleRowRatio, 5,
                "Camera", DrawCameraPanel,
                "Dialogue", DrawDialoguePanel);
            Rect middleAndRightBounds = new Rect(middleColumn.x, bounds.y, middleWidth + rightWidth, bounds.height);
            DrawSplitter(secondSplitter, middleAndRightBounds, ref _middleColumnRatio, true, 3);
            DrawPanel(rightColumn, "Diagnostics", DrawDiagnosticsPanel);
        }

        private void DrawColumnStack(
            Rect bounds,
            float splitSize,
            ref float ratio,
            int splitterId,
            string upperTitle,
            System.Action drawUpper,
            string lowerTitle,
            System.Action drawLower)
        {
            float upperHeight = (bounds.height - splitSize) * ratio;
            Rect upperRect = new Rect(bounds.x, bounds.y, bounds.width, upperHeight);
            Rect splitter = new Rect(bounds.x, upperRect.yMax, bounds.width, splitSize);
            Rect lowerRect = new Rect(bounds.x, splitter.yMax, bounds.width,
                bounds.height - upperHeight - splitSize);

            DrawPanel(upperRect, upperTitle, drawUpper);
            DrawSplitter(splitter, bounds, ref ratio, false, splitterId);
            DrawPanel(lowerRect, lowerTitle, drawLower);
        }

        private void DrawPanel(Rect rect, string title, System.Action drawContents)
        {
            if (rect.width <= 0f || rect.height <= 0f) return;

            GUI.BeginGroup(rect);
            GUI.Box(new Rect(0f, 0f, rect.width, rect.height), GUIContent.none, EditorStyles.helpBox);
            GUILayout.BeginArea(new Rect(7f, 5f, rect.width - 14f, rect.height - 10f));
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            _panelScrollPositions.TryGetValue(title, out Vector2 scrollPosition);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            drawContents();
            EditorGUILayout.EndScrollView();
            _panelScrollPositions[title] = scrollPosition;
            GUILayout.EndArea();
            GUI.EndGroup();
        }

        private void DrawSplitter(Rect rect, Rect bounds, ref float ratio, bool splitsColumns, int splitterId)
        {
            int controlId = GUIUtility.GetControlID(7000 + splitterId, FocusType.Passive, rect);
            EditorGUIUtility.AddCursorRect(rect, splitsColumns ? MouseCursor.ResizeHorizontal : MouseCursor.ResizeVertical);
            Event current = Event.current;

            if (current.type == EventType.MouseDown && current.button == 0 && rect.Contains(current.mousePosition))
            {
                _activeSplitterId = controlId;
                GUIUtility.hotControl = controlId;
                current.Use();
            }
            else if (current.type == EventType.MouseDrag && _activeSplitterId == controlId)
            {
                float positionOnAxis = splitsColumns ? current.mousePosition.x - bounds.x : current.mousePosition.y - bounds.y;
                float length = splitsColumns ? bounds.width : bounds.height;
                ratio = Mathf.Clamp(positionOnAxis / length, 0.18f, 0.82f);
                Repaint();
                current.Use();
            }
            else if (current.type == EventType.MouseUp && _activeSplitterId == controlId)
            {
                _activeSplitterId = 0;
                GUIUtility.hotControl = 0;
                current.Use();
            }

            EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin
                ? new Color(0.16f, 0.16f, 0.16f)
                : new Color(0.68f, 0.68f, 0.68f));
        }

        private void DrawFlowPanel()
        {
            EditorGUILayout.LabelField("State", GameFlowDebugInfo.CurrentState.ToString());
            EditorGUILayout.LabelField("Phase", GameFlowDebugInfo.CurrentPhase.ToString());
            EditorGUILayout.LabelField("Input Map", GameFlowDebugInfo.CurrentInputMap);
            EditorGUILayout.LabelField("Transition", GameFlowDebugInfo.IsTransitioning ? "In progress" : "Idle");
            EditorGUILayout.LabelField("Scenes", SceneManager.sceneCount.ToString());
            if (GUILayout.Button("Reset Transition History"))
                GameFlowDebugInfo.Reset();
        }

        private void DrawSavePanel()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to monitor saves.", MessageType.Info);
                return;
            }

            if (_messageTracer == null)
                EditorGUILayout.HelpBox("Save tracer unavailable.", MessageType.Warning);
            else
            {
                EditorGUILayout.LabelField("Checkpoint", _messageTracer.LastCheckpoint);
                EditorGUILayout.LabelField("Status", _messageTracer.LastSaveStatus);
                EditorGUILayout.LabelField("Latest Event", _messageTracer.LastSaveEvent, EditorStyles.wordWrappedLabel);
            }

            if (_saveManager == null)
            {
                EditorGUILayout.LabelField("Save Manager", "Unavailable");
                return;
            }

            string path = _saveManager.SaveFilePath;
            EditorGUILayout.LabelField("File", path, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("On Disk", File.Exists(path) ? "Yes" : "No");
            if (GUILayout.Button("Reveal Save Folder"))
                EditorUtility.RevealInFinder(Path.GetDirectoryName(path));
        }

        private void DrawCameraPanel()
        {
            EditorGUILayout.LabelField("Mode", _cameraMode.ToString());
            EditorGUILayout.LabelField("Active Camera", _activeCameraName);
            EditorGUILayout.LabelField("Confiner", _cameraConfinerBounds, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("Damping", $"{_cameraDamping.x:F2}, {_cameraDamping.y:F2}, {_cameraDamping.z:F2}");
            EditorGUILayout.LabelField("Impulse", _cameraImpulseActive ? "Active" : "Inactive");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Center")) ForceCameraSwitch(CameraMode.CenterFollow);
                if (GUILayout.Button("No Y")) ForceCameraSwitch(CameraMode.NoYFollow);
                if (GUILayout.Button("Room")) ForceCameraSwitch(CameraMode.LockedRoom);
            }

            EditorGUILayout.LabelField("Pan Offset", $"{_cameraPanOffset.x:F2}, {_cameraPanOffset.y:F2}");
            EditorGUI.BeginDisabledGroup(!Application.isPlaying || !_cameraPanAvailable);
            _cameraPanTarget = EditorGUILayout.Vector2Field("Target Offset", _cameraPanTarget);
            _cameraPanDuration = EditorGUILayout.FloatField("Duration", _cameraPanDuration);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Pan")) RequestCameraPan(_cameraPanTarget);
                if (GUILayout.Button("Reset Pan"))
                {
                    _cameraPanTarget = Vector2.zero;
                    RequestCameraPan(Vector2.zero);
                }
            }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Reset Bounds"))
                GlobalMessagePipe.GetPublisher<CameraBoundsResetRequest>().Publish(default);
        }

        private void DrawDialoguePanel()
        {
            EditorGUILayout.LabelField("State", _dialogueState);
            EditorGUILayout.LabelField("Active", _dialogueActive ? "Yes" : "No");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Force End")) InvokeDialogueMethod("EndConversation");
                if (GUILayout.Button("Advance")) InvokeDialogueMethod("Advance");
            }
        }

        private void DrawDiagnosticsPanel()
        {
            EditorGUILayout.LabelField("Message Trace", EditorStyles.boldLabel);
            if (_messageTracer == null)
                EditorGUILayout.LabelField("Tracer unavailable.");
            else
            {
                string[] entries = _messageTracer.GetRecent(8);
                for (int i = entries.Length - 1; i >= 0; i--)
                    EditorGUILayout.LabelField(entries[i], EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("Injection Validation", EditorStyles.boldLabel);
            if (!InjectionValidator.HasRun)
                EditorGUILayout.LabelField("Waiting for validation.");
            else if (InjectionValidator.Failures.Count == 0)
                EditorGUILayout.LabelField("All injected fields resolved.");
            else
            {
                foreach (var failure in InjectionValidator.Failures)
                    EditorGUILayout.LabelField(
                        $"{failure.ComponentName}.{failure.FieldName}: {failure.GameObjectName}",
                        EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void ResolveSaveServices()
        {
            if (!Application.isPlaying) return;

            try
            {
                ProjectLifetimeScope rootScope = UnityEngine.Object.FindFirstObjectByType<ProjectLifetimeScope>();
                if (rootScope == null) return;
                _messageTracer = rootScope.Container.Resolve<MessagePipeTracer>();
                _saveManager = rootScope.Container.Resolve<SaveManager>();
            }
            catch
            {
                _messageTracer = null;
                _saveManager = null;
            }
        }

        private void RefreshCameraState()
        {
            if (!Application.isPlaying) return;

            CinemachineBrain brain = UnityEngine.Object.FindFirstObjectByType<CinemachineBrain>();
            if (brain != null && brain.ActiveVirtualCamera is CinemachineCamera camera)
            {
                _activeCameraName = camera.gameObject.name;
                _cameraMode = InferCameraMode(_activeCameraName);

                CinemachinePositionComposer composer = camera.GetComponent<CinemachinePositionComposer>();
                if (composer != null)
                {
                    _cameraDamping = composer.Damping;
                    _cameraPanOffset = composer.TargetOffset;
                    _cameraPanAvailable = true;
                }
                else
                {
                    _cameraPanAvailable = false;
                }

                CinemachineConfiner2D confiner = camera.GetComponent<CinemachineConfiner2D>();
                _cameraConfinerBounds = confiner != null && confiner.BoundingShape2D != null
                    ? confiner.BoundingShape2D.bounds.ToString()
                    : "None";
            }
            else
            {
                _activeCameraName = "None";
                _cameraConfinerBounds = brain == null ? "- (no CinemachineBrain)" : "- (no active CinemachineCamera)";
                _cameraPanAvailable = false;
            }

            CinemachineImpulseSource impulse = UnityEngine.Object.FindFirstObjectByType<CinemachineImpulseSource>();
            _cameraImpulseActive = impulse != null && impulse.ImpulseDefinition.AmplitudeGain > 0f;
        }

        private void RefreshDialogueState()
        {
            if (!Application.isPlaying)
            {
                _dialogueState = "Not Playing";
                _dialogueActive = false;
                return;
            }

            DialogueController controller = UnityEngine.Object.FindFirstObjectByType<DialogueController>();
            _dialogueState = controller != null ? controller.State.ToString() : "Controller Not Found";
            _dialogueActive = controller != null && controller.IsConversationActive;
        }

        private static void ForceCameraSwitch(CameraMode mode)
        {
            GlobalMessagePipe.GetPublisher<CameraSwitchRequest>().Publish(new CameraSwitchRequest(mode));
        }

        private void RequestCameraPan(Vector2 targetOffset)
        {
            GlobalMessagePipe.GetPublisher<CameraPanRequest>().Publish(
                new CameraPanRequest(targetOffset, Mathf.Max(0.01f, _cameraPanDuration)));
        }

        private static void InvokeDialogueMethod(string methodName)
        {
            DialogueController controller = UnityEngine.Object.FindFirstObjectByType<DialogueController>();
            if (controller == null) return;

            MethodInfo method = typeof(DialogueController).GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(controller, null);
        }

        private static CameraMode InferCameraMode(string name)
        {
            if (name.Contains("NoYFollow")) return CameraMode.NoYFollow;
            if (name.Contains("LockedRoom")) return CameraMode.LockedRoom;
            return CameraMode.CenterFollow;
        }
    }
}