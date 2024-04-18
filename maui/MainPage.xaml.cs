using Microsoft.AspNetCore.SignalR.Client;
using openaidemo_webapp.Shared;
using System.Collections.ObjectModel;
using maui.ViewModels;

namespace maui
{
    public partial class MainPage : ContentPage
    {       
        ChatViewModel chatViewModel = new ChatViewModel();

        public MainPage()
        {
            InitializeComponent();

            BindingContext = chatViewModel;
            chatViewModel.SetupConnection();
        }

        private async void SendTestMessage(object sender, EventArgs e)
        {
            await chatViewModel.SendMessage();
        }

    }

}
