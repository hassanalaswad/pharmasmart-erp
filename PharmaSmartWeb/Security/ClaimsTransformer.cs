using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using PharmaSmartWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PharmaSmartWeb.Security
{
    // 🚀 كائن بسيط جداً لمنع انهيار EF Core
    public class CachedPermission
    {
        public string ScreenName { get; set; }
        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

    public class ClaimsTransformer : IClaimsTransformation
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMemoryCache _cache;

        public ClaimsTransformer(IServiceProvider serviceProvider, IMemoryCache cache)
        {
            _serviceProvider = serviceProvider;
            _cache = cache;
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var identity = principal.Identity as ClaimsIdentity;
            if (identity == null || !identity.IsAuthenticated) return principal;

            if (principal.HasClaim(c => c.Type == "PermissionsLoaded")) return principal;

            var clone = principal.Clone();
            var newIdentity = (ClaimsIdentity)clone.Identity;

            var roleIdStr = principal.FindFirst("RoleID")?.Value ?? principal.FindFirst("RoleId")?.Value;

            if (int.TryParse(roleIdStr, out int roleId))
            {
                string cacheKey = $"Permissions_Role_{roleId}";

                if (!_cache.TryGetValue(cacheKey, out List<CachedPermission> rolePermissions))
                {
                    try
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                            // 🚀 الحل السحري: جلب النصوص البسيطة فقط لمنع الخطأ 500
                            rolePermissions = await db.Screenpermissions
                                .Where(p => p.RoleId == roleId && p.Screen != null)
                                .Select(p => new CachedPermission
                                {
                                    ScreenName = p.Screen.ScreenName,
                                    CanView = p.CanView,
                                    CanAdd = p.CanAdd,
                                    CanEdit = p.CanEdit,
                                    CanDelete = p.CanDelete
                                })
                                .ToListAsync();
                        }

                        if (rolePermissions != null && rolePermissions.Count > 0)
                        {
                            _cache.Set(cacheKey, rolePermissions, TimeSpan.FromHours(12));
                        }
                    }
                    catch (Exception)
                    {
                        rolePermissions = new List<CachedPermission>();
                    }
                }

                // زرع الصلاحيات لكي تظهر الأزرار
                if (rolePermissions != null && rolePermissions.Count > 0)
                {
                    var uniquePermissions = rolePermissions.GroupBy(p => p.ScreenName).Select(g => g.First());
                    foreach (var p in uniquePermissions)
                    {
                        if (p.CanView) newIdentity.AddClaim(new Claim("Permission", $"{p.ScreenName}.View"));
                        if (p.CanAdd) newIdentity.AddClaim(new Claim("Permission", $"{p.ScreenName}.Add"));
                        if (p.CanEdit) newIdentity.AddClaim(new Claim("Permission", $"{p.ScreenName}.Edit"));
                        if (p.CanDelete) newIdentity.AddClaim(new Claim("Permission", $"{p.ScreenName}.Delete"));
                    }
                }
            }

            newIdentity.AddClaim(new Claim("PermissionsLoaded", "true"));
            return clone;
        }
    }
}