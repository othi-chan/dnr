using System;
using System.Collections.Concurrent;

namespace Dnr.Service.Game.Models.Abstractions
{
    public interface ISession
    {
        Guid Id { get; }

        ConcurrentDictionary<long, Player> Players { get; }

        Player? Winner { get; set; }

        int PlayersCapacity { get; }

        bool GameStarted { get; }

        IGameMap? Map { get; set; }
    }
}