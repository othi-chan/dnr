using System;

namespace Dnr.Service.Game.Models.Abstractions
{
    public interface IArmy
    {
        IPlayer Owner { get; }

        ILocation Source { get; }

        ILocation Target { get; }

        IRoad Road { get; }

        double SpeedModifier { get; }

        DateTime StartTime { get; }

        DateTime FinishTime { get; }

        int Count { get; set; }
    }
}
