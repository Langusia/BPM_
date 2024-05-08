using Credo.Core.Shared.Messages;

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

    public class ValidationFailedApiServiceResponse : ApiServiceResponse
    {
        public ValidationFailedApiServiceResponse()
        {
            ErrorCode = ResponseErrorCode.BadRequest;
            Message = "Input validation failed";
            State = ApiStatus.BadRequest;
        }

        public ValidationFailedApiServiceResponse(string param, string errorCode = ResponseErrorCode.BadRequest)
        {
            ErrorCode = errorCode;
            Message = $"Invalid parameter '{param}'";
            State = ApiStatus.BadRequest;
        }
    }

    public class ValidationFailedApiGenericServiceResponse<T> : ApiServiceResponse<T>
    {
        public ValidationFailedApiGenericServiceResponse()
        {
            ErrorCode = ResponseErrorCode.BadRequest;
            Message = "Input validation failed";
            State = ApiStatus.BadRequest;
        }

        public ValidationFailedApiGenericServiceResponse(string param, string errorCode = ResponseErrorCode.BadRequest)
        {
            ErrorCode = errorCode;
            Message = $"Invalid parameter '{param}'";
            State = ApiStatus.BadRequest;
        }
    }

    public class ExternalServiceFailedApiGenericServiceResponse<T> : ApiServiceResponse<T>
    {
        public ExternalServiceFailedApiGenericServiceResponse(string serviceName, string message, string status, ExternalApiStatus errorStatus, string errorCode = ResponseErrorCode.ServiceCallError)
        {
            Message = message;
            DetailsMessage = $"External service '{serviceName}' failed: {status} message: {message}";
            State = ApiStatus.Failed;
            ExternalState = errorStatus;
            ErrorCode = errorCode;
        }
    }

    public class ExternalServiceFailedApiServiceResponse : ApiServiceResponse
    {
        public ExternalServiceFailedApiServiceResponse(string serviceName, string message, string status, ExternalApiStatus errorStatus, string errorCode = ResponseErrorCode.ServiceCallError)
        {
            Message = message;
            DetailsMessage = $"External service '{serviceName}' failed: {status} message: {message}";
            State = ApiStatus.Failed;
            ExternalState = errorStatus;
            ErrorCode = errorCode;
        }
    }

    public class InternalServiceFailedApiServiceResponse : ApiServiceResponse
    {
        public InternalServiceFailedApiServiceResponse(string message, string errorCode = ResponseErrorCode.GeneralError)
        {
            Message = "Internal service error";
            DetailsMessage = $"Internal service error '{message}'";
            State = ApiStatus.Failed;
            ErrorCode = errorCode;
        }

        public InternalServiceFailedApiServiceResponse(Exception ex, string errorCode = ResponseErrorCode.GeneralError)
        {
            //TODO: analyze header trace value

            Message = "Internal service error";
            DetailsMessage = $"Internal service error '{ex}'";
            State = ApiStatus.Failed;
            ErrorCode = errorCode;
        }
    }


    public class InternalServiceFailedApiGenericServiceResponse<T> : ApiServiceResponse<T>
    {
        public InternalServiceFailedApiGenericServiceResponse(string message, string errorCode = ResponseErrorCode.GeneralError)
        {
            Message = "Internal service error";
            DetailsMessage = $"Internal service error '{message}'";
            State = ApiStatus.Failed;
            ErrorCode = errorCode;
        }

        public InternalServiceFailedApiGenericServiceResponse(Exception ex, string errorCode = ResponseErrorCode.GeneralError)
        {
            //TODO: analyze header trace value

            Message = "Internal service error";
            DetailsMessage = $"Internal service error '{ex}'";
            State = ApiStatus.Failed;
            ErrorCode = errorCode;
        }
    }


    public class SuccessApiServiceResponse : ApiServiceResponse
    {
        public SuccessApiServiceResponse(string message = null)
        {
            State = ApiStatus.Ok;
            Message = message;
        }
    }

    public class SuccessApiServiceResponse<T> : ApiServiceResponse<T>
    {
        public SuccessApiServiceResponse(T data, string message = null, bool isAuth = false)
        {
            Data = data;
            State = ApiStatus.Ok;
            Message = message;
            IsAuth = isAuth;
        }
    }

    public class AlreadyExistsApiServiceResponse<T> : ApiServiceResponse<T>
    {
        public AlreadyExistsApiServiceResponse(string message = null, string errorCode = ResponseErrorCode.BadRequest)
        {
            ErrorCode = errorCode;
            State = ApiStatus.AlreadyExists;
            Message = message;
        }
    }

    public class NotFoundApiServiceResponse<T> : ApiServiceResponse<T>
    {
        public NotFoundApiServiceResponse(string message = null, string errorCode = ResponseErrorCode.NotFound)
        {
            ErrorCode = errorCode;
            State = ApiStatus.NotFound;
            Message = message;
        }
    }

    public class BadRequestApiServiceResponse<T> : ApiServiceResponse<T>
    {
        public BadRequestApiServiceResponse(T data, string message = null, string errorCode = ResponseErrorCode.BadRequest, List<string> validationErrors = null, bool isAuth = false)
        {
            ErrorCode = errorCode;
            State = ApiStatus.BadRequest;
            Message = message;
            Data = data;
            ValidationErrors = validationErrors;
            IsAuth = isAuth;
        }
    }

    public class BadRequestApiServiceResponse : ApiServiceResponse
    {
        public BadRequestApiServiceResponse(string message = null, string errorCode = ResponseErrorCode.BadRequest)
        {
            ErrorCode = errorCode;
            State = ApiStatus.BadRequest;
            Message = message;
        }
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
