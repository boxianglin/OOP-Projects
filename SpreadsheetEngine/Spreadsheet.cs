﻿// <copyright file="Spreadsheet.cs" company="Boxiang Lin - WSU 011601661">
// Copyright (c) Boxiang Lin - WSU 011601661. All rights reserved.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using CptS321;

namespace SpreadsheetEngine
{
    /// <summary>
    /// Spreadsheet class.
    /// </summary>
    public class Spreadsheet
    {
        /// <summary>
        /// 2D Array stored the cells.
        /// </summary>
#pragma warning disable SA1401 // Public as PDF instruction.
        public TheCell[,] Cells;
#pragma warning restore SA1401 // Public as PDF instruction.

        private int columnCount;
        private int rowCount;

        /// <summary>
        /// Undo stack to store specic command that implements the ICommand Interface.
        /// </summary>
        private Stack<ICommand> undos;

        /// <summary>
        /// Redo stack to store specifc command that implements the Icoomand Interface.
        /// </summary>
        private Stack<ICommand> redos;

        /// <summary>
        /// Initializes a new instance of the <see cref="Spreadsheet"/> class.
        /// </summary>
        /// <param name="row"> row number. </param>
        /// <param name="col"> column number. </param>
        public Spreadsheet(int row, int col)
        {
            this.CellsInit(row, col);
            this.columnCount = col;
            this.rowCount = row;
            this.undos = new Stack<ICommand>();
            this.redos = new Stack<ICommand>();
        }

        /// <summary>
        /// CellPropertyChanged Event Handler.
        /// </summary>
        public event PropertyChangedEventHandler CellPropertyChanged;

        /// <summary>
        /// Gets the columnCount.
        /// </summary>
        public int ColumnCount
        {
            get
            {
                return this.columnCount;
            }
        }

        /// <summary>
        /// Gets the rowCount.
        /// </summary>
        public int RowCount
        {
            get
            {
                return this.rowCount;
            }
        }

        /// <summary>
        /// Get Cell by location.
        /// </summary>
        /// <param name="row"> row. </param>
        /// <param name="col"> col. </param>
        /// <returns> Specific Cell. </returns>
        public TheCell GetCell(int row, int col)
        {
            return row >= 0 && row < this.rowCount && col >= 0 && col < this.rowCount ? this.Cells[row, col] : null;
        }

        /// <summary>
        /// When brand new command add, redo should be disable.
        /// By instruction, disable is to make the redo stack empty.
        /// </summary>
        /// <param name="command"> incoming command. </param>
        public void NewCommandAdd(ICommand command)
        {
            this.redos.Clear();
            command.Execute();
            this.undos.Push(command);
        }

        /// <summary>
        /// Undo Command execution.
        /// pop the undo stack, unexecute, and push it to redo stack.
        /// </summary>
        public void RunUndoCommand()
        {
            if (this.undos.Count > 0)
            {
                ICommand undoCommand = this.undos.Pop();
                undoCommand.Unexecute();
                this.redos.Push(undoCommand);
            }
        }

        /// <summary>
        /// Redo command execution.
        /// pop the redo stack, execute, and push it to undo stack.
        /// </summary>
        public void RunRedoCommand()
        {
            if (this.redos.Count > 0)
            {
                ICommand redoCommand = this.redos.Pop();
                redoCommand.Execute();
                this.undos.Push(redoCommand);
            }
        }

        /// <summary>
        /// Returning the info fo the command in string for UI to display.
        /// </summary>
        /// <returns> string description of the type. </returns>
        public string GetRedoCommandInfo()
        {
            return this.redos.Peek().ToString();
        }

        /// <summary>
        /// Returning the info fo the command in string for UI to display.
        /// </summary>
        /// <returns> string description of the command. </returns>
        public string GetUndoCommandInfo()
        {
            return this.undos.Peek().ToString();
        }

        /// <summary>
        /// Return bool empty if true otherwise false for use of UI determination.
        /// </summary>
        /// <returns> bool. </returns>
        public bool IsEmptyUndoStack()
        {
            return this.undos.Count <= 0;
        }

        /// <summary>
        /// Return bool empty if true otherwise false for use of UI determination.
        /// </summary>
        /// <returns> bool. </returns>
        public bool IsEmptyRedoStack()
        {
            return this.redos.Count <= 0;
        }

        /// <summary>
        /// Load the xml to spreadsheet from stream.
        /// </summary>
        /// <param name="s"> stream. </param>
        public void LoadFromXml(Stream s)
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(s);

            // root pick
            XmlNode root = xDoc.SelectSingleNode("spreadsheet");

            if (root == null)
            {
                return;
            }

