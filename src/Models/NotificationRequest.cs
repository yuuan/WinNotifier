namespace WinNotifier.Models;

public class NotificationRequest
{
    public string? Title { get; set; }
    public string? Message { get; set; }
    public string? From { get; set; }
    public string? Icon { get; set; }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Title))
            errors.Add("Title is required.");

        if (string.IsNullOrWhiteSpace(Message))
            errors.Add("Message is required.");

        return errors;
    }
}
