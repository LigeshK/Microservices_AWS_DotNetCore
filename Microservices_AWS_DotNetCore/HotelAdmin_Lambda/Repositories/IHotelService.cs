using HotelAdmin_Lambda.Models;
namespace HotelAdmin_Lambda.Interfaces;

public interface IHotelService
{
    Task<IEnumerable<Hotel>> GetHotelsAsync(string userId);
    Task SaveHotelAsync(Hotel hotel);
}
