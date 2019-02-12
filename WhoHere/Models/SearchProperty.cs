using System.Collections.Generic;

namespace WhoHere.Models
{
  public enum GraphUserProperty
  {
    displayName,
    jobTitle,
    userPrincipalName,
    aboutMe,
    interests,
    skills,
    projects
  }

  public enum PropertyType
  {
    String,
    List
  }

  public class SearchProperty
  {
    public GraphUserProperty graphUserProperty { get; set; }
    public string Singular { get; set; }
    public PropertyType PropertyType { get; set; }
    public string Plural { get; set; }
    public List<string> Tenses { get; set; }
  }
}