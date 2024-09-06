namespace SSW.Rules.AzFuncs.Domain;

public class RuleHistoryData
{
    public string file { get; set; }
    public string? title { get; set; }
    public string? uri { get; set; }
    public bool? isArchived { get; set; }
    public string lastUpdated { get; set; }
    public string lastUpdatedBy { get; set; }
    public string lastUpdatedByEmail { get; set; }
    public string created { get; set; }
    public string createdBy { get; set; }
    public string createdByEmail { get; set; }
}
