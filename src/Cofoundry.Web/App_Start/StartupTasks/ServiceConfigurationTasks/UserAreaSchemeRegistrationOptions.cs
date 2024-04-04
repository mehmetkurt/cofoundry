using Cofoundry.Web.Extendable;

namespace Cofoundry.Web;

/// <summary>
/// Options used to configure the authentication scheme(s) for a user 
/// area in <see cref="DefaultAuthConfiguration.ConfigureUserAreaScheme"/>.
/// These options are provided as a convenience for developers overriding the
/// default behaviour.
/// </summary>
public class UserAreaSchemeRegistrationOptions
{
    public UserAreaSchemeRegistrationOptions(
        IUserAreaDefinition userArea,
        string scheme,
        string cookieNamespace
        )
    {
        UserArea = userArea;
        Scheme = scheme;
        CookieNamespace = cookieNamespace;
    }

    /// <summary>
    /// The user area to be registered.
    /// </summary>
    public IUserAreaDefinition UserArea { get; private set; }

    /// <summary>
    /// The default auth Scheme name used by Cofoundry to register the scheme.
    /// This is constructed using <see cref="AuthenticationSchemeNames.UserArea(string)"/>
    /// and is the same scheme name use to authenticate users when manging user sessions.
    /// </summary>
    public string Scheme { get; private set; }

    /// <summary>
    /// This prefix is generated by <see cref="IAuthCookieNamespaceProvider"/>
    /// and can be used to construct a unique cookie name for the application
    /// instance.
    /// </summary>
    public string CookieNamespace { get; private set; }
}
