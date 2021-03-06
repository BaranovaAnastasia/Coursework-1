﻿using DotsMovementModelingAppLib;
using DotsMovementModelingAppLib.Commands;
using System;
using System.Drawing;
using System.Windows.Forms;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

namespace DotsMovementModelingApp
{
    public partial class MainWindow : Form, IDisposable
    {
        public MainWindow()
        {
            InitializeComponent();
            GraphBuilder_SizeChanged(null, null);

            graphDrawing = new GraphDrawing(DrawingSurface.Width, DrawingSurface.Height);

            graphDrawing.RadiusChanged += (sender, args1) =>
            {
                if (BasicTypeCheckBox.Checked) graphDrawing.DrawTheWholeGraph(digraph);
                else graphDrawing.DrawTheWholeGraphSandpile(digraph, false);
                DrawingSurface.Image = graphDrawing.Image;
            };

            VerticesColorPanel.BackColor = graphDrawing.VerticesColor;
            ArcsColorPanel.BackColor = graphDrawing.ArcsColor;

            graphDrawing.SandpilePaletteChanged +=
                (sender, e) =>
                    DigraphInformationDemonstration.DisplaySandpileColors(graphDrawing, SandpilePalette);

            CommandsManager.CanRedoChanged += (sender, e) => RedoButton.Enabled = (bool)sender;
            CommandsManager.CanUndoChanged += (sender, e) => UndoButton.Enabled = (bool)sender;
        }

        /// <summary>
        /// Adjusts controls to fit the form's size
        /// </summary>
        private void GraphBuilder_SizeChanged(object sender, EventArgs e)
        {
            #region Main menu items

            Build.Location = new Point(Size.Width / 2 - Build.Size.Width / 2, Size.Height / 2 - Build.Size.Height - 50);
            RandomGraph.Location = new Point(Build.Location.X, Build.Location.Y + Build.Size.Height + 10);
            Open.Location = new Point(RandomGraph.Location.X, RandomGraph.Location.Y + RandomGraph.Size.Height + 10);
            SquareLattice.Location = new Point(Open.Location.X, Open.Location.Y + Open.Size.Height + 10);
            TriangleLattice.Location = new Point(Open.Location.X + Open.Size.Width - TriangleLattice.Size.Width,
                Open.Location.Y + Open.Size.Height + 10);

            #endregion

            if (graphDrawing == null) return;

            Cursor = Cursors.WaitCursor;
            graphDrawing.Size = DrawingSurface.Size;
            if (BasicTypeCheckBox.Checked || !isOnMovement) graphDrawing.DrawTheWholeGraph(digraph);
            else graphDrawing.DrawTheWholeGraphSandpile(digraph, false);
            DrawingSurface.Image = graphDrawing.Image;
            Cursor = Cursors.Default;
        }

        /// <summary>
        /// Moves digraph image on the drawing surface
        /// </summary>
        private void GraphBuilder_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ControlKey) isControlPressed = true;
            if (e.Modifiers != Keys.Control && !(sender is MouseEventArgs)) return;

            switch (e.KeyCode)
            {
                case Keys.Right:
                    xCoefficient += 10;
                    break;
                case Keys.Left:
                    xCoefficient -= 10;
                    break;
                case Keys.Up:
                    yCoefficient -= 10;
                    break;
                case Keys.Down:
                    yCoefficient += 10;
                    break;
                case Keys.Oemplus:
                    resizeCoefficient *= 1.1;
                    break;
                case Keys.OemMinus:
                    resizeCoefficient *= 0.9;
                    break;
                case Keys.Z:
                    if (isOnMovement) break;
                    UndoButton_Click(sender, e);
                    break;
                case Keys.Y:
                    if (isOnMovement) break;
                    RedoButton_Click(sender, e);
                    break;
                default: return;
            }

