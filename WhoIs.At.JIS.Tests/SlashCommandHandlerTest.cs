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
    public void TestGetAnkush()
    {
      Assert.Equal("Ankush Parab - ParabA@courts.mi.gov", SlashCommandHandler.getMsGraphResultsForName("Ankush Pa")[0]);
    }

    [Fact]
    public void TestGetAnkushByEmail()
    {
      Assert.Equal("Ankush Parab - ParabA@courts.mi.gov", SlashCommandHandler.getMsGraphResultsForEmail("ParabA@courts.mi.gov"));
    }
  }
}
