using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System.Collections.Generic;
using WhoIs.At.JIS.Models;
using System;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace WhoIs.At.JIS.Helpers
{

  public static class SlashCommandHandler
  {
    static bool isUpdating = false;
    static List<string> VALID_COMMANDS = new List<string> { "debug", "help", "email", "name", "skillslist", "withskill", "interestslist", "withinterest", "projectslist", "withproject" };
    static string[] CONFIG_VARIABLES = new string[] { "applicationId", "applicationSecret", "redirectUri", "tenantId", "domain" };
    static string CACHE_FILE = "snapShot.json";

    public static string getHelpMessage()
    {
      return @"Available commands:
  `help`: showsthis message
  `email <email@courts.mi.gov>`: shows information for the given email address
  `name <search text>`: shows matches where the display name (formatted as <first> <last>) starts with the search text
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

    public static List<GraphUser> getAllMsGraphUsers(IConfiguration graphConfig)
    {
      GraphServiceClient graphClient = GetAuthenticatedGraphClient(graphConfig);
      List<GraphUser> allUsers = new List<GraphUser>();
      for (char letter = 'A'; letter <= 'Z'; letter++)
      {
        Console.WriteLine($"*** Pulling users for {letter}");
        List<QueryOption> options = new List<QueryOption>
        {
            new QueryOption("$filter", $"startsWith(mail,'{letter}')"),
            new QueryOption("$select", "userPrincipalName")
        };
        try
        {
          var graphResult = graphClient.Users.Request(options).GetAsync().Result;
          var retVal = new List<string>();
          foreach (var result in graphResult)
          {
            List<QueryOption> userOptions = new List<QueryOption>
          {
              new QueryOption("$select", "displayName,jobTitle,userPrincipalName,interests,skills,pastProjects,aboutMe")
          };
            try
            {
              if (isValidEmail(result.UserPrincipalName, "courts.mi.gov"))
              {
                GraphUser graphUser = asGraphUser(graphClient.Users[result.UserPrincipalName].Request(userOptions).GetAsync().Result);
                if (!String.IsNullOrEmpty(graphUser.jobTitle))
                {
                  allUsers.Add(asGraphUser(graphClient.Users[result.UserPrincipalName].Request(userOptions).GetAsync().Result));
                }

              }
            }
            catch (System.Exception)
            {
              System.Console.WriteLine($"*** Error encountered trying to pull {result.UserPrincipalName}");
            }
          }
        }
        catch (System.Exception)
        {

          System.Console.WriteLine($"*** Error pulling emails starting with {letter}");
        }
      }
      return allUsers;
    }

    async public static void updateUserCache(IConfiguration graphConfig)
    {
      if (!isUpdating)
      {
        Stopwatch watch = new Stopwatch();
        watch.Start();
        //Do things
        isUpdating = true;
        Console.WriteLine("*** Starting update process");
        await Task.Run(() =>
        {
          var allUsers = getAllMsGraphUsers(graphConfig);
          string json = JsonConvert.SerializeObject(allUsers.ToArray());

          //write string to file
          System.IO.File.WriteAllText(CACHE_FILE, json);
          watch.Stop();
          Console.WriteLine($"*** Update ran for {watch.Elapsed.TotalSeconds.ToString()} seconds");
          isUpdating = false;
        });
      }
      else
      {
        Console.WriteLine("*** Update was already running");
      }
    }

    public static List<GraphUser> getCachedUsers()
    {
      return getCachedUsers(CACHE_FILE);

    }

    public static List<GraphUser> getCachedUsers(string filePath)
    {
      using (StreamReader r = new StreamReader(filePath))
      {
        string json = r.ReadToEnd();
        return JsonConvert.DeserializeObject<List<GraphUser>>(json);
      }

    }

    public static List<string> getSkillsList()
    {
      return getSkillsList(getCachedUsers());
    }

    public static List<string> getSkillsList(List<GraphUser> graphUsers)
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

    public static string formatUserForSlack(GraphUser user)
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

    public static string formatUserListForSlack(List<GraphUser> users)
    {
      List<string> details = users.Select(user => formatUserForSlack(user)).ToList();
      return string.Join("\n==========\n", details);
    }

    public static List<GraphUser> getUsersWithSkill(string skill)
    {
      return getUsersWithSkill(getCachedUsers(), skill);
    }

    public static List<GraphUser> getUsersWithSkill(List<GraphUser> graphUsers, string skill)
    {
      return graphUsers.Where(user => user.skills.Contains(skill, StringComparer.InvariantCultureIgnoreCase)).ToList();
    }

    public static List<GraphUser> getUsersWithInterest(string interest)
    {
      return getUsersWithInterest(getCachedUsers(), interest);
    }

    public static List<GraphUser> getUsersWithInterest(List<GraphUser> graphUsers, string interest)
    {
      return graphUsers.Where(user => user.interests.Contains(interest, StringComparer.InvariantCultureIgnoreCase)).ToList();
    }

    public static List<GraphUser> getUsersWithProject(string project)
    {
      return getUsersWithProject(getCachedUsers(), project);
    }

    public static List<GraphUser> getUsersWithProject(List<GraphUser> graphUsers, string project)
    {
      return graphUsers.Where(user => user.pastProjects.Contains(project, StringComparer.InvariantCultureIgnoreCase)).ToList();
    }

    public static List<string> getProjectsList()
    {
      return getProjectsList(getCachedUsers());
    }

    public static List<string> getProjectsList(List<GraphUser> graphUsers)
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

    public static List<string> getInterestsList()
    {
      return getInterestsList(getCachedUsers());
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

    private static GraphUser asGraphUser(User user)
    {
      return new GraphUser
      {
        displayName = user.DisplayName,
        jobTitle = user.JobTitle,
        userPrincipalName = user.UserPrincipalName,
        aboutMe = user.AboutMe,
        interests = user.Interests,
        skills = user.Skills,
        pastProjects = user.PastProjects
      };
    }

    public static List<string> getMsGraphResultsForName(string name, IConfiguration graphConfig)
    {
      GraphServiceClient graphClient = GetAuthenticatedGraphClient(graphConfig);
      List<QueryOption> options = new List<QueryOption>
        {
            new QueryOption("$top", "100"),
            new QueryOption("$filter", $"startsWith(displayName,'{name}')")
        };

      var graphResult = graphClient.Users.Request(options).GetAsync().Result;
      var retVal = new List<string>();
      foreach (var result in graphResult)
      {
        if (!string.IsNullOrEmpty(result.JobTitle) && isValidEmail(result.Mail, "courts.mi.gov"))
        {
          retVal.Add($"{result.DisplayName} - {result.JobTitle} - {result.Mail}");

        }
      }
      return retVal;
    }

    public static string getMsGraphResultsForEmail(string email, IConfiguration graphConfig)
    {
      GraphServiceClient graphClient = GetAuthenticatedGraphClient(graphConfig);
      List<QueryOption> options = new List<QueryOption>
        {
            new QueryOption("$top", "10"),
            new QueryOption("$select", "displayName,jobTitle,mail,interests,skills,pastProjects,aboutMe")
        };

      var graphResult = graphClient.Users[email].Request(options).GetAsync().Result;
      return formatUserForSlack(asGraphUser(graphResult));
    }

    private static GraphServiceClient GetAuthenticatedGraphClient(IConfiguration graphConfig)
    {
      var authenticationProvider = CreateAuthorizationProvider(graphConfig);
      var graphServiceClient = new GraphServiceClient(authenticationProvider);
      return graphServiceClient;
    }

    private static IAuthenticationProvider CreateAuthorizationProvider(IConfiguration graphConfig)
    {
      var clientId = graphConfig.GetValue<string>("applicationId");
      var clientSecret = graphConfig.GetValue<string>("applicationSecret");
      var redirectUri = graphConfig.GetValue<string>("redirectUri");
      var tenantId = graphConfig.GetValue<string>("tenantId");
      var authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";

      //this specific scope means that application will default to what is defined in the application registration rather than using dynamic scopes
      List<string> scopes = new List<string>();
      scopes.Add("https://graph.microsoft.com/.default");

      var cca = new ConfidentialClientApplication(clientId, authority, redirectUri, new ClientCredential(clientSecret), null, null);
      return new MsalAuthenticationProvider(cca, scopes.ToArray());
    }
  }
}