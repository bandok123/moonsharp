﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MoonSharp.Interpreter.Execution;

namespace MoonSharp.Debugger
{
	public partial class ValueBrowser : Form
	{
		Stack<RValue> m_ValueStack = new Stack<RValue>();
		public static void StartBrowse(RValue v)
		{
			if (v == null)
				return;

			ValueBrowser b = new ValueBrowser();
			b.m_ValueStack.Push(v);
			b.ShowDialog();
		}

		public ValueBrowser()
		{
			InitializeComponent();
		}

		private void ValueBrowser_Load(object sender, EventArgs e)
		{
			InvalidateData();
		}

		private void InvalidateData()
		{
			RValue V = m_ValueStack.Peek();
			toolBack.Enabled = (m_ValueStack.Count > 1);

			lvMetaTable.BeginUpdate();
			lvProps.BeginUpdate();
			lvTableData.BeginUpdate();

			lvMetaTable.Items.Clear();
			lvProps.Items.Clear();
			txtString.Text = "";
			lvTableData.Items.Clear();
			lblData.Text = "VALUE";
			lvTableData.Visible = false;
			txtString.Visible = false;

			AddProperty("Ref ID#", V.ReferenceID.ToString("X8"));
			AddProperty("Read Only", V.ReadOnly);
			AddProperty("VM Type", V.Type);

			switch (V.Type)
			{
				case DataType.Nil:
					txtString.Visible = true;
					txtString.Text = "Value is nil";
					break;
				case DataType.Boolean:
					txtString.Visible = true;
					txtString.Text = V.Boolean.ToString();
					break;
				case DataType.Number:
					txtString.Visible = true;
					txtString.Text = V.Boolean.ToString();
					break;
				case DataType.String:
					txtString.Visible = true;
					txtString.Text = V.String.ToString();
					AddProperty("Raw Length", V.GetLength());
					break;
				case DataType.Function:
					lvTableData.Visible = true;
					lblData.Text = "CLOSURE SCOPE";
					BuildFunctionTable(V);
					break;
				case DataType.Table:
					lvTableData.Visible = true;
					lblData.Text = "TABLE CONTENTS";
					AddProperty("Raw Length", V.GetLength());
					BuildTableTable(lvTableData, V);
					break;
				case DataType.Tuple:
					lvTableData.Visible = true;
					lblData.Text = "TUPLE";
					AddProperty("Count", V.Tuple.Length);
					BuildTupleTable(V);
					break;
				case DataType.Symbol:
					lblData.Text = "SYMBOL / TABLE-REF";
					txtString.Text = V.String.ToString();
					BuildSymbolTable(V);
					break;
				case DataType.ClrFunction:
					txtString.Visible = true;
					txtString.Text = "Value is a CLR function.";
					break;
				case DataType.UNSUPPORTED_UserData:
					txtString.Visible = true;
					txtString.Text = "Value is a CLR object (userdata).";
					break;
				case DataType.UNSUPPORTED_Thread:
					txtString.Visible = true;
					txtString.Text = "Value is a coroutine.";
					break;
				default:
					break;
			}

			Colorize(lvMetaTable);
			Colorize(lvProps);
			Colorize(lvTableData);

			lvMetaTable.EndUpdate();
			lvProps.EndUpdate();
			lvTableData.EndUpdate();
		}

		private void Colorize(ListView lv)
		{
			foreach (ListViewItem lvi in lv.Items)
			{
				if (lvi.Tag is RValue)
				{
					lvi.ForeColor = Color.Blue;
				}
			}
		}

		private void BuildSymbolTable(RValue V)
		{
			var S = V.Symbol;
			lvTableData.Add("Type", S.Type);
			lvTableData.Add("Index", S.Index);
			lvTableData.Add("Name", S.Name);
			lvTableData.Add("Table Ref Index", S.TableRefIndex);
			lvTableData.Add("Table Ref Object", S.TableRefObject).Tag = S.TableRefObject;
		}

		private void BuildFunctionTable(RValue V)
		{
			var F = V.Function;
			var C = F.ClosureContext;
			lvProps.Add("Bytecode Location", F.ByteCodeLocation);

			for (int i = 0; i < C.Count; i++)
			{
				lvTableData.Add(C.Symbols[i], C[i]).Tag = C[i];
			}
		}

		private void BuildTupleTable(RValue V)
		{
			var T = V.Tuple;

			for (int i = 0; i < T.Length; i++)
			{
				lvTableData.Add(i.ToString(), T[i]).Tag = T[i];
			}
		}

		private void BuildTableTable(ListView listView, RValue V)
		{
			var T = V.Table;

			foreach (var kvp in T.DebugPairs())
			{
				listView.Add(kvp.Key, kvp.Value).Tag = kvp.Value;
			}
		}

		private void AddProperty(string p1, object p2)
		{
			lvProps.Add(p1, p2.ToString());
		}

		private void lvTableData_SelectedIndexChanged(object sender, EventArgs e)
		{
			lvMetaTable.SelectedIndices.Clear();
			lvProps.SelectedIndices.Clear();
		}

		private void lvMetaTable_SelectedIndexChanged(object sender, EventArgs e)
		{
			lvTableData.SelectedIndices.Clear();
			lvProps.SelectedIndices.Clear();
		}

		private void lvProps_SelectedIndexChanged(object sender, EventArgs e)
		{
			lvTableData.SelectedIndices.Clear();
			lvMetaTable.SelectedIndices.Clear();
		}

		private void lvAnyTable_DoubleClick(object sender, EventArgs e)
		{
			DigData(sender as ListView);
		}

		private void DigData(ListView listView)
		{
			if (listView == null) return;
			ListViewItem lvi = listView.SelectedItems.OfType<ListViewItem>().FirstOrDefault();
			if (lvi == null) return;

			RValue v = lvi.Tag as RValue;

			if (v != null)
			{
				m_ValueStack.Push(v);
				InvalidateData();
			}
		}

		private void toolDigData_Click(object sender, EventArgs e)
		{
			ListView[] lvs = new ListView[] { lvMetaTable, lvTableData, lvProps };
			DigData(lvs.FirstOrDefault(lv => lv.SelectedItems.Count > 0));
		}

		private void toolBack_Click(object sender, EventArgs e)
		{
			m_ValueStack.Pop();
			InvalidateData();
		}









	}
}