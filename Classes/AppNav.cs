using SwipeDirection = MudBlazor.SwipeDirection;

namespace NevApps.Classes;

/// <summary>
/// Ordered top-bar destinations and URI matching used by swipe navigation.
/// </summary>
internal static class AppNav
{
    public const string Home = "/";
    public const string Expense = "/expense-tracker/add";
    public const string Mileage = "/mileage-tracker/add";
    public const string Shenshai = "/shenshai/view";
    public const string Steps = "/steps-tracker/add";
    public const string Settings = "/view-settings";

    public static IReadOnlyList<string> GetDestinations(
        bool expenseEnabled,
        bool mileageEnabled,
        bool shenshaiEnabled,
        bool stepsEnabled)
    {
        var destinations = new List<string>(6);
        if (expenseEnabled || mileageEnabled || shenshaiEnabled)
            destinations.Add(Home);
        if (expenseEnabled)
            destinations.Add(Expense);
        if (mileageEnabled)
            destinations.Add(Mileage);
        if (shenshaiEnabled)
            destinations.Add(Shenshai);
        if (stepsEnabled)
            destinations.Add(Steps);
        destinations.Add(Settings);
        return destinations;
    }

    public static string NormalizePath(string? relativeUri)
    {
        if (string.IsNullOrWhiteSpace(relativeUri))
            return Home;

        var path = relativeUri.Split('?', 2)[0].Split('#', 2)[0].Trim();
        if (string.IsNullOrEmpty(path) || path == "/")
            return Home;

        return path.StartsWith('/') ? path : "/" + path;
    }

    public static string ResolveDestination(string? relativeUri)
    {
        var path = NormalizePath(relativeUri);
        if (path.StartsWith("/expense-tracker", StringComparison.OrdinalIgnoreCase))
            return Expense;
        if (path.StartsWith("/mileage-tracker", StringComparison.OrdinalIgnoreCase))
            return Mileage;
        if (path.StartsWith("/shenshai", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/reminders", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/sunrisesunset", StringComparison.OrdinalIgnoreCase))
            return Shenshai;
        if (path.StartsWith("/steps-tracker", StringComparison.OrdinalIgnoreCase))
            return Steps;
        if (path.StartsWith("/view-settings", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/settings", StringComparison.OrdinalIgnoreCase))
            return Settings;
        return path == Home ? Home : path;
    }

    public static bool IsActive(string? relativeUri, string destinationPath) =>
        string.Equals(
            ResolveDestination(relativeUri),
            NormalizePath(destinationPath),
            StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the next or previous top-bar destination for a horizontal swipe, wrapping at both ends.
    /// Right-to-left advances; left-to-right goes back. Returns null when there is only one destination.
    /// </summary>
    public static string? GetSwipeTarget(
        string? relativeUri,
        SwipeDirection direction,
        IReadOnlyList<string> destinations)
    {
        if (destinations.Count <= 1)
            return null;

        var step = direction switch
        {
            SwipeDirection.RightToLeft => 1,
            SwipeDirection.LeftToRight => -1,
            _ => 0
        };
        if (step == 0)
            return null;

        var current = ResolveDestination(relativeUri);
        var index = IndexOfPath(destinations, current);
        if (index < 0)
            return null;

        var nextIndex = (index + step + destinations.Count) % destinations.Count;
        var target = destinations[nextIndex];
        return string.Equals(target, current, StringComparison.OrdinalIgnoreCase) ? null : target;
    }

    private static int IndexOfPath(IReadOnlyList<string> destinations, string path)
    {
        for (var i = 0; i < destinations.Count; i++)
        {
            if (string.Equals(destinations[i], path, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }
}

/// <summary>
/// Lets the current page consume a horizontal swipe (for example Home dashboard tabs)
/// before the layout navigates to another app section.
/// </summary>
internal sealed class SwipeCoordinator
{
    public Func<SwipeDirection, bool>? TryHandlePageSwipe { get; set; }
}
