namespace CanonPrintBridge.Services;

/// <summary>
/// Parses a Windows-style page-selection string ("1,3,5-12") into an ordered,
/// de-duplicated, ascending list of 1-based page numbers, clamped to the document.
/// Commas are the canonical separator; semicolons and whitespace are accepted too.
/// Ranges use '-', open at either end ("5-" = 5..end, "-3" = 1..3).
/// An empty/whitespace input means "all pages".
/// </summary>
public static class PageSelection
{
    private static readonly char[] Separators = { ',', ';', ' ', '\t' };

    /// <summary>
    /// Expands the selection against a document of <paramref name="total"/> pages.
    /// Returns null for "all pages" (empty input); otherwise the ordered page list
    /// (possibly empty if nothing valid was selected).
    /// </summary>
    public static IReadOnlyList<int>? Parse(string? input, int total)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        var set = new SortedSet<int>();
        foreach (var raw in input.Split(Separators, StringSplitOptions.RemoveEmptyEntries))
        {
            var tok = raw.Trim();
            if (tok.Length == 0) continue;

            var dash = tok.IndexOf('-');
            if (dash < 0)
            {
                if (int.TryParse(tok, out var n)) AddClamped(set, n, n, total);
                continue;
            }

            var loStr = tok[..dash].Trim();
            var hiStr = tok[(dash + 1)..].Trim();
            var lo = loStr.Length == 0 ? 1 : (int.TryParse(loStr, out var l) ? l : 0);
            var hi = hiStr.Length == 0 ? total : (int.TryParse(hiStr, out var h) ? h : 0);
            if (lo == 0 || hi == 0) continue;              // unparseable endpoint -> skip token
            if (lo > hi) (lo, hi) = (hi, lo);
            AddClamped(set, lo, hi, total);
        }
        return set.ToList();
    }

    private static void AddClamped(SortedSet<int> set, int lo, int hi, int total)
    {
        lo = Math.Max(1, lo);
        if (total > 0) hi = Math.Min(total, hi);
        for (var i = lo; i <= hi; i++) set.Add(i);
    }

    /// <summary>
    /// Normalizes a raw field to a SumatraPDF-friendly range ("1-4,7"):
    /// semicolons/whitespace become commas. Order is preserved (SumatraPDF sorts).
    /// </summary>
    public static string NormalizeForPrint(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        var parts = input.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(",", parts.Select(p => p.Trim()).Where(p => p.Length > 0));
    }
}
