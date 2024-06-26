﻿using Microsoft.AspNetCore.SignalR.Client;
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
            chatViewModel.ChatMessageAdded += ScrollToBottom;
            chatViewModel.SetupConnection();
        }

        private void ScrollToBottom()
        {
            var lastMessage = chatViewModel.ChatMessages.LastOrDefault();
            if (lastMessage != null)
            {
                //ChatListView.ScrollTo(lastMessage, ScrollToPosition.End, true);
            }
        }

        private async void SendMessage(object sender, EventArgs e)
        {
            await chatViewModel.SendMessage();
        }

    }

}
