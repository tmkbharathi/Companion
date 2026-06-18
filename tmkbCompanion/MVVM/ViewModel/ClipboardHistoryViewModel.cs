using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using tmkbCompanion.MVVM.Core;
using tmkbCompanion.MVVM.Model;

namespace tmkbCompanion.MVVM.ViewModel
{
    public class ClipboardHistoryViewModel : ViewModelBase
    {
        private const string HistoryFileName = "clipboard_history.json";
        private const int MaxHistoryCount = 50;

        public ObservableCollection<ClipboardItem> ClipboardItems { get; } = new ObservableCollection<ClipboardItem>();

        public ICommand ClearHistoryCommand { get; }
        public ICommand DeleteItemCommand { get; }
        public ICommand CopyItemCommand { get; }

        public ClipboardHistoryViewModel()
        {
            ClearHistoryCommand = new RelayCommand(ClearHistory);
            DeleteItemCommand = new RelayCommand(DeleteItem);
            CopyItemCommand = new RelayCommand(CopyItem);

            LoadHistory();
        }

        private string HistoryFilePath => Path.Combine(AppPaths.BaseDataDirectory, HistoryFileName);

        public void AddClipboardItem(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            string trimmedText = text.Trim();

            // Check if this is a duplicate of the most recent item
            var firstItem = ClipboardItems.FirstOrDefault();
            if (firstItem != null && firstItem.Text.Trim() == trimmedText)
            {
                return;
            }

            // Run on UI thread to ensure collection changes are thread-safe
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                var newItem = new ClipboardItem
                {
                    Text = text,
                    CopiedAt = DateTime.Now
                };

                // Check again on the UI thread to prevent race condition duplicates
                var firstItemUI = ClipboardItems.FirstOrDefault();
                if (firstItemUI != null && firstItemUI.Text.Trim() == trimmedText)
                {
                    return;
                }

                ClipboardItems.Insert(0, newItem);

                while (ClipboardItems.Count > MaxHistoryCount)
                {
                    ClipboardItems.RemoveAt(ClipboardItems.Count - 1);
                }

                SaveHistory();
            }));
        }

        private void CopyItem(object? parameter)
        {
            if (parameter is ClipboardItem item)
            {
                try
                {
                    System.Windows.Clipboard.SetText(item.Text);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to set clipboard text: {ex.Message}");
                }
            }
        }

        private void DeleteItem(object? parameter)
        {
            if (parameter is ClipboardItem item)
            {
                ClipboardItems.Remove(item);
                SaveHistory();
            }
        }

        public void ClearHistory()
        {
            ClipboardItems.Clear();
            SaveHistory();
        }

        private void LoadHistory()
        {
            try
            {
                string path = HistoryFilePath;
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var items = JsonSerializer.Deserialize<ClipboardItem[]>(json);
                    if (items != null)
                    {
                        ClipboardItems.Clear();
                        foreach (var item in items)
                        {
                            ClipboardItems.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load clipboard history: {ex.Message}");
            }
        }

        private void SaveHistory()
        {
            try
            {
                string path = HistoryFilePath;
                var itemsArray = ClipboardItems.ToArray();
                string json = JsonSerializer.Serialize(itemsArray, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save clipboard history: {ex.Message}");
            }
        }
    }
}
