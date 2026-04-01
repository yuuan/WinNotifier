using System.ComponentModel;

namespace IconDownloader.Pages;

public class SelectionPage : UserControl
{
    private readonly CheckBox _chkTwemoji;
    private readonly CheckBox _chkYaru;
    private readonly CheckBox _chkGemoji;

    public SelectionPage()
    {
        var lblThemes = new Label
        {
            Text = "アイコンセット",
            Font = new Font(Font.FontFamily, 10f, FontStyle.Bold),
            Location = new Point(20, 20),
            AutoSize = true
        };

        _chkTwemoji = new CheckBox
        {
            Text = "Twemoji  (jdecked/twemoji) — 絵文字 PNG 72x72, ~3800 アイコン",
            Location = new Point(40, 50),
            AutoSize = true,
            Checked = true
        };

        _chkYaru = new CheckBox
        {
            Text = "Yaru  (ubuntu/yaru) — Ubuntu デスクトップアイコン (SVG → PNG 変換)",
            Location = new Point(40, 80),
            AutoSize = true,
            Checked = false
        };

        var lblMappings = new Label
        {
            Text = "アイコン名マッピング",
            Font = new Font(Font.FontFamily, 10f, FontStyle.Bold),
            Location = new Point(20, 120),
            AutoSize = true
        };

        _chkGemoji = new CheckBox
        {
            Text = "gemoji  (github/gemoji) — GitHub 絵文字ショートコード → Unicode マッピング",
            Location = new Point(40, 150),
            AutoSize = true,
            Checked = true
        };

        Controls.Add(lblThemes);
        Controls.Add(_chkTwemoji);
        Controls.Add(_chkYaru);
        Controls.Add(lblMappings);
        Controls.Add(_chkGemoji);

        Dock = DockStyle.Fill;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool TwemojiSelected { get => _chkTwemoji.Checked; set => _chkTwemoji.Checked = value; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool YaruSelected { get => _chkYaru.Checked; set => _chkYaru.Checked = value; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool GemojiSelected { get => _chkGemoji.Checked; set => _chkGemoji.Checked = value; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool AnySelected => _chkTwemoji.Checked || _chkYaru.Checked || _chkGemoji.Checked;
}
