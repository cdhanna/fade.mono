using System;
using System.Threading;
using FadeBasic;
using FadeBasic.Json;
using FadeBasic.Launch;
using FadeBasic.Virtual;

namespace Fade.MonoGame.Game;

public class CustomDebug : DebugSession
{
    public CustomDebug(VirtualMachine vm, DebugData dbg, CommandCollection commandCollection = null, LaunchOptions options = null, string label = null) : base(vm, dbg, commandCollection, options, label)
    {
    }

    public override void StartDebugging(int ops = 0)
    {
        int num1 = ops;
    while (this._options.debugWaitForConnection && this.hasConnectedDebugger == 0 && (ops <= 0 || num1-- != 0))
    {
      this.ReadMessage();
     // Thread.Sleep(1);
    }
    
    while (this._vm.instructionIndex < this._vm.program.Length)
    {
      if ((ops <= 0 || num1-- > 0) && !this.requestedExit && !_vm.isSuspendRequested)
      {
        this.ReadMessage();
        DebugToken currentToken;
        bool tokenBeforeIndex = this.instructionMap.TryFindClosestTokenBeforeIndex(this._vm.instructionIndex, out currentToken);
        if (tokenBeforeIndex)
        {
          if (this.hitBreakpointToken != null && currentToken != this.hitBreakpointToken)
            this.hitBreakpointToken = (DebugToken) null;
          if (!this.IsPaused && this.breakpointTokens.Contains(currentToken) && this.hitBreakpointToken == null)
          {
            this.logger.Log("HIT BREAKPOINT " + currentToken.Jsonify());
            this.pauseRequestedByMessageId = 1;
            this.resumeRequestedByMessageId = 0;
            this.hitBreakpointToken = currentToken;
            this.SendStopMessage();
            continue;
          }
        }
        if (!this.IsPaused)
        {
          if (this._vm.error.type != VirtualRuntimeErrorType.NONE)
          {
            this.logger.Error("Due to runtime exception, breaking out of exection");
            break;
          }
          bool movedOff = false;
          int num2;
          try
          {
            var wasSus = _vm.isSuspendRequested;
            if (!wasSus)
            {
              num2 = this._vm.Execute3(ops > 0 ? (num1 > 0 ? num1 : 1) : 0, (Func<int, bool>)(ins =>
              {
                if (this.receivedMessages.Count > 0)
                  return true;
                DebugToken token;
                if (this.instructionMap.TryFindClosestTokenBeforeIndex(ins, out token))
                {
                  if (token != currentToken)
                  {
                    movedOff = true;
                    this.hitBreakpointToken = (DebugToken)null;
                  }

                  if (movedOff && this.breakpointTokens.Contains(token))
                    return true;
                }

                return false;
              }));
            }
            else
            {
              num2 = 0;
            }
          }
          catch (Exception ex)
          {
            this.logger.Error("Unhandled VM exception during execution: " + ex.Message);
            this.pauseRequestedByMessageId = this.resumeRequestedByMessageId + 1;
            this.SendRuntimeErrorMessage(ex.Message);
            num2 = 0;
          }
          num1 -= num2;
          if (num1 < 0)
            num1 = 0;
          if (this._vm.error.type != VirtualRuntimeErrorType.NONE)
          {
            this.logger.Error($"Hit a runtime exception! message=[{this._vm.error.message}]");
            this.pauseRequestedByMessageId = this.resumeRequestedByMessageId + 1;
            this.SendRuntimeErrorMessage(this._vm.error.message);
          }
        }
        else if (this.stepNextMessage != null)
        {
          if (!tokenBeforeIndex)
          {
            this.Ack<StepNextResponseMessage>(this.stepNextMessage, new StepNextResponseMessage()
            {
              reason = $"no source location available while stepping. ins=[{this._vm.instructionIndex}]",
              status = -1
            });
            this.stepNextMessage = (DebugMessage) null;
          }
          else
          {
            bool flag1 = currentToken.insIndex != this.stepOverFromToken.insIndex;
            bool flag2 = this._vm.methodStack.Count <= this.stepStackDepth;
            bool flag3 = currentToken.isComputed == 0;
            this.logger.Log($"[VRB] looking for into-over real=[{flag3}]  is-new=[{flag1}] ins=[{this._vm.instructionIndex}] depth=[{this._vm.methodStack.Count}] start-depth=[{this.stepStackDepth}] token=[{currentToken.Jsonify()}] ");
            if (flag1 & flag2 & flag3)
            {
              this.Ack<StepNextResponseMessage>(this.stepNextMessage, new StepNextResponseMessage()
              {
                reason = "hit next",
                status = 1
              });
              this.stepNextMessage = (DebugMessage) null;
            }
            else
              this.StepExecute(ref this.stepNextMessage);
          }
        }
        else if (this.stepIntoMessage != null)
        {
          if (!tokenBeforeIndex)
          {
            this.logger.Log("[ERR] Failed to find step-into, so cancelling!");
            this.Ack<StepNextResponseMessage>(this.stepIntoMessage, new StepNextResponseMessage()
            {
              reason = $"no source location available while stepping in. ins=[{this._vm.instructionIndex}]",
              status = -1
            });
            this.stepIntoMessage = (DebugMessage) null;
          }
          else if (currentToken.insIndex != this.stepInFromToken.insIndex & currentToken.isComputed == 0)
          {
            this.Ack<StepNextResponseMessage>(this.stepIntoMessage, new StepNextResponseMessage()
            {
              reason = "hit in",
              status = 1
            });
            this.stepIntoMessage = (DebugMessage) null;
          }
          else
            this.StepExecute(ref this.stepIntoMessage);
        }
        else if (this.stepOutMessage != null)
        {
          if (!tokenBeforeIndex)
          {
            this.Ack<StepNextResponseMessage>(this.stepOutMessage, new StepNextResponseMessage()
            {
              reason = $"no source location available while stepping out. ins=[{this._vm.instructionIndex}]",
              status = -1
            });
            this.stepOutMessage = (DebugMessage) null;
          }
          else
          {
            bool flag4 = currentToken.insIndex != this.stepOutFromToken.insIndex;
            bool flag5 = this._vm.methodStack.Count < this.stepStackDepth || this._vm.methodStack.Count == 0;
            bool flag6 = currentToken.isComputed == 0;
            this.logger.Log($"[VRB] looking for out-step is-new=[{flag4}] ins=[{this._vm.instructionIndex}] depth=[{this._vm.methodStack.Count}] start-depth=[{this.stepStackDepth}] token=[{currentToken.Jsonify()}] ");
            if (flag4 & flag5 & flag6)
            {
              this.Ack<StepNextResponseMessage>(this.stepOutMessage, new StepNextResponseMessage()
              {
                reason = "hit out",
                status = 1
              });
              this.stepOutMessage = (DebugMessage) null;
            }
            else
              this.StepExecute(ref this.stepOutMessage);
          }
        }
      }
      else
        break;
    }
    
    if (this._vm.instructionIndex < this._vm.program.Length && !this.requestedExit)
      return;
    
    this.SendExitedMessage();
    }
}