namespace CampusX.Options;

public class UniversityEmailOptions
{
    public const string SectionName = "UniversityEmail";
    public List<string> ApprovedDomains { get; set; } = [];
}
