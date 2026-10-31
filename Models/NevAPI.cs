namespace NevApps.Models;

public class FeedbackResponse
{
    public int Count { get; set; }
    public List<Dictionary<string, object>> Items { get; set; } = new();
}

public class IssueResponse
{
    public int Count { get; set; }
    public List<Dictionary<string, object>> Items { get; set; } = new();
}

public class BroadcastResponse
{
    public int Count { get; set; }

    public List<List<Dictionary<string, object>>> Items { get; set; } = new();
}