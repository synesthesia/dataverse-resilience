# DV Console

The programmes in this directory demonstrate resilience techniques for calling Dataverse 
from console applications.



## Setting up to build examples

- Make sure you have Net 6.0 SDK on your machine
- Set an environment variable `DOTNET_ENVIRONMENT` to value `Development` 
(this allows you to make use of user secrets).


## Running the examples

- you will need a Dataverse environment to run the samples against
- you will need to register an application in Azure AD 
see [Register an app with Azure Active Directory](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/walkthrough-register-app-azure-active-directory#create-an-application-registration) 
    make a note of the client ID and client secret
- if you want to run the samples in ClientSecret mode, [create an S2S user]()
in your Dataverse environment the ClientId of your registered app, grant that user sufficient security permissions
- select "Manage User Secrets" in Visual studio, or in the project directory run `dotnet user-secrets init`
- copy the attributes needed for User Secrets from `appsettings.json` and add values from your own configuration
