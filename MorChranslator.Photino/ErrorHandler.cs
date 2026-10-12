using MorChranslator.Photino.Handlers;
using MorChranslator.Utility;
using Newtonsoft.Json;

namespace MorChranslator.Photino;

public class ErrorHandler(Connector connector, Action<string>? notify = null) : IErrorHandler
{
    public void OnError(string message)
    {
        notify?.Invoke(message);
        connector.Send(
            JsonConvert.SerializeObject(new 
            {
                method = "notification",
                data = JsonConvert.SerializeObject(new
                {
                    msg = message,
                    level = 2
                })
            }));
    }
}