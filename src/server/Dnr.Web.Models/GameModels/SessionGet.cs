using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dnr.Service.Game.Models;

namespace Dnr.Web.Api.Models
{
    public class SessionGet
    {
        public Guid Id { get; set; }

        public IEnumerable<Player>? Players { get; set; }

        public int PlayersCapacity { get; set; }

        public bool GameStarted { get; set; }

        public GameMapGet? Map { get; set; }

        public Player? Winner { get; set; }
    }
}