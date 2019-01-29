using System.Collections.Generic;

namespace WhoIs.At.JIS.Models
{
  public class SlackResponse
  {
    public readonly string response_type = "ephemeral";
    public string text { get; set; }
    public List<Attachment> attachments { get; set; }
  }

  public class Attachment
  {
    public string pretext { get; set; }
    public string title { get; set; }
    public string text { get; set; }
    public readonly List<string> mrkdwn_in = new List<string> { "text", "pretext" };
  }
}