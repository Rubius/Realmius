using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Realms;

namespace RealmTst
{
    public class Cat : RealmObject
    {
        public string Tst { get; set; }
        public string Tst2 { get; set; }

        public Dog Dog { get; set; }
    }

    public class Dog : RealmObject
    {
        public string Tst { get; set; }

    }

    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            //var realm = Realm.GetInstance(@"c:\123\s.dat");
            //realm.Write(() =>
            //{
            //    realm.Add(new Cat()
            //    {
            //        Tst = "123" + DateTime.Now.ToString(),
            //        Tst2 = "123",
            //    });
            //});

            //var dt = realm.All<Cat>().Where(x => x.Tst2 == "123");
            ////.ToList();
            //dt.AsRealmCollection().SubscribeForNotifications(cat =>
            //{
            //    //cat.
            //});

        }
    }
}
