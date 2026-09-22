using System;
using System.Collections.Generic;

namespace DartsRoguelike.Mock
{
    public enum CardKind { Strike, Guard, Heal, Poison, Drain, Coin }
    public enum HazardKind { None, Mine, Fire, Curse }
    public enum BattlePhase { Player, Victory, Defeat }
    public enum IntentKind { Attack, Heal, Mine, Fire, Curse, Panic, Tremor, Drunk }
    public enum TargetRewardKind { Attack, Heal, Guard }

    [Serializable]
    public sealed class Sector
    {
        public CardKind Card;
        public HazardKind Hazard;
        public int Turns;
        public int Uses;
    }

    public struct Hit
    {
        public int Sector, Multiplier, Score;
        public bool Miss;
        public Hit(int sector, int multiplier, int score, bool miss = false)
        { Sector = sector; Multiplier = multiplier; Score = score; Miss = miss; }
    }

    public struct Intent
    {
        public IntentKind Kind;
        public int Value;
        public Intent(IntentKind kind, int value) { Kind = kind; Value = value; }
        public override string ToString()
        {
            switch (Kind)
            {
                case IntentKind.Attack: return "攻撃： " + Value + "ダメージ";
                case IntentKind.Heal: return "回復： " + Value + "体力";
                case IntentKind.Mine: return "地雷設置：マス " + BattleModel.Numbers[Value];
                case IntentKind.Fire: return "炎設置：マス " + BattleModel.Numbers[Value] + "（2ターン）";
                case IntentKind.Curse: return "呪い設置：マス " + BattleModel.Numbers[Value] + "（永続）";
                case IntentKind.Panic: return "焦りを付与（2ターン）";
                case IntentKind.Tremor: return "震えを付与（2ターン）";
                default: return "酔いを付与（2ターン）";
            }
        }
    }

    // Pure combat state: UI and input never participate in damage or phase resolution.
    public sealed class BattleModel
    {
        public static readonly int[] Numbers = { 20,1,18,4,13,6,10,15,2,17,3,19,7,16,8,11,14,9,12,5 };
        public static readonly string[] CardNames = { "攻撃", "防御", "回復", "毒", "吸収", "コイン" };
        public static readonly int[] CardValues = { 8,7,5,3,4,5 };
        public static readonly int[] TargetScores = { 61, 81, 101, 121, 141 };
        public readonly Sector[] Board = new Sector[21];
        public readonly bool[] Used = new bool[3];
        public readonly List<Intent> Intents = new List<Intent>();
        public readonly List<string> Log = new List<string>();
        readonly Random random;
        public int Hp = 70, MaxHp = 70, EnemyHp = 135, EnemyMaxHp = 135;
        public int Shield, Poison, Coins, Score, Turn = 1, Panic, Tremor, Drunk;
        public int TurnScore { get; private set; }
        public int TargetScore { get; private set; }
        public TargetRewardKind TargetReward { get; private set; }
        public bool TargetAchieved { get; private set; }
        public string TargetRewardDescription
        {
            get
            {
                switch (TargetReward)
                {
                    case TargetRewardKind.Attack: return "大攻撃：敵に45ダメージ";
                    case TargetRewardKind.Heal: return "大回復：HPを最大25回復";
                    default: return "鉄壁：防御35＋操作デバフ解除";
                }
            }
        }
        public BattlePhase Phase = BattlePhase.Player;
        public int Remaining { get { int n = 0; foreach (bool used in Used) if (!used) n++; return n; } }
        public BattleModel(int seed)
        {
            random = new Random(seed);
            for (int i = 0; i < Board.Length; i++) Board[i] = new Sector { Card = Draw() };
            // Ensure the first board exposes every core effect; coins exist only at battle start.
            for (int i = 0; i < 6; i++) Board[i * 3].Card = (CardKind)i;
            Board[20].Card = CardKind.Strike;
            SetHazard(6, HazardKind.Mine, -1, 1);
            SetHazard(12, HazardKind.Fire, 2, -1);
            SetHazard(18, HazardKind.Curse, -1, -1);
            DrawTarget();
            Plan();
            Note("ダーツを選び、盤面で長押しして離そう。");
        }

