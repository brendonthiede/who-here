using WhoIs.At.JIS.Helpers;
using WhoIs.At.JIS.Models;
using Xunit;

namespace WhoIs.At.JIS.Tests
{
  public class TextToIntentParserTest
  {
    [Fact]
    public void TestEmptyString()
    {
      var whoIsContext = TextToIntentParser.getPropertyIntentFromText("");
      Assert.Equal(ActionType.Help, whoIsContext.action);
    }

    [Fact]
    public void TestHelpString()
    {
      var whoIsContext = TextToIntentParser.getPropertyIntentFromText("help");
      Assert.Equal(ActionType.Help, whoIsContext.action);
    }

    [Fact]
    public void TestInterestString()
    {
      var whoIsContext = TextToIntentParser.getPropertyIntentFromText("interested in skiing");
      Assert.Equal(GraphUserProperty.interests, whoIsContext.property.graphUserProperty);
      Assert.Equal(PropertyType.List, whoIsContext.property.propertyType);
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
      Assert.Equal(PropertyType.List, whoIsContext.property.propertyType);
      Assert.Equal(GraphUserProperty.interests, whoIsContext.property.graphUserProperty);
      Assert.Equal("bowling", whoIsContext.filter);
      Assert.Equal(ActionType.Search, whoIsContext.action);
      Assert.Equal(MatchType.Contains, whoIsContext.matchType);
      whoIsContext = TextToIntentParser.getPropertyIntentFromText("has interest in bowling");
      Assert.Equal(GraphUserProperty.interests, whoIsContext.property.graphUserProperty);
      Assert.Equal("bowling", whoIsContext.filter);
      Assert.Equal(ActionType.Search, whoIsContext.action);
      whoIsContext = TextToIntentParser.getPropertyIntentFromText("with an interest in bowling");
      Assert.Equal(GraphUserProperty.interests, whoIsContext.property.graphUserProperty);
      Assert.Equal("bowling", whoIsContext.filter);
      Assert.Equal(ActionType.Search, whoIsContext.action);
      whoIsContext = TextToIntentParser.getPropertyIntentFromText("interests list");
      Assert.Equal(GraphUserProperty.interests, whoIsContext.property.graphUserProperty);
      Assert.Equal(ActionType.List, whoIsContext.action);
    }

    [Fact]
    public void TestContextForEmail()
    {
      var whoIsContext = TextToIntentParser.getPropertyIntentFromText("has email as mail@mail.com");
      Assert.Equal(PropertyType.String, whoIsContext.property.propertyType);
      Assert.Equal(GraphUserProperty.userPrincipalName, whoIsContext.property.graphUserProperty);
      Assert.Equal("mail@mail.com", whoIsContext.filter);
      Assert.Equal(ActionType.Search, whoIsContext.action);
    }

    [Fact]
    public void TestContextForEmailAlone()
    {
      var whoIsContext = TextToIntentParser.getPropertyIntentFromText("mail@mail.com");
      Assert.Equal(PropertyType.String, whoIsContext.property.propertyType);
      Assert.Equal(GraphUserProperty.userPrincipalName, whoIsContext.property.graphUserProperty);
      Assert.Equal("mail@mail.com", whoIsContext.filter);
      Assert.Equal(ActionType.Search, whoIsContext.action);
    }

    [Fact]
    public void TestDefaultsToName()
    {
      var whoIsContext = TextToIntentParser.getPropertyIntentFromText("Don Juan");
      Assert.Equal(PropertyType.String, whoIsContext.property.propertyType);
      Assert.Equal(GraphUserProperty.displayName, whoIsContext.property.graphUserProperty);
      Assert.Equal("Don Juan", whoIsContext.filter);
      Assert.Equal(ActionType.Search, whoIsContext.action);
    }
  }
}
