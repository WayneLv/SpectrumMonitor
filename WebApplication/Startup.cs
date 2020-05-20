using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(FirstWebApplication.WebApplication.Startup))]
namespace FirstWebApplication.WebApplication
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
