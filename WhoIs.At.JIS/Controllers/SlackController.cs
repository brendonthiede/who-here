using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WhoIs.At.JIS.Models;

namespace WhoIs.At.JIS.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class SlackController : ControllerBase
  {
    // GET api/slack
    [HttpGet]
    public ActionResult<string> Get()
    {
      return "You should set up your profile in <a href='https://delve-gcc.office.com'>SharePoint</a>";
    }

    // POST api/slack
    [HttpPost]
    [Produces("application/json")]
    public ActionResult<SlackResponse> Post([FromForm] SlashCommandPayload slashCommandPayload)
    {
      return new SlackResponse {
        response_type = "ephemeral",
        text = "Make sure your profile is up to date at https://delve-gcc.office.com"
      };
    }
  }
}
