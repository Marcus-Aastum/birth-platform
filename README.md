# Birth platform (Fødselsplattformen)

This is a project made for a test vocational exam in Software Development.

## Setup

To run this project, you will need an MSSQL database set up with an empty database called `birth-platform`. You will also need HelseID credentials to authenticate users. 

1. Setup the required secrets, either as user secrets or ENV-variables
    ```json
    {
      "PrivateJwt": "your private key from HelseID",
      "ClientId": "your HelseID ClientId",
      "ConnectionString": "Connection string to your MSSQL server"
    }
    ```
2. Run the necessary migrations using `dotnet ef database update`
    1. If you don't have the ef-tool already, install it with `dotnet tool install --global dotnet-ef`
3. Run the project

Alternativly, switch out .UseSqlServer() with .UseInMemoryDatabase("birthplatform") in the `Program.cs`-file to run with an In memory database. In that case you will not need to run the migrations. 