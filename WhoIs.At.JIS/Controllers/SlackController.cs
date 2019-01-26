﻿using System;
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
      return "This API only accepts posts in the format of a Slack Slash Command";
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
      return SlashCommandHandler.getHelpMessage();
    }
  }
}
