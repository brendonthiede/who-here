using System.Collections.Generic;
using System.Text.RegularExpressions;
using WhoIs.At.JIS.Models;

namespace WhoIs.At.JIS.Helpers
{
  public class TextToIntentParser
  {
    private static List<SearchProperty> AVAILABLE_PROPERTIES = new List<SearchProperty>(){
      new SearchProperty() {
        graphUserProperty = GraphUserProperty.displayName,
        singular = "name",
        plural = "names",
        propertyType = PropertyType.String,
        tenses = new List<string> {"withname", "displayname", "display name", "name"}
      },
      new SearchProperty() {
        graphUserProperty = GraphUserProperty.jobTitle,
        singular = "job title",
        plural = "job titles",
        propertyType = PropertyType.String,
        tenses = new List<string> {"jobtitle", "job title", "title", "titles", "titled", "job", "joblist", "jobslist", "titlelist", "titleslist", "jobtitlelist", "jobtitleslist", "withjob", "withjobtitle"}
      },
      new SearchProperty() {
        graphUserProperty = GraphUserProperty.userPrincipalName,
        singular = "email",
        plural = "emails",
        propertyType = PropertyType.String,
        tenses = new List<string> {"email", "withemail"}
      },
      new SearchProperty() {
        graphUserProperty = GraphUserProperty.interests,
        singular = "interest",
        plural = "interests",
        propertyType = PropertyType.List,
        tenses = new List<string> {"interest", "interests", "interested", "likes", "withinterest", "interestslist", "interestlist"}
      },
      new SearchProperty() {
        graphUserProperty = GraphUserProperty.skills,
        singular = "skill",
        plural = "skills",
        propertyType = PropertyType.List,
        tenses = new List<string> {"skill", "skills", "skilled", "withskill", "skilllist", "skillslist"}
      },
      new SearchProperty() {
        graphUserProperty = GraphUserProperty.projects,
        singular = "project",
        plural = "projects",
        propertyType = PropertyType.List,
        tenses = new List<string> {"project", "projects", "work on", "works on", "worked on", "withproject", "projectslist", "projectlist"}
      }
    };

    public static WhoIsContext getPropertyIntentFromText(string text)
    {
      text = text.Trim();
      var context = new WhoIsContext
      {
        property = null
      };
      if (string.IsNullOrEmpty(text) || text.Equals(ActionType.Help.ToString(), System.StringComparison.InvariantCultureIgnoreCase))
      {
        context.action = ActionType.Help;
      }
      else
      {
        string foundTense = null;
        foreach (var property in AVAILABLE_PROPERTIES)
        {
          foreach (var tense in property.tenses)
          {
            if (Regex.IsMatch(text, $"\\b{tense}\\b", RegexOptions.IgnoreCase))
            {
              foundTense = tense;
              context.property = property;
              break;
            }
          }
          if (context.property != null)
          {
            break;
          }
        }
        if (context.property == null)
        {
          if (SlashCommandHandler.isValidEmail(text, null))
          {
            return getPropertyIntentFromText($"email {text}");
          } else {
            return getPropertyIntentFromText($"name {text}");
          }
        }
        else
        {
          if (context.property.Equals(GraphUserProperty.userPrincipalName))
          {
            context.matchType = MatchType.Equals;
          }
          else
          {
            context.matchType = MatchType.Contains;
          }
          context.text = removeNoiseAroundWords(text, foundTense);
          context.filter = Regex.Replace(context.text, $"\\b{foundTense}\\b", "", RegexOptions.IgnoreCase).Trim();
          if (string.IsNullOrEmpty(context.filter) || context.filter.Equals("list", System.StringComparison.InvariantCultureIgnoreCase))
          {
            context.action = ActionType.List;
          }
          else
          {
            context.action = ActionType.Search;
          }
        }
      }
      return context;
    }

    public static string removeNoiseAroundWords(string originalText, string targetWords)
    {
      List<string> noiseWords = new List<string>() { "as", "is", "are", "to", "in", "of", "the", "a", "an", "with", "that", "has", "had" };
      var finalText = originalText;
      var updated = false;
      do
      {
        updated = false;
        Match match = Regex.Match(finalText, @"(?<before>\w*)\s*" + targetWords + @"\s*(?<after>\w*)", RegexOptions.IgnoreCase);
        var before = match.Groups["before"].Value;
        var after = match.Groups["after"].Value;
        foreach (var noiseWord in noiseWords)
        {
          if (noiseWord.Equals(before))
          {
            finalText = Regex.Replace(finalText, $"{noiseWord}\\s+({targetWords})", "$1", RegexOptions.IgnoreCase);
            updated = true;
          }
          if (noiseWord.Equals(after))
          {
            finalText = Regex.Replace(finalText, $"({targetWords})\\s+{noiseWord}", "$1", RegexOptions.IgnoreCase);
            updated = true;
          }
        }
      } while (updated);
      return finalText.Trim();
    }
  }
}
