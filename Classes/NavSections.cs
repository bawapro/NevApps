namespace NevApps.Classes;

/// <summary>
/// Top-level shell sections in app-bar order. Swipe +1 is next (left swipe); -1 is previous.
/// Both ends wrap. Detail routes (edit/view/…) are not sections.
/// </summary>
public static class NavSections
{
    public readonly record struct Features(
        bool Expense,
        bool Mileage,
        bool Shenshai,
        bool Steps,
        bool Admin);

    public const int Next = 1;
    public const int Previous = -1;

    public const string Home = "/";
    public const string Expense = "/expense-tracker/add";
    public const string Mileage = "/mileage-tracker/add";
    public const string Shenshai = "/shenshai/view";
    public const string Steps = "/steps-tracker/add";
    public const string Settings = "/view-settings";
    public const string Support = "/view-support";

    public static readonly IReadOnlyList<string> Canonical =
    [
        Home,
        Expense,
        Mileage,
        Shenshai,
        Steps,
        Settings,
        Support
    ];

    public static bool IsShellRoute(string relativePath)
    {
        var path = "/" + Normalize(relativePath);
        if (path.Length > 1)
            path = path.TrimEnd('/');

        foreach (var known in Canonical)
        {
            if (string.Equals(known, path, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static IReadOnlyList<string> VisiblePages(Features features, string? currentPath = null)
    {
        var pages = new List<string>(8);

        foreach (var path in Canonical)
        {
            if (IsEnabled(path, features))
                pages.Add(path);
        }

        var current = CanonicalShell(currentPath);
        if (current is not null && !pages.Exists(p => string.Equals(p, current, StringComparison.OrdinalIgnoreCase)))
        {
            var insertAt = CanonicalIndex(current);
            var i = 0;
            while (i < pages.Count && CanonicalIndex(pages[i]) < insertAt)
                i++;
            pages.Insert(i, current);
        }

        return pages;
    }

    public static string? Neighbor(string relativePath, int direction, Features features)
    {
        if (direction is not (Next or Previous))
            return null;

        var pages = VisiblePages(features, relativePath);
        if (pages.Count < 2)
            return null;

        var index = IndexOf(relativePath, pages);
        if (index < 0)
            return null;

        var count = pages.Count;
        var next = ((index + direction) % count + count) % count;
        return pages[next];
    }

    public static int IndexOf(string relativePath, IReadOnlyList<string> pages)
    {
        var path = CanonicalShell(relativePath);
        if (path is null)
            return -1;

        for (var i = 0; i < pages.Count; i++)
        {
            if (string.Equals(pages[i], path, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Circular offset of <paramref name="index"/> relative to <paramref name="selected"/>.
    /// Neighbors are -1 and +1 so the last page sits left of the first.
    /// Two pages are ambiguous both ways; <paramref name="dragX"/> picks the incoming side.
    /// </summary>
    public static int Slot(int index, int selected, int count, double dragX = 0)
    {
        if (count <= 1)
            return 0;
        if (index == selected)
            return 0;

        var slot = ((index - selected) % count + count) % count;
        if (count == 2)
            return dragX > 0 ? -1 : 1;
        if (slot > count / 2)
            slot -= count;
        return slot;
    }

    public static string? CanonicalShell(string? relativePath)
    {
        var path = "/" + Normalize(relativePath);
        if (path.Length > 1)
            path = path.TrimEnd('/');

        foreach (var known in Canonical)
        {
            if (string.Equals(known, path, StringComparison.OrdinalIgnoreCase))
                return known;
        }

        return null;
    }

    private static bool IsEnabled(string path, Features features) => path switch
    {
        Home => features.Expense || features.Mileage || features.Shenshai,
        Expense => features.Expense,
        Mileage => features.Mileage,
        Shenshai => features.Shenshai,
        Steps => features.Steps,
        Settings => true,
        Support => features.Admin,
        _ => false
    };

    private static int CanonicalIndex(string path)
    {
        for (var i = 0; i < Canonical.Count; i++)
        {
            if (string.Equals(Canonical[i], path, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return Canonical.Count;
    }

    private static string Normalize(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return string.Empty;

        var path = relativePath.Trim();
        var cut = path.IndexOfAny(['?', '#']);
        if (cut >= 0)
            path = path[..cut];
        return path.Trim('/');
    }
}
