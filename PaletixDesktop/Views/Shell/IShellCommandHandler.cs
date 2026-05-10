namespace PaletixDesktop.Views.Shell
{
    public interface IShellCommandHandler
    {
        bool CanHandleShellCommand(string commandId);
        void HandleShellCommand(string commandId);
    }
}
