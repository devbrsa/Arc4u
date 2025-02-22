﻿using Arc4u.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Arc4u.OAuth2.Events;

public class StandardOpenIdConnectEvents : OpenIdConnectEvents
{
    private readonly ILogger<StandardOpenIdConnectEvents> _logger;
    public StandardOpenIdConnectEvents(ILogger<StandardOpenIdConnectEvents> logger)
    {
        _logger = logger;
    }

    public override Task RedirectToIdentityProvider(RedirectContext context)
    {
        // Has been introduced for AzureAD => works also for Keykloack.
        context.ProtocolMessage.State = Guid.NewGuid().ToString();
        return base.RedirectToIdentityProvider(context);
    }

    public override async Task AuthenticationFailed(AuthenticationFailedContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.HandleResponse();

        context.Response.StatusCode = 500;
        context.Response.ContentType = "text/plain";

        _logger.Technical().LogException(context.Exception);

        await context.Response.WriteAsync("<html><p>You are not authenticated.</p></html>");

    }

    public override Task AccessDenied(AccessDeniedContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.HandleResponse();

        return context.Response.WriteAsync("<html><p>You are not authorized to use this api</p></html>");

    }

    public override Task RemoteFailure(RemoteFailureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.HandleResponse();

        return context.Response.WriteAsync("<html><p>There was an issue to contact the remote authority.</p></html>");
    }
}
