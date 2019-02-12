using System.Collections.Generic;

namespace WhoHere.Models
{
  public enum ActionType
  {
    Help,
    Search,
    List
  }

  public enum MatchType
  {
    Equals,
    Contains,
    StartsWith,
    EndsWith
  }

  public class WhoIsContext
  {
    public string Text { get; set; }
    public string FoundTense { get; set; }
    public SearchProperty SearchProperty { get; set; }
    public ActionType Action { get; set; }
    public MatchType MatchType { get; set; }
    public string Filter { get; set; }
  }
}