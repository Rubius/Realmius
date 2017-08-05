using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Microsoft.Owin.Builder;
using Microsoft.Owin.Cors;
using Owin;
using Realmius.Server.Models;
using Realmius.Server.QuickStart;
using RealmiusAdvancedExample_Web.DAL;
using RealmiusAdvancedExample_Web.Models;

namespace RealmiusAdvancedExample_Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}
