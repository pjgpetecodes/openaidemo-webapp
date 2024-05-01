using openaidemo_webapp.Shared;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
    private static void OnMessageChanged(BindableObject bindable, object oldValue, object newValue)
    {
        Console.WriteLine($"OnMessageChanged: {newValue}");

        var chatBubble = (ChatBubble)bindable;
        Application.Current.MainPage.Dispatcher.Dispatch(async () =>
        {
            chatBubble.SetDocSources();
        });

    }

    //public static readonly BindableProperty CitationsProperty = BindableProperty.Create(nameof(Message), typeof(ObservableCollection<CognitiveSearchResult>), typeof(ChatBubble));
    public static readonly BindableProperty CitationsProperty = BindableProperty.Create(
        propertyName: nameof(Citations),
        returnType: typeof(ObservableCollection<CognitiveSearchResult>),
        declaringType: typeof(ChatBubble),
        defaultValue: null,
        defaultBindingMode: BindingMode.OneWay,
        propertyChanged: OnCitationsChanged);

    public ObservableCollection<CognitiveSearchResult> Citations
    {
        get => (ObservableCollection<CognitiveSearchResult>)GetValue(ChatBubble.CitationsProperty);
        set => SetValue(ChatBubble.CitationsProperty, value);
    }

    private static void OnCitationsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        // Cast the bindable object to ChatBubble so we can access its properties
        var chatBubble = (ChatBubble)bindable;

        // Cast the newValue to the correct type
        var newCitations = (ObservableCollection<CognitiveSearchResult>)newValue;

        // Update the UI
        chatBubble.CitationsCollectionView.ItemsSource = newCitations;
    }

    private void SetDocSources()
    {

        Application.Current.MainPage.Dispatcher.Dispatch(async () =>
        {
            Citations.Clear();

            if (Message != null && Message.Sources != null)
            {
                Debug.WriteLine("GetDocSources for Message: " + Message.Content);

                foreach (var source in Message.Sources)
                {
                    if (Message.Content.Contains("DOC " + source.Id))
                    {
                        Citations.Add(source);
                        Debug.WriteLine("Citations: " + Citations.Count);
                    }
                }
            }

            Debug.WriteLine("Total Citations: " + Citations.Count);

        });        

    }

    private string chatBubbleStyle => $"chatBubbleStyle {Message.Type}";
    private string typingStyle => "typing";

    // Removed shouldRender as it's not being used

    public ChatBubble()
    {
        InitializeComponent();
        Citations = new ObservableCollection<CognitiveSearchResult>();

    }
}
