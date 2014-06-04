﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MoonSharp.Interpreter.DataStructs;
using MoonSharp.Interpreter.Debugging;

namespace MoonSharp.Interpreter.Execution.VM
{
	// This part is practically written procedural style - it looks more like C than C#.
	// This is intentional so to avoid this-calls and virtual-calls as much as possible.
	// Same reason for the "sealed" declaration.
	sealed partial class Processor
	{
		internal void AttachDebugger(IDebugger debugger)
		{
			m_DebuggerAttached = debugger;
		}

		private void ListenDebugger(Instruction instr)
		{
			if (instr.Breakpoint)
			{
				m_DebuggerCurrentAction = DebuggerAction.ActionType.None;
				m_DebuggerCurrentActionTarget = -1;
			}

			if (m_DebuggerCurrentAction == DebuggerAction.ActionType.Run)
				return;

			if (m_DebuggerCurrentAction == DebuggerAction.ActionType.StepOver && m_DebuggerCurrentActionTarget != m_InstructionPtr)
				return;

			RefreshDebugger();

			while (true)
			{
				var action = m_DebuggerAttached.GetAction(m_InstructionPtr);

				switch (action.Action)
				{
					case DebuggerAction.ActionType.StepIn:
						m_DebuggerCurrentAction = DebuggerAction.ActionType.StepIn;
						m_DebuggerCurrentActionTarget = -1;
						return;
					case DebuggerAction.ActionType.StepOver:
						m_DebuggerCurrentAction = DebuggerAction.ActionType.StepOver;
						m_DebuggerCurrentActionTarget = m_InstructionPtr + 1;
						return;
					case DebuggerAction.ActionType.Run:
						m_DebuggerCurrentAction = DebuggerAction.ActionType.Run;
						m_DebuggerCurrentActionTarget = -1;
						return;
					case DebuggerAction.ActionType.ToggleBreakpoint:
						m_CurChunk.Code[action.InstructionPtr].Breakpoint = !m_CurChunk.Code[action.InstructionPtr].Breakpoint;
						break;
					case DebuggerAction.ActionType.Refresh:
						RefreshDebugger();
						break;
					case DebuggerAction.ActionType.None:
					default:
						break;
				}
			}
		}

		private void RefreshDebugger()
		{
			List<string> watchList = m_DebuggerAttached.GetWatchItems();
			List<WatchItem> callStack = Debugger_GetCallStack();
			List<WatchItem> watches = Debugger_RefreshWatches(watchList);
			List<WatchItem> vstack = Debugger_RefreshVStack();
			m_DebuggerAttached.Update(WatchType.CallStack, callStack);
			m_DebuggerAttached.Update(WatchType.Watches, watches);
			m_DebuggerAttached.Update(WatchType.VStack, vstack);
		}

		private List<WatchItem> Debugger_RefreshVStack()
		{
			List<WatchItem> lwi = new List<WatchItem>();
			for (int i = 0; i < Math.Min(16, m_ValueStack.Count); i++)
			{
				lwi.Add(new WatchItem()
				{
					Address = i,
					Value = m_ValueStack.Peek(i)
				});
			}

			return lwi;
		}

		private List<WatchItem> Debugger_RefreshWatches(List<string> watchList)
		{
			return watchList.Select(w => Debugger_RefreshWatch(w)).ToList();
		}

		private WatchItem Debugger_RefreshWatch(string name)
		{
			LRef L = m_Scope.FindRefByName(name);

			if (L != null)
			{
				RValue v = m_Scope.Get(L);

				return new WatchItem()
				{
					LValue = L,
					Value = v,
					Name = name
				};
			}
			else
			{
				return new WatchItem() { Name = name };
			}
		}

		private List<WatchItem> Debugger_GetCallStack()
		{
			List<WatchItem> wis = new List<WatchItem>();

			for (int i = 0; i < m_ExecutionStack.Count; i++)
			{
				var c = m_ExecutionStack.Peek(i);

				var I = m_CurChunk.Code[c.Debug_EntryPoint];

				string callname = I.OpCode == OpCode.DebugFn ? I.Name : null;


				wis.Add(new WatchItem()
				{
					Address = c.Debug_EntryPoint,
					BasePtr = c.BasePointer,
					RetAddress = c.ReturnAddress,
					Name = callname
				});
			}

			return wis;
		}
	}
}