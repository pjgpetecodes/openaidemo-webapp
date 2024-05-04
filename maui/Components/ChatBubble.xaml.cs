using openaidemo_webapp.Shared;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        var chatBubble = (ChatBubble)bindable;

        // Unsubscribe from the PropertyChanged event of the old message
        if (oldValue is OpenAIChatMessage oldMessage)
        {
            oldMessage.PropertyChanged -= chatBubble.OnMessagePropertyChanged;
        }

        // Subscribe to the PropertyChanged event of the new message
        if (newValue is OpenAIChatMessage newMessage)
        {
            newMessage.PropertyChanged += chatBubble.OnMessagePropertyChanged;
        }

        Application.Current.MainPage.Dispatcher.Dispatch(async () =>
        {
            chatBubble.SetDocSources();
        });
    }

    private void OnMessagePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OpenAIChatMessage.Content))
        {
            Application.Current.MainPage.Dispatcher.Dispatch(async () =>
            {
                SetDocSources();
            });
        }
    }

    public static readonly BindableProperty CitationsProperty = BindableProperty.Create(
        propertyName: nameof(Citations),
        returnType: typeof(ObservableCollection<CognitiveSearchResult>),
        declaringType: typeof(ChatBubble),
        defaultValue: null,
        defaultBindingMode: BindingMode.OneWay);

    public ObservableCollection<CognitiveSearchResult> Citations
    {
        get => (ObservableCollection<CognitiveSearchResult>)GetValue(ChatBubble.CitationsProperty);
        set => SetValue(ChatBubble.CitationsProperty, value);
    }

    public static readonly BindableProperty CitationsAvailableProperty = BindableProperty.Create(
        propertyName: nameof(CitationsAvailable),
        returnType: typeof(Boolean),
        declaringType: typeof(ChatBubble),
        defaultValue: false,
        defaultBindingMode: BindingMode.OneWay);

    public Boolean CitationsAvailable
    {
        get => (Boolean)GetValue(ChatBubble.CitationsAvailableProperty);
        set => SetValue(ChatBubble.CitationsAvailableProperty, value);
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

        if (Citations != null && Citations.Count > 0)
        {
            CitationsAvailable = true;
        }
        else
        {
            CitationsAvailable = false;
        }

        Debug.WriteLine("Total Citations: " + Citations.Count);
    }

    private string chatBubbleStyle => $"chatBubbleStyle {Message.Type}";
    private string typingStyle => "typing";

    public ChatBubble()
    {
        InitializeComponent();
        Citations = new ObservableCollection<CognitiveSearchResult>();
    }
}
