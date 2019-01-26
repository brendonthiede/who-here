using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System.Collections.Generic;
using WhoIs.At.JIS.Models;
using System;

namespace WhoIs.At.JIS.Helpers
{

  public static class SlashCommandHandler
  {
    static List<string> VALID_COMMANDS = new List<string> { "help", "email", "name" };
    static string[] CONFIG_VARIABLES = new string[] { "applicationId", "applicationSecret", "redirectUri", "tenantId", "domain" };

    public static bool isValidCommand(string command)
    {
      return VALID_COMMANDS.Contains(command);
    }

    public static bool isEmail(string email)
    {
      try
      {
        var addr = new System.Net.Mail.MailAddress(email);
        return addr.Address == email;
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
        else if (isEmail(whoIsCommand.command))
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
        retVal.Add($"{result.DisplayName} - {result.UserPrincipalName}");
      }
      return retVal;
    }

    public static string getMsGraphResultsForEmail(string email)
    {
      GraphServiceClient graphClient = GetAuthenticatedGraphClient(getAuthConfig());
      List<QueryOption> options = new List<QueryOption>
        {
            new QueryOption("$top", "10"),
            new QueryOption("$select", "displayName,jobTitle,userPrincipalName,interests,skills,pastProjects,aboutMe")
        };

      var graphResult = graphClient.Users[email].Request(options).GetAsync().Result;
      return $"{graphResult.DisplayName} - {graphResult.UserPrincipalName}";
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