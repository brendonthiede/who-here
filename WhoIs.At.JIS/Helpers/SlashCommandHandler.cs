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
    private readonly GraphHandler _graphHandler;
    private readonly string _emailDomain;
    private readonly IConfiguration _slackConfiguration;

    static List<string> VALID_COMMANDS = new List<string> { "debug", "help", "email", "name", "skillslist", "withskill", "interestslist", "withinterest", "projectslist", "withproject" };

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
      whoIsCommand.parameters = text.Split(' ');
      if (whoIsCommand.parameters.Length > 0 && whoIsCommand.parameters[0].Length > 0)
      {
        whoIsCommand.command = whoIsCommand.parameters[0].ToLower();
        if (isValidCommand(whoIsCommand.command))
        {
          var tmp = new List<string>(text.Split(' '));
          tmp.RemoveAt(0);
          whoIsCommand.parameters = tmp.ToArray();
        }
        else if (isValidEmail(whoIsCommand.command))
        {
          whoIsCommand.parameters = new string[] { whoIsCommand.command };
          whoIsCommand.command = "email";
        }
        else
        {
          whoIsCommand.command = "name";
        }
      }
      return whoIsCommand;
    }

    public List<string> getSkillsList()
    {
      return getSkillsList(_graphHandler.getCachedUsers());
    }

    public List<string> getSkillsList(List<GraphUser> graphUsers)
    {
      var dict = new Dictionary<string, string>();
      foreach (var graphUser in graphUsers)
      {
        foreach (var skill in graphUser.skills)
        {
          dict[skill] = skill;
        }
      }
      return dict.Values.ToList();
    }

    public string formatUserForSlack(GraphUser user)
    {
      var profileData = $"*{user.displayName}*\n{user.jobTitle}\n{user.userPrincipalName}";
      if (!string.IsNullOrEmpty(user.aboutMe))
      {
        profileData += $"\n>{user.aboutMe}";
      }
      var pastProjects = new List<string>(user.pastProjects);
      if (pastProjects.Count > 0)
      {
        profileData += $"\n_Projects:_ {string.Join(", ", user.pastProjects)}";
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

    public List<GraphUser> getUsersWithSkill(string skill)
    {
      return getUsersWithSkill(_graphHandler.getCachedUsers(), skill);
    }

    public List<GraphUser> getUsersWithSkill(List<GraphUser> graphUsers, string skill)
    {
      return graphUsers.Where(user => user.skills.Contains(skill, StringComparer.InvariantCultureIgnoreCase)).ToList();
    }

    public List<GraphUser> getUsersWithInterest(string interest)
    {
      return getUsersWithInterest(_graphHandler.getCachedUsers(), interest);
    }

    public List<GraphUser> getUsersWithInterest(List<GraphUser> graphUsers, string interest)
    {
      return graphUsers.Where(user => user.interests.Contains(interest, StringComparer.InvariantCultureIgnoreCase)).ToList();
    }

    public List<GraphUser> getUsersWithProject(string project)
    {
      return getUsersWithProject(_graphHandler.getCachedUsers(), project);
    }

    public List<GraphUser> getUsersWithProject(List<GraphUser> graphUsers, string project)
    {
      return graphUsers.Where(user => user.pastProjects.Contains(project, StringComparer.InvariantCultureIgnoreCase)).ToList();
    }

    public List<GraphUser> getUsersWithName(string name)
    {
      return getUsersWithName(_graphHandler.getCachedUsers(), name);
    }

    public List<GraphUser> getUsersWithName(List<GraphUser> graphUsers, string name)
    {
      return graphUsers.Where(user => user.displayName.StartsWith(name, StringComparison.InvariantCultureIgnoreCase)).ToList();
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

    public List<string> getProjectsList()
    {
      return getProjectsList(_graphHandler.getCachedUsers());
    }

    public List<string> getProjectsList(List<GraphUser> graphUsers)
    {
      var dict = new Dictionary<string, string>();
      foreach (var graphUser in graphUsers)
      {
        foreach (var project in graphUser.pastProjects)
        {
          dict[project] = project;
        }
      }
      return dict.Values.ToList();
    }

    public List<string> getInterestsList()
    {
      return getInterestsList(_graphHandler.getCachedUsers());
    }

    public static List<string> getInterestsList(List<GraphUser> graphUsers)
    {
      var dict = new Dictionary<string, string>();
      foreach (var graphUser in graphUsers)
      {
        foreach (var interest in graphUser.interests)
        {
          dict[interest] = interest;
        }
      }
      return dict.Values.ToList();
    }
  }
}