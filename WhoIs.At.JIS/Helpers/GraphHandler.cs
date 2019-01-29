using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using WhoIs.At.JIS.Models;

namespace WhoIs.At.JIS.Helpers
{
  public class GraphHandler
  {
    private readonly IConfiguration _graphConfiguration;
    private static bool _isUpdating;
    private const string CACHE_FILE = "snapShot.json";
    private const string GRAPH_USER_PROPERTIES = "displayName,jobTitle,userPrincipalName,interests,skills,pastProjects,aboutMe";

    public GraphHandler(IConfiguration configuration)
    {
      try
      {
        _graphConfiguration = configuration.GetSection("graph");

      }
      catch (System.NullReferenceException)
      {
        _graphConfiguration = null;
      }
    }

    public void updateUserCache()
    {
      if (!_isUpdating)
      {
        Stopwatch watch = new Stopwatch();
        watch.Start();
        //Do things
        _isUpdating = true;
        Console.WriteLine("*** Starting update process");
        // await Task.Run(() =>
        // {
        var allUsers = getAllMsGraphUsers();
        string json = JsonConvert.SerializeObject(allUsers.ToArray());

        //write string to file
        System.IO.File.WriteAllText(CACHE_FILE, json);
        watch.Stop();
        Console.WriteLine($"*** Update ran for {watch.Elapsed.TotalSeconds.ToString()} seconds");
        _isUpdating = false;
        // });
      }
      else
      {
        Console.WriteLine("*** Update was already running");
      }
    }

    public List<GraphUser> getAllMsGraphUsers()
    {
      GraphServiceClient graphClient = GetAuthenticatedGraphClient();
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
              new QueryOption("$select", GRAPH_USER_PROPERTIES)
          };
            try
            {
              if (SlashCommandHandler.isValidEmail(result.UserPrincipalName, _graphConfiguration["domain"]))
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

    public GraphUser getMsGraphResultsForEmail(string email)
    {
      GraphServiceClient graphClient = GetAuthenticatedGraphClient();
      return getMsGraphResultsForEmail(graphClient, email);
    }

    public GraphUser getMsGraphResultsForEmail(GraphServiceClient graphClient, string email)
    {
      List<QueryOption> options = new List<QueryOption>
        {
            new QueryOption("$select", GRAPH_USER_PROPERTIES)
        };

      var graphResult = graphClient.Users[email].Request(options).GetAsync().Result;
      return asGraphUser(graphResult);
    }

    private GraphServiceClient GetAuthenticatedGraphClient()
    {
      var authenticationProvider = CreateAuthorizationProvider();
      var graphServiceClient = new GraphServiceClient(authenticationProvider);
      return graphServiceClient;
    }

    private IAuthenticationProvider CreateAuthorizationProvider()
    {
      var clientId = _graphConfiguration.GetValue<string>("applicationId");
      var clientSecret = _graphConfiguration.GetValue<string>("applicationSecret");
      var redirectUri = _graphConfiguration.GetValue<string>("redirectUri");
      var tenantId = _graphConfiguration.GetValue<string>("tenantId");
      var authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";

      //this specific scope means that application will default to what is defined in the application registration rather than using dynamic scopes
      List<string> scopes = new List<string>();
      scopes.Add("https://graph.microsoft.com/.default");

      var cca = new ConfidentialClientApplication(clientId, authority, redirectUri, new ClientCredential(clientSecret), null, null);
      return new MsalAuthenticationProvider(cca, scopes.ToArray());
    }

    public List<GraphUser> getCachedUsers()
    {
      return getCachedUsers(CACHE_FILE);

    }

    public List<GraphUser> getCachedUsers(string filePath)
    {
      using (StreamReader r = new StreamReader(filePath))
      {
        string json = r.ReadToEnd();
        return JsonConvert.DeserializeObject<List<GraphUser>>(json);
      }
    }

    public static GraphUser asGraphUser(User user)
    {
      try
      {
        return new GraphUser
        {
          displayName = user.DisplayName,
          jobTitle = user.JobTitle,
          userPrincipalName = user.UserPrincipalName,
          aboutMe = user.AboutMe,
          interests = user.Interests,
          skills = user.Skills,
          projects = user.PastProjects
        };

      }
      catch (System.Exception)
      {
        return null;
      }
    }
  }
}
