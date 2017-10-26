////////////////////////////////////////////////////////////////////////////
//
// Copyright 2017 Rubius
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Security.Claims;
using Realmius.Server.Infrastructure;
using Realmius.Server.Models;
using Realmius.Contracts.Logger;

namespace Realmius.Server.Configurations
{
    public abstract class RealmiusConfigurationBase : RealmiusConfigurationBase<object>
    {
        protected RealmiusConfigurationBase(Func<ChangeTrackingDbContext> contextFactoryFunc) : base(contextFactoryFunc)
        {
        }
    }

    public abstract class RealmiusConfigurationBase<TUser> : IRealmiusServerConfiguration<TUser>
    {
        /// <summary>
        /// the list of types to be synced. 
        /// </summary>
        public abstract IList<Type> TypesToSync { get; }
        
        public virtual ILogger Logger { get; set; }

        /// <summary>
        /// Whenever an object is updated, this function is called to get the list of Tags associated with object.
        /// To understand how Tag-based security works please read the comment for GetTagsForUser function.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="obj"></param>
        /// <returns>List of tags for the object (max. 3)</returns>
        public abstract IList<string> GetTagsForObject(ChangeTrackingDbContext db, IRealmiusObjectServer obj);

        /// <summary>
        /// This function determines, whether the user is allowed to make changes to the database.
        /// That is, every time the object is uploaded from the client, it's going through the CheckAndProcess.
        /// It's your responsibility to say, whether the update is allowed or not.
        /// </summary>
        /// <param name="args"></param>
        /// <returns>Return true if the changes are allowed; false otherwise.</returns>
        public abstract bool CheckAndProcess(CheckAndProcessArgs<TUser> args);

        protected RealmiusConfigurationBase(Func<ChangeTrackingDbContext> contextFactoryFunc)
        {
            ContextFactoryFunction = contextFactoryFunc;

            QuickStart.RealmiusServer.Configurations[typeof(TUser)] = this;
            ChangeTrackingDbContext.Configurations[contextFactoryFunc().GetType()] = this;
        }

        /// <summary>
        /// This function determines, whether the user is allowed to make changes to the database.
        /// That is, every time the object is uploaded from the client, it's going through the CheckAndProcess.
        /// It's your responsibility to say, whether the update is allowed or not.
        /// </summary>
        /// <returns>Return true if the changes are allowed; false otherwise.</returns>
        public bool CheckAndProcess(ChangeTrackingDbContext ef, IRealmiusObjectServer deserialized, TUser user)
        {
            return CheckAndProcess(new CheckAndProcessArgs<TUser>
            {
                Entity = deserialized,
                Database = ef,
                User = user,
                OriginalDbEntity = ef.CloneWithOriginalValues(deserialized),
            });
        }

        public virtual object[] KeyForType(Type type, string itemPrimaryKey)
        {
            return new object[] { itemPrimaryKey };
        }

        /// <summary>
        /// Realmius uses Tag-based security. 
        /// I.e. each object stored in the database has Tags associated with it (see ``GetTagsForObject`` below). 
        /// Each user has Tags the user has access to. 
        /// If user has access to at least one object's tag, the access to that object is granted and it will be synced to the client. 
        /// If no tags match between the user and the server, then the object won't be synced.      
        /// By convention, we use tag "all" for objects that are accessible to all authorized users.
        /// </summary>
        /// <param name="user">User object returned from AuthenticateUser method</param>
        /// <param name="db">reference to the database to retrieve Tags from it (if needed)</param>
        /// <returns>Return the tags the user has access to from this method.The method is called once, soon after the user is connected.</returns>
        public abstract IList<string> GetTagsForUser(TUser user, ChangeTrackingDbContext db);

        /// <summary>
        /// this is where user authentication happen. 
        /// IRequest is a SignalR object, which has request.QueryString property. 
        /// You should pass user-related information inside the query string from the client 
        /// (e.g. SyncServiceFactory.CreateUsingSignalR(Realm.CreateInstance, new Uri("http://localhost/Realmius?userLogin=John&pass=123"),...)) 
        /// use that information to authenticate the user on the server-side. Note, that passing login and password directly is not recommended (pass some key/hash instead) :).
        /// </summary>
        /// <param name="principal"></param>
        /// <returns>
        /// If user is authenticated, return any object that identifies it (could be a string with username). This object will be passed back to you later in CheckAndProcess and GetTagsForUser
        /// If the user is not authenticated return null or throw an exception.
        ///</returns>
        public abstract TUser AuthenticateUser(ClaimsPrincipal principal);
        public Func<ChangeTrackingDbContext> ContextFactoryFunction { get; set; }
    }
}