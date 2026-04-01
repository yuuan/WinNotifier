using IconDownloader.Models;

namespace IconDownloader.Pages;

public class ProgressPage : UserControl
{
    private readonly Label _lblStatus;
    private readonly Label _lblDetail;
    private readonly ProgressBar _progressBar;

    public ProgressPage()
    {
        _lblStatus = new Label
        {
            Text = "準備中...",
            Location = new Point(20, 20),
            AutoSize = true
        };

        _progressBar = new ProgressBar
        {
            Location = new Point(20, 50),
            Size = new Size(480, 25),
            Style = ProgressBarStyle.Continuous
        };

        _lblDetail = new Label
        {
            Text = "",
            Location = new Point(20, 85),
            AutoSize = true,
            ForeColor = SystemColors.GrayText
        };

        Controls.Add(_lblStatus);
        Controls.Add(_progressBar);
        Controls.Add(_lblDetail);

        Dock = DockStyle.Fill;
    }

    public void UpdateProgress(DownloadProgress p)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdateProgress(p));
            return;
        }

        _lblStatus.Text = p.Status;
        _lblDetail.Text = p.Detail;

        if (p.Total > 0)
        {
            _progressBar.Style = ProgressBarStyle.Continuous;
            _progressBar.Maximum = p.Total;
            _progressBar.Value = Math.Min(p.Current, p.Total);
        }
        else
        {
            _progressBar.Style = ProgressBarStyle.Marquee;
        }
    }
}
