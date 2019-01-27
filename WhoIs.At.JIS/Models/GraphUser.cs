using System.Collections.Generic;

namespace WhoIs.At.JIS.Models
{
  public class GraphUser
  {
    public string displayName { get; set; }
    public string jobTitle { get; set; }
    public string userPrincipalName { get; set; }
    public string aboutMe { get; set; }
    public IEnumerable<string> interests { get; set; }
    public IEnumerable<string> skills { get; set; }
    public IEnumerable<string> pastProjects { get; set; }
  }
}