using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RealmiusAdvancedExample.RealmEntities;
using Xamarin.Forms;

namespace RealmiusAdvancedExample
{
    public class MessageDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate IncomingMessageTemplate { get; set; }

        public DataTemplate OutgoingMessageTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            var message = item as ChatMessageRealm;

            if (message == null)
                return null;
            if (message.AuthorName == App.CurrentUser.Name)
            {
                return OutgoingMessageTemplate;
            }
            return IncomingMessageTemplate;
        }
    }
}
