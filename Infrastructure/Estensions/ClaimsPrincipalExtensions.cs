using System;
using System.Security.Claims;

namespace Infrastructure.Estensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)
                           ?? user.FindFirst("sub")
                           ?? user.FindFirst("id"); // por si usas otro claim

            if (userIdClaim == null)
                return Guid.Empty;

            return Guid.TryParse(userIdClaim.Value, out var id) ? id : Guid.Empty;
        }
    }
}
