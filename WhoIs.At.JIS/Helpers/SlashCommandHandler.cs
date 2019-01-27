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

namespace WhoIs.At.JIS.Helpers
{

  public static class SlashCommandHandler
  {
    static bool isUpdating = false;
    static List<string> VALID_COMMANDS = new List<string> { "help", "email", "name" };
    static string[] CONFIG_VARIABLES = new string[] { "applicationId", "applicationSecret", "redirectUri", "tenantId", "domain" };

    public static string getHelpMessage()
    {
      return @"Available commands:
  `help`: showsthis message
  `email <email@courts.mi.gov>`: shows information for the given email address
  `name <search text>`: shows the first 10 matches where the display name (formatted as <first> <last>) starts with the search text";
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

    public static List<GraphUser> getAllMsGraphUsers()
    {
      GraphServiceClient graphClient = GetAuthenticatedGraphClient(getAuthConfig());
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

    async public static void updateUserCache()
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
          var allUsers = getAllMsGraphUsers();
          string json = JsonConvert.SerializeObject(allUsers.ToArray());

          //write string to file
          System.IO.File.WriteAllText("snapShot.json", json);
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
      using (StreamReader r = new StreamReader("snapShot.json"))
      {
        string json = r.ReadToEnd();
        return JsonConvert.DeserializeObject<List<GraphUser>>(json);
      }

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

    public static List<string> getMsGraphResultsForName(string name)
    {
      GraphServiceClient graphClient = GetAuthenticatedGraphClient(getAuthConfig());
      List<QueryOption> options = new List<QueryOption>
        {
            new QueryOption("$top", "10"),
            new QueryOption("$filter", $"startsWith(displayName,'{name}')")
        };

      var graphResult = graphClient.Users.Request(options).GetAsync().Result;
      var retVal = new List<string>();
      foreach (var result in graphResult)
      {
        retVal.Add($"{result.DisplayName} - {result.JobTitle} - {result.Mail}");
      }
      return retVal;
    }

    public static string getMsGraphResultsForEmail(string email)
    {
      GraphServiceClient graphClient = GetAuthenticatedGraphClient(getAuthConfig());
      List<QueryOption> options = new List<QueryOption>
        {
            new QueryOption("$top", "10"),
            new QueryOption("$select", "displayName,jobTitle,mail,interests,skills,pastProjects,aboutMe")
        };

      var graphResult = graphClient.Users[email].Request(options).GetAsync().Result;
      var profileData = $"{graphResult.DisplayName}\n{graphResult.JobTitle}\n{graphResult.Mail}";
      if (!string.IsNullOrEmpty(graphResult.AboutMe)) {
        profileData += $"\n>{graphResult.AboutMe}";
      }
      var pastProjects = new List<string>(graphResult.PastProjects);
      if (pastProjects.Count > 0) {
        profileData += $"\nProjects:\n  {string.Join("\n  ", graphResult.PastProjects)}";
      }
      var skills = new List<string>(graphResult.Skills);
      if (skills.Count > 0) {
        profileData += $"\nSkills:\n  {string.Join("\n  ", graphResult.Skills)}";
      }
      var interests = new List<string>(graphResult.Interests);
      if (interests.Count > 0) {
        profileData += $"\nInterests:\n  {string.Join("\n  ", graphResult.Interests)}";
      }
      return profileData;
    }

    private static Dictionary<string, string> getAuthConfig()
    {
      IConfigurationRoot config;
      Dictionary<string, string> graphConfig = new Dictionary<string, string>();
      try
      {
        var basePath = System.IO.Directory.GetCurrentDirectory();
        config = new ConfigurationBuilder()
        .SetBasePath(basePath)
        .AddJsonFile("appsettings.json", false, false)
        .Build();
      }
      catch (Exception)
      {
        config = null;
      }
      var envVars = System.Environment.GetEnvironmentVariables();
      if (config != null)
      {
        foreach (var configVar in CONFIG_VARIABLES)
        {
          if (!string.IsNullOrEmpty(config[configVar]))
          {
            graphConfig.Add(configVar, config[configVar]);
          }
          else if (envVars.Contains(configVar))
          {
            graphConfig.Add(configVar, (string)envVars[configVar]);
          }
        }
      }
      return graphConfig;
    }

    private static GraphServiceClient GetAuthenticatedGraphClient(Dictionary<string, string> config)
    {
      var authenticationProvider = CreateAuthorizationProvider(config);
      var graphServiceClient = new GraphServiceClient(authenticationProvider);
      return graphServiceClient;
    }

    private static IAuthenticationProvider CreateAuthorizationProvider(Dictionary<string, string> config)
    {
      var clientId = config["applicationId"];
      var clientSecret = config["applicationSecret"];
      var redirectUri = config["redirectUri"];
      var authority = $"https://login.microsoftonline.com/{config["tenantId"]}/v2.0";

      //this specific scope means that application will default to what is defined in the application registration rather than using dynamic scopes
      List<string> scopes = new List<string>();
      scopes.Add("https://graph.microsoft.com/.default");

      var cca = new ConfidentialClientApplication(clientId, authority, redirectUri, new ClientCredential(clientSecret), null, null);
      return new MsalAuthenticationProvider(cca, scopes.ToArray());
    }
  }
}