namespace Art.Web.Api.Models.Common
{
    public class ServerError
    {
        public ServerError(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }
}