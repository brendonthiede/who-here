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
    public ActionResult<string> Post([FromForm] SlashCommandPayload slashCommandPayload)
    {
      return "You should set up your profile at https://delve-gcc.office.com";
    }
  }
}
