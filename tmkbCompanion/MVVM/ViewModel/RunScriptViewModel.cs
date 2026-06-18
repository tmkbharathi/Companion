using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using tmkbCompanion.MVVM.Core;

namespace tmkbCompanion.MVVM.ViewModel
{
    public class RunScriptViewModel : ViewModelBase
    {
        private readonly SettingsViewModel _settingsVM;
        private string _consoleOutput = string.Empty;
        private bool _isRunning;
        private string _statusText = "Idle";
        private Process? _activeProcess;

        public SettingsViewModel SettingsVM => _settingsVM;

        public string TerminalType
        {
            get => _settingsVM.TerminalType;
            set
            {
                if (_settingsVM.TerminalType != value)
                {
                    _settingsVM.TerminalType = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ScriptContent
        {
            get => _settingsVM.ScriptContent;
            set
            {
                if (_settingsVM.ScriptContent != value)
                {
                    _settingsVM.ScriptContent = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool RunInExternalWindow
        {
            get => _settingsVM.RunInExternalWindow;
            set
            {
                if (_settingsVM.RunInExternalWindow != value)
                {
                    _settingsVM.RunInExternalWindow = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ConsoleOutput
        {
            get => _consoleOutput;
            set => SetProperty(ref _consoleOutput, value);
        }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    // Raise can execute changes
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public ICommand RunScriptCommand { get; }
        public ICommand StopScriptCommand { get; }
        public ICommand ClearOutputCommand { get; }

        public RunScriptViewModel(SettingsViewModel settingsVM)
        {
            _settingsVM = settingsVM ?? throw new ArgumentNullException(nameof(settingsVM));

            RunScriptCommand = new RelayCommand(async () => await RunScriptAsync(), () => !IsRunning && !string.IsNullOrWhiteSpace(ScriptContent));
            StopScriptCommand = new RelayCommand(StopScript, () => IsRunning);
            ClearOutputCommand = new RelayCommand(() => ConsoleOutput = string.Empty);
        }

        public async Task RunScriptAsync()
        {
            if (IsRunning) return;

            IsRunning = true;
            StatusText = "Running...";
            AppendOutput($"[{DateTime.Now:HH:mm:ss}] Starting execution using {TerminalType}...");

            string tempDir = Path.Combine(AppPaths.BaseDataDirectory, "Temp");
            string ext = TerminalType == "PowerShell" ? ".ps1" : (TerminalType == "Command Prompt" ? ".bat" : ".sh");
            string tempFile = Path.Combine(tempDir, $"run_script_{Guid.NewGuid().ToString().Substring(0, 8)}{ext}");

            try
            {
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }

                // Write the script to the temp file
                await File.WriteAllTextAsync(tempFile, ScriptContent);

                await Task.Run(() =>
                {
                    var startInfo = new ProcessStartInfo();

                    if (TerminalType == "PowerShell")
                    {
                        startInfo.FileName = "powershell.exe";
                        if (RunInExternalWindow)
                        {
                            startInfo.Arguments = $"-NoExit -ExecutionPolicy Bypass -File \"{tempFile}\"";
                            startInfo.UseShellExecute = true;
                            startInfo.CreateNoWindow = false;
                        }
                        else
                        {
                            startInfo.Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{tempFile}\"";
                            startInfo.UseShellExecute = false;
                            startInfo.CreateNoWindow = true;
                            startInfo.RedirectStandardOutput = true;
                            startInfo.RedirectStandardError = true;
                            startInfo.RedirectStandardInput = true;
                        }
                    }
                    else if (TerminalType == "Command Prompt")
                    {
                        startInfo.FileName = "cmd.exe";
                        if (RunInExternalWindow)
                        {
                            startInfo.Arguments = $"/k \"{tempFile}\"";
                            startInfo.UseShellExecute = true;
                            startInfo.CreateNoWindow = false;
                        }
                        else
                        {
                            startInfo.Arguments = $"/c \"{tempFile}\"";
                            startInfo.UseShellExecute = false;
                            startInfo.CreateNoWindow = true;
                            startInfo.RedirectStandardOutput = true;
                            startInfo.RedirectStandardError = true;
                            startInfo.RedirectStandardInput = true;
                        }
                    }
                    else // Git Bash
                    {
                        string gitBashPath = @"C:\Program Files\Git\bin\bash.exe";
                        if (!File.Exists(gitBashPath))
                        {
                            gitBashPath = @"C:\Program Files\Git\git-bash.exe";
                        }
                        if (!File.Exists(gitBashPath))
                        {
                            gitBashPath = "bash.exe"; // Fallback to PATH
                        }

                        if (RunInExternalWindow)
                        {
                            // Launch command prompt that calls bash, keeping it open
                            startInfo.FileName = "cmd.exe";
                            startInfo.Arguments = $"/k \"\"{gitBashPath}\" \"{tempFile}\"\"";
                            startInfo.UseShellExecute = true;
                            startInfo.CreateNoWindow = false;
                        }
                        else
                        {
                            startInfo.FileName = gitBashPath;
                            startInfo.Arguments = $"\"{tempFile}\"";
                            startInfo.UseShellExecute = false;
                            startInfo.CreateNoWindow = true;
                            startInfo.RedirectStandardOutput = true;
                            startInfo.RedirectStandardError = true;
                            startInfo.RedirectStandardInput = true;
                        }
                    }

                    _activeProcess = new Process { StartInfo = startInfo };

                    if (!RunInExternalWindow)
                    {
                        _activeProcess.OutputDataReceived += (sender, args) =>
                        {
                            if (args.Data != null) AppendOutput(args.Data);
                        };
                        _activeProcess.ErrorDataReceived += (sender, args) =>
                        {
                            if (args.Data != null) AppendOutput($"[ERROR] {args.Data}");
                        };
                    }

                    _activeProcess.Start();

                    if (!RunInExternalWindow)
                    {
                        _activeProcess.BeginOutputReadLine();
                        _activeProcess.BeginErrorReadLine();
                        _activeProcess.WaitForExit();
                        
                        int exitCode = _activeProcess.ExitCode;
                        UpdateStatusAndIsRunning(exitCode == 0 ? "Success" : $"Failed (Exit Code {exitCode})", false);
                        AppendOutput($"[{DateTime.Now:HH:mm:ss}] Execution finished with exit code {exitCode}.");
                    }
                    else
                    {
                        UpdateStatusAndIsRunning("Running in external window", false);
                        AppendOutput($"[{DateTime.Now:HH:mm:ss}] External window opened.");
                    }
                });
            }
            catch (Exception ex)
            {
                AppendOutput($"[EXCEPTION] {ex.Message}");
                UpdateStatusAndIsRunning("Exception occurred", false);
            }
            finally
            {
                _activeProcess = null;
                // Delete temp file after some time or immediately if we don't need it
                try
                {
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }
                }
                catch
                {
                    // Ignore temp file deletion errors (e.g. file locked)
                }
            }
        }

        private void StopScript()
        {
            if (!IsRunning) return;

            try
            {
                if (_activeProcess != null && !_activeProcess.HasExited)
                {
                    _activeProcess.Kill(true); // Kill process tree
                    AppendOutput($"[{DateTime.Now:HH:mm:ss}] Execution stopped by user.");
                    StatusText = "Stopped";
                }
            }
            catch (Exception ex)
            {
                AppendOutput($"[ERROR STOPPING PROCESS] {ex.Message}");
            }
            finally
            {
                IsRunning = false;
            }
        }

        private void AppendOutput(string text)
        {
            if (text == null) return;
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                ConsoleOutput += text + Environment.NewLine;
            }));
        }

        private void UpdateStatusAndIsRunning(string status, bool isRunning)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                StatusText = status;
                IsRunning = isRunning;
            }));
        }
    }
}
