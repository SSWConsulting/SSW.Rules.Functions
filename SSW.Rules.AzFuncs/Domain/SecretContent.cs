using AzureGems.Repository.Abstractions;

namespace SSW.Rules.AzFuncs.Domain;

public class SecretContent : BaseEntity
{
    public string OrganisationId { get; set; }
    public string Content { get; set; }
}

