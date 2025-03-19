using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;

namespace Mohcka.SassToUss.Editor
{
    public class SassToUssTool : EditorWindow
    {
        private Process sassToUssProcess;
        private string sassDirectory = "Assets/UI/Styles";
        private bool isConverting = false;

        // UI Elements
        private TextField directoryField;
        private Button convertButton;
        private ScrollView logScrollView;
        private string logOutput = "";

        [MenuItem("Tools/SASS to USS")]
        public static void ShowWindow()
        {
            SassToUssTool wnd = GetWindow<SassToUssTool>();
            wnd.titleContent = new GUIContent("SASS to USS");
            wnd.minSize = new Vector2(400, 300);
        }

        public void CreateGUI()
        {
            // Load and use the UXML file (optional - you can create one later)
            // var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/SassToUssTool.uxml");
            // if (visualTree != null)
            //     visualTree.CloneTree(rootVisualElement);
            // else
            CreateUIElements();

            // Register event handlers
            convertButton.clicked += ToggleConverting;
        }

        private void CreateUIElements()
        {
            VisualElement root = rootVisualElement;

            // Add title
            Label titleLabel = new Label("SASS to USS Converter");
            titleLabel.style.fontSize = 18;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 10;
            root.Add(titleLabel);

            // Create a horizontal container for directory field and browse button
            VisualElement directoryContainer = new VisualElement();
            directoryContainer.style.flexDirection = FlexDirection.Row;
            directoryContainer.style.marginBottom = 10;
            root.Add(directoryContainer);

            // Add directory field to the container
            directoryField = new TextField("SCSS Directory:");
            directoryField.value = sassDirectory;
            directoryField.RegisterValueChangedCallback(evt => sassDirectory = evt.newValue);
            directoryField.style.flexGrow = 1; // Make it take available space
            directoryContainer.Add(directoryField);

            // Add browse button
            Button browseButton = new Button(BrowseForDirectory);
            browseButton.text = "Browse...";
            browseButton.style.marginLeft = 5;
            directoryContainer.Add(browseButton);

            // Add button
            convertButton = new Button();
            convertButton.text = "Start Converting";
            convertButton.style.height = 30;
            convertButton.style.marginBottom = 15;
            root.Add(convertButton);

            // Add log output section
            Label logLabel = new Label("Log Output:");
            root.Add(logLabel);

            logScrollView = new ScrollView();   
            logScrollView.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            logScrollView.style.flexGrow = 1;
            logScrollView.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            root.Add(logScrollView);

            UpdateLogView();
        }

        private void BrowseForDirectory()
        {
            // Get project and assets paths
            string projectPath = Path.GetDirectoryName(Application.dataPath);
            string assetsPath = Application.dataPath;
            
            // Use current value as starting point if possible
            string initialPath = string.IsNullOrEmpty(sassDirectory) ? 
                assetsPath : 
                Path.GetFullPath(Path.Combine(projectPath, sassDirectory));
            
            // Open folder browser dialog
            string selectedPath = EditorUtility.OpenFolderPanel("Select SCSS Directory", initialPath, "");
            
            if (!string.IsNullOrEmpty(selectedPath))
            {
                // Check if the selected path is within the Assets folder
                if (!selectedPath.StartsWith(assetsPath))
                {
                    // Show error - path must be inside Assets folder
                    EditorUtility.DisplayDialog("Invalid Directory", 
                        "The selected directory must be within your project's Assets folder.", "OK");
                    AddToLog("ERROR: Selected directory must be within the Assets folder.");
                    UnityEngine.Debug.Log($"Selected path is not within Assets folder: {selectedPath} vs {assetsPath}");
                    return;
                }
                
                // Convert absolute path to be relative to Assets folder
                string relativePath = "Assets" + selectedPath.Substring(assetsPath.Length).Replace("\\", "/");
                
                // Update both the field and the backing variable
                sassDirectory = relativePath;
                directoryField.value = sassDirectory;
                
                AddToLog($"Directory set to: {sassDirectory}");
            }
        }

        private void ToggleConverting()
        {
            if (!isConverting)
                StartConverting();
            else
                StopConverting();
        }

