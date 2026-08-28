using System;
using System.Collections.Generic;

namespace MeraBrand.Expo.Booking
{
    [Serializable]
    public sealed class StallBookingRecord
    {
        public string stallId;
        public bool isBooked;
        public string exhibitorName;
        public string logoReference;
        public string updatedUtc;
    }

    [Serializable]
    public sealed class StallBookingDatabase
    {
        public List<StallBookingRecord> records = new();
    }
}
