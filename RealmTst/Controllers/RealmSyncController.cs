using System;
using System.Collections.Generic;
using System.Web.Http;
using Newtonsoft.Json;
using RealmSync.Model;
using RealmSync.Server;
using RealmSync.SyncService;

namespace RealmTst.Controllers
{
    public class RealmSyncController : ApiController
    {
        private RealmSyncServerProcessor _sync;

        public RealmSyncController()
        {
            _sync = new RealmSyncServerProcessor(() => new SyncDbContext(), typeof(ChatMessage));
        }

        [Route("realmupload")]
        [HttpPost]
        public UploadDataResponse Upload([FromBody]UploadDataRequest request)
        {
            return _sync.Upload(request);
        }

        [Route("realmdownload")]
        [HttpPost]
        public DownloadDataResponse Download([FromBody]DownloadDataRequest request)
        {
            return _sync.Download(request);
        }

        // GET api/<controller>
        [Route("asd")]
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new[] { "value1", "value2" };
        }

        [HttpGet]
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