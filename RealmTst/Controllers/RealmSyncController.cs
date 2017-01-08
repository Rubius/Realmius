using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Web.Http;
using Newtonsoft.Json;
using RealmSync.Model;
using RealmSync.SyncService;
using RealmTst.Migrations;

namespace RealmTst.Controllers
{
    public class SyncDbContext : DbContext
    {
        static SyncDbContext()
        {
            System.Data.Entity.Database.SetInitializer<SyncDbContext>(new MigrateDatabaseToLatestVersion<SyncDbContext, Configuration>());
        }

        public DbSet<ChatMessage> ChatMessages { get; set; }
        //public DbSet<ToDoItem> ToDoItems { get; set; }
        //public DbSet<Project> Projects { get; set; }
    }

    public class RealmSyncController : ApiController
    {

        public UploadDataResponse Upload([FromBody]UploadDataRequest request)
        {
            var result = new UploadDataResponse();

            var ef = new SyncDbContext();
            foreach (var item in request.ChangeNotifications)
            {
                var type = Type.GetType("RealmSync.Model." + item.Type);
                var deserialized = JsonConvert.DeserializeObject(item.SerializedObject, type);

                ef.Set(type).Attach(deserialized);
                CheckAndProcess((dynamic)deserialized);
                //item.Type
            }
            //request.ChangeNotifications
            //ef.
            //ef.ChatMessages

            return null;
        }

        private void CheckAndProcess(ChatMessage chatMessage)
        {
            Console.WriteLine("123");
        }

        private void CheckAndProcess(object chatMessage)
        {
            Console.WriteLine("456");
        }

        // GET api/<controller>
        public IEnumerable<string> Get()
        {
            return new[] { "value1", "value2" };
        }

        // GET api/<controller>/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<controller>
        public void Post([FromBody]string value)
        {
        }

        // PUT api/<controller>/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
        public void Delete(int id)
        {
        }
    }
}