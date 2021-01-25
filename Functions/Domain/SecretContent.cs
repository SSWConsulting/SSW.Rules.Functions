using System;
using AzureGems.Repository.Abstractions;

namespace SSW.Rules.Functions
{
    public class SecretContent : BaseEntity
    {
        public string OrganisationId { get; set; }
        public string Content { get; set; }
    }
}