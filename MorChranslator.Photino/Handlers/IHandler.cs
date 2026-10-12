namespace MorChranslator.Photino.Handlers;

public interface IHandler
{
    public Task<string?> OnDataReceived(string data);
}