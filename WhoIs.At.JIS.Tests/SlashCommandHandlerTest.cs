using WhoIs.At.JIS.Helpers;
using Xunit;

namespace WhoIs.At.JIS.Tests
{
  public class SlashCommandHandlerTest
  {
    private readonly string TEST_DATA_PATH = $"{System.Environment.CurrentDirectory}\\..\\..\\..\\testdata.json";
    private readonly SlashCommandHandler slashCommandHandler;

    public SlashCommandHandlerTest()
    {
      slashCommandHandler = new SlashCommandHandler(null);
      slashCommandHandler.setGraphCacheLocation(TEST_DATA_PATH);
    }

    [Fact]
    public void TestEmptyString()
    {
      var response = slashCommandHandler.EvaluateSlackCommand("");
      var helpText = slashCommandHandler.getHelpMessage();
      Assert.Equal(helpText, response.text);
    }

    [Fact]
    public void TestHelpString()
    {
      var response = slashCommandHandler.EvaluateSlackCommand("help");
      var helpText = slashCommandHandler.getHelpMessage();
      Assert.Equal(helpText, response.text);
    }

    [Fact]
    public void TestUnrecognizedTextDefaultingToName()
    {
      var response = slashCommandHandler.EvaluateSlackCommand("Frank Abagnale Jr.");
      Assert.Equal("No users were found with name Frank Abagnale Jr.", response.text);
    }

    [Fact]
    public void TestEmailWithNoMatch()
    {
      var response = slashCommandHandler.EvaluateSlackCommand("noreply@mail.com");
      Assert.Equal("No users were found with email noreply@mail.com", response.text);
    }

    [Fact]
    public void TestEmailWithMatch()
    {
      var response = slashCommandHandler.EvaluateSlackCommand("CarlDamico@mail.com");
      Assert.Equal("ephemeral", response.response_type);
      Assert.Null(response.text);
      Assert.Single(response.attachments);
      Assert.Equal("*Carl Damico*", response.attachments[0].pretext);
      Assert.Equal("Support Technician", response.attachments[0].title);
      Assert.StartsWith("CarlDamico@mail.com", response.attachments[0].text);
    }

    [Fact]
    public void TestInterestedInRunning()
    {
      var response = slashCommandHandler.EvaluateSlackCommand("interested in running");
      Assert.Equal(2, response.attachments.Count);
      Assert.Equal("*Clyde Reynolds*", response.attachments[0].pretext);
      Assert.StartsWith("ClydeReynolds@mail.com", response.attachments[0].text);
      Assert.Equal("*Larry Gonzalez*", response.attachments[1].pretext);
      Assert.StartsWith("LarryGonzalez@mail.com", response.attachments[1].text);
    }
  }
}
