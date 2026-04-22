using System;
using System.Linq;
using System.Security.Claims;

namespace AttendVisionReportsApi.Helpers
{
    public static class ClaimsHelper
    {
        /// <summary>
        /// Attempts to extract the user ID (Guid) from a ClaimsPrincipal using common claim types.
        /// Optionally logs all claims for debugging.
        /// </summary>
        /// <param name="user">The ClaimsPrincipal to extract from.</param>
        /// <param name="userId">The extracted Guid if found.</param>
        /// <param name="logClaims">If true, logs all claims to debug output.</param>
        /// <returns>True if a valid Guid user ID was found, false otherwise.</returns>
        public static bool TryGetUserId(ClaimsPrincipal user, out Guid userId, bool logClaims = false)
        {
            userId = Guid.Empty;
            if (logClaims && user != null)
            {
                foreach (var claim in user.Claims)
                {
                    System.Diagnostics.Debug.WriteLine($"CLAIM: {claim.Type} = {claim.Value}");
                }
            }
            var userIdClaim = user?.Claims.FirstOrDefault(c =>
                c.Type == "sub" ||
                c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" ||
                c.Type.EndsWith("nameidentifier"));
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var id))
            {
                userId = id;
                return true;
            }
            return false;
        }
    }
}
