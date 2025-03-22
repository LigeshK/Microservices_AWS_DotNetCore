using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.S3;
using HotelAdmin_Lambda.Interfaces;
using HotelAdmin_Lambda.Models;
using HotelAdmin_Lambda.Repositories;
using HttpMultipartParser;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using System.Text.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace HotelAdmin_Lambda;

/// <summary>
/// Lambda function to perform CRUD operations on Hotel Management - for Admins - Cognito
/// Create 2 lambda for List Hotels and Add Hotels
/// </summary>
public class HotelAdmin
{
    private readonly IHotelService _hotelService; 
    private readonly IFileStorageService _fileStorageService; 
    private readonly string _bucketName; 

    /// <summary>
    /// Constructor initializes region, service, filestorage.
    /// </summary>
    public HotelAdmin()
    {
        // Fetch AWS region from environment variable - by default AWS provides this
        var region = Environment.GetEnvironmentVariable("AWS_REGION");

        // Initialize AWS DynamoDB and S3 clients, service instances 
        var dbClient = new AmazonDynamoDBClient(RegionEndpoint.GetBySystemName(region));
        var s3Client = new AmazonS3Client(RegionEndpoint.GetBySystemName(region));

        _hotelService = new HotelService(dbClient);
        _fileStorageService = new S3FileStorageService(s3Client);

        // Fetch S3 bucket name from environment variable
        _bucketName = Environment.GetEnvironmentVariable("HotelBucketName") ?? string.Empty;
    }

    /// <summary>
    /// Retrieves a list of hotels for a given user based on the token from lambda request.
    /// </summary>
    /// <param name="request">API Gateway request - contains the token.</param>
    /// <returns>Returns the list of hotels in JSON format.</returns>
    public async Task<APIGatewayProxyResponse> GetHotels(APIGatewayProxyRequest request)
    {
        // Build API response template
        var response = CreateApiResponse(); 

        try
        {
            // Validate if the token parameter exists in query string
            if (!request.QueryStringParameters.TryGetValue("token", out var token))
                return CreateErrorResponse(HttpStatusCode.BadRequest, "Token is required.");

            // Extract user ID from the JWT token
            var userId = new JwtSecurityToken(token).Claims.FirstOrDefault(x => x.Type == "sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return CreateErrorResponse(HttpStatusCode.Unauthorized, "Invalid token.");

            // Retrieve hotels from the database for the given user
            var hotels = await _hotelService.GetHotelsAsync(userId);

            // Serialize response as JSON
            response.Body = JsonSerializer.Serialize(hotels);
        }
        catch (Exception ex)
        {
            //Send errors to cloudwatch logs
            LambdaLogger.Log($"Error in GetHotels: {ex.Message}"); // Log error
            return CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
        }

        return response;
    }

    /// <summary>
    /// Add new hotel entry after validating the user as an Admin.
    /// </summary>
    /// <param name="request">API Gateway request containing form/multipart data.</param>
    /// <param name="context">Lambda execution context.</param>
    /// <returns>Returns success or failure response.</returns>
    public async Task<APIGatewayProxyResponse> AddHotel(APIGatewayProxyRequest request, ILambdaContext context)
    {
        // Build API response template
        var response = CreateApiResponse();

        try
        {
            // Decode request body if it is Base64 encoded
            var bodyContent = request.IsBase64Encoded ? Convert.FromBase64String(request.Body) : Encoding.UTF8.GetBytes(request.Body);
            using var memStream = new MemoryStream(bodyContent);

            // Parse the multipart form data
            var formData = MultipartFormDataParser.Parse(memStream);

            // Retrieve uploaded file
            var file = formData.Files.FirstOrDefault();
            if (file == null)
                return CreateErrorResponse(HttpStatusCode.BadRequest, "No file uploaded.");

            // Extract user details from form data
            var userId = formData.GetParameterValue("userId");
            var idToken = formData.GetParameterValue("idToken");

            // Validate if user belongs to the Admin group using JWT token
            var token = new JwtSecurityToken(idToken);
            var group = token.Claims.FirstOrDefault(x => x.Type == "cognito:groups")?.Value;
            if (group != "Admin")
                return CreateErrorResponse(HttpStatusCode.Unauthorized, "Unauthorized. Must be a member of Admin group.");

            // Prepare file stream for upload
            await using var fileContentStream = new MemoryStream();
            await file.Data.CopyToAsync(fileContentStream);
            fileContentStream.Position = 0;

            // Upload file to S3 bucket
            await _fileStorageService.UploadFileAsync(_bucketName, file.FileName, fileContentStream);

            // Create a new hotel object with provided details
            var hotel = new Hotel
            {
                UserId = userId,
                Id = Guid.NewGuid().ToString(), // Generate unique hotel ID
                Name = formData.GetParameterValue("hotelName"),
                CityName = formData.GetParameterValue("hotelCity"),
                Price = int.Parse(formData.GetParameterValue("hotelPrice")),
                Rating = int.Parse(formData.GetParameterValue("hotelRating")),
                FileName = file.FileName // Store uploaded file name
            };

            // Save hotel entry in DynamoDB
            await _hotelService.SaveHotelAsync(hotel);

            // Return success message
            response.Body = JsonSerializer.Serialize(new { Message = "Hotel added successfully." });
        }
        catch (Exception ex)
        {
            LambdaLogger.Log($"Error in AddHotel: {ex.Message}"); // Log error
            return CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
        }

        return response;
    }

    /// <summary>
    /// Creates a standard API Gateway response template with necessary headers.
    /// </summary>
    /// <returns>Returns a default API response template.</returns>
    private static APIGatewayProxyResponse CreateApiResponse()
    {
        return new APIGatewayProxyResponse
        {
            Headers = new Dictionary<string, string>
            {
                { "Access-Control-Allow-Origin", "*" }, // Allow cross-origin requests
                { "Access-Control-Allow-Headers", "*" }, // Allow all headers
                { "Access-Control-Allow-Methods", "OPTIONS,GET,POST" }, // Allow specific HTTP methods
                { "Content-Type", "application/json" } // Set response type as JSON
            },
            StatusCode = (int)HttpStatusCode.OK // Default status code
        };
    }

    /// <summary>
    /// Creates an error response with the provided status code and message.
    /// </summary>
    /// <param name="statusCode">HTTP status code of the error.</param>
    /// <param name="message">Error message to be returned.</param>
    /// <returns>Returns an API Gateway error response.</returns>
    private static APIGatewayProxyResponse CreateErrorResponse(HttpStatusCode statusCode, string message)
    {
        LambdaLogger.Log($"Error Response: {message}"); // Log error details
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)statusCode, // Assign HTTP status code
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }, // Set response type
            Body = JsonSerializer.Serialize(new { Error = message }) // Serialize error message
        };
    }
}
