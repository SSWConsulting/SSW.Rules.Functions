using AzureGems.Repository.Abstractions;

namespace SSW.Rules.AzFuncs.Domain;

public class Reaction : BaseEntity
{
    public ReactionType Type { get; set; }
    public string RuleGuid { get; set; }
    public string UserId { get; set; }
}

public enum ReactionType
{
    SuperDislike,
    Dislike,
    Like,
    SuperLike,
}