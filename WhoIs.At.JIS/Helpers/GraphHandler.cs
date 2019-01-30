using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using WhoIs.At.JIS.Models;

namespace WhoIs.At.JIS.Helpers
{
  public class GraphHandler
  {
    private readonly IConfiguration _graphConfiguration;
    private static bool _isUpdating;
    private const string DEFAULT_CACHE_FILE = "snapShot.json";
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
      cacheLocation = DEFAULT_CACHE_FILE;
    }

    public string cacheLocation { get; set; }

    public void updateUserCache()
    {
      if (!_isUpdating)
      {
        Stopwatch watch = new Stopwatch();
        watch.Start();
        _isUpdating = true;
        Console.WriteLine("*** Starting update process");
        var allUsers = getAllMsGraphUsers();
        string json = JsonConvert.SerializeObject(allUsers.ToArray());

        //write string to file
        System.IO.File.WriteAllText(this.cacheLocation, json);
        watch.Stop();
        Console.WriteLine($"*** Update ran for {watch.Elapsed.TotalSeconds.ToString()} seconds");
        _isUpdating = false;
      }
      else
      {
        Console.WriteLine("*** Update was already running");
      }
    }

    public List<GraphUser> getAllMsGraphUsers()
    {
      GraphServiceClient graphClient = GetAuthenticatedGraphClient();
      var tasks = new List<Task<List<GraphUser>>>();
      for (char letter = 'A'; letter <= 'Z'; letter++)
      {
        string currentLetter = letter.ToString();
        tasks.Add(Task.Run(async () => { return await getMsGraphUsersForLetter(graphClient, currentLetter); }));
      }

      var continuation = Task.WhenAll(tasks);
      try
      {
        continuation.Wait();
      }
      catch (AggregateException)
      { }

      List<GraphUser> allUsers = new List<GraphUser>();
      if (continuation.Status == TaskStatus.RanToCompletion)
      {
        foreach (var result in continuation.Result)
        {
          allUsers.AddRange(result);
        }
      }
      // Display information on faulted tasks.
      else
      {
        foreach (var task in tasks)
        {
          Console.WriteLine($"Task {task.Id}: {task.Status}");
        }
      }

      return allUsers;
    }

    private async Task<List<GraphUser>> getMsGraphUsersForLetter(GraphServiceClient graphClient, string currentLetter)
    {
      List<GraphUser> letterUsers = new List<GraphUser>();
      Console.WriteLine($"*** Pulling users for {currentLetter}");
      List<QueryOption> options = new List<QueryOption>
        {
          new QueryOption("$filter", $"startsWith(mail,'{currentLetter}')"),
          new QueryOption("$select", "userPrincipalName")
        };
      try
      {
        var graphResult = await graphClient.Users.Request(options).GetAsync();
        var retVal = new List<string>();
        foreach (var result in graphResult.CurrentPage)
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
                letterUsers.Add(asGraphUser(graphClient.Users[result.UserPrincipalName].Request(userOptions).GetAsync().Result));
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
        System.Console.WriteLine($"*** Error pulling emails starting with {currentLetter}");
      }
      return letterUsers;
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
      using (StreamReader r = new StreamReader(this.cacheLocation))
      {
        string json = r.ReadToEnd();
        return JsonConvert.DeserializeObject<List<GraphUser>>(json);
      }
    }

    private static GraphUser asGraphUser(User user)
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
