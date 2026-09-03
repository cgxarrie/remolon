namespace RetroBackend.Mappings;

public static class CreateRetrospectiveDtoMappings
{
    public static Models.CreateRetrospectiveRequest ToServiceRequest(this Dtos.CreateRetrospectiveRequest request)
    {
        var serviceRequest = new Models.CreateRetrospectiveRequest
        {
            Title = request.Title,
            OrganizationId = request.OrganizationId ?? Guid.Empty,
        };

        foreach (var column in request.Columns)
        {
            serviceRequest.Columns.Add(column.ToServiceRequest());
        }

        return serviceRequest;
    }

    public static Models.CreateRetrospectiveRequestColumn ToServiceRequest(this Dtos.CreateRetrospectiveRequestColumn request)
    {
        var serviceRequest = new Models.CreateRetrospectiveRequestColumn
        {
            Title = request.Title,
            Position = request.Position
        };

        return serviceRequest;
    }
}