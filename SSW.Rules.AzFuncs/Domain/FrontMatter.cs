using YamlDotNet.Serialization;

namespace SSW.Rules.AzFuncs.Domain;

public class FrontMatter
{
    public string Type { get; set; }
    
    [YamlMember(Alias = "archivedreason", ApplyNamingConventions = false)]
    
    public string ArchivedReason { get; set; }

    public string Title { get; set; }
    public string Guid { get; set; }
    public string Uri { get; set; }
    public DateTime Created { get; set; }
    public List<Author> Authors { get; set; }
    public List<string> Related { get; set; }
    public List<string> Redirects { get; set; }

    [YamlMember(Alias = "SeoDescription", ApplyNamingConventions = false)]
    public string SeoDescription { get; set; }


    public FrontMatter()
    {
        Authors = new List<Author>();
        Related = new List<string>();
        Redirects = new List<string>();
    }
}

public class Author
{
    public string Title { get; set; }
    public string? Url { get; set; }
    public string? Img { get; set; }
    
    [YamlMember(Alias = "noimage", ApplyNamingConventions = false)]
    public bool? NoImage { get; set; }
}