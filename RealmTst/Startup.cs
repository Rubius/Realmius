using System.Linq;
using Microsoft.Owin;
using Owin;
using RealmTst.Controllers;

[assembly: OwinStartupAttribute(typeof(RealmTst.Startup))]
namespace RealmTst
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);

            var db = new SyncDbContext();
            var count = db.ChatMessages.Count();
        }
    }
}
