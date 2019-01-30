using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using WhoIs.At.JIS.Models;

namespace WhoIs.At.JIS.Helpers
{

  public class SlashCommandHandler
  {
    #region Private Properties and Constants
    private readonly GraphHandler _graphHandler;
    private readonly string _emailDomain;
    private readonly IConfiguration _slackConfiguration;
    private static List<string> VALID_COMMANDS = new List<string> { "help", "email", "name", "jobtitlelist", "withjobtitle", "skillslist", "withskill", "interestslist", "withinterest", "projectslist", "withproject" };
    #endregion

    public SlashCommandHandler(IConfiguration configuration)
    {
      try
      {
        _emailDomain = configuration.GetSection("graph")["domain"];
        _slackConfiguration = configuration.GetSection("slack");
      }
      catch (System.NullReferenceException)
      {
        _emailDomain = null;
        _slackConfiguration = null;
      }
      _graphHandler = new GraphHandler(configuration);
    }

    public void setGraphCacheLocation(string cacheLocation) {
      _graphHandler.cacheLocation = cacheLocation;
    }

    #region Validators and Converters
    public static bool isValidCommand(string command)
    {
      return VALID_COMMANDS.Contains(command);
    }

    public static bool isValidEmail(string email)
    {
      return isValidEmail(email, null);
    }

    public static bool isValidEmail(string email, string domain)
    {
      try
      {
        var addr = new System.Net.Mail.MailAddress(email);
        if (String.IsNullOrEmpty(domain))
        {
          return addr.Address == email;
        }
        else
        {
          return addr.Address == email && addr.Host == domain;
        }
      }
      catch
      {
        return false;
      }
    }
    public static WhoIsCommand getCommandFromString(string text)
    {
      var whoIsCommand = new WhoIsCommand();
      whoIsCommand.command = "help";
      var paramArray = text.Split(' ');
      if (paramArray.Length > 0 && paramArray[0].Length > 0)
      {
        whoIsCommand.command = paramArray[0].ToLower();
        if (isValidCommand(whoIsCommand.command))
        {
          var tmp = new List<string>(text.Split(' '));
          tmp.RemoveAt(0);
          paramArray = tmp.ToArray();
        }
        else if (isValidEmail(whoIsCommand.command))
        {
          paramArray = new string[] { whoIsCommand.command };
          whoIsCommand.command = "email";
        }
        else
        {
          whoIsCommand.command = "name";
        }
      }
      whoIsCommand.parameters = string.Join(' ', paramArray);
      return whoIsCommand;
    }
    #endregion

    #region Formatters
    public static Attachment formatUserForSlack(GraphUser user)
    {
      Attachment userAttachment = new Attachment();
      userAttachment.pretext = $"*{user.displayName}*";
      userAttachment.title = user.jobTitle;
      userAttachment.text = user.userPrincipalName;
      if (!string.IsNullOrEmpty(user.aboutMe))
      {
        userAttachment.text += $"\n>{user.aboutMe}";
      }
      var projects = new List<string>(user.projects);
      if (projects.Count > 0)
      {
        userAttachment.text += $"\n_Projects:_ {string.Join(", ", user.projects)}";
      }
      var skills = new List<string>(user.skills);
      if (skills.Count > 0)
      {
        userAttachment.text += $"\n_Skills:_ {string.Join(", ", user.skills)}";
      }
      var interests = new List<string>(user.interests);
      if (interests.Count > 0)
      {
        userAttachment.text += $"\n_Interests:_ {string.Join(", ", user.interests)}";
      }
      return userAttachment;
    }

    public static List<Attachment> formatUserListForSlack(List<GraphUser> users)
    {
      return users.Select(user => formatUserForSlack(user)).ToList();
    }
    #endregion

    #region Getters
    public string getHelpMessage()
    {
      return @"Available commands:
  `help`: showsthis message"
  + $"\n  `email <email@{_emailDomain}>`: shows information for the given email address"
  + @"\n`name <search text>`: shows matches where the display name (formatted as <first> <last>) starts with the search text
  `jobtitlelist`: shows a list of all job titles listed for users
  `projectslist`: shows a list of all projects that any users have identified
  `withproject <project>`: shows all users that have identified the given project in their profile
  `interestslist`: shows a list of all interests that any users have identified
  `withinterest <interest>`: shows all users that have identified the given interest in their profile
  `skillslist`: shows a list of all skills that any users have identified
  `withskill <skill>`: shows all users that have identified the given skill in their profile
>Make sure your profile is up to date at https://delve-gcc.office.com";
    }

    public List<GraphUser> getJobTitleWithName(string title)
    {
      return _graphHandler.getCachedUsers().Where(user => user.jobTitle.Contains(title, StringComparison.InvariantCultureIgnoreCase)).ToList();
    }

    public GraphUser getUserWithEmail(string email)
    {
      var matches = _graphHandler.getCachedUsers().Where(user => user.userPrincipalName.Equals(email, StringComparison.InvariantCultureIgnoreCase)).ToList();
      if (matches.Count != 1)
      {
        return null;
      }
      return matches[0];
    }

    public List<string> getUniqueValuesForListProperty(string propertyName)
    {
      var dict = new Dictionary<string, string>();
      foreach (var graphUser in _graphHandler.getCachedUsers())
      {
        foreach (var value in (List<string>)graphUser.GetType().GetProperty(propertyName).GetValue(graphUser))
        {
          dict[value] = value;
        }
      }
      return dict.Values.ToList();
    }

    public List<string> getUniqueValuesForStringProperty(string propertyName)
    {
      var dict = new Dictionary<string, string>();
      foreach (var graphUser in _graphHandler.getCachedUsers())
      {
        var value = (string)graphUser.GetType().GetProperty(propertyName).GetValue(graphUser, null);
        if (!string.IsNullOrEmpty(value))
        {
          dict[value] = value;
        }
      }
      return dict.Values.ToList();
    }

    public List<GraphUser> getUsersWithStringProperty(string propertyName, string propertyValue)
    {
      return _graphHandler.getCachedUsers().Where(user => ((string)user.GetType().GetProperty(propertyName).GetValue(user)).Contains(propertyValue, StringComparison.InvariantCultureIgnoreCase)).ToList();
    }

    public List<GraphUser> getUsersWithListProperty(string propertyName, string propertyValue)
    {
      return _graphHandler.getCachedUsers().Where(user => ((List<string>)user.GetType().GetProperty(propertyName).GetValue(user)).Contains(propertyValue, StringComparer.InvariantCultureIgnoreCase)).ToList();
    }
    #endregion
  }
}