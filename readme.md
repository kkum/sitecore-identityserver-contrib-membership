# IdentityServer4.Contrib.Membership

## ASP.NET 2.0 Membership Database as Identity Server User Store
Identity Server is a framework and doesn't provide implementations of user data sources out of the box.
If you have an existing ASP.NET 2.0 Membership Database containing user data for existing systems then you can install the following package:

This will validate user logins and passwords against an existing database.  No support is provided for maintaining users and it is not recommended that you use this for a new implementation. 
IdentityServer provides a [plugin](https://github.com/IdentityServer/IdentityServer4) that supports [ASP.NET Identity](http://www.asp.net/identity).

#### Creating an empty ASP.NET 2.0 Membership database
To create an empty ASP.NET 2.0 Membership database, run the following command:
    `C:\Windows\Microsoft.NET\Framework\v2.0.50727\aspnet_regsql.exe`

When the wizard opens, select next then "Configure SQL Server for application services" then next again. Select the Server instance on which the database will run and give the Database name "Membership" then continue.

The sample code can be viewed [here](/samples/IdentityServer4.Contrib.Membership.IdsvrDemo)
