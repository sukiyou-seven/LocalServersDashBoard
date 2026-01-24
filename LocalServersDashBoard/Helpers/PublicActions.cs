using Wpf.Ui;
using Wpf.Ui.Controls;

namespace LocalServersDashBoard.Helpers;

public class PublicActions(ISnackbarService snackbarService)
{
    
    public void OnOpenSnackbar(string message, ControlAppearance type)
    {
        
        snackbarService.Show(
            "系统提示",
            message,
            type,
            new SymbolIcon(SymbolRegular.Speaker020),
            TimeSpan.FromSeconds(2)
        );
    }
}