            this.undos.Clear();
            this.redos.Clear();
            XmlNodeList childList = root.ChildNodes;

            // traverse through cells in spreadsheet.
            foreach (XmlNode child in childList)
            {
                XmlElement element = (XmlElement)child;

                // encounter a cell.
                if (element.Name == "cell")
                {
                    // retrieve the spreadsheet cell.
                    string cellname = element.GetAttribute("name");
                    TheCell cell = this.GetCellByName(cellname);

                    // traverse the attributes of a cell --> for cell's text and bgcolors.
                    foreach (XmlNode cchild in child.ChildNodes)
                    {
                        XmlElement childElement = (XmlElement)cchild;
                        if (childElement.Name == "bgcolor")
                        {
                            string color = childElement.InnerText;

                            // if no input in bgcolor tag, continue for next tag
                            if (color.Length == 0)
                            {
                                continue;
                            }

                            // if such bgcolor not in ARGB format we want its prefix to be 0 until most significant bit - A
                            while (color.Length < 6)
                            {
                                color = "0" + color;
                            }

                            // if such bgcolor not in ARGB we want its A(alpha component value) to be fully opaque.
                            while (color.Length < 8)
                            {
                                color = "F" + color;
                            }

                            uint newColor = Convert.ToUInt32(color, 16);
                            cell.BGColor = newColor;
                        }

                        if (childElement.Name == "text")
                        {
                            cell.Text = childElement.InnerText;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Spreadsheet save to XML from stream.
        /// </summary>
        /// <param name="s"> stream. </param>
        public void SaveToXML(Stream s)
        {
            XmlWriter wr = XmlWriter.Create(s);

            // root
            wr.WriteStartElement("spreadsheet");

            // refer to our local cell data.
            foreach (TheCell cell in this.Cells)
            {
                // if not default value we need to save info to the XML
                if (cell.Text != string.Empty || cell.BGColor != 0xFFFFFFFF)
                {
                    string cellname = cell.ColumnIndex + (cell.RowIndex + 1).ToString();

                    // second layer by cell.
                    wr.WriteStartElement("cell");
                    wr.WriteAttributeString("name", cellname);

                    // write the color first if not default.
                    if (cell.BGColor != 0xFFFFFFFF)
                    {
                        // third layer.
                        wr.WriteStartElement("bgcolor");

                        // for 16 bits int in string representation.
                        wr.WriteString(cell.BGColor.ToString("X"));
                        wr.WriteEndElement();
                    }

                    if (cell.Text != string.Empty)
                    {
                        // third layer.
                        wr.WriteStartElement("text");
                        wr.WriteString(cell.Text);
                        wr.WriteEndElement();
                    }

                    // end second layer.
                    wr.WriteEndElement();
                }
            }

            // close first layer.
            wr.WriteEndElement();
            wr.Close();
        }

        /// <summary>
        /// Init the 2D cell elements and configure the CellPropertyChange event for each cell in array.
        /// </summary>
        /// <param name="row"> row number. </param>
        /// <param name="col"> column number. </param>
        private void CellsInit(int row, int col)
        {
            this.Cells = new TheCell[row, col];
            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    char colIndex = (char)('A' + j);
                    TheCell cell = new TheCell(i, colIndex);
                    this.Cells[i, j] = cell;
                    cell.PropertyChanged += this.OnCellPropertyChanged;
                    cell.RefCellValueChanged += this.OnRefCellValueChanged;
                }
            }
        }

        /// <summary>
        /// Cell property event listener. Update the value when text property get changed.
        /// </summary>
        /// <param name="sender"> object.</param>
        /// <param name="e"> event.</param>
        private void OnCellPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("Text"))
            {
                this.SetCellValue(sender as TheCell);
            }

            if (e.PropertyName.Equals("BGColor"))
            {
                this.CellPropertyChanged?.Invoke(sender as TheCell, new PropertyChangedEventArgs("BGColor"));
            }
        }

        /// <summary>
        /// Reference cell event listener. Update value as ref cell property changes.
        /// </summary>
        /// <param name="sender">Ref cell.</param>
        /// <param name="e">Event.</param>
        private void OnRefCellValueChanged(object sender, EventArgs e)
        {
            this.SetCellValue(sender as TheCell);
        }

