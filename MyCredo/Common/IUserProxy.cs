namespace MyCredo.Common;

public interface IUserProxy
{
    Task<ApiServiceResponse<bool>> IsMobileNumberChangeAllowed(string personalNumber = null, long? externalId = null);
}