

namespace MyCredo.Common
{
    public abstract class ApiServiceResponse
    {
        protected ILogger _logger { get; set; }
        public string Message { get; set; }
        public string DetailsMessage { get; set; }
        public ExternalApiStatus ExternalState { get; set; }
        public ApiStatus State { get; set; }
        public string ErrorCode { get; set; }
        public List<string> ValidationErrors { get; set; }
        public bool IsOk() => State == ApiStatus.Ok;
        public bool IsAuth { get; set; }

    }

    public class ApiServiceResponse<T> : ApiServiceResponse
    {
        public ApiServiceResponse() { }
        public ApiServiceResponse(T data, ApiServiceResponse response)
        {
            ErrorCode = response.ErrorCode;
            DetailsMessage = response.DetailsMessage;
            ExternalState = response.ExternalState;
            Message = response.Message;
            State = response.State;
            ValidationErrors = response.ValidationErrors;
            Data = data;
            IsAuth = response.IsAuth;
        }

        public ApiServiceResponse(T data)
        {
            Data = data;
        }
        public T Data { get; protected set; }
    }


    public enum ExternalApiStatus
    {
        Ok,
        NotFound,
        Failed
    }

    public enum ApiStatus
    {
        Ok,
        NotFound,
        Failed,
        BadRequest,
        AlreadyExists
    }
}
