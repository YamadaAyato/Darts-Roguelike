using System;
using DartsRoguelike.Core.Model;

namespace DartsRoguelike.Definitions
{
    /// <summary>
    ///     敵の行動をInspectorで設定するための基底型
    /// </summary>
    [Serializable]
    public abstract class EnemyActionDefinition
    {
        /// <summary>
        ///     設定から予告に使う戦闘用の行動を作る
        /// </summary>
        internal abstract IEnemyAction CreateAction();
    }
}
