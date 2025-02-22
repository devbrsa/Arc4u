﻿using Arc4u.Dependency.Attribute;
using Arc4u.OAuth2.Token;
using Arc4u.Security.Principal;
using Microsoft.Extensions.Logging;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Arc4u.OAuth2.TokenProviders;

[Export(BootstrapContextTokenProvider.ProviderName, typeof(ITokenProvider))]
public class BootstrapContextTokenProvider : ITokenProvider
{
    const string ProviderName = "Bootstrap";

    public BootstrapContextTokenProvider(ILogger<OidcTokenProvider> logger, IApplicationContext applicationContext)
    {
        _logger = logger;
        _applicationContext = applicationContext;
    }

    private readonly ILogger<OidcTokenProvider> _logger;
    private readonly IApplicationContext _applicationContext;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="platformParameters"></param>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException" />
    /// <exception cref="TimeoutException" />
    /// <returns><see cref="TokenInfo"/></returns>
    public Task<TokenInfo> GetTokenAsync(IKeyValueSettings settings, object platformParameters)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));

        ArgumentNullException.ThrowIfNull(_applicationContext.Principal, nameof(_applicationContext.Principal));

        if (_applicationContext.Principal.Identity is ClaimsIdentity identity && !String.IsNullOrWhiteSpace(identity?.BootstrapContext?.ToString()))
        {
            var token = identity.BootstrapContext.ToString();

            JwtSecurityToken jwt = new(token);

            if (jwt.ValidTo > DateTime.UtcNow)
                return Task.FromResult(new TokenInfo("Bearer", token, jwt.ValidTo));

            throw new TimeoutException("The token provided is expired.");
        }

        throw new AppPrincipalException("No Access token stored in the Identity.");
    }

    /// <summary>
    /// There is no way to signout in this scenario.
    /// </summary>
    /// <param name="settings"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void SignOut(IKeyValueSettings settings)
    {
        throw new NotImplementedException();
    }
}

