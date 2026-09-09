using System.Text.RegularExpressions;
using Gloria.Domain.Common;
using Gloria.Domain.CommissionRules.Engine;

namespace Gloria.Domain.CommissionRules;

/// <summary>
/// Filter that decides which sales a commission rule applies to. Every field is optional;
/// a null field means "any". The product-code pattern supports the '%' wildcard
/// (e.g. "SPA_%", "%_MSJ%").
/// </summary>
public sealed class RuleScope : ValueObject
{
    public string? DepartmentCode { get; }
    public string? ProductCodePattern { get; }
    public string? SourceSystemCode { get; }

    public RuleScope(string? departmentCode, string? productCodePattern, string? sourceSystemCode)
    {
        DepartmentCode = NullIfBlank(departmentCode)?.ToUpperInvariant();
        ProductCodePattern = NullIfBlank(productCodePattern)?.ToUpperInvariant();
        SourceSystemCode = NullIfBlank(sourceSystemCode)?.ToUpperInvariant();
    }

    public static RuleScope Any => new(null, null, null);

    public bool Matches(CommissionInput input) =>
        (DepartmentCode is null || string.Equals(DepartmentCode, input.DepartmentCode, StringComparison.OrdinalIgnoreCase)) &&
        (SourceSystemCode is null || string.Equals(SourceSystemCode, input.SourceSystemCode, StringComparison.OrdinalIgnoreCase)) &&
        (ProductCodePattern is null || ProductCodeMatches(input.ProductCode));

    private bool ProductCodeMatches(string productCode)
    {
        if (!ProductCodePattern!.Contains('%'))
            return string.Equals(ProductCodePattern, productCode, StringComparison.OrdinalIgnoreCase);

        var regex = "^" + Regex.Escape(ProductCodePattern).Replace("%", ".*") + "$";
        return Regex.IsMatch(productCode, regex, RegexOptions.IgnoreCase);
    }

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return DepartmentCode;
        yield return ProductCodePattern;
        yield return SourceSystemCode;
    }
}
