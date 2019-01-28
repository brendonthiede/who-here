using Microsoft.Extensions.Configuration;
using System;
using WhoIs.At.JIS.Helpers;
using Xunit;

namespace WhoIs.At.JIS.Tests
{
  public class SlashCommandHandlerTest
  {
    [Fact]
    public void TestEmptyString()
    {
      var whoIsCommand = SlashCommandHandler.getCommandFromString("");
      Assert.Equal("help", whoIsCommand.command);
    }

    [Fact]
    public void TestHelpString()
    {
      var whoIsCommand = SlashCommandHandler.getCommandFromString("Help");
      Assert.Equal("help", whoIsCommand.command);
    }

    [Fact]
    public void TestEmailString()
    {
      var whoIsCommand = SlashCommandHandler.getCommandFromString("test@mail.com");
      Assert.Equal("email", whoIsCommand.command);
      Assert.Equal("test@mail.com", whoIsCommand.parameters);
    }

    [Fact]
    public void TestNameString()
    {
      var whoIsCommand = SlashCommandHandler.getCommandFromString("That Guy");
      Assert.Equal("name", whoIsCommand.command);
      Assert.Equal("That Guy", whoIsCommand.parameters);
    }

    [Fact]
    public void TestTwoPartEmailString()
    {
      var whoIsCommand = SlashCommandHandler.getCommandFromString("email test@mail.com");
      Assert.Equal("email", whoIsCommand.command);
      Assert.Equal("test@mail.com", whoIsCommand.parameters);
    }

    [Fact]
    public void TestGetSkillList()
    {
      SlashCommandHandler slashCommandHandler = new SlashCommandHandler(null);
      GraphHandler graphHandler = new GraphHandler(null);
      var cachedUsers = graphHandler.getCachedUsers($"{System.Environment.CurrentDirectory}\\..\\..\\..\\testdata.json");
      var skills = slashCommandHandler.getUniqueValuesForListProperty(cachedUsers, "skills");
      Assert.Equal(9, skills.Count);
      Assert.Contains("Database Administration", skills);
      Assert.Contains("DevOps", skills);
      Assert.Contains("JavaScript", skills);
      Assert.Contains("Functional Programming", skills);
      Assert.Contains("agile", skills);
      Assert.Contains("Scrum", skills);
      Assert.Contains("Kanban", skills);
      Assert.Contains("SAFe", skills);
      Assert.Contains("Troubleshooting", skills);
    }

    [Fact]
    public void TestGetJobTitleList()
    {
      SlashCommandHandler slashCommandHandler = new SlashCommandHandler(null);
      GraphHandler graphHandler = new GraphHandler(null);
      var cachedUsers = graphHandler.getCachedUsers($"{System.Environment.CurrentDirectory}\\..\\..\\..\\testdata.json");
      var titles = slashCommandHandler.getUniqueValuesForStringProperty(cachedUsers, "jobTitle");
      Assert.Equal(5, titles.Count);
      Assert.Contains("Code Slinger", titles);
      Assert.Contains("Database Administrator", titles);
      Assert.Contains("Facilities", titles);
      Assert.Contains("DevOps Evangelist", titles);
      Assert.Contains("Support Technician", titles);
    }

    [Fact]
    public void TestGetUsersWithSkill()
    {
      SlashCommandHandler slashCommandHandler = new SlashCommandHandler(null);
      GraphHandler graphHandler = new GraphHandler(null);
      var cachedUsers = graphHandler.getCachedUsers($"{System.Environment.CurrentDirectory}\\..\\..\\..\\testdata.json");
      var users = slashCommandHandler.getUsersWithListProperty(cachedUsers, "skills", "DevOps");
      Assert.Equal(2, users.Count);
    }
  }
}
