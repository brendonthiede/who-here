using System;
using Xunit;
using System.Collections.Generic;
using WhoIs.At.JIS.Models;
using WhoIs.At.JIS.Helpers;

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
      var whoIsCommand = SlashCommandHandler.getCommandFromString("test@courts.mi.gov");
      Assert.Equal("email", whoIsCommand.command);
      Assert.Equal(new string[] { "test@courts.mi.gov" }, whoIsCommand.parameters);
    }

    [Fact]
    public void TestNameString()
    {
      var whoIsCommand = SlashCommandHandler.getCommandFromString("Juan Valdez");
      Assert.Equal("name", whoIsCommand.command);
      Assert.Equal(new string[] { "Juan", "Valdez" }, whoIsCommand.parameters);
    }

    [Fact]
    public void TestTwoPartEmailString()
    {
      var whoIsCommand = SlashCommandHandler.getCommandFromString("email test@courts.mi.gov");
      Assert.Equal("email", whoIsCommand.command);
      Assert.Equal(new string[] { "test@courts.mi.gov" }, whoIsCommand.parameters);
    }

    [Fact]
    public void TestGetSkillList()
    {
      var cachedUsers = SlashCommandHandler.getCachedUsers($"{System.Environment.CurrentDirectory}\\..\\..\\..\\snapShot.json");
      var skills = SlashCommandHandler.getSkillsList(cachedUsers);
      Console.WriteLine(string.Join("\n", skills));
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
    public void TestGetUsersWithSkill()
    {
      var cachedUsers = SlashCommandHandler.getCachedUsers($"{System.Environment.CurrentDirectory}\\..\\..\\..\\snapShot.json");
      var users = SlashCommandHandler.getUsersWithSkill(cachedUsers, "DevOps");
      Assert.Equal(2, users.Count);
    }
  }
}
