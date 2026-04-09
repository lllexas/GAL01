using NekoGraph;
using UnityEngine;

namespace GAL01.Dialog.Runtime
{
    /// <summary>
    /// 对话播放器 - MonoBehaviour 协程驱动喵~
    /// 
    /// 职责：
    /// - 接收 DialogSequenceSO 并逐条播放 Entries
    /// - 播放完成后调用 runner.InjectSignal 恢复信号流
    /// 
    /// TODO: 实现具体播放逻辑（协程、PostSystem 事件、等待用户输入等）
    /// </summary>
    public class DialogPlayer : MonoBehaviour
    {
        private static DialogPlayer _instance;
        
        public static void Play(
            DialogSequenceSO sequence,
            SignalContext context,
            BasePackData pack,
            GraphRunner runner,
            string packInstanceID)
        {
            // TODO: 实现播放逻辑
            // 1. 确保 Instance 存在
            // 2. 启动协程播放 Entries
            // 3. 播放完成后 runner.InjectSignal 恢复信号流
            
            Debug.Log($"[DialogPlayer] TODO: 播放序列 {sequence.name}，共 {sequence.Entries.Count} 条");
            
            // 临时：直接放行（后续改为真正播放完成后调用）
            // runner.InjectSignal(packInstanceID, new SignalContext(nextNodeId, context.Args));
        }
    }
}
