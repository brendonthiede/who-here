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

    public void setGraphCacheLocation(string cacheLocation)
    {
      _graphHandler.cacheLocation = cacheLocation;
    }

    #region Validators and Converters
    public static bool isValidEmail(string email, string domain)
    {
      try
      {
        var addr = new System.Net.Mail.MailAddress(email);
        if (string.IsNullOrEmpty(domain))
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
    #endregion

    #region Formatters
    public static Attachment formatUserForSlack(GraphUser user)
    {
      Attachment userAttachment = new Attachment
      {
        pretext = $"*{user.displayName}*",
        title = user.jobTitle,
        text = user.userPrincipalName
      };
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
  + $"\n  `email <email@{_emailDomain}>`: shows information for the given email address\n"
  + @"  `name <search text>`: shows matches where the display name (formatted as <first> <last>) starts with the search text
  Other available properties that you can search on: job title, interests, skills projects
>Make sure your profile is up to date at https://delve-gcc.office.com";
    }

    public List<string> getUniqueValuesForProperty(SearchProperty property)
    {
      if (property.propertyType.Equals(PropertyType.String))
      {
        return getUniqueValuesForStringProperty(property.graphUserProperty, _graphHandler.getCachedUsers());
      }
      return getUniqueValuesForListProperty(property.graphUserProperty, _graphHandler.getCachedUsers());
    }

    public List<string> getUniqueValuesForStringProperty(GraphUserProperty propertyName, List<GraphUser> cachedUsers)
    {
      var dict = new Dictionary<string, string>();
      foreach (var graphUser in cachedUsers)
      {
        var value = (string)graphUser.GetType().GetProperty(propertyName.ToString()).GetValue(graphUser, null);
        if (!string.IsNullOrEmpty(value))
        {
          dict[value] = value;
        }
      }
      return dict.Values.ToList();
    }

    public List<string> getUniqueValuesForListProperty(GraphUserProperty propertyName, List<GraphUser> cachedUsers)
    {
      var dict = new Dictionary<string, string>();
      foreach (var graphUser in cachedUsers)
      {
        foreach (var value in (List<string>)graphUser.GetType().GetProperty(propertyName.ToString()).GetValue(graphUser))
        {
          dict[value] = value;
        }
      }
      return dict.Values.ToList();
    }

    public List<GraphUser> getUsersWithProperty(SearchProperty property, string filter, MatchType matchType)
    {
      List<GraphUser> users;
      if (property.propertyType.Equals(PropertyType.String))
      {
        users = getUsersWithStringProperty(property.graphUserProperty, filter, matchType, _graphHandler.getCachedUsers());
      }
      else
      {
        users = getUsersWithListProperty(property.graphUserProperty, filter, matchType, _graphHandler.getCachedUsers());
      }
      return users.OrderBy(user => (string)user.GetType().GetProperty(property.graphUserProperty.ToString()).GetValue(user) + user.userPrincipalName).ToList();
    }

    public List<GraphUser> getUsersWithStringProperty(GraphUserProperty propertyName, string propertyValue, MatchType matchType, List<GraphUser> cachedUsers)
    {
      if (matchType.Equals(MatchType.Equals))
      {
        return cachedUsers.Where(user => ((string)user.GetType().GetProperty(propertyName.ToString()).GetValue(user)).Equals(propertyValue, StringComparison.InvariantCultureIgnoreCase)).ToList();
      }
      return cachedUsers.Where(user => ((string)user.GetType().GetProperty(propertyName.ToString()).GetValue(user)).Contains(propertyValue, StringComparison.InvariantCultureIgnoreCase)).ToList();
    }

    public List<GraphUser> getUsersWithListProperty(GraphUserProperty propertyName, string propertyValue, MatchType matchType, List<GraphUser> cachedUsers)
    {
      if (matchType.Equals(MatchType.Equals))
      {
        return cachedUsers.Where(user => ((List<string>)user.GetType().GetProperty(propertyName.ToString()).GetValue(user)).Contains(propertyValue, StringComparer.InvariantCultureIgnoreCase)).ToList();
      }
      return cachedUsers.Where(user => ((List<string>)user.GetType().GetProperty(propertyName.ToString()).GetValue(user)).Any(property => ((string)property.GetType().GetProperty(propertyName.ToString()).GetValue(property)).Contains(propertyValue, StringComparison.InvariantCultureIgnoreCase))).ToList();
    }
    #endregion

    public SlackResponse EvaluateSlackCommand(string text)
    {
      SlackResponse response = new SlackResponse();
      WhoIsContext context = TextToIntentParser.getPropertyIntentFromText(text);
      switch (context.action)
      {
        case ActionType.Help:
          response.text = getHelpMessage();
          break;
        case ActionType.List:
          response.text = $"_Available {context.property.plural}:_ {string.Join(", ", getUniqueValuesForProperty(context.property))}";
          break;
        case ActionType.Search:
          if (context.property.graphUserProperty.Equals(GraphUserProperty.userPrincipalName) && !SlashCommandHandler.isValidEmail(context.filter, _emailDomain))
          {
            response.text = $"You must provide a valid email address with the {_emailDomain} domain";
            break;
          }
          List<GraphUser> matchingUsers = getUsersWithProperty(context.property, context.filter, context.matchType);
          if (matchingUsers.Count.Equals(0))
          {
            response.text = $"No users were found with {context.property.singular} {context.filter}";
          }
          else
          {
            response.attachments = SlashCommandHandler.formatUserListForSlack(matchingUsers);
          }
          break;
      }
      return response;
    }
  }
}