using System;
using System.Collections.Generic;
using Realmius.Server;
using Realmius.Server.Models;
using Realmius.Server.ServerConfiguration;
using Server.Entities;

namespace Server.Sync
{
    public class SyncConfiguration : SyncConfigurationBase<User>
    {
        private static SyncConfiguration _instance;

        public static SyncConfiguration Instance => _instance ?? (_instance = new SyncConfiguration());

        public static string PropertySyncGroup(string propertyId)
        {
            return "PROP" + propertyId;
        }

        private SyncConfiguration()
        {
        }

        public override IList<Type> TypesToSync { get; } = new List<Type>()
        {
            typeof(User),
            typeof(Message)
        };

        public override IList<string> GetTagsForObject(ChangeTrackingDbContext changeTrackingContext, IRealmiusObjectServer obj)
        {
            //if (obj is ItemCategory)
            //    return new[] { "all" };
            
            var user = obj as User;
            var message = obj as Message;
            
            var db = (MessagingContext)changeTrackingContext;

            if (user != null)
            {
                return new[] { "all" };
                //if (itemModel.IsUserMade)
                //{
                //    return new[] { itemModel.MadeByUserId.ToString() };
                //}
                //else
                //{
                //    return new[] { "MDL" + user.Id };
                //}
            }
            if (message != null)
            {
                return new[] { "all" };
                //if (planCategory.IsUserMade)
                //{
                //    return new[] { planCategory.MadeByUserId.ToString() };
                //}
                //else
                //{
                //    return new[] { "all" };
                //}
            }
            
            //var entityWithUserId = obj as IEntityWithUserId;
            //if (entityWithUserId != null)
            //{
            //    return new[] { entityWithUserId.UserId };
            //}
            return new List<string> { };
        }

        public override bool CheckAndProcess(CheckAndProcessArgs<User> args)
        {
            return true;
        }

        //private IList<string> CreateTagsForItemPosition(MessagingContext db, string itemPositionId)
        //{
        //    var itemPositionRef = db.ItemPositions.Find(itemPositionId);
        //    if (itemPositionRef == null)
        //        return CreateTagsForProperty(db, null);

        //    return CreateTagsForProperty(db, itemPositionRef?.PropertyId);
        //}
        //private IList<string> CreateTagsForProperty(MessagingContext db, string propertyId)
        //{
        //    if (string.IsNullOrEmpty(propertyId))
        //        return new string[] { };

        //    return new[] { PropertySyncGroup(propertyId) };
        //    //var propertyRef = db.Properties.Find(propertyId);
        //    //return CreateTagsForProfile(propertyRef?.ProfileId);
        //}

        //private IList<string> CreateTagsForProfile(int? profileId)
        //{
        //    if (profileId == null || profileId == 0)
        //        return new string[] { };

        //    return new[] { profileId.ToString() };
        //}

        //public override bool CheckAndProcess(CheckAndProcessArgs<User> args)
        //{
        //    var ef = args.Database;
        //    var deserialized = args.Entity;
        //    var userInfo = args.User;

        //    var user = userInfo.Profile;
        //    var profile = deserialized as Profile;
        //    var property = deserialized as Property;
        //    var itemPosition = deserialized as ItemPosition;
        //    var item = deserialized as Item;
        //    var maintenance = deserialized as Maintenance;
        //    var scheduledMaintenance = deserialized as HistoricalMaintenance;
        //    var itemModel = deserialized as ItemModel;
        //    var uploadedFile = deserialized as UploadedFile;
        //    var contact = deserialized as Contact;
        //    var plan = deserialized as Plan;
        //    var planCategory = deserialized as PlanCategory;

        //    var db = (HouseManagementDbContext)ef;

        //    if (deserialized is ItemCategory)
        //        return false;

        //    if (itemModel != null)
        //    {
        //        var dbItemModel = (ItemModel)args.OriginalDbEntity;
        //        if (dbItemModel != null)
        //        {
        //            if (dbItemModel.IsUserMade && dbItemModel.MadeByUserId == user.Id)
        //                return true;

        //            return false;
        //        }

        //        itemModel.IsUserMade = true;
        //        itemModel.MadeByUserId = user.Id;

        //        return true;
        //    }


        //    if (profile != null)
        //    {
        //        if (profile.Id == user.Id)
        //            return true;
        //        return false;
        //    }
        //    if (property != null)
        //    {
        //        var result = false;
        //        var dbProperty = (Property)args.OriginalDbEntity;

