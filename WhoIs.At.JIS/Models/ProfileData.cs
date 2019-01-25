namespace WhoIs.At.JIS.Models
{
  public class ProfileData
  {
    public string displayName { get; set; }
    public string jobTitle { get; set; }
    public string userPrincipalName { get; set; }
    public string[] interests { get; set; }
    public string [] skills { get; set; }
    public string [] pastProjects { get; set; }
  }
}