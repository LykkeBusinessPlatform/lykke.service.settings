using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Web.Extensions
{
    public static class HttpContextExtensions
    {
        public static string GetUserEmail(this HttpContext ctx) =>
            ctx.User.Claims.FirstOrDefault(u => u.Type.Equals(ClaimTypes.Sid))?.Value;

        public static string GetUserName(this HttpContext ctx) =>
            ctx.User.Claims.FirstOrDefault(u => u.Type.Equals(ClaimTypes.Name))?.Value;

        public static bool IsAdmin(this HttpContext ctx) =>
            bool.TryParse(ctx.User.Claims.FirstOrDefault(u => u.Type.Equals("IsAdmin"))?.Value, out bool res) && res;
    }
}
