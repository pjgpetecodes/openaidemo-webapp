using openaidemo_webapp.Shared;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace maui.Components;

public partial class ChatBubble : ContentView
{
    
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
        Debug.WriteLine($"OnMessageChanged: {newValue}");

        var chatBubble = (ChatBubble)bindable;
        chatBubble.SetDocSources();

    }

    public static readonly BindableProperty CitationsProperty = BindableProperty.Create(
        propertyName: nameof(Citations),
        returnType: typeof(ObservableCollection<CognitiveSearchResult>),
        declaringType: typeof(ChatBubble),
        defaultValue: new ObservableCollection<CognitiveSearchResult>(),
        defaultBindingMode: BindingMode.OneWay);

    public ObservableCollection<CognitiveSearchResult> Citations
    {
        get => (ObservableCollection<CognitiveSearchResult>)GetValue(ChatBubble.CitationsProperty);
        set => SetValue(ChatBubble.CitationsProperty, value);
    }

    private void SetDocSources()
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
    }

    private string chatBubbleStyle => $"chatBubbleStyle {Message.Type}";
    private string typingStyle => "typing";

    public ChatBubble()
    {
        InitializeComponent();
        
    }
}
