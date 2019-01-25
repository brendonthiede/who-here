using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WhoIs.At.JIS.Models;

namespace WhoIs.At.JIS.Data
{
  public class ProfileDataService
  {
    private ProfileData[] dummyData;

    public ProfileDataService() {
      dummyData = new ProfileData[] {
        new ProfileData{
          displayName = "My name",
          jobTitle = "Job"
        }
      };
    }
  }
}