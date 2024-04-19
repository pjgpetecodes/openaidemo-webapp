using openaidemo_webapp.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace maui.ValueConverters
{
    internal class MessageTemplateSelector : DataTemplateSelector
    {
        public DataTemplate UserMessageTemplate { get; set; }
        public DataTemplate AIMessageTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            var message = item as OpenAIChatMessage;
            if (message == null)
                return null;

            return message.Type == "ai" ? AIMessageTemplate : UserMessageTemplate;
        }
    }
}
