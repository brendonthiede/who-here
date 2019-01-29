using WhoIs.At.JIS.Models;
using Xunit;

namespace WhoIs.At.JIS.Tests
{
  public class SlackRespoonseTest
  {
    [Fact]
    public void TestResponseTypeIsAlwaysEphemeral ()
    {
      SlackResponse response = new SlackResponse();
      Assert.Equal("ephemeral", response.response_type);
    }
  }
}
