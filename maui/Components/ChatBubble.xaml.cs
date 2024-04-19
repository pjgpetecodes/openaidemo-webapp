using openaidemo_webapp.Shared;

namespace maui.Components;

public partial class ChatBubble : ContentView
{
    public static readonly BindableProperty MessageProperty = BindableProperty.Create(nameof(Message), typeof(OpenAIChatMessage), typeof(ChatBubble), null);


    public OpenAIChatMessage Message
    {
        get => (OpenAIChatMessage)GetValue(ChatBubble.MessageProperty);
        set => SetValue(ChatBubble.MessageProperty, value);
    }    

    private string chatBubbleStyle => $"chatBubbleStyle {Message.Type}";
    private string typingStyle => "typing";

    // Removed shouldRender as it's not being used

    public ChatBubble()
    {
        InitializeComponent();
    }
}
