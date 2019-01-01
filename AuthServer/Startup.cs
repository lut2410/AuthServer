using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuthServer.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AuthServer.Data.DataSeeding;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace AuthServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));
            services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultUI()
            .AddDefaultTokenProviders();

            services.AddScoped<IDataSeeding, DataSeeding>();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            try
            {
                UpdateDatabase(app);
                SeedData(app);
            }
            catch (Exception ex)
            {

            }
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
        private static void UpdateDatabase(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices
                .GetRequiredService<IServiceScopeFactory>()
                .CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetService<ApplicationDbContext>())
                {
                    context.Database.Migrate();
                }
            }
        }

        async Task SeedData(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices
                .GetRequiredService<IServiceScopeFactory>()
                .CreateScope())
            {



                var userManager = serviceScope.ServiceProvider.GetService<UserManager<IdentityUser>>();
                var user = new IdentityUser("normaluser@mail.com");
                await userManager.CreateAsync(user, "Qq!1234");

                var admin = new IdentityUser("admin@mail.com");
                await userManager.CreateAsync(admin, "Qq!1234");


                var applicationDbContext = serviceScope.ServiceProvider.GetService<ApplicationDbContext>();
                var roleManager = serviceScope.ServiceProvider.GetService<RoleManager<IdentityRole>>();
                var roleNames = new List<string>() { "User", "Admin", "Super Admin" };
                foreach (var roleName in roleNames)
                {
                    await applicationDbContext.Roles.AddAsync(new IdentityRole(roleName));
                }
                var userRole = new IdentityUserRole<string>
                {
                    UserId = applicationDbContext.Users.FirstOrDefault(r => r.UserName == "normaluser@mail.com").Id,
                    RoleId = applicationDbContext.Roles.Local.FirstOrDefault(r => r.Name == "User").Id
                };
                applicationDbContext.UserRoles.Add(userRole);
                var adminRole = new IdentityUserRole<string>
                {
                    UserId = applicationDbContext.Users.FirstOrDefault(r => r.UserName == "admin@mail.com").Id,
                    RoleId = applicationDbContext.Roles.Local.FirstOrDefault(r => r.Name == "Admin").Id
                };
                applicationDbContext.UserRoles.Add(adminRole);
                applicationDbContext.SaveChanges();
            }
        }
    }
}
