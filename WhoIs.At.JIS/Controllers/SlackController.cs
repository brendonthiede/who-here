using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WhoIs.At.JIS.Helpers;
using WhoIs.At.JIS.Models;

namespace WhoIs.At.JIS.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class SlackController : ControllerBase
  {
    private readonly IConfiguration _slackConfiguration;
    private readonly IConfiguration _graphConfiguration;
    private readonly SlashCommandHandler _slashCommandHandler;
    private readonly GraphHandler _graphHandler;

    public SlackController(IConfiguration configuration)
    {
      _slackConfiguration = configuration.GetSection("slack");
      _graphConfiguration = configuration.GetSection("graph");
      _slashCommandHandler = new SlashCommandHandler(configuration);
      _graphHandler = new GraphHandler(configuration);
    }

    // GET api/slack
    [HttpGet]
    public ActionResult<string> Get()
    {
      return "Silence is golden";
    }

    // GET api/slack/updatecache
    [HttpGet]
    [Route("updatecache")]
    public ActionResult<string> UpdateCache()
    {
      _graphHandler.updateUserCache();
      return "Update initiated";
    }

    // GET api/slack/help
    [HttpGet]
    [Route("help")]
    public ActionResult<string> GetHelp()
    {
      return _slashCommandHandler.getHelpMessage();
    }

    // POST api/slack
    [HttpPost]
    [Produces("application/json")]
    public ActionResult<SlackResponse> Post([FromForm] SlashCommandPayload slashCommandPayload)
    {
      var authToken = _slackConfiguration.GetValue<string>("slashCommandToken");
      if (string.IsNullOrEmpty(authToken))
      {
        return new SlackResponse
        {
          text = "Authentication token is not set up correctly on the whoisatjis server"
        };
      }
      if (!slashCommandPayload.token.Equals(authToken))
      {
        return new SlackResponse
        {
          text = "Invalid authentication token"
        };
      }
      var responseUrl = new Uri(slashCommandPayload.response_url);
      if (!responseUrl.Host.Equals("hooks.slack.com"))
      {
        return new SlackResponse
        {
          text = $"The host {responseUrl.Host} is not allowed"
        };
      }
      Task.Run(() => _graphHandler.updateUserCache());
      return EvaluateSlackCommand(slashCommandPayload);
    }

    private SlackResponse EvaluateSlackCommand(SlashCommandPayload slashCommandPayload)
    {
      SlackResponse response = new SlackResponse();
      WhoIsCommand command = SlashCommandHandler.getCommandFromString(slashCommandPayload.text);
      #region Commands without parameters
      if (command.command.Equals("help") || !SlashCommandHandler.isValidCommand(command.command))
      {
        response.text = _slashCommandHandler.getHelpMessage();
      }
      else if (command.command.Equals("jobtitlelist"))
      {
        response.text = $"_Available job titles:_ {string.Join(", ", _slashCommandHandler.getUniqueValuesForStringProperty("jobTitle"))}";
      }
      else if (command.command.EndsWith("list"))
      {
        var listType = command.command.Replace("list", "");
        response.text = $"_Available {listType}:_ {string.Join(", ", _slashCommandHandler.getUniqueValuesForListProperty(listType))}";
      }
      if (!string.IsNullOrEmpty(response.text)) return response;
      #endregion

      #region Commands with required parameters
      List<GraphUser> users = new List<GraphUser>();
      if (string.IsNullOrEmpty(command.parameters))
      {
        response.text = $"You need to provide a search value: `/whois-at-jis {command.command} <value>`";
      }
      else if (command.command.Equals("email"))
      {
        var email = command.parameters;
        var domain = _graphConfiguration["domain"];
        if (SlashCommandHandler.isValidEmail(email, domain))
        {
          users.Add(_slashCommandHandler.getUserWithEmail(email));
        }
        else
        {
          response.text = $"You must provide a valid email address with the {domain} domain";
        }
      }
      else if (command.command.Equals("name"))
      {
        users.AddRange(_slashCommandHandler.getUsersWithStringProperty("displayName", command.parameters));
      }
      else if (command.command.Equals("withjobtitle"))
      {
        users.AddRange(_slashCommandHandler.getUsersWithStringProperty("jobTitle", command.parameters));
      }
      else if (command.command.StartsWith("with"))
      {
        var searchType = command.command.Replace("with", "");
        var plural = $"{searchType}s";
        users.AddRange(_slashCommandHandler.getUsersWithListProperty(plural, command.parameters));
      }
      #endregion
      if (users.Count.Equals(0))
      {
        response.text = $"No matches were found for `/whois-at-jis {command.command} {command.parameters}`";
      }
      else
      {
        response.attachments = SlashCommandHandler.formatUserListForSlack(users);
      }
      return response;
    }
  }
}
