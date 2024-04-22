using openaidemo_webapp.Shared;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace maui.Components;

public partial class ChatBubble : ContentView
{
    //public static readonly BindableProperty MessageProperty = BindableProperty.Create(nameof(Message), typeof(OpenAIChatMessage), typeof(ChatBubble), null, BindingMode.OneWay, null, OnMessageChanged);

    // Create a BindableProperty for Message, use the onMessageChanged handler
    public static readonly BindableProperty MessageProperty = BindableProperty.Create(
        propertyName: nameof(Message),
        returnType: typeof(OpenAIChatMessage),
        declaringType: typeof(ChatBubble),
        defaultValue: null,
        defaultBindingMode: BindingMode.OneWay,
        propertyChanged: OnMessageChanged);

    public OpenAIChatMessage Message
    {
        get => (OpenAIChatMessage)GetValue(ChatBubble.MessageProperty);
        set => SetValue(ChatBubble.MessageProperty, value);
    }

    public static readonly BindableProperty CitationsProperty = BindableProperty.Create(nameof(Message), typeof(ObservableCollection<CognitiveSearchResult>), typeof(ChatBubble));

    public ObservableCollection<CognitiveSearchResult> Citations
    {
        get => (ObservableCollection<CognitiveSearchResult>)GetValue(ChatBubble.CitationsProperty);
        set => SetValue(ChatBubble.CitationsProperty, value);
    }

    private static void OnMessageChanged(BindableObject bindable, object oldValue, object newValue)
    {
        Console.WriteLine($"OnMessageChanged: {newValue}");

        var chatBubble = (ChatBubble)bindable;
        chatBubble.SetDocSources();
    }

    private void SetDocSources()
    {

        Console.WriteLine("GetDocSources");

        Citations = new ObservableCollection<CognitiveSearchResult>();

        if (Message != null && Message.Sources != null)
        {
            foreach (var source in Message.Sources)
            {
                if (Message.Content.Contains("DOC " + source.Id))
                {
                    Citations.Add(source);
                }
            }
        }

        Console.WriteLine("docSources.Count: " + Citations.Count);

    }

    private string chatBubbleStyle => $"chatBubbleStyle {Message.Type}";
    private string typingStyle => "typing";

    // Removed shouldRender as it's not being used

    public ChatBubble()
    {
        InitializeComponent();


    }
}
