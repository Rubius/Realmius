// /****************************** MessagingSystem ******************************\
// Project:            MessageClient
// Filename:           Message.cs
// Created:            16.05.2017
// 
// <summary>
// 
// </summary>
// \***************************************************************************/

using System;
using Realmius.SyncService;
using Realms;

namespace MessageClient.Models
{
    public class Message : RealmObject, IRealmiusObjectClient
    {
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string MobilePrimaryKey => Id;

        public DateTime DateTime { get; set; }

        public string UserId { get; set; }

        public string Text { get; set; }
    }
}