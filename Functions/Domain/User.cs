using System;
using AzureGems.Repository.Abstractions;

namespace SSW.Rules.Functions
{
    public class User : BaseEntity
    {
        public string UserId { get; set; }
        public string OrganisationId { get; set; }
    }
}