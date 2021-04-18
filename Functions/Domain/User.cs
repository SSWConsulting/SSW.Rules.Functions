using System;
using AzureGems.Repository.Abstractions;

namespace SSW.Rules.Functions
{
    public class User : BaseEntity
    {
        public string UserId { get; set; }
        public string CommentsUserId { get; set; }
        public int OrganisationId { get; set; }
    }
}