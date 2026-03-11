using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using static Duende.IdentityModel.OidcConstants;

namespace BirthPlatform.Services;

public class OidcEvents(IConfiguration configuration) : OpenIdConnectEvents
{
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var exp = DateTimeOffset.UtcNow.AddSeconds(double.Parse(context.TokenEndpointResponse!.ExpiresIn));

        await base.TokenValidated(context);
    }



    public override async Task AuthorizationCodeReceived(AuthorizationCodeReceivedContext context)
    {
        // Invoked after security token validation if an authorization code is present in the protocol message.

        // Gets parameters for the payload that is used in the client assertion;
        // for instance the underenhet org number if needed.
        // This will typically be aligned with the logged on user:

        // We have received the authorization code (the user has logged on), and we need to
        // authorize ourselves for HelseId. This requires a client assertion, which we must build.
        // The ClientAssertion type comes from the IdentityModel library.
        var clientAssertion = BuildClientAssertion(DateTime.Now.AddMinutes(1));
        // The client assertion type is hardcoded as a Jwt bearer type
        context.TokenEndpointRequest!.ClientAssertionType = ClientAssertionTypes.JwtBearer;
        // Asserts the client by using the generated Jwt (the value from the type)
        context.TokenEndpointRequest.ClientAssertion = clientAssertion;
    }

    public override async Task RedirectToIdentityProvider(RedirectContext context)
    {
        // Invoked before redirecting to the identity provider to authenticate. This can be used to
        // set a ProtocolMessage.State that will be persisted through the authentication process.
        // The ProtocolMessage can also be used to add or customize parameters sent to the identity provider.

        // For certain features, we need to establish a custom request message for creating
        // request objects or resource indicators.  The implementation of the former ('resource')
        // is not in conformance with the specification (https://www.rfc-editor.org/rfc/rfc8707), and
        // the (optional) 'request' parameter (https://openid.net/specs/openid-connect-core-1_0.html#JWTRequests)
        // is not currently implemented
        if (context.ProtocolMessage.RequestType == OpenIdConnectRequestType.Authentication)
        {
            var pushedAuthorizationResponse = await PushAuthorizationParameters(context);

            // Remove all the parameters from the protocol message, and replace with what we got from the PAR response
            context.ProtocolMessage.Parameters.Clear();

            // Then, set client id and request uri as parameters
            context.ProtocolMessage.ClientId = configuration.GetValue<string>("ClientId");
            context.ProtocolMessage.RequestUri = pushedAuthorizationResponse.RequestUri;

            // Mark the request as handled, because we don't want the normal
            // behavior that attaches state to the outgoing request (we already
            // did that in the PAR request).
            context.HandleResponse();

            // Finally redirect to the authorize endpoint
            RedirectToAuthorizeEndpoint(context);
        }
    }



    private string BuildClientAssertion(DateTime expiredDate)
    {
        try
        {
            var signingCredentials = new SigningCredentials(new JsonWebKey(configuration.GetValue<string>("PrivateJwt")), "RS256");

            var claims = new Dictionary<string, object>()
            {
                { JwtClaimTypes.Subject, configuration.GetValue<string>("ClientId") ?? throw new ArgumentNullException() },
                { JwtClaimTypes.IssuedAt, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() },
                { JwtClaimTypes.JwtId, Guid.NewGuid().ToString("N") },
            };

            var std = new SecurityTokenDescriptor
            {
                Issuer = configuration.GetValue<string>("ClientId"),
                Audience = configuration.GetValue<string>("Authority"),
                Claims = claims,
                Expires = expiredDate,
                NotBefore = DateTime.Now,
                SigningCredentials = signingCredentials,
                TokenType = "client-authentication+jwt",
            };

            return new JsonWebTokenHandler().CreateToken(std);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    private async Task<PushedAuthorizationResponse> PushAuthorizationParameters(RedirectContext redirectContext)
    {
        // See https://helseid.atlassian.net/wiki/spaces/HELSEID/pages/5571426/Use+of+ID-porten for more examples:
        // If you want to authenticate the user on behalf of a specific organization, you can add this parameter:
        // redirectContext.ProtocolMessage.Parameters.Add("on_behalf_of", "912159523");

        // Construct the state parameter and add it to the protocol message so that we can include it in the pushed authorization request
        redirectContext.Properties.Items.Add(OpenIdConnectDefaults.RedirectUriForCodePropertiesKey, redirectContext.ProtocolMessage.RedirectUri);
        redirectContext.Properties.Items.Add("test_security_level", "4");
        redirectContext.ProtocolMessage.State = redirectContext.Options.StateDataFormat.Protect(redirectContext.Properties);

        // The client assertion is required for HelseID to authenticate the client
        var clientAssertion = BuildClientAssertion(DateTime.Now.AddMinutes(1));

        var par = new PushedAuthorizationRequest
        {
            // Most parameters are take from the protocol message:
            Parameters = new Parameters(redirectContext.ProtocolMessage.Parameters),
            Address = configuration.GetValue<string>("Authority") + "connect/par",
            ClientCredentialStyle = ClientCredentialStyle.PostBody,
            ClientAssertion = new ClientAssertion
            {
                Type = ClientAssertionTypes.JwtBearer,
                Value = clientAssertion
            },
            RequestUri = new Uri(configuration.GetValue<string>("Authority") + "connect/par"),

            // The metadata endpoint https://helseid-sts.test.nhn.no/connect/availableidps list the available IDPs
            // in the test environment. If you want to redirect the user to a specific IDP, you can specify this value.
            // AcrValues = "idp:testidpnew-oidc",
            // Use Buypass with smart card:
            // AcrValues = "idp:buypass-oidc bp:amr:sc",
        };

        using var httpClient = new HttpClient();

        var response = await httpClient.PushAuthorizationAsync(par);

        if (response.IsError)
        {
            throw new Exception($"PAR failure: {response.Json.ToString()}");
        }

        return response;
    }

    private static void RedirectToAuthorizeEndpoint(RedirectContext context)
    {
        // This code is copied from the ASP.NET handler. We use most of its default behavior related to
        // redirecting to the identity provider, except that we have already pushed the state parameter,
        // so that is left out here.
        // See https://github.com/dotnet/aspnetcore/blob/c85baf8db0c72ae8e68643029d514b2e737c9fae/src/Security/Authentication/OpenIdConnect/src/OpenIdConnectHandler.cs#L364

        var message = context.ProtocolMessage;
        if (string.IsNullOrEmpty(message.IssuerAddress))
        {
            throw new InvalidOperationException(
                "Cannot redirect to the authorization endpoint, the configuration may be missing or invalid.");
        }
        var redirectUri = message.CreateAuthenticationRequestUrl();
        if (!Uri.IsWellFormedUriString(redirectUri, UriKind.Absolute))
        {
            throw new InvalidOperationException($"The redirect URI is not well-formed. The URI is: '{redirectUri}'.");
        }

        context.Response.Redirect(redirectUri);
    }
}
