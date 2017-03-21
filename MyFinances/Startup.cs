using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(MyFinances.Startup))]
namespace MyFinances
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