        //        if (dbProperty == null)
        //        {
        //            property.ProfileId = user.Id;
        //            result = true;
        //        }
        //        else
        //        {
        //            if (dbProperty.ProfileId != user.Id)
        //            {
        //                result = false;
        //            }
        //            else
        //            {
        //                result = true;
        //            }

        //        }

        //        return result;
        //    }
        //    if (contact != null)
        //    {
        //        var dbContact = (Contact)args.OriginalDbEntity;

        //        if (dbContact == null)
        //        {
        //            contact.ProfileId = user.Id;
        //            return true;
        //        }
        //        else
        //        {
        //            if (dbContact.ProfileId != user.Id)
        //                return false;
        //            return true;
        //        }
        //    }

        //    if (planCategory != null)
        //    {
        //        var dbPlanCategory = (PlanCategory)args.OriginalDbEntity;
        //        if (dbPlanCategory != null)
        //        {
        //            if (dbPlanCategory.IsUserMade && dbPlanCategory.MadeByUserId == user.Id)
        //                return true;

        //            return false;
        //        }

        //        planCategory.IsUserMade = true;
        //        planCategory.MadeByUserId = user.Id;

        //        return true;
        //    }
        //    if (plan != null)
        //    {
        //        var dbProperty = db.Properties.Find(plan.PropertyId);

        //        if (dbProperty == null)
        //        {
        //            return false;
        //        }
        //        else
        //        {
        //            if (dbProperty.ProfileId != user.Id)
        //                return false;
        //            return true;
        //        }
        //    }
        //    if (itemPosition != null)
        //    {
        //        var dbProperty = db.Properties.Find(itemPosition.PropertyId);

        //        if (dbProperty == null)
        //        {
        //            return false;
        //        }
        //        else
        //        {
        //            if (dbProperty.ProfileId != user.Id)
        //                return false;
        //            return true;
        //        }
        //    }
        //    if (item != null)
        //    {
        //        var result = CheckItemPosition(db, item.ItemPositionId, user);
        //        if (result)
        //            SyncHub.AddUserGroup<SyncHub>(x => x.Profile.Id == user.Id, HouseManagementDbContext.GetTagForItemModel(item.ItemModelId));
        //        return result;
        //    }
        //    if (maintenance != null)
        //    {
        //        return CheckItemPosition(db, maintenance.ItemPositionId, user);
        //    }
        //    if (scheduledMaintenance != null)
        //    {
        //        var dbMaintenance = db.Maintenances.Find(scheduledMaintenance.MaintenanceId);
        //        return CheckItemPosition(db, dbMaintenance?.ItemPositionId, user);
        //    }
        //    if (uploadedFile != null)
        //    {
        //        var dbFile = (UploadedFile)args.OriginalDbEntity;
        //        if (dbFile != null)
        //        {
        //            if (!string.IsNullOrEmpty(dbFile.Url))
        //                uploadedFile.Url = dbFile.Url;

        //            if (dbFile.OwnerId == user.Id)
        //                return true;
        //            else
        //                return false;
        //        }

        //        uploadedFile.OwnerId = user.Id;
        //        return true;
        //    }
        //    return true;
        //}

        //private bool CheckItemPosition(MessagingContext db, string itemPositionId, Profile user)
        //{
        //    if (string.IsNullOrEmpty(itemPositionId))
        //        return false;

        //    var dbItemPosition = db.ItemPositions.Find(itemPositionId);
        //    if (dbItemPosition == null)
        //        return false;

        //    return CheckProperty(db, dbItemPosition.PropertyId, user);
        //}

        //private bool CheckProperty(MessagingContext db, string propertyId, Profile user)
        //{
        //    var dbProperty = db.Properties.Find(propertyId);

        //    if (dbProperty == null)
        //        return false;

        //    if (dbProperty.ProfileId != user.Id)
        //        return false;

        //    return true;
        //}

        //private MessagingContext CreateDatabase()
        //{
        //    return new MessagingContext();
        //}

        //public override object[] KeyForType(Type type, string itemPrimaryKey)
        //{
        //    if (type == typeof(Profile))
        //    {
        //        return new object[] { int.Parse(itemPrimaryKey) };
        //    }
        //    return base.KeyForType(type, itemPrimaryKey);
        //}
    }
}