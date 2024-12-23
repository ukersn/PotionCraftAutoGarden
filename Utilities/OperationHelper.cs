using PotionCraft.LocalizationSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace PotionCraftAutoGarden.Utilities
{
    public class OperationHelper
    {
        private int operationCount;
        private bool isInterrupted;
        private string msg_noOperableItemsMessage; //没有操作(任何物品的时候)时弹出的信息
        private string msg_operationCompletedMessageKey; //操作完成时候弹出的信息
        private string msg_operationInterruptedMessage; //操作中断时候弹出的信息(比如施肥没有药水)

        public OperationHelper(string msg_noOperableItemsMessage, string msg_operationCompletedMessageKey, string msg_operationInterruptedMessage)
        {
            this.operationCount = 0;
            this.isInterrupted = false;
            this.msg_noOperableItemsMessage = msg_noOperableItemsMessage;
            this.msg_operationCompletedMessageKey = msg_operationCompletedMessageKey;
            this.msg_operationInterruptedMessage = msg_operationInterruptedMessage;
        }
        public void IncrementCount(){ operationCount++; }
        public void ResetStatus(){ operationCount = 0; isInterrupted = false; }
        public int GetOperationCount(){ return operationCount; }
        public bool IsInterrupted(){ return isInterrupted; }
        public void ShowCompletedMessage(string customNoOperableItemsMessage = null, string customOperationCompletedMessage = null)
        {
            if (isInterrupted) { return; }
            string noOperableMessage = customNoOperableItemsMessage ?? msg_noOperableItemsMessage;
            string completedMessage = customOperationCompletedMessage ?? msg_operationCompletedMessageKey;

            if (operationCount == 0)
            {
                Tooltis.SpawnMessageText(LocalizationManager.GetText(noOperableMessage));
            }
            else
            {
                string message = string.Format(LocalizationManager.GetText(completedMessage), operationCount);
                Tooltis.SpawnMessageText(message);
            }
            ResetStatus();
        }
        public void ShowInterruptedMessage(string customInterruptedMessage = null)
        {
            isInterrupted = true;
            string interruptedMessage = customInterruptedMessage ?? msg_operationInterruptedMessage;

            string message = LocalizationManager.GetText(interruptedMessage);
            Tooltis.SpawnMessageText(message);
        }

    }
}