using System;
using AzureGems.Repository.Abstractions;

namespace SSW.Rules.Functions {
    public class LikeDislike : BaseEntity {
        public ReactionType Type { get; set; }
        public string RuleGuid { get; set; }
        public string UserId { get; set; }
    }

    public enum ReactionType {
        Like,
        Dislike
    }
}