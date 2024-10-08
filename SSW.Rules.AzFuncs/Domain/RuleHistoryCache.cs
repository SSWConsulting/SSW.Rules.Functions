using AzureGems.Repository.Abstractions;

namespace SSW.Rules.AzFuncs.Domain;

public class RuleHistoryCache : BaseEntity
{
    public string MarkdownFilePath { get; set; }
    public string? Title { get; set; }
    public string? Uri { get; set; }
    public bool? IsArchived { get; set; }
    public DateTime ChangedAtDateTime { get; set; }
    public string ChangedByDisplayName { get; set; }
    public string ChangedByEmail { get; set; }
    public DateTime CreatedAtDateTime { get; set; }
    public string CreatedByDisplayName { get; set; }
    public string CreatedByEmail { get; set; }
}