        CardKind Draw() { return (CardKind)random.Next(5); }
        void DrawTarget()
        {
            TurnScore = 0;
            TargetAchieved = false;
            TargetScore = TargetScores[random.Next(TargetScores.Length)];
            TargetReward = (TargetRewardKind)random.Next(3);
        }
        void AwardTarget()
        {
            TargetAchieved = true;
            switch (TargetReward)
            {
                case TargetRewardKind.Attack:
                    EnemyHp -= 45;
                    Note("目標達成！ 大攻撃で45ダメージ");
                    break;
                case TargetRewardKind.Heal:
                    int before = Hp;
                    Hp = Math.Min(MaxHp, Hp + 25);
                    Note("目標達成！ HPを" + (Hp - before) + "回復");
                    break;
                case TargetRewardKind.Guard:
                    Shield += 35;
                    Panic = Tremor = Drunk = 0;
                    Note("目標達成！ 防御35＋操作デバフ解除");
                    break;
            }
        }
        public void Note(string value) { Log.Insert(0, value); if (Log.Count > 6) Log.RemoveAt(6); }
        public void SetHazard(int sector, HazardKind kind, int turns, int uses)
        { Board[sector].Hazard = kind; Board[sector].Turns = turns; Board[sector].Uses = uses; }
        public static Hit Evaluate(float x, float y)
        {
            double r = Math.Sqrt(x*x+y*y);
            if (r > 1) return new Hit(-1,0,0,true);
            if (r <= .0375) return new Hit(20,2,50);
            if (r <= .0935) return new Hit(20,1,25);
            double degrees = Math.Atan2(x,y)*180/Math.PI;
            int index = (int)Math.Floor((degrees + 369) % 360 / 18);
            int multiplier = r >= .94 ? 2 : (r >= .56 && r <= .62 ? 3 : 1);
            return new Hit(index,multiplier,Numbers[index]*multiplier);
        }
        public bool Throw(Hit hit, int dart)
        {
            if (Phase != BattlePhase.Player || Remaining == 0 || dart < 0 || dart >= 3 || Used[dart]) return false;
            Used[dart] = true;
            if (hit.Miss) { Note("ミス！　ダーツを1本消費"); return true; }
            if (hit.Sector < 0 || hit.Sector >= Board.Length || hit.Multiplier < 1 || hit.Multiplier > 3)
                throw new ArgumentOutOfRangeException(nameof(hit));
            Score += hit.Score;
            TurnScore += hit.Score;
            Sector sector = Board[hit.Sector];
            int amount = CardValues[(int)sector.Card] * hit.Multiplier;
            switch (sector.Card)
            {
                case CardKind.Strike: EnemyHp -= amount; break;
                case CardKind.Guard: Shield += amount; break;
                case CardKind.Heal: Hp = Math.Min(MaxHp, Hp + amount); break;
                case CardKind.Poison: Poison += amount; break;
                case CardKind.Drain: EnemyHp -= amount; Hp = Math.Min(MaxHp, Hp + amount); break;
                case CardKind.Coin: Coins += amount; break;
            }
            // Dart effects apply to every successful hit, including bull.
            if (dart == 0) EnemyHp -= 3;
            if (dart == 1) Shield += 4;
            if (dart == 2) Poison += 2;
            Note((hit.Sector == 20 ? "ブル" : Numbers[hit.Sector].ToString()) + " x" + hit.Multiplier +
                 "  /  " + CardNames[(int)sector.Card] + " " + amount);
            if (sector.Hazard != HazardKind.None)
            {
                int damage = sector.Hazard == HazardKind.Mine ? 9 : sector.Hazard == HazardKind.Fire ? 5 : 4;
                Hurt(damage);
                Note((sector.Hazard == HazardKind.Mine ? "地雷" : sector.Hazard == HazardKind.Fire ? "炎" : "呪い") + "  /  " + damage + "ダメージ（防御で軽減）");
                if (sector.Uses > 0 && --sector.Uses == 0) sector.Hazard = HazardKind.None;
            }
            if (!TargetAchieved && TurnScore == TargetScore) AwardTarget();
            // Simultaneous death is defeat. Hazards resolve even on a killing throw.
            if (Finish()) return true;
            sector.Card = Draw();
            return true;
        }
        void Hurt(int amount)
        {
            int blocked = Math.Min(Shield, amount);
            Shield -= blocked;
            Hp = Math.Max(0, Hp - amount + blocked);
        }
        bool Finish()
        {
            EnemyHp = Math.Max(0, EnemyHp);
            if (Hp <= 0) Phase = BattlePhase.Defeat;
            else if (EnemyHp <= 0) Phase = BattlePhase.Victory;
            return Phase != BattlePhase.Player;
        }
        public bool EndTurn()
        {
            if (Phase != BattlePhase.Player) return false;
            if (Poison > 0) { EnemyHp -= Poison; Note("毒： " + Poison + "ダメージ"); Poison = Math.Max(0, Poison - 1); }
            if (Finish()) return true;
            // Old hazards and statuses expire before new enemy effects are installed.
            foreach (Sector s in Board)
                if (s.Hazard != HazardKind.None && s.Turns > 0 && --s.Turns == 0) s.Hazard = HazardKind.None;
            Panic = Math.Max(0, Panic - 1); Tremor = Math.Max(0, Tremor - 1); Drunk = Math.Max(0, Drunk - 1);
            foreach (Intent intent in Intents)
            {
                Note("敵： " + intent);
                switch (intent.Kind)
                {
                    case IntentKind.Attack: Hurt(intent.Value); break;
                    case IntentKind.Heal: EnemyHp = Math.Min(EnemyMaxHp, EnemyHp + intent.Value); break;
                    case IntentKind.Mine: SetHazard(intent.Value, HazardKind.Mine, -1, 1); break;
                    case IntentKind.Fire: SetHazard(intent.Value, HazardKind.Fire, 2, -1); break;
                    case IntentKind.Curse: SetHazard(intent.Value, HazardKind.Curse, -1, -1); break;
                    case IntentKind.Panic: Panic = 2; break;
                    case IntentKind.Tremor: Tremor = 2; break;
                    case IntentKind.Drunk: Drunk = 2; break;
                }
                if (Finish()) return true;
            }
            Shield = 0;
            Array.Clear(Used, 0, Used.Length);
            Turn++;
            DrawTarget();
            Plan();
            return true;
        }
        void Plan()
        {
            Intents.Clear();
            Intents.Add(new Intent(IntentKind.Attack, 9 + Math.Min(12, Turn * 2)));
            int slot = random.Next(20);
            switch ((Turn - 1) % 4)
            {
                case 0: Intents.Add(new Intent(IntentKind.Mine,slot)); Intents.Add(new Intent(IntentKind.Panic,2)); break;
                case 1: Intents.Add(new Intent(IntentKind.Heal,8)); Intents.Add(new Intent(IntentKind.Fire,slot)); break;
                case 2: Intents.Add(new Intent(IntentKind.Tremor,2)); Intents.Add(new Intent(IntentKind.Curse,slot)); break;
                case 3: Intents.Add(new Intent(IntentKind.Heal,10)); Intents.Add(new Intent(IntentKind.Drunk,2)); break;
            }
        }
    }
}
