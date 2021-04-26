using Dnr.Service.Game.Models.Abstractions;
using System;
using System.Collections.Concurrent;

namespace Dnr.Service.Game.Models
{
    public class Session : ISession
    {
        public Guid Id { get; }

        public ConcurrentDictionary<long, Player> Players { get; }

        public Player? Winner { get; set; }

        public int PlayersCapacity { get; }

        public bool GameStarted => Map != null;

        public IGameMap? Map { get; set; }

        public Session(Guid sessionId)
        {
            Id = sessionId;
            Players = new ConcurrentDictionary<long, Player>();
            Winner = null;
            PlayersCapacity = 2;
            Map = null;
        }
    }
}