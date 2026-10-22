using System;
using System.Collections.Generic;

namespace HealthTrack.Models
{
    public class HealthRecordsViewModel
    {
        // Summary Cards (Top) data
        public decimal CurrentWeight { get; set; }
        public decimal CurrentHeight { get; set; }
        public decimal CurrentBMI { get; set; }
        public int LastHeartRate { get; set; } = 72; // if only UI needs it, otherwise fetch from DB

        // the history should show in the table
        public List<HealthRecord> Records { get; set; } = new List<HealthRecord>();
    }
}