        private void StartConverting()
        {
            if (isConverting) return;

            // Find our package location using a marker script in the Editor folder
            string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            string packageRootPath = Path.GetDirectoryName(Path.GetDirectoryName(scriptPath));
            
            // Path is now relative to package location
            string executableRelativePath = "Tools/sass-to-uss/sass-to-uss.exe";
            string executablePath = Path.Combine(packageRootPath, executableRelativePath);
            executablePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", executablePath));
            
            // Ensure executable exists
            if (!File.Exists(executablePath))
            {
                UnityEngine.Debug.LogError($"Executable not found: {executablePath}");
                AddToLog($"ERROR: Executable not found: {executablePath}");
                return;
            }
            
            // Create a proper absolute path that works with external processes
            string projectPath = Path.GetDirectoryName(Application.dataPath);
            string fullDirectoryPath = Path.GetFullPath(Path.Combine(projectPath, sassDirectory));
            
            // Convert backslashes to forward slashes for Deno
            string denoPath = fullDirectoryPath.Replace("\\", "/");
            
            // Log the path to help debug
            UnityEngine.Debug.Log($"Converting directory: {denoPath}");
            
            // Check if directory exists
            if (!Directory.Exists(fullDirectoryPath))
            {
                try {
                    Directory.CreateDirectory(fullDirectoryPath);
                    UnityEngine.Debug.Log($"Created directory: {fullDirectoryPath}");
                    AddToLog($"Created directory: {fullDirectoryPath}");
                } catch (Exception ex) {
                    UnityEngine.Debug.LogError($"Failed to create directory: {ex.Message}");
                    AddToLog($"ERROR: Failed to create directory: {ex.Message}");
                    return;
                }
            }

            sassToUssProcess = new Process();
            sassToUssProcess.StartInfo.FileName = executablePath;
            sassToUssProcess.StartInfo.Arguments = $"\"{denoPath}\""; // Quote the path
            sassToUssProcess.StartInfo.UseShellExecute = false;
            sassToUssProcess.StartInfo.RedirectStandardOutput = true;
            sassToUssProcess.StartInfo.RedirectStandardError = true;
            sassToUssProcess.StartInfo.CreateNoWindow = true;

            sassToUssProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    UnityEngine.Debug.Log($"[SASS to USS] {e.Data}");
                    AddToLog(e.Data);
                }
            };

            sassToUssProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    UnityEngine.Debug.LogError($"[SASS to USS] {e.Data}");
                    AddToLog("ERROR: " + e.Data);
                }
            };

            sassToUssProcess.Start();
            sassToUssProcess.BeginOutputReadLine();
            sassToUssProcess.BeginErrorReadLine();

            isConverting = true;
            convertButton.text = "Stop Converting";
            AddToLog($"Started converting {fullDirectoryPath}");
        }

        private void StopConverting()
        {
            if (!isConverting) return;

            if (sassToUssProcess != null && !sassToUssProcess.HasExited)
            {
                sassToUssProcess.Kill();
                sassToUssProcess.Dispose();
                sassToUssProcess = null;
            }

            isConverting = false;
            convertButton.text = "Start Converting";
            AddToLog("Stopped converting");
        }

        private void AddToLog(string message)
        {
            // Update the log text
            logOutput += message + "\n";

            // Use Dispatch to update UI from a different thread if needed
            if (logScrollView != null)
            {
                rootVisualElement.schedule.Execute(() =>
                {
                    UpdateLogView();
                });
            }
        }

        private void UpdateLogView()
        {
            if (logScrollView == null) return;

            logScrollView.Clear();

            var textElement = new TextField();
            textElement.multiline = true;
            textElement.value = logOutput;
            textElement.isReadOnly = true;
            textElement.style.whiteSpace = WhiteSpace.Normal;
            textElement.style.flexGrow = 1;
            textElement.style.width = new StyleLength(new Length(100, LengthUnit.Percent));

            logScrollView.Add(textElement);

            // Scroll to bottom
            logScrollView.scrollOffset = new Vector2(0, float.MaxValue);
        }

        void OnDestroy()
        {
            StopConverting();
        }
    }
}