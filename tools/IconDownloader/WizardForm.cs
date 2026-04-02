using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using IconDownloader.Downloaders;
using IconDownloader.Models;
using IconDownloader.Pages;

namespace IconDownloader;

public class WizardForm : Form
{
    private readonly Panel _contentPanel;
    private readonly Button _btnBack;
    private readonly Button _btnNext;
    private readonly Button _btnCancel;

    private readonly SelectionPage _selectionPage;
    private readonly ProgressPage _progressPage;
    private readonly CompletePage _completePage;

    private readonly List<UserControl> _pages;
    private int _currentPage;
    private CancellationTokenSource? _cts;

    private readonly string _outputDir;

    public WizardForm()
    {
        Text = "WinNotifier Icon Downloader";
        Size = new Size(560, 360);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        _outputDir = Path.GetDirectoryName(Environment.ProcessPath) ?? ".";

        _contentPanel = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(560, 270),
            Dock = DockStyle.Top
        };

        var bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 50
        };

        _btnBack = new Button { Text = "< 戻る", Size = new Size(90, 30), Location = new Point(240, 10), Enabled = false };
        _btnNext = new Button { Text = "次へ >", Size = new Size(90, 30), Location = new Point(340, 10) };
        _btnCancel = new Button { Text = "キャンセル", Size = new Size(90, 30), Location = new Point(440, 10) };

        _btnBack.Click += (_, _) => ShowPage(0);
        _btnNext.Click += OnNextClick;
        _btnCancel.Click += (_, _) => Close();

        bottomPanel.Controls.AddRange([_btnBack, _btnNext, _btnCancel]);

        _selectionPage = new SelectionPage();
        _progressPage = new ProgressPage();
        _completePage = new CompletePage();

        _pages = [_selectionPage, _progressPage, _completePage];

        Controls.Add(_contentPanel);
        Controls.Add(bottomPanel);

        ShowPage(0);
    }

    private void ShowPage(int index)
    {
        _currentPage = index;
        _contentPanel.Controls.Clear();
        _contentPanel.Controls.Add(_pages[index]);

        _btnBack.Enabled = false;
        _btnCancel.Enabled = true;

        switch (index)
        {
            case 0:
                _btnNext.Text = "ダウンロード";
                _btnNext.Enabled = true;
                break;
            case 1:
                _btnNext.Text = "次へ >";
                _btnNext.Enabled = false;
                _btnCancel.Enabled = false;
                break;
            case 2:
                _btnNext.Text = "完了";
                _btnNext.Enabled = true;
                _btnBack.Enabled = true;
                _btnBack.Text = "< 選択に戻る";
                _btnCancel.Enabled = false;
                break;
        }
    }

    private async void OnNextClick(object? sender, EventArgs e)
    {
        switch (_currentPage)
        {
            case 0:
                if (!_selectionPage.AnySelected)
                {
                    MessageBox.Show("アイコンセットを1つ以上選択してください。",
                        Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var downloaders = BuildDownloaders();
                if (!ConfirmOverwrites(downloaders))
                    return;
                ShowPage(1);
                await RunDownloadsAsync(downloaders);
                break;
            case 2:
                Close();
                break;
        }
    }

    private List<IIconSetDownloader> BuildDownloaders()
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("IconDownloader/1.0");

        var downloaders = new List<IIconSetDownloader>();
        if (_selectionPage.TwemojiSelected)
            downloaders.Add(new TwemojiDownloader(httpClient));
        if (_selectionPage.YaruSelected)
            downloaders.Add(new YaruDownloader(httpClient));
        if (_selectionPage.GemojiSelected)
            downloaders.Add(new GemojiDownloader(httpClient));
        return downloaders;
    }

    private bool ConfirmOverwrites(List<IIconSetDownloader> downloaders)
    {
        var rejected = new List<IIconSetDownloader>();

        foreach (var downloader in downloaders)
        {
            var metaPath = downloader.ExistingMetaPath(_outputDir);
            if (!File.Exists(metaPath))
                continue;

            try
            {
                var json = File.ReadAllText(metaPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var version = root.GetProperty("source").GetProperty("version").GetString() ?? "?";
                var downloadedAt = root.TryGetProperty("downloadedAt", out var dt) ? dt.GetString() ?? "?" : "?";

                var message =
                    $"{downloader.Name} は既にダウンロード済みです。上書きしますか？\n\n" +
                    $"現在のバージョン: {version}\n" +
                    $"ダウンロード日時: {downloadedAt}\n" +
                    $"新しいバージョン: {downloader.NewVersion}";

                var result = MessageBox.Show(message, Text,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.No)
                    rejected.Add(downloader);
            }
            catch
            {
                // meta.json の読み取りに失敗した場合は上書きを許可
            }
        }

        foreach (var r in rejected)
        {
            downloaders.Remove(r);
            switch (r.Name)
            {
                case "Twemoji": _selectionPage.TwemojiSelected = false; break;
                case "Yaru": _selectionPage.YaruSelected = false; break;
                case "gemoji": _selectionPage.GemojiSelected = false; break;
            }
        }

        return downloaders.Count > 0;
    }

    private async Task RunDownloadsAsync(List<IIconSetDownloader> downloaders)
    {
        _cts = new CancellationTokenSource();

        var progress = new Progress<DownloadProgress>(p => _progressPage.UpdateProgress(p));
        var results = new List<DownloadResult>();

        try
        {
            foreach (var downloader in downloaders)
            {
                var result = await Task.Run(
                    () => downloader.DownloadAsync(_outputDir, progress, _cts.Token),
                    _cts.Token);
                results.Add(result);
            }

            UpdateConfigJson(downloaders);
            _completePage.SetResults(results, _outputDir);
            ShowPage(2);
        }
        catch (OperationCanceledException)
        {
            MessageBox.Show("ダウンロードがキャンセルされました。",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            ShowPage(0);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"ダウンロード中にエラーが発生しました:\n{ex.Message}",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            ShowPage(0);
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
        }
    }

    private void UpdateConfigJson(List<IIconSetDownloader> downloaders)
    {
        var configPath = Path.Combine(_outputDir, "config.json");

        JsonNode? root;
        if (File.Exists(configPath))
        {
            var json = File.ReadAllText(configPath);
            root = JsonNode.Parse(json) ?? new JsonObject();
        }
        else
        {
            root = new JsonObject();
        }

        var icons = root["icons"]?.AsObject() ?? new JsonObject();
        root["icons"] = icons;

        foreach (var downloader in downloaders)
        {
            var key = downloader.Category == "theme" ? "themes" : "mappings";
            var array = icons[key]?.AsArray() ?? new JsonArray();
            icons[key] = array;

            var existing = array.Select(n => n?.GetValue<string>()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!existing.Contains(downloader.Name))
                array.Add(downloader.Name);
        }

        var options = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        File.WriteAllText(configPath, root.ToJsonString(options));
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _cts?.Cancel();
        base.OnFormClosing(e);
    }
}
