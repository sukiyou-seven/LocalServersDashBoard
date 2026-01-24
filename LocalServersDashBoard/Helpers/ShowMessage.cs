namespace LocalServersDashBoard.Helpers;

public class ShowMessage
{
    public void ShowMSG(string msg, string title, string closeText)
    {
        Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            UiMessageBox umb = new()
            {
                Title = title,
                Content = msg,
                CloseButtonText = closeText,
                MinWidth = 300
            };
            await umb.ShowDialogAsync();
        });
    }
}