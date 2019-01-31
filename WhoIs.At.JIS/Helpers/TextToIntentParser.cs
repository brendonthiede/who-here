using System.Collections.Generic;
using System.Text.RegularExpressions;
using WhoIs.At.JIS.Models;

namespace WhoIs.At.JIS.Helpers
{
  public class TextToIntentParser
  {
    private static List<SearchProperty> AVAILABLE_PROPERTIES = new List<SearchProperty> {
      new SearchProperty {
        graphUserProperty = GraphUserProperty.displayName,
        Singular = "name",
        Plural = "names",
        PropertyType = PropertyType.String,
        Tenses = new List<string> {"withname", "displayname", "display name", "name"}
      },
      new SearchProperty {
        graphUserProperty = GraphUserProperty.jobTitle,
        Singular = "job title",
        Plural = "job titles",
        PropertyType = PropertyType.String,
        Tenses = new List<string> {"jobtitle", "job title", "title", "titles", "titled", "job", "joblist", "jobslist", "titlelist", "titleslist", "jobtitlelist", "jobtitleslist", "withjob", "withjobtitle"}
      },
      new SearchProperty {
        graphUserProperty = GraphUserProperty.userPrincipalName,
        Singular = "email",
        Plural = "emails",
        PropertyType = PropertyType.String,
        Tenses = new List<string> {"email", "withemail"}
      },
      new SearchProperty {
        graphUserProperty = GraphUserProperty.interests,
        Singular = "interest",
        Plural = "interests",
        PropertyType = PropertyType.List,
        Tenses = new List<string> {"interest", "interests", "interested", "likes", "withinterest", "interestslist", "interestlist"}
      },
      new SearchProperty {
        graphUserProperty = GraphUserProperty.skills,
        Singular = "skill",
        Plural = "skills",
        PropertyType = PropertyType.List,
        Tenses = new List<string> {"skill", "skills", "skilled", "withskill", "skilllist", "skillslist"}
      },
      new SearchProperty {
        graphUserProperty = GraphUserProperty.projects,
        Singular = "project",
        Plural = "projects",
        PropertyType = PropertyType.List,
        Tenses = new List<string> {"project", "projects", "work on", "works on", "worked on", "withproject", "projectslist", "projectlist"}
      }
    };

    public static WhoIsContext getPropertyIntentFromText(string text)
    {
      text = text.Trim();
      var context = new WhoIsContext
      {
        Text = text,
        SearchProperty = null
      };
      if (string.IsNullOrEmpty(text) || text.Equals(ActionType.Help.ToString(), System.StringComparison.InvariantCultureIgnoreCase))
      {
        context.Action = ActionType.Help;
      }
      else
      {
        FindPropertyReference(context);
        if (context.SearchProperty.Equals(GraphUserProperty.userPrincipalName))
        {
          context.MatchType = MatchType.Equals;
        }
        else
        {
          context.MatchType = MatchType.Contains;
        }
        context.Text = removeNoiseAroundWords(context.Text, context.FoundTense);
        context.Filter = Regex.Replace(context.Text, $"\\b{context.FoundTense}\\b", "", RegexOptions.IgnoreCase).Trim();
        if (string.IsNullOrEmpty(context.Filter) || context.Filter.Equals("list", System.StringComparison.InvariantCultureIgnoreCase))
        {
          context.Action = ActionType.List;
        }
        else
        {
          context.Action = ActionType.Search;
        }
      }
      return context;
    }

    private static void FindPropertyReference(WhoIsContext context)
    {
      // scan text for intended property
      foreach (var property in AVAILABLE_PROPERTIES)
      {
        foreach (var tense in property.Tenses)
        {
          if (Regex.IsMatch(context.Text, $"\\b{tense}\\b", RegexOptions.IgnoreCase))
          {
            context.SearchProperty = property;
            context.FoundTense = tense;
            return;
          }
        }
      }
      // if the whole text is an email, use email search
      if (SlashCommandHandler.isValidEmail(context.Text, null))
      {
        context.Text = $"email {context.Text}";
        FindPropertyReference(context);
      }
      // if no property reference is found, assume a name search
      else
      {
        context.Text = $"name {context.Text}";
        FindPropertyReference(context);
      }
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
