using WhoHere.Models;
using Xunit;

namespace WhoHere.Tests
{
  public class SlackResponseTest
  {
    [Fact]
    public void TestResponseTypeIsAlwaysEphemeral ()
    {
      SlackResponse response = new SlackResponse();
      Assert.Equal("ephemeral", response.response_type);
    }
  }
}
