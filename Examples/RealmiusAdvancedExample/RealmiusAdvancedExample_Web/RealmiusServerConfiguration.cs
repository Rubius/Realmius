using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Security.Provider;
using Realmius.Server;
using Realmius.Server.Configurations;
using Realmius.Server.Models;
using Realmius.Server.Infrastructure;
using Realmius.Contracts.Logger;
using RealmiusAdvancedExample_Web.DAL;
using RealmiusAdvancedExample_Web.Models;

namespace RealmiusAdvancedExample_Web
{
    public class RealmiusServerAuthConfiguration : RealmiusConfigurationBase<User>
    {
        public RealmiusServerAuthConfiguration() : base(() => new RealmiusServerContext())
        {
            Logger = new Logger();
        }

        public IList<Type> TypesToSyncList { get; set; }

        public override IList<Type> TypesToSync => Startup.TypesForSync;

        public override User AuthenticateUser(IRequest request)
        {
            var userName = request.QueryString["userLogin"];
            var password = request.QueryString["pass"];
            var user = UsersCredentialsDict.GetUser(userName, password);
            return user;
        }

        //allows to check and process the addition and editing of user's objects
        public override bool CheckAndProcess(CheckAndProcessArgs<User> args)
        {
            //var db = args.Database as RealmiusServerContext;

            if (args.Entity is NoteRealm)
            {
                //if the first upload of the object
                if (args.OriginalDbEntity == null)
                {
                    var newNote = args.Entity as NoteRealm;
                    newNote.UserRole = args.User.Role;
                    return true;
                }
                //otherwise check user's rights to edit the object
                if (args.User.Role >= (args.OriginalDbEntity as NoteRealm).UserRole)
                {
                    return true;
                }
            }

            if (args.Entity is PhotoRealm)
            {
                //photos are editable by everyone
                return true;
            }
            if (args.Entity is ChatMessageRealm)
            {
                //chat messages are not editable at all
                if (args.OriginalDbEntity != null)
                    return false;

                return true;
            }

            return false;
        }

        public override IList<string> GetTagsForObject(ChangeTrackingDbContext db, IRealmiusObjectServer obj)
        {
            if (obj is NoteRealm)
            {
                var currentNote = obj as NoteRealm;
                var tagsList = GetAllRolesAsInt().Where(v => v >= currentNote.UserRole).Select(x => x.ToString()).ToList();
                return tagsList;
            }

            if (obj is PhotoRealm)
            {
                //all photos are available to everyone
                return new List<string>() { ((int)UserRole.Anonymous).ToString() };
            }

            if (obj is ChatMessageRealm)
            {
                //all chat messages are available to everyone
                return new List<string>() { ((int)UserRole.Anonymous).ToString() };
            }

            return null;
        }

        private IEnumerable<int> GetAllRolesAsInt()
        {
            return Enum.GetValues(typeof(UserRole)).Cast<int>();
        }

        public override IList<string> GetTagsForUser(User user, ChangeTrackingDbContext db)
        {
            return GetAllRolesAsInt().Where(v => v <= user.Role).Select(x => x.ToString()).ToList();
        }
    }
}