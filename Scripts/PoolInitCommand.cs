using System;
using UnityEngine;

namespace FPS.Pool
{
    public class PoolInitCommand : SyncCommand
    {
        public override void Do()
        {
            try
            {
                FluffyPool.Init();
                Status = CommandStatus.Success;
            }
            catch (Exception e)
            {
                Status = CommandStatus.Error;
                Debug.LogError(e);
            }
        }
    }
}