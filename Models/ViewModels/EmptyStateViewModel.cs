namespace HealthTrack.Models.ViewModels
{
    public class EmptyStateViewModel
    {
        public EmptyStateViewModel(string icon, string title, string message,
            string? actionText = null, string? actionController = null, string actionName = "Create")
        {
            Icon = icon;
            Title = title;
            Message = message;
            ActionText = actionText;
            ActionController = actionController;
            ActionName = actionName;
        }

        public string Icon { get; }
        public string Title { get; }
        public string Message { get; }
        public string? ActionText { get; }
        public string? ActionController { get; }
        public string ActionName { get; }
    }
}
