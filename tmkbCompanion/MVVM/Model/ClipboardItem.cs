using System;

namespace tmkbCompanion.MVVM.Model
{
    public class ClipboardItem
    {
        public string Text { get; set; } = string.Empty;
        public DateTime CopiedAt { get; set; }

        public string DisplaySnippet
        {
            get
            {
                if (string.IsNullOrEmpty(Text)) return string.Empty;
                string cleanText = Text.Replace('\r', ' ').Replace('\n', ' ').Trim();
                return cleanText.Length > 80 ? cleanText.Substring(0, 77) + "..." : cleanText;
            }
        }

        public string FormattedTime => CopiedAt.ToString("hh:mm tt - MMM dd, yyyy");
    }
}
