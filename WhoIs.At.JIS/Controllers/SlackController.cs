﻿using Microsoft.AspNetCore.Mvc;
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
      #region Commands without parameters
      if (command.command.Equals("help") || !SlashCommandHandler.isValidCommand(command.command))
      {
        return _slashCommandHandler.getHelpMessage();
      }
      if (command.command.EndsWith("list"))
      {
        var listType = command.command.Replace("list", "");
        return $"_Available {listType}:_ {string.Join(", ", _slashCommandHandler.getUniqueValuesForListProperty(listType))}";
      }
      #endregion

      #region Commands with required parameters
      if (string.IsNullOrEmpty(command.parameters))
      {
        return $"You need to provide a search value: /whois-at-jis {command.command} <value>";
      }
      if (command.command.Equals("email"))
      {
        var email = command.parameters;
        var domain = _graphConfiguration["domain"];
        if (!SlashCommandHandler.isValidEmail(email, domain))
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
        var users = _slashCommandHandler.getUsersWithName(command.parameters);
        if (users.Count.Equals(0))
        {
          return $"No users found with a name that starts with {command.parameters}";
        }
        return string.Join('\n', _slashCommandHandler.formatUserListForSlack(users));
      }
      if (command.command.Equals("withskill"))
      {
        var skill = command.parameters;
        if (string.IsNullOrEmpty(skill))
        {
          return "You need to provide a skill: /whois-at-jis withskill DevOps";
        }
        var users = _slashCommandHandler.getUsersWithSkill(skill);
        if (users.Count.Equals(0))
        {
          return $"No users found with skill {skill}\n_Available skills:_ {string.Join(", ", _slashCommandHandler.getUniqueValuesForListProperty("skills"))}";
        }
        return string.Join('\n', _slashCommandHandler.formatUserListForSlack(users));
      }
      if (command.command.Equals("withproject"))
      {
        var project = command.parameters;
        if (string.IsNullOrEmpty(project))
        {
          return "You need to provide a project: /whois-at-jis withproject DevOps";
        }
        var users = _slashCommandHandler.getUsersWithProject(project);
        if (users.Count.Equals(0))
        {
          return $"No users found with project {project}\n_Available projects:_ {string.Join(", ", _slashCommandHandler.getUniqueValuesForListProperty("projects"))}";
        }
        return string.Join('\n', _slashCommandHandler.formatUserListForSlack(users));
      }
      if (command.command.Equals("withinterest"))
      {
        var interest = command.parameters;
        if (string.IsNullOrEmpty(interest))
        {
          return "You need to provide a interest: /whois-at-jis withinterest bowling";
        }
        var users = _slashCommandHandler.getUsersWithInterest(interest);
        if (users.Count.Equals(0))
        {
          return $"No users found with interest {interest}\n_Available interests:_ {string.Join(", ", _slashCommandHandler.getUniqueValuesForListProperty("interests"))}";
        }
        return string.Join('\n', _slashCommandHandler.formatUserListForSlack(users));
      }
      #endregion

      return _slashCommandHandler.getHelpMessage();
    }
  }
}
