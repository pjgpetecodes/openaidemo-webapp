using Microsoft.AspNetCore.SignalR.Client;
using openaidemo_webapp.Shared;
using System.Collections.ObjectModel;
using maui.ViewModels;
using maui.ValueConverters;

namespace maui
{
    public partial class MainPage : ContentPage
    {       
        ChatViewModel chatViewModel = new ChatViewModel();

        public MainPage()
        {
            this.Resources["MessageTemplateSelector"] = new MessageTemplateSelector
            {
                UserMessageTemplate = (DataTemplate)this.Resources["UserMessageTemplate"],
                AIMessageTemplate = (DataTemplate)this.Resources["AIMessageTemplate"]
            };

            BindingContext = chatViewModel;
            chatViewModel.SetupConnection();
        }

        private async void SendTestMessage(object sender, EventArgs e)
        {
            await chatViewModel.SendMessage();
        }

    }

}
