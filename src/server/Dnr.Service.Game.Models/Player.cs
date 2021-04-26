using Dnr.Service.Game.Models.Abstractions;
using System;

namespace Dnr.Service.Game.Models
{
    public class Player : IPlayer
    {
        public long Id { get; }

        public string Name { get; }

        public int Influence { get; set; }

        public Player(long playerId, string playerName)
        {
            Id = playerId;
            Name = playerName;
            Influence = 0;
        }
    }
}