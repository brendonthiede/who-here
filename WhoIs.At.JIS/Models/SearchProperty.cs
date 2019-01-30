using System.Collections.Generic;

namespace WhoIs.At.JIS.Models
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
    public string singular { get; set; }
    public PropertyType propertyType { get; set; }
    public string plural { get; set; }
    public List<string> tenses { get; set; }
  }
}