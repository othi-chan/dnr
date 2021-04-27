using Dnr.Service.Game.Abstractions;
using Dnr.Service.Game.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dnr.Service.Game
{
    public class GameService : IGameService
    {
        public ConcurrentDictionary<Guid, Session> Sessions { get; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "<Pending>")]
        private Timer GeneralTimer { get; }

        private ConcurrentDictionary<Guid, Timer> GameTimers { get; }

        private ConcurrentDictionary<Guid, Timer> WinTimers { get; }

        private LinkedList<(Army army, Timer timer, Guid sessionId)> BattleTimers { get; }

        public GameService()
        {
            Sessions = new ConcurrentDictionary<Guid, Session>();
            GeneralTimer = new Timer(
                new TimerCallback(UpdateSessionStates),
                state: null,
                dueTime: 1000,
                period: 1000);
            GameTimers = new ConcurrentDictionary<Guid, Timer>();
            WinTimers = new ConcurrentDictionary<Guid, Timer>();
            BattleTimers = new LinkedList<(Army army, Timer timer, Guid sessionId)>();
        }

        public (bool succeed, Session? session) AttachSession(long accauntId, string playerLogin, Guid sessionId)
        {
            if (!Sessions.ContainsKey(sessionId))
                return (succeed: false, session: null);

            var succeedSessionGet = Sessions.TryGetValue(sessionId, out var session);
            if (!succeedSessionGet || session == null)
                return (succeed: false, session);

            if (session.Players.Count >= session.PlayersCapacity)
                return (succeed: false, session);

            var player = new Player(accauntId, playerLogin);
            var succeedPlayerAdd = session.Players.TryAdd(accauntId, player);

            return (succeed: succeedPlayerAdd, session);
        }

        public (bool succeed, Session? session) CreateSession(long accauntId, string creatorLogin)
        {
            var sessionKey = Guid.NewGuid();
            while (Sessions.ContainsKey(sessionKey))
                sessionKey = Guid.NewGuid();

            var newSession = new Session(sessionKey);
            var succeedSessionAdd = Sessions.TryAdd(sessionKey, newSession);
            return !succeedSessionAdd
                ? (succeed: false, session: null)
                : AttachSession(accauntId, creatorLogin, sessionKey);
        }

        public (bool succeed, Session? session) DetachSession(long accauntId, Guid sessionId)
        {
            if (!Sessions.ContainsKey(sessionId))
                return (succeed: false, session: null);

            var succeedSessionGet = Sessions.TryGetValue(sessionId, out var session);
            if (!succeedSessionGet || session == null)
                return (succeed: false, session);

            var succeedPlayerGet = session.Players.TryGetValue(accauntId, out var player);
            if (!succeedPlayerGet || player == null)
                return (succeed: false, session);

            var succeedPlayerRemove = session.Players.TryRemove(player.Id, out _);
            if (session.Players.Any())
                return (succeed: succeedPlayerRemove, session);

            var succeedSessionRemove = Sessions.TryRemove(sessionId, out _);
            return (succeed: succeedSessionRemove, session: null);
        }

        public (bool succeed, Session? session) StartGame(Guid sessionId)
        {
            if (!Sessions.ContainsKey(sessionId))
                return (succeed: false, session: null);

            var succeedSessionGet = Sessions.TryGetValue(sessionId, out var session);
            if (!succeedSessionGet || session == null)
                return (succeed: false, session);

            if (session.Players.Count != session.PlayersCapacity)
                return (succeed: false, session);

            var gameTimer = new Timer(
                new TimerCallback(GameOver),
                state: sessionId,
                dueTime: (int)TimeSpan.FromMinutes(10).TotalMilliseconds,
                period: Timeout.Infinite);
            GameTimers.TryAdd(sessionId, gameTimer);

            session.Map = new GameMap(session.Players.Values);
            return (succeed: true, session);
        }

        private void UpdateSessionStates(object? state)
        {
            foreach (var session in Sessions.Values)
            {
                if (!session.GameStarted)
                    continue;

                foreach (var player in session.Players.Values)
                {
                    foreach (var location in session.Map!.Locations.Where(_ => _.Owner != null && _.Owner.Id == player.Id))
                    {
                        if (location is Village village)
                            player.Influence += village.InfluenceGrowth;
                        if (location is Castle castle)
                            player.Influence += castle.InfluenceGrowth;
                    }
                }

                foreach (var location in session.Map!.Locations)
                {
                    if (location is Village village)
                        village.ArmyCount += village.ArmyGrowth;
                    if (location is Castle castle)
                        castle.ArmyCount += castle.ArmyGrowth;
                }
            }
        }

        public (bool succeed, Session? session) VillageLevelUp(Guid sessionId, string villageName)
        {
            if (!Sessions.ContainsKey(sessionId))
                return (succeed: false, session: null);

            var succeedSessionGet = Sessions.TryGetValue(sessionId, out var session);
            if (!succeedSessionGet || session == null)
                return (succeed: false, session);

            var village = (Village)session.Map!.Locations.Single(_ => _.Name == villageName);
            if (village.Owner == null || village.Owner.Influence < village.LevelUpCost)
                return (succeed: false, session);

            village.Level++;
            return (succeed: true, session);
        }

        public (bool succeed, Session? session) SendArmy(
            Guid sessionId,
            string sourceName,
            string targetName,
            int armyCount)
        {
            if (!Sessions.ContainsKey(sessionId))
                return (succeed: false, session: null);

            var succeedSessionGet = Sessions.TryGetValue(sessionId, out var session);
            if (!succeedSessionGet || session == null)
                return (succeed: false, session);

            var sourceLocation = session.Map!.Locations.Single(_ => _.Name == sourceName);
            var road = sourceLocation.Roads.Single(_ => _.Ends.Item1.Name == targetName || _.Ends.Item2.Name == targetName);
            var targetLocation = road.Ends.Item1.Name == targetName ? road.Ends.Item1 : road.Ends.Item2;

            if (armyCount > sourceLocation.ArmyCount)
                return (succeed: false, session);
            sourceLocation.ArmyCount -= armyCount;

            var army = new Army(owner: sourceLocation.Owner!, sourceLocation, targetLocation, road, armyCount);
            road.Armies.Add(army);
            session.Map.Armies.Add(army);

            var elapsedBattleTime = (army.FinishTime - army.StartTime).TotalSeconds;
            var elapsedDistance = road.Length;

            var battleTimer = new Timer(
                new TimerCallback(SimulateBattle),
                state: army,
                dueTime: (int)(elapsedBattleTime * 1000),
                period: Timeout.Infinite);
            BattleTimers.AddLast((army, battleTimer, sessionId));

            return (succeed: true, session);
        }

        private void SimulateBattle(object? state)
        {
            var army = (Army)state!;

            // Если деревня принадлежит владельцу армии
            if (army.Target.Owner != null && army.Target.Owner.Name == army.Owner.Name)
            {
                // Добавляем армию к гарнизону
                army.Target.ArmyCount += army.Count;
            }
            // Если деревня не принадлежит владельцу армии
            else
            {
                // Расчет итогов битвы
                if (army.Target is Village village)
                {
                    var battleResult = army.Count - (int)(village.ArmyCount * village.DefenseModifier);
                    if (battleResult > 0)
                    {
                        village.Owner = army.Owner;
                        village.ArmyCount = battleResult;
                    }
                    else
                    {
                        village.ArmyCount = (int)(-battleResult / village.DefenseModifier);
                    }
                }
                if (army.Target is Castle castle)
                {
                    var battleResult = army.Count - (int)(castle.ArmyCount * castle.DefenseModifier);
                    if (battleResult > 0)
                    {
                        castle.Owner = army.Owner;
                        castle.ArmyCount = battleResult;

                        var winTimer = new Timer(
                            new TimerCallback(GameOver),
                            state: army.Owner,
                            dueTime: 60000,
                            period: Timeout.Infinite);
                        var sessionId2 = BattleTimers.Single(
                            _ => _.army.Owner.Id == army.Owner.Id && _.army.StartTime == army.StartTime).sessionId;

                        if (WinTimers.ContainsKey(sessionId2))
                            _ = WinTimers.TryRemove(sessionId2, out _);
                        WinTimers.TryAdd(sessionId2, winTimer);
                    }
                    else
                    {
                        castle.ArmyCount = (int)(-battleResult / castle.DefenseModifier);
                    }
                }

                // Проверяем не закончилась ли игра
                var sessionId = BattleTimers.Single(
                    _ => _.army.Owner.Id == army.Owner.Id && _.army.StartTime == army.StartTime).sessionId;
                _ = Sessions.TryGetValue(sessionId, out var session);

                if (!session?.Map?.Locations.Any(
                    _ => _.Owner != null && _.Owner.Id == session.Players.First().Value.Id)
                    ?? throw new Exception())
                {
                    _ = WinTimers.TryRemove(sessionId, out _);
                    _ = GameTimers.TryRemove(sessionId, out _);

                    session!.Winner = session.Players.Last().Value;
                    var gameTimer = new Timer(
                        new TimerCallback(GameOver),
                        state: sessionId,
                        dueTime: (int)TimeSpan.FromSeconds(60).TotalMilliseconds,
                        period: Timeout.Infinite);
                    GameTimers.TryAdd(sessionId, gameTimer);
                }
                if (!session?.Map?.Locations.Any(
                    _ => _.Owner != null && _.Owner.Id == session.Players.Last().Value.Id)
                    ?? throw new Exception())
                {
                    _ = WinTimers.TryRemove(sessionId, out _);
                    _ = GameTimers.TryRemove(sessionId, out _);

                    session!.Winner = session.Players.First().Value;
                    var gameTimer = new Timer(
                        new TimerCallback(GameOver),
                        state: sessionId,
                        dueTime: (int)TimeSpan.FromSeconds(60).TotalMilliseconds,
                        period: Timeout.Infinite);
                    GameTimers.TryAdd(sessionId, gameTimer);
                }

                // Удаляем использованный таймер
                BattleTimers.Remove(
                        BattleTimers.Single(_ => _.army.Owner.Id == army.Owner.Id && _.army.StartTime == army.StartTime));
            }
        }

        private void GameOver(object? state)
        {
            // Закончилось время
            if (state is Guid sessionId2)
            {
                _ = Sessions.TryGetValue(sessionId2, out var session);
                if (session!.Winner == null)
                {
                    // Победа по очкам
                    session!.Winner = session.Players.First().Value.Influence > session.Players.Last().Value.Influence
                        ? session.Players.First().Value : session.Players.Last().Value;

                    _ = WinTimers.TryRemove(sessionId2, out _);
                    _ = GameTimers.TryRemove(sessionId2, out _);

                    var gameTimer = new Timer(
                        new TimerCallback(GameOver),
                        state: sessionId2,
                        dueTime: (int)TimeSpan.FromSeconds(60).TotalMilliseconds,
                        period: Timeout.Infinite);
                    GameTimers.TryAdd(sessionId2, gameTimer);
                }
                else
                {
                    // Конец игры
                    _ = GameTimers.TryRemove(sessionId2, out _);
                    _ = Sessions.TryRemove(sessionId2, out _);
                }
            }
            else
            {
                // Замок захвачен
                if (state is Player player)
                {
                    var session = Sessions.Values.Where(_ => _.Players.Any(_ => _.Key == player.Id)).Single();
                    var sessionId = session.Id;
                    session.Winner = player;

                    _ = WinTimers.TryRemove(sessionId, out _);
                    _ = GameTimers.TryRemove(sessionId, out _);

                    var gameTimer = new Timer(
                        new TimerCallback(GameOver),
                        state: sessionId,
                        dueTime: (int)TimeSpan.FromSeconds(60).TotalMilliseconds,
                        period: Timeout.Infinite);
                    GameTimers.TryAdd(sessionId, gameTimer);
                }
            }
        }
    }
}