using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WhoIs.At.JIS.Models;

namespace WhoIs.At.JIS.Helpers
{

  public class SlashCommandHandler
  {
    #region Private Properties
    private readonly GraphHandler _graphHandler;
    private readonly string _emailDomain;
    private readonly IConfiguration _slackConfiguration;
    private static List<string> VALID_COMMANDS = new List<string> { "debug", "help", "email", "name", "skillslist", "withskill", "interestslist", "withinterest", "projectslist", "withproject" };
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
    public string formatUserForSlack(GraphUser user)
    {
      var profileData = $"*{user.displayName}*\n{user.jobTitle}\n{user.userPrincipalName}";
      if (!string.IsNullOrEmpty(user.aboutMe))
      {
        profileData += $"\n>{user.aboutMe}";
      }
      var projects = new List<string>(user.projects);
      if (projects.Count > 0)
      {
        profileData += $"\n_Projects:_ {string.Join(", ", user.projects)}";
      }
      var skills = new List<string>(user.skills);
      if (skills.Count > 0)
      {
        profileData += $"\n_Skills:_ {string.Join(", ", user.skills)}";
      }
      var interests = new List<string>(user.interests);
      if (interests.Count > 0)
      {
        profileData += $"\n_Interests:_ {string.Join(", ", user.interests)}";
      }
      return profileData;

    }

    public string formatUserListForSlack(List<GraphUser> users)
    {
      List<string> details = users.Select(user => formatUserForSlack(user)).ToList();
      return string.Join("\n================\n", details);
    }
    #endregion

    #region Getters
    public string getHelpMessage()
    {
      return @"Available commands:
  `help`: showsthis message"
  + $"`email <email@{_emailDomain}>`: shows information for the given email address"
  + @"`name <search text>`: shows matches where the display name (formatted as <first> <last>) starts with the search text
  `projectslist`: shows a list of all projects that any users have identified
  `withproject <project>`: shows all users that have identified the given project in their profile
  `interestslist`: shows a list of all interests that any users have identified
  `withinterest <interest>`: shows all users that have identified the given interest in their profile
  `skillslist`: shows a list of all skills that any users have identified
  `withskill <skill>`: shows all users that have identified the given skill in their profile";
    }

    public List<GraphUser> getUsersWithName(string name)
    {
      return getUsersWithName(_graphHandler.getCachedUsers(), name);
    }

    public List<GraphUser> getUsersWithName(List<GraphUser> graphUsers, string name)
    {
      return graphUsers.Where(user => user.displayName.Contains(name, StringComparison.InvariantCultureIgnoreCase)).ToList();
    }

    public List<GraphUser> getJobTitleWithName(string title)
    {
      return getJobTitleWithName(_graphHandler.getCachedUsers(), title);
    }

    public List<GraphUser> getJobTitleWithName(List<GraphUser> graphUsers, string title)
    {
      return graphUsers.Where(user => user.jobTitle.Contains(title, StringComparison.InvariantCultureIgnoreCase)).ToList();
    }

    public GraphUser getUserWithEmail(string email)
    {
      return getUserWithEmail(_graphHandler.getCachedUsers(), email);
    }

    public GraphUser getUserWithEmail(List<GraphUser> graphUsers, string email)
    {
      var matches = graphUsers.Where(user => user.userPrincipalName.Equals(email, StringComparison.InvariantCultureIgnoreCase)).ToList();
      if (matches.Count != 1)
      {
        return null;
      }
      return matches[0];
    }

    public List<string> getUniqueValuesForListProperty(string propertyName)
    {
      return getUniqueValuesForListProperty(_graphHandler.getCachedUsers(), propertyName);
    }

    public List<string> getUniqueValuesForListProperty(List<GraphUser> graphUsers, string propertyName)
    {
      var dict = new Dictionary<string, string>();
      foreach (var graphUser in graphUsers)
      {
        foreach (var value in (List<string>)graphUser.GetType().GetProperty(propertyName).GetValue(graphUser))
        {
          dict[value] = value;
        }
      }
      return dict.Values.ToList();
    }

    public List<GraphUser> getUsersWithInterest(string interest)
    {
      return getUsersWithInterest(_graphHandler.getCachedUsers(), interest);
    }

    public List<GraphUser> getUsersWithInterest(List<GraphUser> graphUsers, string interest)
    {
      return graphUsers.Where(user => user.interests.Contains(interest, StringComparer.InvariantCultureIgnoreCase)).ToList();
    }

    public List<GraphUser> getUsersWithSkill(string skill)
    {
      return getUsersWithSkill(_graphHandler.getCachedUsers(), skill);
    }

    public List<GraphUser> getUsersWithSkill(List<GraphUser> graphUsers, string skill)
    {
      return graphUsers.Where(user => user.skills.Contains(skill, StringComparer.InvariantCultureIgnoreCase)).ToList();
    }

    public List<GraphUser> getUsersWithProject(string project)
    {
      return getUsersWithProject(_graphHandler.getCachedUsers(), project);
    }

    public List<GraphUser> getUsersWithProject(List<GraphUser> graphUsers, string project)
    {
      return graphUsers.Where(user => user.projects.Contains(project, StringComparer.InvariantCultureIgnoreCase)).ToList();
    }
    #endregion
  }
}