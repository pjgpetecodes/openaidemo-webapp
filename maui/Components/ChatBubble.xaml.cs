using openaidemo_webapp.Shared;

namespace maui.Components;

public partial class ChatBubble : ContentView
{
    public static readonly BindableProperty MessageProperty = BindableProperty.Create(
        propertyName: "Message",
        returnType: typeof(OpenAIChatMessage),
        declaringType: typeof(ChatBubble),
        defaultValue: null,
        defaultBindingMode: BindingMode.TwoWay,
        propertyChanged: MessagePropertyChanged);

    public OpenAIChatMessage Message
    {
        get { return (OpenAIChatMessage)GetValue(MessageProperty); }
        set { SetValue(MessageProperty, value); }
    }

    private static void MessagePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        //var control = (ChatBubble)bindable;
        //control.BindingContext = newValue; // Set the BindingContext of the Frame to the new Message
    }

    private string chatBubbleStyle => $"chatBubbleStyle {Message.Type}";
    private string typingStyle => "typing";

    // Removed shouldRender as it's not being used

    public ChatBubble()
    {

    }
}
