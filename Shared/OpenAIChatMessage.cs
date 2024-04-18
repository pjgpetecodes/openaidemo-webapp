using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace openaidemo_webapp.Shared
{
    public class OpenAIChatMessage : INotifyPropertyChanged
    {
        private string _chatBubbleId;
        private string _type;
        private string _content;
        private bool _isTemporaryResponse;
        private List<CognitiveSearchResult>? _sources;

        public string ChatBubbleId
        {
            get => _chatBubbleId;
            set
            {
                _chatBubbleId = value;
                OnPropertyChanged();
            }
        }

        public string Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged();
            }
        }

        public string Content
        {
            get => _content;
            set
            {
                _content = value;
                OnPropertyChanged();
            }
        }

        public bool IsTemporaryResponse
        {
            get => _isTemporaryResponse;
            set
            {
                _isTemporaryResponse = value;
                OnPropertyChanged();
            }
        }

        public List<CognitiveSearchResult>? Sources
        {
            get => _sources;
            set
            {
                _sources = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public OpenAIChatMessage()
        {
            ChatBubbleId = "";
            Type = "";
            Content = "";
            IsTemporaryResponse = false;
        }
    }
}
