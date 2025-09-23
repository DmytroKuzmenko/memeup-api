using Microsoft.AspNetCore.Identity;

namespace Memeup.Api.Domain.Auth;

public class ApplicationUser : IdentityUser<Guid>
{
    // можно расширить, пока достаточно стандартных полей
}
