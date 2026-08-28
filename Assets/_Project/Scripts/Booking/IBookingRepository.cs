namespace MeraBrand.Expo.Booking
{
    public interface IBookingRepository
    {
        StallBookingDatabase Load();
        void Save(StallBookingDatabase database);
    }
}
