using System.Collections.Generic;

namespace WhoIs.At.JIS.Models
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
    public string text { get; set; }
    public SearchProperty property { get; set; }
    public ActionType action { get; set; }
    public MatchType matchType { get; set; }
    public string filter { get; set; }
  }
}