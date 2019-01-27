using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WhoIs.At.JIS.Models;
using WhoIs.At.JIS.Helpers;

namespace WhoIs.At.JIS.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class SlackController : ControllerBase
  {
    private static HttpClient httpClient = new HttpClient();

    // GET api/slack
    [HttpGet]
    public ActionResult<string> Get()
    {
      return "Silence is golden";
    }

    // // GET api/slack/allusers
    // [HttpGet]
    // [Route("allusers")]
    // public ActionResult<List<GraphUser>> GetAllUsers()
    // {
    //   var users = SlashCommandHandler.getAllMsGraphUsers();
    //   return users;
    // }

    // // GET api/slack/cachedusers
    // [HttpGet]
    // [Route("cachedusers")]
    // public ActionResult<List<GraphUser>> GetCachedUsers()
    // {
    //   return SlashCommandHandler.getCachedUsers();
    // }

    // GET api/slack/updatecache
    [HttpGet]
    [Route("updatecache")]
    public ActionResult<string> UpdateCache()
    {
      SlashCommandHandler.updateUserCache();
      return "Update initiated";
    }

    // GET api/slack/help
    [HttpGet]
    [Route("help")]
    public ActionResult<string> GetHelp()
    {
      return SlashCommandHandler.getHelpMessage();
    }

    // POST api/slack
    [HttpPost]
    [Produces("application/json")]
    public ActionResult<SlackResponse> Post([FromForm] SlashCommandPayload slashCommandPayload)
    {
      var responseUrl = new Uri(slashCommandPayload.response_url);
      if (!responseUrl.Host.Equals("hooks.slack.com"))
      {
        return new SlackResponse
        {
          response_type = "ephemeral",
          text = $"The host {responseUrl.Host} is not allowed"
        };
      }
      RespondToSlack(slashCommandPayload);
      return new SlackResponse
      {
        response_type = "ephemeral",
        text = $">Make sure your profile is up to date at https://delve-gcc.office.com"
      };
    }

    static async void RespondToSlack(SlashCommandPayload slashCommandPayload)
    {
      await httpClient.PostAsJsonAsync(slashCommandPayload.response_url, new SlackResponse
      {
        response_type = "ephemeral",
        text = EvaluateSlackCommand(slashCommandPayload)
      });
    }

    static string EvaluateSlackCommand(SlashCommandPayload slashCommandPayload)
    {
      WhoIsCommand command = SlashCommandHandler.getCommandFromString(slashCommandPayload.text);
      if (command.command.Equals("email"))
      {
        return SlashCommandHandler.getMsGraphResultsForEmail(command.parameters[0]);
      }
      if (command.command.Equals("name"))
      {
        var startsWith = string.Join(' ', command.parameters);
        var matches = SlashCommandHandler.getMsGraphResultsForName(startsWith);
        if (matches.Count.Equals(0))
        {
          return $"No names could be found starting with {startsWith}\nMake sure you provide names in the format of <first> <last>";
        }
        return string.Join('\n', matches.ToArray());
      }
      if (command.command.Equals("skillslist"))
      {
        return $"_Available skills:_ {string.Join(", ", SlashCommandHandler.getSkillsList())}";
      }
      if (command.command.Equals("withskill"))
      {
        var skill = string.Join(" ", command.parameters);
        if (string.IsNullOrEmpty(skill))
        {
          return "You need to provide a skill: /whois-at-jis withskill DevOps";
        }
        var users = SlashCommandHandler.getUsersWithSkill(skill);
        if (users.Count.Equals(0)) {
          return $"No users found with skill {skill}\n_Available skills:_ {string.Join(", ", SlashCommandHandler.getSkillsList())}";
        }
        return string.Join('\n', SlashCommandHandler.formatUserListForSlack(users));
      }
      if (command.command.Equals("projectslist"))
      {
        return $"_Available projects:_ {string.Join(", ", SlashCommandHandler.getProjectsList())}";
      }
      if (command.command.Equals("withproject"))
      {
        var project = string.Join(" ", command.parameters);
        if (string.IsNullOrEmpty(project))
        {
          return "You need to provide a project: /whois-at-jis withproject DevOps";
        }
        var users = SlashCommandHandler.getUsersWithProject(project);
        if (users.Count.Equals(0)) {
          return $"No users found with project {project}\n_Available projects:_ {string.Join(", ", SlashCommandHandler.getProjectsList())}";
        }
        return string.Join('\n', SlashCommandHandler.formatUserListForSlack(users));
      }
      if (command.command.Equals("interestslist"))
      {
        return $"_Available interests:_ {string.Join(", ", SlashCommandHandler.getInterestsList())}";
      }
      if (command.command.Equals("withinterest"))
      {
        var skill = string.Join(" ", command.parameters);
        if (string.IsNullOrEmpty(skill))
        {
          return "You need to provide a interest: /whois-at-jis withinterest bowling";
        }
        var users = SlashCommandHandler.getUsersWithInterest(skill);
        if (users.Count.Equals(0)) {
          return $"No users found with interest {skill}\n_Available interests:_ {string.Join(", ", SlashCommandHandler.getInterestsList())}";
        }
        return string.Join('\n', SlashCommandHandler.formatUserListForSlack(users));
      }
      return SlashCommandHandler.getHelpMessage();
    }
  }
}