            if (isOnMovement && SandpileTypeCheckBox.Checked)
                graphDrawing.DrawTheWholeGraphSandpile(digraph, false, xCoefficient, yCoefficient, resizeCoefficient);
            else graphDrawing.DrawTheWholeGraph(digraph, xCoefficient, yCoefficient, resizeCoefficient);
            DrawingSurface.Image = graphDrawing.Image;
        }

        /// <summary>
        /// Executes commands to move digraph itself
        /// </summary>
        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ControlKey) isControlPressed = false;
            if (e.Modifiers != Keys.Control && sender != MouseWheelTimer) return;
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down ||
                e.KeyCode == Keys.Right || e.KeyCode == Keys.Left)
            {
                if (xCoefficient == 0 && yCoefficient == 0) return;
                var command = new MoveDigraphCommand(digraph, xCoefficient, yCoefficient);
                commandsManager.Execute(command);
                xCoefficient = yCoefficient = 0;
            }

            if (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Oemplus)
            {
                var command = new ResizeDigraphCommand(digraph, resizeCoefficient);
                commandsManager.Execute(command);
                resizeCoefficient = 1;
            }
        }

        /// <summary>
        /// Shifts focus to allow user to move the graph
        /// </summary>
        private void Movement_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.Modifiers == Keys.Control)
                Tools.Focus();
        }

        /// <summary>
        /// Moves digraph image according to mouse wheel scrolling
        /// </summary>
        private void MainWindow_MouseWheel(object sender, MouseEventArgs e)
        {
            MouseWheelTimer.Start();
            if (isControlPressed)
            {
                if (e.Delta > 0) GraphBuilder_KeyDown(e, new KeyEventArgs(Keys.Oemplus));
                else if (e.Delta < 0) GraphBuilder_KeyDown(e, new KeyEventArgs(Keys.OemMinus));
            }
            else
            {
                if (e.Delta > 0) GraphBuilder_KeyDown(e, new KeyEventArgs(Keys.Up));
                else if (e.Delta < 0) GraphBuilder_KeyDown(e, new KeyEventArgs(Keys.Down));
            }
        }

        /// <summary>
        /// Moves digraph itself according to mouse wheel scrolling
        /// </summary>
        private void WheelStopped(object sender, EventArgs e)
        {
            MouseWheelTimer.Stop();
            if (xCoefficient != 0 || yCoefficient != 0)
                MainWindow_KeyUp(MouseWheelTimer, new KeyEventArgs(Keys.Up));
            if (Math.Abs(resizeCoefficient - 1) > 0)
                MainWindow_KeyUp(MouseWheelTimer, new KeyEventArgs(Keys.OemMinus));

            if (!radiusChanged) return;
            radiusChanged = false;
            var command = new ChangeRadiusCommand(graphDrawing, radius, graphDrawing.R);
            command.Executed += (s, ea) => RadiusTrackBar.Value = (int)s;
            commandsManager.Execute(command);
            radius = graphDrawing.R;
        }

        /// <summary>
        /// Removes selections
        /// </summary>
        private void EscButton_Click(object sender, EventArgs e)
        {
            if (isOnMovement) return;
            vStart = vEnd = -1;
            movedVertexIndex = -1;
            graphDrawing.DrawTheWholeGraph(digraph, xCoefficient, yCoefficient, resizeCoefficient);
            DrawingSurface.Image = graphDrawing.Image;
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (digraph.Vertices.Count == 0) return;
            if (SaveGraph("Would you like to save the graph before leaving? Otherwise, your graph will be lost", "Saving") == DialogResult.Cancel)
                e.Cancel = true;
        }

        public new void Dispose()
        {
            graphDrawing.Dispose();
        }

        /// <summary>
        /// Subscribes handlers to digraph events
        /// </summary>
        private void SubscribeToDigraphEvents()
        {
            digraph.VertexAdded += (sender, e) =>
            {
                graphDrawing.DrawVertex(((Vertex)sender).X, ((Vertex)sender).Y,
                    e.Index + 1);
                DrawingSurface.Image = graphDrawing.Image;
                AddVertexToGridAdjacencyMatrix(e.Index);
                AddVertexToGridParameters(e.Index);
                ArcName.Items.Clear();
                foreach (var arc in digraph.Arcs)
                    ArcName.Items.Add(arc.ToString());
            };

            digraph.ArcAdded += (sender, e) =>
            {
                ArcName.Items.Insert(e.Index, ((Arc)sender).ToString());
                graphDrawing.DrawArc(digraph.Vertices[((Arc)sender).StartVertex],
                    digraph.Vertices[((Arc)sender).EndVertex],
                    (Arc)sender);
                DrawingSurface.Image = graphDrawing.Image;
                GridAdjacencyMatrix[((Arc)sender).EndVertex, ((Arc)sender).StartVertex].Value = ((Arc)sender).Length;
                vStart = vEnd = -1;
            };

            digraph.VertexRemoved += (sender, e) =>
            {
                RemoveVertexFromGridAdjacencyMatrix(e.Index);
                RemoveVertexFromGridParameters(e.Index);
                ArcName.Items.Clear();
                foreach (var arc in digraph.Arcs)
                    ArcName.Items.Add(arc.ToString());
            };

            digraph.ArcRemoved += (sender, e) =>
            {
                GridAdjacencyMatrix[((Arc)sender).EndVertex, ((Arc)sender).StartVertex].Value = 0;
                ArcName.Items.RemoveAt(e.Index);
            };
        }
    }
}
