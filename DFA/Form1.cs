using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace DFAVisualizer
{
    public class Form1 : Form
    {
        // =====================================================================
        // DFA DEFINITION  ->  M = (Q, Sigma, delta, q0, F)
        // =====================================================================

        private readonly string[] states = { "q0", "q1" };
        private readonly char[] alphabet = { 'a', 'b' };
        private readonly string startState = "q0";
        private readonly HashSet<string> finalStates = new HashSet<string> { "q1" };

        // delta(q0,a)=q0   delta(q0,b)=q1   delta(q1,a)=q1   delta(q1,b)=q0
        private readonly Dictionary<string, Dictionary<char, string>> transitions =
            new Dictionary<string, Dictionary<char, string>>
            {
                { "q0", new Dictionary<char, string> { { 'a', "q0" }, { 'b', "q1" } } },
                { "q1", new Dictionary<char, string> { { 'a', "q1" }, { 'b', "q0" } } }
            };

        // Where each state is drawn, keyed by state name. Recomputed on resize
        // so the diagram never breaks when the window is resized - this was
        // hardcoded to fixed pixel coordinates in the original version.
        private Dictionary<string, PointF> statePositions = new Dictionary<string, PointF>();
        private const int StateRadius = 42;

        // =====================================================================
        // CONTROLS
        // =====================================================================

        private Panel drawingPanel;
        private DataGridView transitionTable;
        private TextBox testInput;
        private Label testResult;

        public Form1()
        {
            InitializeForm();
            CreateInterface();
        }

        private void InitializeForm()
        {
            Text = "DFA Visualizer - M = (Q, Sigma, delta, q0, F)";
            Width = 1150;
            Height = 720;
            MinimumSize = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10, FontStyle.Regular);
        }

        private void CreateInterface()
        {
            var titleLabel = new Label
            {
                Text = "DFA Visualizer",
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                AutoSize = true,
                Location = new Point(30, 15)
            };
            Controls.Add(titleLabel);

            var tupleLabel = new Label
            {
                Text = "M = (Q, \u03A3, \u03B4, q\u2080, F)     Q = {q\u2080, q\u2081}     \u03A3 = {a, b}     q\u2080 = q0     F = {q\u2081}",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 70, 120),
                AutoSize = true,
                Location = new Point(32, 58)
            };
            Controls.Add(tupleLabel);

            // -------------------------------------------------------------
            // Drawing panel: docked/anchored so it resizes with the window
            // -------------------------------------------------------------
            drawingPanel = new Panel
            {
                Location = new Point(30, 95),
                Size = new Size(700, 480),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            drawingPanel.Paint += DrawingPanel_Paint;
            drawingPanel.Resize += (s, e) => drawingPanel.Invalidate();
            Controls.Add(drawingPanel);

            var tableTitle = new Label
            {
                Text = "Transition table",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(750, 95),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Controls.Add(tableTitle);

            CreateTransitionTable();
            CreateStringTester();

            var legend = new Label
            {
                Text = "Arrow into q0 = start state.   Double circle = final (accepting) state.",
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.DimGray,
                AutoSize = true,
                Location = new Point(32, 585),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            Controls.Add(legend);
        }

        private void CreateTransitionTable()
        {
            transitionTable = new DataGridView
            {
                Location = new Point(750, 130),
                Size = new Size(360, 110),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 11)
            };

            transitionTable.Columns.Add("state", "State");
            transitionTable.Columns.Add("a", "a");
            transitionTable.Columns.Add("b", "b");

            foreach (var state in states)
            {
                string label = state == startState ? "\u2192 " + state : state;
                if (finalStates.Contains(state)) label = "* " + label;

                int row = transitionTable.Rows.Add();
                transitionTable.Rows[row].Cells[0].Value = label;
                transitionTable.Rows[row].Cells[1].Value = transitions[state]['a'];
                transitionTable.Rows[row].Cells[2].Value = transitions[state]['b'];
            }

            transitionTable.EnableHeadersVisualStyles = false;
            transitionTable.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(55, 75, 120),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            transitionTable.DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11),
                SelectionBackColor = Color.LightBlue,
                SelectionForeColor = Color.Black
            };

            Controls.Add(transitionTable);
        }

        // A small bonus over the original: type a string of a's and b's and
        // the app runs it through the DFA and tells you accept/reject, with
        // the exact state path. Useful for actually demonstrating the DFA
        // works instead of only showing a static picture.
        private void CreateStringTester()
        {
            var testTitle = new Label
            {
                Text = "Test a string",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(750, 260),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Controls.Add(testTitle);

            testInput = new TextBox
            {
                Location = new Point(750, 295),
                Size = new Size(240, 28),
                Font = new Font("Segoe UI", 11),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Controls.Add(testInput);

            var runButton = new Button
            {
                Text = "Run",
                Location = new Point(998, 293),
                Size = new Size(110, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            runButton.Click += (s, e) => RunTestString();
            Controls.Add(runButton);

            testResult = new Label
            {
                Text = "",
                Font = new Font("Consolas", 10),
                AutoSize = false,
                Size = new Size(360, 160),
                Location = new Point(750, 335),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Controls.Add(testResult);
        }

        private void RunTestString()
        {
            string input = testInput.Text.Trim();

            if (input.Length == 0)
            {
                testResult.ForeColor = Color.DarkOrange;
                testResult.Text = "Enter a string of a's and b's first.";
                return;
            }

            string current = startState;
            var path = new List<string> { current };
            bool valid = true;

            foreach (char c in input)
            {
                if (!alphabet.Contains(c))
                {
                    valid = false;
                    testResult.ForeColor = Color.DarkRed;
                    testResult.Text = $"Invalid symbol '{c}'. Alphabet is only {{ a, b }}.";
                    return;
                }

                current = transitions[current][c];
                path.Add(current);
            }

            bool accepted = finalStates.Contains(current);

            testResult.ForeColor = accepted ? Color.DarkGreen : Color.DarkRed;
            testResult.Text =
                (accepted ? "ACCEPTED" : "REJECTED") +
                $"\nEnds in state: {current}\nPath: {string.Join(" -> ", path)}";
        }

        // =====================================================================
        // LAYOUT + PAINT
        // =====================================================================

        private void DrawingPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            ComputeStatePositions(drawingPanel.ClientSize);
            DrawDFA(g);
        }

        // Lays states out evenly along a horizontal line, centered in the
        // panel, so this scales to more states without editing coordinates.
        private void ComputeStatePositions(Size panelSize)
        {
            statePositions.Clear();

            int count = states.Length;
            int spacing = 285;
            int totalWidth = spacing * Math.Max(count - 1, 1);
            int startX = (panelSize.Width - totalWidth) / 2;
            int y = panelSize.Height / 2 - 25;

            for (int i = 0; i < count; i++)
            {
                int x = count == 1 ? panelSize.Width / 2 : startX + i * spacing;
                statePositions[states[i]] = new PointF(x, y);
            }
        }

        private void DrawDFA(Graphics g)
        {
            using var statePen = new Pen(Color.FromArgb(30, 30, 30), 3);
            using var arrowPen = new Pen(Color.FromArgb(40, 40, 40), 3);
            using var labelFont = new Font("Segoe UI", 13, FontStyle.Bold);
            using var stateFont = new Font("Segoe UI", 13, FontStyle.Bold);

            // Self loops and straight/curved transitions first, so the state
            // circles paint on top and always look clean at every edge.
            foreach (var state in states)
            {
                PointF center = statePositions[state];

                foreach (char symbol in alphabet)
                {
                    string target = transitions[state][symbol];

                    if (target == state)
                    {
                        DrawSelfLoop(g, arrowPen, labelFont, center, symbol);
                    }
                    else
                    {
                        PointF targetCenter = statePositions[target];

                        // If there is also a transition going the opposite
                        // direction (target -> state), curve both edges so
                        // they do not overlap. Otherwise draw a straight line.
                        bool hasReverse = transitions.ContainsKey(target) &&
                                           transitions[target].Values.Contains(state);

                        if (hasReverse)
                            DrawCurvedArrow(g, arrowPen, labelFont, center, targetCenter, symbol, state.CompareTo(target) < 0);
                        else
                            DrawStraightArrow(g, arrowPen, labelFont, center, targetCenter, symbol);
                    }
                }
            }

            foreach (var state in states)
            {
                DrawState(g, statePen, stateFont, statePositions[state], state, finalStates.Contains(state));
            }

            DrawStartArrow(g, arrowPen, statePositions[startState]);

            using var infoFont = new Font("Segoe UI", 11, FontStyle.Bold);
            int infoY = drawingPanel.ClientSize.Height - 90;
            g.DrawString($"Start state: {startState}", infoFont, Brushes.DarkGreen, 20, infoY);
            g.DrawString($"Final state(s): {string.Join(", ", finalStates)}", infoFont, Brushes.DarkRed, 20, infoY + 25);
            g.DrawString($"Alphabet: {{ {string.Join(", ", alphabet)} }}", infoFont, Brushes.DarkBlue, 20, infoY + 50);
        }

        private void DrawState(Graphics g, Pen pen, Font font, PointF center, string name, bool isFinal)
        {
            var outer = new RectangleF(center.X - StateRadius, center.Y - StateRadius, StateRadius * 2, StateRadius * 2);
            g.FillEllipse(Brushes.White, outer);
            g.DrawEllipse(pen, outer);

            if (isFinal)
            {
                int inner = StateRadius - 7;
                var innerRect = new RectangleF(center.X - inner, center.Y - inner, inner * 2, inner * 2);
                g.DrawEllipse(pen, innerRect);
            }

            SizeF size = g.MeasureString(name, font);
            g.DrawString(name, font, Brushes.Black, center.X - size.Width / 2, center.Y - size.Height / 2);
        }

        private void DrawStartArrow(Graphics g, Pen pen, PointF state)
        {
            var start = new PointF(state.X - 110, state.Y);
            var end = new PointF(state.X - StateRadius - 3, state.Y);

            g.DrawLine(pen, start, end);
            DrawArrowHead(g, start, end, pen.Color);

            using var font = new Font("Segoe UI", 10, FontStyle.Bold);
            g.DrawString("start", font, Brushes.DarkGreen, start.X - 5, start.Y - 26);
        }

        private void DrawStraightArrow(Graphics g, Pen pen, Font labelFont, PointF from, PointF to, char label)
        {
            double angle = Math.Atan2(to.Y - from.Y, to.X - from.X);

            PointF start = OffsetPoint(from, angle, StateRadius);
            PointF end = OffsetPoint(to, angle, -StateRadius - 4);

            g.DrawLine(pen, start, end);
            DrawArrowHead(g, start, end, pen.Color);

            float midX = (start.X + end.X) / 2f;
            float midY = (start.Y + end.Y) / 2f;

            // Keep the label clearly above the transition.
            g.DrawString(label.ToString(), labelFont, Brushes.DarkBlue,
                midX - 5, midY - 25);
        }

        // Draws the two opposite transitions as smooth, parallel curves.
        // q0 -> q1 is drawn below the states.
        // q1 -> q0 is drawn above the states.
        private void DrawCurvedArrow(
            Graphics g,
            Pen pen,
            Font labelFont,
            PointF from,
            PointF to,
            char label,
            bool bulgeDown)
        {
            // IMPORTANT: These are deliberately simple, fixed-side curves.
            // The upper b arrow always goes q1 -> q0.
            // The lower b arrow always goes q0 -> q1.
            // This prevents the two transitions from collapsing into one curve.

            bool leftToRight = from.X < to.X;
            float side = bulgeDown ? 1f : -1f;
            const float curveHeight = 78f;
            const float controlPull = 95f;

            PointF start;
            PointF end;

            if (leftToRight)
            {
                start = new PointF(from.X + StateRadius, from.Y);
                end = new PointF(to.X - StateRadius - 3, to.Y);
            }
            else
            {
                start = new PointF(from.X - StateRadius, from.Y);
                end = new PointF(to.X + StateRadius + 3, to.Y);
            }

            float midX = (start.X + end.X) / 2f;
            float midY = (start.Y + end.Y) / 2f + side * curveHeight;

            // Control points stay horizontally close to the two states and
            // vertically on the same side, producing a clean symmetric arc.
            PointF c1 = new PointF(
                start.X + (leftToRight ? controlPull : -controlPull),
                midY);

            PointF c2 = new PointF(
                end.X + (leftToRight ? -controlPull : controlPull),
                midY);

            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddBezier(start, c1, c2, end);
                g.DrawPath(pen, path);
            }

            // Arrowhead follows the final tangent of the curve.
            DrawArrowHead(g, c2, end, pen.Color);

            SizeF textSize = g.MeasureString(label.ToString(), labelFont);
            float labelY = bulgeDown
                ? midY + 10f
                : midY - textSize.Height - 10f;

            g.DrawString(label.ToString(), labelFont, Brushes.DarkBlue,
                midX - textSize.Width / 2f, labelY);
        }

        private void DrawSelfLoop(Graphics g, Pen pen, Font labelFont, PointF center, char label)
        {
            // Clean inverted-U self loop above the state.
            // The arrowhead is kept at the upper-right side of the loop,
            // so it clearly points back into the state instead of sitting
            // awkwardly on the circle edge.
            PointF start = new PointF(
                center.X - 25,
                center.Y - StateRadius + 2);

            PointF control1 = new PointF(
                center.X - 55,
                center.Y - StateRadius - 78);

            PointF control2 = new PointF(
                center.X + 55,
                center.Y - StateRadius - 78);

            PointF end = new PointF(
                center.X + 25,
                center.Y - StateRadius + 2);

            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddBezier(start, control1, control2, end);
                g.DrawPath(pen, path);
            }

            // Final Bezier tangent is from control2 to end.
            DrawArrowHead(g, control2, end, pen.Color);

            SizeF textSize = g.MeasureString(label.ToString(), labelFont);
            g.DrawString(label.ToString(), labelFont, Brushes.DarkBlue,
                center.X - textSize.Width / 2f,
                center.Y - StateRadius - 105);
        }

        private void DrawArrowHead(Graphics g, PointF from, PointF to, Color color)
        {
            double angle = Math.Atan2(to.Y - from.Y, to.X - from.X);

            const float size = 12f;
            const double spread = Math.PI / 6.0;

            PointF p1 = OffsetPoint(to, angle + Math.PI - spread, size);
            PointF p2 = OffsetPoint(to, angle + Math.PI + spread, size);

            using (SolidBrush brush = new SolidBrush(color))
            {
                g.FillPolygon(brush, new[] { to, p1, p2 });
            }
        }

        private static PointF OffsetPoint(PointF p, double angle, double distance)
        {
            return new PointF(
                (float)(p.X + distance * Math.Cos(angle)),
                (float)(p.Y + distance * Math.Sin(angle)));
        }
    }
}
