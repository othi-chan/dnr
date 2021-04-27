using Dnr.Service.Game.Models;
using System;

namespace Dnr.Web.Api.Models
{
    public class ArmyGet
    {
        public Player? Owner { get; set; }

        public string? SourceName { get; set; }

        public string? TargetName { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime FinishTime { get; set; }

        public double SpeedModifier { get; set; }

        public int Count { get; set; }
    }
}