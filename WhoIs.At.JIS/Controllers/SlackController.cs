using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

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
        public void Post([FromBody] string value)
        {
        }
    }
}
