using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using Realmius_mancheck_Web.Models;

namespace Realmius_mancheck_Web.DAL
{
    public class DataBaseInitializer :
        System.Data.Entity.DropCreateDatabaseIfModelChanges<RealmiusServerContext>
    {
        protected override void Seed(RealmiusServerContext context)
        {
            var notes = new List<NoteRealm>
            {
                new NoteRealm()
                {
                    Description = "Apples, tomatoes, crisps",
                    Id = "1",
                    Title = "Shopping list",
                    PostTime = DateTime.Now,
                    UserRole = 2
                },

                new NoteRealm()
                {
                    Description = "In bus station",
                    Id = "2",
                    Title = "Meet Dan",
                    PostTime = DateTime.Now,
                    UserRole = 0
                },

                new NoteRealm()
                {
                    Description = "Mech. system, computer science ",
                    Id = "3",
                    Title = "Prepare to pass",
                    PostTime = DateTime.Now,
                    UserRole = 3
                }
            };

            context.Notes.AddRange(notes);
            
            var photos = new List<PhotoRealm>()
            {
                new PhotoRealm()
                {
                    Id = "4",
                    Title = "Car",
                    PhotoUri ="http://media.caranddriver.com/images/media/51/25-cars-worth-waiting-for-lp-ford-gt-photo-658253-s-original.jpg",
                    PostTime = DateTime.Now
                },

                new PhotoRealm()
                {
                    Id = "5",
                    Title = "Dog",
                    PhotoUri ="https://static.pexels.com/photos/356378/pexels-photo-356378.jpeg",
                    PostTime = DateTime.Now
                },

                new PhotoRealm()
                {
                    Id = "6",
                    Title = "Cat",
                    PhotoUri ="https://static.pexels.com/photos/126407/pexels-photo-126407.jpeg",
                    PostTime = DateTime.Now
                }
            };
            context.Photos.AddRange(photos);

            var messages = new List<ChatMessageRealm>()
            {
                new ChatMessageRealm()
                {
                    AuthorName = "admin",
                    Id = "10003",
                    CreatingDateTime = DateTimeOffset.Now,
                    Text = "Hi all. It's Admin's msg from server"
                },

                new ChatMessageRealm()
                {
                    AuthorName = "john",
                    Id = "10004",
                    CreatingDateTime = DateTimeOffset.Now,
                    Text = "Hi! It's John's msg from server"
                },

                new ChatMessageRealm()
                {
                    AuthorName = "homer",
                    Id = "10005",
                    CreatingDateTime = DateTimeOffset.Now,
                    Text = "What's up? It's Homer's msg from server"
                }
            };
            context.ChatMessages.AddRange(messages);
            context.SaveChanges();
        }
    }
}