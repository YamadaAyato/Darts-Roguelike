using System;
using System.Collections.Generic;
using UnityEngine;

namespace DartsRoguelike.Mock
{
    [Serializable]
    public struct LinkSegment
    {
        public Vector2 From;
        public Vector2 To;
        public int Power;
        public int ActivatedCells;

        public LinkSegment(Vector2 from, Vector2 to, int power, int activatedCells)
        {
            From = from;
            To = to;
            Power = power;
            ActivatedCells = activatedCells;
        }
    }

    // Standalone rules for MockBattle2. A dart scores normally, while completed
    // lines activate every unique board cell crossed by the segment.
    public sealed class LinkBattleModel
    {
        public const int EmptyCellPercent = 60;
        public static readonly int[] CardValues = { 5, 4, 4, 2, 3, 3, 0 };
        public readonly Sector[] Board = new Sector[21];
        public readonly bool[] Used = new bool[3];
        public readonly List<Vector2> Points = new List<Vector2>();
        public readonly List<LinkSegment> Segments = new List<LinkSegment>();
        public readonly HashSet<int> ActivatedThisTurn = new HashSet<int>();
        public readonly HashSet<int> DangerCells = new HashSet<int>();
        public readonly HashSet<int> TriggeredDanger = new HashSet<int>();
        public readonly List<string> Log = new List<string>();

        readonly System.Random random;
        public int Hp = 70, MaxHp = 70, EnemyHp = 150, EnemyMaxHp = 150;
        public int Shield, Poison, Coins, Score, Turn = 1;
        public int TurnScore { get; private set; }
        public int TargetScore { get; private set; }
        public TargetRewardKind TargetReward { get; private set; }
        public bool TargetAchieved { get; private set; }
        public BattlePhase Phase = BattlePhase.Player;

        public int Remaining
        {
            get { int result = 0; foreach (bool used in Used) if (!used) result++; return result; }
        }

        public string TargetRewardDescription
        {
            get
            {
                switch (TargetReward)
                {
                    case TargetRewardKind.Attack: return "大攻撃：敵に45ダメージ";
                    case TargetRewardKind.Heal: return "大回復：HPを最大25回復";
                    default: return "鉄壁：防御35を獲得";
                }
            }
        }

        public LinkBattleModel(int seed)
        {
            random = new System.Random(seed);
            for (int i = 0; i < Board.Length; i++)
            {
                Board[i] = new Sector();
                for (int band = 0; band < 4; band++) Board[i].Cards[band] = DrawCell();
            }

            // A readable opening layout: a horizontal route crosses several effects.
            Board[5].Cards[2] = CardKind.Strike;
            Board[15].Cards[2] = CardKind.Heal;
            Board[20].Cards[0] = CardKind.Drain;
            DangerCells.Add(CellKey(0, BoardBand.InnerSingle));
            DangerCells.Add(CellKey(10, BoardBand.OuterSingle));
            DangerCells.Add(CellKey(16, BoardBand.Triple));
            DrawTarget();
            Note("1投目で起点を置き、次のダーツで線をつなごう。");
        }

        CardKind DrawCell()
        {
            return random.Next(100) < EmptyCellPercent ? CardKind.Empty : (CardKind)random.Next(6);
        }

        void DrawTarget()
        {
            int[] scores = { 61, 81, 101, 121, 141 };
            TurnScore = 0;
            TargetAchieved = false;
            TargetScore = scores[random.Next(scores.Length)];
            TargetReward = (TargetRewardKind)random.Next(3);
        }

        public static int CellKey(int sector, BoardBand band)
        {
            return sector == 20 ? 80 : sector * 4 + (int)band;
        }

        public static void DecodeCell(int key, out int sector, out int band)
        {
            if (key == 80) { sector = 20; band = 0; return; }
            sector = key / 4;
            band = key % 4;
        }

        public CardKind CardAt(int key)
        {
            int sector, band;
            DecodeCell(key, out sector, out band);
            return Board[sector].Cards[band];
        }

        public bool Throw(Vector2 point, int dart)
        {
            if (Phase != BattlePhase.Player || dart < 0 || dart >= Used.Length || Used[dart]) return false;
            Used[dart] = true;
            Hit hit = BattleModel.Evaluate(point.x, point.y);
            if (hit.Miss)
            {
                Note("ミス！ ダーツを1本消費");
                return true;
            }

            Score += hit.Score;
            TurnScore += hit.Score;
            if (dart == 0) EnemyHp -= 3;
            if (dart == 1) Shield += 4;
            if (dart == 2) Poison += 2;

            Vector2 clamped = Vector2.ClampMagnitude(point, 1f);
            if (Points.Count > 0)
            {
                int power = Segments.Count + 1;
                ResolveLine(Points[Points.Count - 1], clamped, power);
            }
            else
            {
                Note((hit.Sector == 20 ? "ブル" : BattleModel.Numbers[hit.Sector].ToString()) +
                    "に起点 / " + hit.Score + "点");
            }
            Points.Add(clamped);

            if (!TargetAchieved && TurnScore == TargetScore) AwardTarget();
            Finish();
            return true;
        }

