using System.Collections.Generic;

namespace WhoIs.At.JIS.Models
{
  public class WhoIsCommand
  {
    public string command { get; set; }
    public string[] parameters { get; set; }
  }
}