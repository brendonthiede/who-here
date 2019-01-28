using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
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
    private readonly HttpClient _httpClient;
    private readonly SlashCommandHandler _slashCommandHandler;
    private readonly GraphHandler _graphHandler;

    public SlackController(IConfiguration configuration)
    {
      _slackConfiguration = configuration.GetSection("slack");
      _graphConfiguration = configuration.GetSection("graph");
      _slashCommandHandler = new SlashCommandHandler(configuration);
      _graphHandler = new GraphHandler(configuration);
      _httpClient = new HttpClient();
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
          response_type = "ephemeral",
          text = "Authentication token is not set up correctly on the whoisatjis server"
        };
      }
      if (!slashCommandPayload.token.Equals(authToken))
      {
        return new SlackResponse
        {
          response_type = "ephemeral",
          text = "Invalid authentication token"
        };
      }
      var responseUrl = new Uri(slashCommandPayload.response_url);
      if (!responseUrl.Host.Equals("hooks.slack.com"))
      {
        return new SlackResponse
        {
          response_type = "ephemeral",
          text = $"The host {responseUrl.Host} is not allowed"
        };
      }
      return new SlackResponse
      {
        response_type = "ephemeral",
        text = $"{EvaluateSlackCommand(slashCommandPayload)}\n>Make sure your profile is up to date at https://delve-gcc.office.com"
      };
    }

    private string EvaluateSlackCommand(SlashCommandPayload slashCommandPayload)
    {
      WhoIsCommand command = SlashCommandHandler.getCommandFromString(slashCommandPayload.text);
      if (command.command.Equals("debug"))
      {
        return "";
      }
      if (command.command.Equals("email"))
      {
        var email = command.parameters[0];
        var domain = _graphConfiguration["domain"];
        if (string.IsNullOrEmpty(email) || !SlashCommandHandler.isValidEmail(email, domain))
        {
          return $"You must provide a valid email address with the {domain} domain";
        }
        var user = _slashCommandHandler.getUserWithEmail(email);
        if (user == null || !user.userPrincipalName.Equals(email, StringComparison.InvariantCultureIgnoreCase))
        {
          return $"No user could be found with email address {email}";
        }
        return _slashCommandHandler.formatUserForSlack(user);
      }
      if (command.command.Equals("name"))
      {
        var startsWith = string.Join(' ', command.parameters);
        if (string.IsNullOrEmpty(startsWith))
        {
          return "You need to provide a name: /whois-at-jis name Mark";
        }
        var users = _slashCommandHandler.getUsersWithName(startsWith);
        if (users.Count.Equals(0))
        {
          return $"No users found with a name that starts with {startsWith}";
        }
        return string.Join('\n', _slashCommandHandler.formatUserListForSlack(users));
      }
      if (command.command.Equals("skillslist"))
      {
        return $"_Available skills:_ {string.Join(", ", _slashCommandHandler.getSkillsList())}";
      }
      if (command.command.Equals("withskill"))
      {
        var skill = string.Join(" ", command.parameters);
        if (string.IsNullOrEmpty(skill))
        {
          return "You need to provide a skill: /whois-at-jis withskill DevOps";
        }
        var users = _slashCommandHandler.getUsersWithSkill(skill);
        if (users.Count.Equals(0))
        {
          return $"No users found with skill {skill}\n_Available skills:_ {string.Join(", ", _slashCommandHandler.getSkillsList())}";
        }
        return string.Join('\n', _slashCommandHandler.formatUserListForSlack(users));
      }
      if (command.command.Equals("projectslist"))
      {
        return $"_Available projects:_ {string.Join(", ", _slashCommandHandler.getProjectsList())}";
      }
      if (command.command.Equals("withproject"))
      {
        var project = string.Join(" ", command.parameters);
        if (string.IsNullOrEmpty(project))
        {
          return "You need to provide a project: /whois-at-jis withproject DevOps";
        }
        var users = _slashCommandHandler.getUsersWithProject(project);
        if (users.Count.Equals(0))
        {
          return $"No users found with project {project}\n_Available projects:_ {string.Join(", ", _slashCommandHandler.getProjectsList())}";
        }
        return string.Join('\n', _slashCommandHandler.formatUserListForSlack(users));
      }
      if (command.command.Equals("interestslist"))
      {
        return $"_Available interests:_ {string.Join(", ", _slashCommandHandler.getInterestsList())}";
      }
      if (command.command.Equals("withinterest"))
      {
        var skill = string.Join(" ", command.parameters);
        if (string.IsNullOrEmpty(skill))
        {
          return "You need to provide a interest: /whois-at-jis withinterest bowling";
        }
        var users = _slashCommandHandler.getUsersWithInterest(skill);
        if (users.Count.Equals(0))
        {
          return $"No users found with interest {skill}\n_Available interests:_ {string.Join(", ", _slashCommandHandler.getInterestsList())}";
        }
        return string.Join('\n', _slashCommandHandler.formatUserListForSlack(users));
      }
      return _slashCommandHandler.getHelpMessage();
    }
  }
}
