using IconDownloader.Downloaders;

namespace IconDownloader.Pages;

public class CompletePage : UserControl
{
    private readonly Label _lblTitle;
    private readonly ListBox _lstResults;
    private readonly LinkLabel _lnkOpen;
    private string? _outputDir;

    public CompletePage()
    {
        _lblTitle = new Label
        {
            Text = "ダウンロード完了",
            Font = new Font(Font.FontFamily, 10f, FontStyle.Bold),
            Location = new Point(20, 20),
            AutoSize = true
        };

        _lstResults = new ListBox
        {
            Location = new Point(20, 55),
            Size = new Size(480, 120),
            BorderStyle = BorderStyle.FixedSingle
        };

        _lnkOpen = new LinkLabel
        {
            Text = "アイコンフォルダを開く",
            Location = new Point(20, 185),
            AutoSize = true
        };
        _lnkOpen.LinkClicked += (_, _) =>
        {
            if (_outputDir is not null)
            {
                var iconsDir = Path.Combine(_outputDir, "icons", "themes");
                if (Directory.Exists(iconsDir))
                    System.Diagnostics.Process.Start("explorer.exe", iconsDir);
            }
        };

        Controls.Add(_lblTitle);
        Controls.Add(_lstResults);
        Controls.Add(_lnkOpen);

        Dock = DockStyle.Fill;
    }

    public void SetResults(List<DownloadResult> results, string outputDir)
    {
        _outputDir = outputDir;
        _lstResults.Items.Clear();
        foreach (var r in results)
        {
            var text = $"{r.ThemeName}: {r.IconCount} アイコンをダウンロード";
            if (r.SkippedCount > 0)
                text += $" ({r.SkippedCount} スキップ)";
            _lstResults.Items.Add(text);
        }
    }
}