        /// <summary>
        /// <Helper> Set the cell value and if do the expression computation. </Helper>
        /// Case 1: If start with =, use expression tree to evaluate the result.
        /// Case 2: Not start with =, just set text to value.
        /// </summary>
        /// <param name="cell">Spreadsheet cell.</param>
        private void SetCellValue(TheCell cell)
        {
            string newValue = cell.Text;

            if (newValue.StartsWith("="))
            {
                // remove = and whitespaces
                string expression = cell.Text.Substring(1).Replace(" ", string.Empty);
                ExpressionTree exp = new ExpressionTree(expression);
                bool error = this.SetVariable(exp, cell);
                if (!error)
                {
                    newValue = exp.Evaluate().ToString();
                }
                else
                {
                    newValue = cell.Value;
                }
            }

            cell.SetValue(newValue);
            this.CellPropertyChanged?.Invoke(cell, new PropertyChangedEventArgs("Value"));
        }

        /// <summary>
        /// Variable in the Expression tree set with value according to the ref cell.
        /// </summary>
        /// <param name="exp"> ExpressionTree. </param>
        /// <param name="currCell"> current cell.</param>
        /// <returns> error or no error. </returns>
        private bool SetVariable(ExpressionTree exp, TheCell currCell)
        {
            // all variable names captured during the construction of expression tree.
            HashSet<string> variableNames = exp.GetAllVariableName();
            foreach (string cellname in variableNames)
            {
                // Get the reference cell.
                TheCell refCell = this.GetCellByName(cellname);

                if (refCell == null)
                {
                    currCell.SetValue("!(bad reference)");
                    return true;
                }
                else if (currCell == refCell)
                {
                    currCell.SetValue("!(self reference)");
                    return true;
                }
                else if (refCell.Value.Equals("!(bad reference)") || refCell.Value.Equals("!(self reference)") || refCell.Value.Equals("!(circular reference)"))
                {
                    if (refCell.Value.Equals("!(circular reference)"))
                    {
                        currCell.SetValue("!(circular reference)");
                        currCell.SubToCellPropertyChange(refCell);
                        return true;
                    }
                    else
                    {
                        currCell.SetValue("!(reference to invalid)");
                        currCell.SubToCellPropertyChange(refCell);
                        return true;
                    }
                }
                else if (this.BFSforCircular(currCell, refCell))
                {
                    currCell.SetValue("!(circular reference)");
                    return true;
                }
                else
                {
                    // If all above invalid checkes passed good to get refCell value.
                    double num = 0.0;
                    if (double.TryParse(refCell.Value, out num))
                    {
                        num = double.Parse(refCell.Value);
                    }

                    exp.SetVariable(cellname, num);
                    currCell.SubToCellPropertyChange(refCell);
                }
            }

            return false;
        }

        /// <summary>
        /// As required, valName = [char][digit].
        /// </summary>
        /// <param name="valName"> char++digit. </param>
        /// <returns> a specific TheCell object in our 2D storage. </returns>
        private TheCell GetCellByName(string valName)
        {
            try
            {
                // to support lowercase cell name
                char col = char.ToUpper(valName[0]);
                string row = valName.Substring(1);
                int colIndex = col - 'A';
                int rowIndex = int.Parse(row) - 1;
                return this.GetCell(rowIndex, colIndex);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Do BFS for all the reference cell layer per layer.
        /// </summary>
        /// <param name="curCell"> current cell. </param>
        /// <param name="refCell"> reference cell. </param>
        /// <returns> true or false. </returns>
        private bool BFSforCircular(TheCell curCell, TheCell refCell)
        {
            if (refCell.Text.StartsWith("="))
            {
                // reference cell's expression tree to get its ref cells in the formula.
                ExpressionTree referTree = new ExpressionTree(refCell.Text.Substring(1).Replace(" ", string.Empty));
                Queue<TheCell> q = new Queue<TheCell>();
                foreach (string item in referTree.GetAllVariableName())
                {
                    TheCell refRefCell = this.GetCellByName(item);

                    // prepare the queue then we will need to do some adjacency cell (ref's ref cell) checking.
                    q.Enqueue(refRefCell);
                }

                while (q.Count > 0)
                {
                    // Go through the current level
                    int size = q.Count;
                    for (int i = 0; i < size; i++)
                    {
                        TheCell refRefCell = q.Dequeue();

                        // check if the reference cell's references includes current cell, if yes, return true for cirular reference.
                        if (refRefCell.Equals(curCell))
                        {
                            return true;
                        }
                        else
                        {
                            // otherwise if referencell is formula based, we need to check the cells in the formula one by one.
                            if (refRefCell.Text.StartsWith("="))
                            {
                                // from formula form the subtree.
                                ExpressionTree subtree = new ExpressionTree(refRefCell.Text.Substring(1).Replace(" ", string.Empty));

                                // search through the subtree.
                                foreach (string item in subtree.GetAllVariableName())
                                {
                                    // add each referenced cell in the formula into the q for next iteration.
                                    TheCell subRefCell = this.GetCellByName(item);
                                    q.Enqueue(subRefCell);
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}