        void ResolveLine(Vector2 from, Vector2 to, int power)
        {
            var ordered = CellsOnLine(from, to);
            int activated = 0;
            int dangerDamage = 0;
            foreach (int key in ordered)
            {
                if (ActivatedThisTurn.Add(key))
                {
                    ApplyCard(CardAt(key), power);
                    activated++;
                }
                if (DangerCells.Contains(key) && TriggeredDanger.Add(key)) dangerDamage += 7;
            }
            if (dangerDamage > 0) Hurt(dangerDamage);
            int lineDamage = 3 * power;
            EnemyHp -= lineDamage;
            Segments.Add(new LinkSegment(from, to, power, activated));
            Note("線" + Segments.Count + "：" + activated + "マス発動 x" + power +
                " / 線ダメージ" + lineDamage + (dangerDamage > 0 ? " / 罠" + dangerDamage : ""));
        }

        List<int> CellsOnLine(Vector2 from, Vector2 to)
        {
            var result = new List<int>();
            var seen = new HashSet<int>();
            const int samples = 160;
            for (int i = 0; i <= samples; i++)
            {
                Vector2 point = Vector2.Lerp(from, to, i / (float)samples);
                Hit hit = BattleModel.Evaluate(point.x, point.y);
                if (hit.Miss) continue;
                int key = CellKey(hit.Sector, hit.Sector == 20 ? BoardBand.InnerSingle : hit.Band);
                if (seen.Add(key)) result.Add(key);
            }
            return result;
        }

        void ApplyCard(CardKind card, int power)
        {
            int value = CardValues[(int)card] * power;
            switch (card)
            {
                case CardKind.Strike: EnemyHp -= value; break;
                case CardKind.Guard: Shield += value; break;
                case CardKind.Heal: Hp = Math.Min(MaxHp, Hp + value); break;
                case CardKind.Poison: Poison += value; break;
                case CardKind.Drain:
                    EnemyHp -= value;
                    Hp = Math.Min(MaxHp, Hp + value);
                    break;
                case CardKind.Coin: Coins += value; break;
            }
        }

        void AwardTarget()
        {
            TargetAchieved = true;
            switch (TargetReward)
            {
                case TargetRewardKind.Attack:
                    EnemyHp -= 45;
                    Note("目標達成！ 大攻撃45");
                    break;
                case TargetRewardKind.Heal:
                    Hp = Math.Min(MaxHp, Hp + 25);
                    Note("目標達成！ 大回復25");
                    break;
                case TargetRewardKind.Guard:
                    Shield += 35;
                    Note("目標達成！ 防御35");
                    break;
            }
        }

        public bool EndTurn()
        {
            if (Phase != BattlePhase.Player) return false;
            if (Poison > 0)
            {
                EnemyHp -= Poison;
                Note("毒：" + Poison + "ダメージ");
                Poison = Math.Max(0, Poison - 1);
            }
            if (Finish()) return true;

            int attack = 8 + Math.Min(16, Turn * 3);
            Hurt(attack);
            Note("敵の攻撃：" + attack + "ダメージ");
            if (Turn % 2 == 0)
            {
                EnemyHp = Math.Min(EnemyMaxHp, EnemyHp + 8);
                Note("敵が8回復");
            }
            if (Finish()) return true;

            foreach (int key in ActivatedThisTurn)
            {
                int sector, band;
                DecodeCell(key, out sector, out band);
                Board[sector].Cards[band] = DrawCell();
            }
            Shield = 0;
            Array.Clear(Used, 0, Used.Length);
            Points.Clear();
            Segments.Clear();
            ActivatedThisTurn.Clear();
            TriggeredDanger.Clear();
            AddDangerCell();
            Turn++;
            DrawTarget();
            return true;
        }

        void AddDangerCell()
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                int key = random.Next(80);
                if (DangerCells.Add(key)) return;
            }
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

        public void Note(string message)
        {
            Log.Insert(0, message);
            if (Log.Count > 6) Log.RemoveAt(6);
        }
    }
}
