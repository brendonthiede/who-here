# Who Here

## Purpose

The _Who Here_ app was created of a "Hack Day". The problem statement was that there are many people around the office that you don't know, but may have similar interests. Rather than try to come up with a separate CRUD application for people to enter their data, we decided to leverage the information that was already available from SharePoint, which we discovered is exposed by the Microsoft Graph API.

## Technology

The app is a .NET Core Web API, intended for use with Slack as a Slash Command. It uses the Microsoft Graph API to pull user profiles from Office 365.

## Creating an App Registry

you will need to create an App Registration to be used as a App registration to connect to Microsoft Graph. There are some instructions here: [ASP.NET Graph Example](https://docs.microsoft.com/en-us/graph/tutorials/aspnet?tutorial-step=2). When creating the app registration, keep track of the Application ID and the redirect Uri that you chose (it can just be https://localhost:8080 or anything else you choose). You will also need to generate a client secret and keep track of that.

## Running Unit Tests

You can run unit tests by running `dotnet test` against the test csproj directly, or by using PSake:

```powershell
.\scripts\build.ps1 -Task Test
```

## Running Locally

In order to run locally, you will need to create a file named `appsettings.json` with contents like this:

```json
{
  "graph": {
    "applicationId": "ID_FROM_AZURE_ACTIVE_DIRECTORY",
    "applicationSecret": "CLIENT_SECRET_FROM_AZURE_ACTIVE_DIRECTORY",
    "tenantId": "TENANT_ID_OF_AZURE_ACTIVE_DIRECTORY",
    "redirectUri": "https://localhost:8080",
    "domain": "my.mail.com"
  },
  "slack": {
    "slashCommandToken": "abc123"
  }
}
```

The value of the `slashCommandToken` doesn't matter locally, but you just need to match it in Postman or whatever you use to hot your local endpoint.

Now you can run the unit tests

## Deploying

You should set up the Slash Command in Slack (instructions [here](https://api.slack.com/slash-commands)). You can guess at the URL for the application for now and you can go back and update it later, but for now just grab the "Verification Token" shown under the App Credentials configured for the Slash Command.

Now you can deploy the infrastructure via the ARM template. This can be done through `New-ARMTemplateDeployment.ps1` in the scripts directory, passing the necessary parameters, ensuring that your `webAppName` is unique.

## Usage

Once the Slash Command is installed, you invoke it as `/who-here <property> <filter>`. To get a list of available search properties, invoke it as `/who-here help`. All searches go against cached data, which may not show changes made to profiles within the last usage (each invocation causes the cache to refresh, which may take a couple minutes).

## Updating User Profiles

In order to update your user profile to better enable searches with the app, you can add or change details for your profile by going to [https://delve-gcc.office.com](https://delve-gcc.office.com). Once logged in, you can click your initials in the upper right hand corner and select "My profile" in order to get to your profile. Once there, click "Update profile". Now you can scroll to the items you want to update, such as projects, skills, and interests. Looking for inspiration? Check out what people are already listing by looking at one of the list commands, such as `/who-here projects list`
