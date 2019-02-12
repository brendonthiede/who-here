using WhoHere.Helpers;
using WhoHere.Models;
using Xunit;

namespace WhoHere.Tests
{
  public class TextToIntentParserTest
  {
    [Fact]
    public void TestEmptyString()
    {
      var whoIsContext = TextToIntentParser.getPropertyIntentFromText("");
      Assert.Equal(ActionType.Help, whoIsContext.Action);
    }

    [Fact]
    public void TestHelpString()
    {
      var whoIsContext = TextToIntentParser.getPropertyIntentFromText("help");
      Assert.Equal(ActionType.Help, whoIsContext.Action);
    }

    [Fact]
    public void TestInterestString()
    {
      var whoIsContext = TextToIntentParser.getPropertyIntentFromText("interested in skiing");
      Assert.Equal(GraphUserProperty.interests, whoIsContext.SearchProperty.graphUserProperty);
      Assert.Equal(PropertyType.List, whoIsContext.SearchProperty.PropertyType);
    }

    [Fact]
    public void TestNoiseRemoval()
    {
      Assert.Equal("life thing beauty", TextToIntentParser.removeNoiseAroundWords("life is a thing of beauty", "thing"));
      Assert.Equal("life thing beauty", TextToIntentParser.removeNoiseAroundWords("life thing beauty", "thing"));
      Assert.Equal("life thing", TextToIntentParser.removeNoiseAroundWords("life is a thing", "thing"));
    }

    [Fact]
    public void TestContextForInterest()
    {
      var whoIsContext = TextToIntentParser.getPropertyIntentFromText("Interested in bowling");
      Assert.Equal(PropertyType.List, whoIsContext.SearchProperty.PropertyType);
      Assert.Equal(GraphUserProperty.interests, whoIsContext.SearchProperty.graphUserProperty);
      Assert.Equal("bowling", whoIsContext.Filter);
      Assert.Equal(ActionType.Search, whoIsContext.Action);
      Assert.Equal(MatchType.Contains, whoIsContext.MatchType);
      whoIsContext = TextToIntentParser.getPropertyIntentFromText("has interest in bowling");
      Assert.Equal(GraphUserProperty.interests, whoIsContext.SearchProperty.graphUserProperty);
      Assert.Equal("bowling", whoIsContext.Filter);
      Assert.Equal(ActionType.Search, whoIsContext.Action);
      whoIsContext = TextToIntentParser.getPropertyIntentFromText("with an interest in bowling");
      Assert.Equal(GraphUserProperty.interests, whoIsContext.SearchProperty.graphUserProperty);
      Assert.Equal("bowling", whoIsContext.Filter);
      Assert.Equal(ActionType.Search, whoIsContext.Action);
      whoIsContext = TextToIntentParser.getPropertyIntentFromText("interests list");
      Assert.Equal(GraphUserProperty.interests, whoIsContext.SearchProperty.graphUserProperty);
      Assert.Equal(ActionType.List, whoIsContext.Action);
    }

    [Fact]
    public void TestContextForEmail()
    {
      var whoIsContext = TextToIntentParser.getPropertyIntentFromText("has an email of mail@mail.com");
      Assert.Equal(PropertyType.String, whoIsContext.SearchProperty.PropertyType);
      Assert.Equal(GraphUserProperty.userPrincipalName, whoIsContext.SearchProperty.graphUserProperty);
      Assert.Equal("mail@mail.com", whoIsContext.Filter);
      Assert.Equal(ActionType.Search, whoIsContext.Action);
    }

    [Fact]
    public void TestContextForEmailAlone()
    {
      var whoIsContext = TextToIntentParser.getPropertyIntentFromText("mail@mail.com");
      Assert.Equal(PropertyType.String, whoIsContext.SearchProperty.PropertyType);
      Assert.Equal(GraphUserProperty.userPrincipalName, whoIsContext.SearchProperty.graphUserProperty);
      Assert.Equal("mail@mail.com", whoIsContext.Filter);
      Assert.Equal(ActionType.Search, whoIsContext.Action);
    }

    [Fact]
    public void TestDefaultsToName()
    {
      var whoIsContext = TextToIntentParser.getPropertyIntentFromText("Don Juan");
      Assert.Equal(PropertyType.String, whoIsContext.SearchProperty.PropertyType);
      Assert.Equal(GraphUserProperty.displayName, whoIsContext.SearchProperty.graphUserProperty);
      Assert.Equal("Don Juan", whoIsContext.Filter);
      Assert.Equal(ActionType.Search, whoIsContext.Action);
    }
  }
}
