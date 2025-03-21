using Amazon.DynamoDBv2.DataModel;

namespace HotelAdmin_Lambda.Models;

// Specifies that this class maps to the "Hotels" table in DynamoDB
[DynamoDBTable("Hotels")]
public class Hotel
{
    // Defines the partition key (hash key) for the DynamoDB table
    [DynamoDBHashKey("userId")]
    public string? UserId { get; set; }

    // Defines the sort key (range key) for the DynamoDB table
    [DynamoDBRangeKey("Id")]
    public string? Id { get; set; }

    // Hotel name
    public string? Name { get; set; }

    // Price per night (or relevant pricing metric)
    public int Price { get; set; }

    // Rating of the hotel (e.g., out of 5 stars)
    public int Rating { get; set; }

    // Name of the city where the hotel is located
    public string? CityName { get; set; }

    // File name (possibly for storing an image or related document)
    public string? FileName { get; set; }
}
