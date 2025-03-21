using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using HttpMultipartParser;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using System.Text.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace HotelAdmin_Lambda;

public class HotelAdmin
{
    public APIGatewayProxyResponse AddHotel(APIGatewayProxyRequest request, ILambdaContext context)
    {
        // Initialize response object with default headers and a status code of 200 (OK)
        var response = new APIGatewayProxyResponse
        {
            Headers = new Dictionary<string, string>(),
            StatusCode = 200
        };
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Headers", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "OPTIONS,POST");

        // Check if the request body is Base64-encoded and decode it appropriately
        var bodyContent = request.IsBase64Encoded ? Convert.FromBase64String(request.Body) : Encoding.UTF8.GetBytes(request.Body);

        // Create a memory stream from the body content to parse multipart form data (file + fields)
        using var memStream = new MemoryStream(bodyContent);
        var formData = MultipartFormDataParser.Parse(memStream);

        // Extract form data fields
        var hotelName = formData.GetParameterValue("hotelName");
        var hotelRating = formData.GetParameterValue("hotelRating");
        var hotelCity = formData.GetParameterValue("hotelCity");
        var hotelPrice = formData.GetParameterValue("hotelPrice");

        // Check if a file was uploaded
        var file = formData.Files.FirstOrDefault();
        var fileName = file.FileName;


        var userId = formData.GetParameterValue("userId");
        var idToken = formData.GetParameterValue("idToken");

        // Use the JwtSecurityTokenHandler to validate and parse the JWT token
        var token = new JwtSecurityToken(idToken);

        // Check if the token contains the required group claim ('Admin')
        var group = token.Claims.FirstOrDefault(x => x.Type == "cognito:groups");
        if (group == null || group.Value != "Admin")
        {
            // If the user is not an admin, respond with Unauthorized (401)
            response.StatusCode = (int)HttpStatusCode.Unauthorized;
            response.Body = JsonSerializer.Serialize(new { Error = "Unauthorised. Must be a member of Admin group." });
        }

        Console.WriteLine("OK");
        return response;
    }
}
