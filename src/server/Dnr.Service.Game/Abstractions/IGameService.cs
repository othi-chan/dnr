using Dnr.Service.Game.Models;
using System;
using System.Collections.Concurrent;

namespace Dnr.Service.Game.Abstractions
{
    public interface IGameService
    {
        ConcurrentDictionary<Guid, Session> Sessions { get; }

        (bool succeed, Session? session) CreateSession(long accauntId, string creatorLogin);

        (bool succeed, Session? session) AttachSession(long accauntId, string playerLogin, Guid sessionId);

        (bool succeed, Session? session) DetachSession(long accauntId, Guid sessionId);

        (bool succeed, Session? session) StartGame(Guid sessionId);

        (bool succeed, Session? session) VillageLevelUp(Guid sessionId, string villageName);

        (bool succeed, Session? session) SendArmy(
            long accauntId,
            Guid sessionId,
            string sourceName,
            string targetName,
            int armyCount);
    }
}