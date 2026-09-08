using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace RetroBackend.Auth;

public sealed class InvitationTokenProviderOptions : DataProtectionTokenProviderOptions
{
    public const string ProviderName = "Invitation";

    public InvitationTokenProviderOptions()
    {
        Name = ProviderName;
        TokenLifespan = TimeSpan.FromDays(30);
    }
}

public sealed class InvitationTokenProvider<TUser> : DataProtectorTokenProvider<TUser>
    where TUser : class
{
    public InvitationTokenProvider(
        IDataProtectionProvider dataProtectionProvider,
        IOptions<InvitationTokenProviderOptions> options,
        ILogger<DataProtectorTokenProvider<TUser>> logger)
        : base(dataProtectionProvider, options, logger)
    {
    }
}
