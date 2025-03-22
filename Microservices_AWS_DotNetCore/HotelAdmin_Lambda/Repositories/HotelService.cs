using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2;
using HotelAdmin_Lambda.Interfaces;
using HotelAdmin_Lambda.Models;

namespace HotelAdmin_Lambda.Repositories;
public class HotelService(AmazonDynamoDBClient dbClient) : IHotelService
{
    private readonly IDynamoDBContext _dbContext = new DynamoDBContext(dbClient);

    public async Task<IEnumerable<Hotel>> GetHotelsAsync(string userId)
    {
        return await _dbContext.ScanAsync<Hotel>([new ScanCondition("UserId", ScanOperator.Equal, userId)]).GetRemainingAsync();
    }

    public async Task SaveHotelAsync(Hotel hotel)
    {
        await _dbContext.SaveAsync(hotel);
    }
}
