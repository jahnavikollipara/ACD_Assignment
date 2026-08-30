using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace NFAToDFAVisualizer
{
    public class Form1 : Form
    {
        // UI controls
        private Panel nfaPanel;
        private Panel dfaPanel;
        private Panel dfaCanvas;
        private DataGridView nfaTable;
        private DataGridView dfaTable;
        private TextBox inputBox;
        private Label resultLabel;
        private Label conversionLabel;

        // DFA conversion data
        private List<HashSet<string>> dfaStates;
        private Dictionary<string, Dictionary<char, HashSet<string>>> dfaTransitions;
        private HashSet<string> dfaFinalStates;

        // Diagram positions
        private Dictionary<string, PointF> nfaPositions;
        private Dictionary<string, PointF> dfaPositions;
        private const int Radius = 38;
        // =============================================================
        // NFA matching the friend's reference diagram
        // q0 --0--> q1, q1 --0--> q2
        // q0 has loops on 0 and 1
        // q2 has loops on 0 and 1
        // q2 is the accepting state
        // =============================================================
        private readonly string[] nfaStates = { "q0", "q1", "q2" };
        private readonly char[] alphabet = { '0', '1' };
        private readonly string nfaStart = "q0";
        private readonly HashSet<string> nfaFinal = new HashSet<string> { "q2" };

        private readonly Dictionary<string, Dictionary<char, HashSet<string>>> nfa =
            new Dictionary<string, Dictionary<char, HashSet<string>>>
            {
                { "q0", new Dictionary<char, HashSet<string>>
                    {
                        { '0', new HashSet<string> { "q0", "q1" } },
                        { '1', new HashSet<string> { "q0" } }
                    }
                },
                { "q1", new Dictionary<char, HashSet<string>>
                    {
                        { '0', new HashSet<string> { "q2" } },
                        { '1', new HashSet<string>() }
                    }
                },
                { "q2", new Dictionary<char, HashSet<string>>
                    {
                        { '0', new HashSet<string> { "q2" } },
                        { '1', new HashSet<string> { "q2" } }
                    }
                }
            };

        public Form1()
        {
            BuildDFA();
            InitializeForm();
            CreateInterface();
        }

        // =============================================================
        // FORM
        // =============================================================
        private void InitializeForm()
        {
            Text = "NFA to DFA Conversion Visualizer";
            Width = 1400;
            Height = 900;
            MinimumSize = new Size(1200, 760);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10);
        }

        private void CreateInterface()
        {
            Controls.Add(new Label
            {
                Text = "NFA → DFA CONVERSION VISUALIZER",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(28, 18),
                ForeColor = Color.FromArgb(35, 35, 35)
            });

            Controls.Add(new Label
            {
                Text = "Subset Construction / Equivalence of NFA and DFA",
                Font = new Font("Segoe UI", 11, FontStyle.Italic),
                AutoSize = true,
                Location = new Point(32, 62),
                ForeColor = Color.DimGray
            });

            Controls.Add(new Label
            {
                Text = "NFA: M = (Q, Σ, δ, q₀, F)     Q = {q₀,q₁,q₂}     Σ = {0,1}     q₀ = q₀     F = {q₂}",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(32, 91),
                ForeColor = Color.FromArgb(45, 70, 120)
            });

            Controls.Add(new Label
            {
                Text = "1. NFA transition table    →    2. Subset construction    →    3. Equivalent DFA transition table and diagram",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(32, 124),
                ForeColor = Color.FromArgb(70, 70, 70)
            });

            CreateNfaSection();
            CreateDfaSection();
            CreateSimulator();
        }

        private void CreateNfaSection()
        {
            Controls.Add(new Label
            {
                Text = "NFA Transition Table",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(30, 160)
            });

            nfaTable = CreateTable(30, 195, 400, 125);
            nfaTable.Columns.Add("state", "State");
            nfaTable.Columns.Add("zero", "0");
            nfaTable.Columns.Add("one", "1");

            foreach (string state in nfaStates)
            {
                int row = nfaTable.Rows.Add();
                nfaTable.Rows[row].Cells[0].Value = state == nfaStart ? "→ " + state : state;
                nfaTable.Rows[row].Cells[1].Value = FormatSet(nfa[state]['0']);
                nfaTable.Rows[row].Cells[2].Value = FormatSet(nfa[state]['1']);
            }
            StyleTable(nfaTable);
            Controls.Add(nfaTable);

            Controls.Add(new Label
            {
                Text = "NFA State Diagram",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(30, 390)
            });

            nfaPanel = new Panel
            {
                Location = new Point(30, 425),
                Size = new Size(600, 350),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            nfaPanel.Paint += (s, e) => DrawNFA(e.Graphics);
            Controls.Add(nfaPanel);

            Controls.Add(new Label
            {
                Text = "δ(q₀,0)={q₀,q₁}   δ(q₀,1)=∅   δ(q₁,0)=∅   δ(q₁,1)={q₁,q₂}   δ(q₂,0)={q₀}   δ(q₂,1)=∅",
                Font = new Font("Consolas", 9, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(30, 785),
                ForeColor = Color.FromArgb(55, 55, 55)
            });
        }

        private void CreateDfaSection()
        {
            Controls.Add(new Label
            {
                Text = "Equivalent DFA Transition Table",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(700, 160)
            });

            // Table is only as tall as its actual rows.
            dfaTable = CreateTable(700, 195, 455, 150);
            dfaTable.Columns.Add("state", "DFA State");
            dfaTable.Columns.Add("zero", "0");
            dfaTable.Columns.Add("one", "1");
            FillDfaTable();
            StyleTable(dfaTable);
            Controls.Add(dfaTable);

            Controls.Add(new Label
            {
                Text = "DFA State Diagram (Subset Construction)",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(700, 365)
            });

            // The viewport is fixed, but the drawing canvas is larger.
            // This gives the DFA diagram its own scrollbar so the complete
            // diagram can be viewed without shrinking the states.
            dfaPanel = new Panel
            {
                Location = new Point(700, 400),
                Size = new Size(650, 360),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true
            };

            dfaCanvas = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(650, 500),
                BackColor = Color.White
            };
            dfaCanvas.Paint += (s, e) => DrawDFA(e.Graphics);
            dfaCanvas.Resize += (s, e) => dfaCanvas.Invalidate();

            dfaPanel.Controls.Add(dfaCanvas);
            Controls.Add(dfaPanel);

            conversionLabel = new Label
            {
                Text = BuildConversionExplanation(),
                Font = new Font("Consolas", 8, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(700, 770),
                ForeColor = Color.FromArgb(55, 55, 55)
            };
            Controls.Add(conversionLabel);
        }

        private void CreateSimulator()
        {
            Controls.Add(new Label
            {
                Text = "Test the converted DFA",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(1180, 160)
            });

            inputBox = new TextBox
            {
                Location = new Point(1180, 195),
                Size = new Size(155, 30),
                Font = new Font("Segoe UI", 11)
            };
            Controls.Add(inputBox);

            Button run = new Button
            {
                Text = "Run",
                Location = new Point(1180, 235),
                Size = new Size(155, 34)
            };
            run.Click += (s, e) => RunDFA();
            Controls.Add(run);

            resultLabel = new Label
            {
                Text = "Enter a string containing 0 and 1.",
                Font = new Font("Consolas", 9),
                Location = new Point(1180, 280),
                Size = new Size(170, 160)
            };
            Controls.Add(resultLabel);
        }

        private DataGridView CreateTable(int x, int y, int w, int h)
        {
            return new DataGridView
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowTemplate = { Height = 28 },
                Font = new Font("Segoe UI", 10)
            };
        }

        private void StyleTable(DataGridView table)
        {
            table.EnableHeadersVisualStyles = false;
            table.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(55, 75, 120),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            table.DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10),
                SelectionBackColor = Color.LightBlue,
                SelectionForeColor = Color.Black
            };
        }

        // =============================================================
        // SUBSET CONSTRUCTION: NFA -> DFA
        // =============================================================
        private void BuildDFA()
        {
            dfaStates = new List<HashSet<string>>();
            dfaTransitions = new Dictionary<string, Dictionary<char, HashSet<string>>>();
            dfaFinalStates = new HashSet<string>();

            Queue<HashSet<string>> queue = new Queue<HashSet<string>>();
            HashSet<string> start = new HashSet<string> { nfaStart };
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                HashSet<string> current = queue.Dequeue();
                string currentKey = SetKey(current);

                if (dfaStates.Any(s => s.SetEquals(current)))
                    continue;

                dfaStates.Add(new HashSet<string>(current));
                dfaTransitions[currentKey] = new Dictionary<char, HashSet<string>>();

                if (current.Any(s => nfaFinal.Contains(s)))
                    dfaFinalStates.Add(currentKey);

                foreach (char symbol in alphabet)
                {
                    HashSet<string> next = new HashSet<string>();

                    foreach (string state in current)
                        next.UnionWith(nfa[state][symbol]);

                    dfaTransitions[currentKey][symbol] = next;

                    if (!dfaStates.Any(s => s.SetEquals(next)) &&
                        !queue.Any(s => s.SetEquals(next)))
                    {
                        queue.Enqueue(new HashSet<string>(next));
                    }
                }
            }
        }

        private void FillDfaTable()
        {
            dfaTable.Rows.Clear();

            foreach (HashSet<string> state in dfaStates)
            {
                string key = SetKey(state);
                int row = dfaTable.Rows.Add();

                string label = FormatSet(state);
                if (state.SetEquals(new HashSet<string> { nfaStart }))
                    label = "→ " + label;
                if (dfaFinalStates.Contains(key))
                    label = "* " + label;

                dfaTable.Rows[row].Cells[0].Value = label;
                dfaTable.Rows[row].Cells[1].Value = FormatSet(dfaTransitions[key]['0']);
                dfaTable.Rows[row].Cells[2].Value = FormatSet(dfaTransitions[key]['1']);
            }
        }

        private string BuildConversionExplanation()
        {
            return "q0 --0--> q0q1    q0 --1--> q0\n" +
                   "q0q1 --0--> q0q1q2    q0q1 --1--> q0\n" +
                   "q0q1q2 --0--> q0q1q2    q0q1q2 --1--> q0q2\n" +
                   "q0q2 --0--> q0q1q2    q0q2 --1--> q0q2";
        }

        private string SetKey(HashSet<string> set)
        {
            if (set.Count == 0) return "∅";
            return string.Join(",", set.OrderBy(x => x));
        }

        private string FormatSet(HashSet<string> set)
        {
            if (set.Count == 0) return "∅";
            return "{" + string.Join(",", set.OrderBy(x => x)) + "}";
        }

        // =============================================================
        // NFA DRAWING
        // =============================================================
        private void DrawNFA(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            nfaPositions = new Dictionary<string, PointF>
            {
                ["q0"] = new PointF(135, 205),
                ["q1"] = new PointF(335, 205),
                ["q2"] = new PointF(535, 205)
            };

            using Pen pen = new Pen(Color.FromArgb(35, 35, 35), 2.5f);
            using Font stateFont = new Font("Segoe UI", 12, FontStyle.Bold);
            using Font labelFont = new Font("Segoe UI", 12, FontStyle.Bold);

            DrawStartArrow(g, pen, nfaPositions["q0"]);

            // Main chain exactly like the friend's NFA:
            // q0 --0--> q1 --0--> q2
            DrawStraightArrow(g, pen, labelFont,
                nfaPositions["q0"], nfaPositions["q1"], "0");

            DrawStraightArrow(g, pen, labelFont,
                nfaPositions["q1"], nfaPositions["q2"], "0");

            // q0: both 0 and 1 are self transitions.
            DrawSelfLoop(g, pen, labelFont, nfaPositions["q0"], "0,1");

            // q2: both 0 and 1 are self transitions.
            DrawSelfLoop(g, pen, labelFont, nfaPositions["q2"], "0,1");

            DrawState(g, stateFont, nfaPositions["q0"], "q0", false);
            DrawState(g, stateFont, nfaPositions["q1"], "q1", false);
            DrawState(g, stateFont, nfaPositions["q2"], "q2", true);
        }

        private void DrawNfaReturnArrow(Graphics g, Pen pen, Font font,
            PointF from, PointF to, string label)
        {
            // q2 -> q0 is the large lower return transition.
            PointF start = new PointF(from.X - Radius - 2, from.Y + 5);
            PointF end = new PointF(to.X + Radius + 2, to.Y + 5);

            PointF c1 = new PointF(from.X - 5, from.Y + 150);
            PointF c2 = new PointF(to.X + 5, to.Y + 150);

            using GraphicsPath path = new GraphicsPath();
            path.AddBezier(start, c1, c2, end);
            g.DrawPath(pen, path);

            DrawArrowHead(g, c2, end, pen.Color);

            SizeF size = g.MeasureString(label, font);
            g.DrawString(label, font, Brushes.DarkBlue,
                (from.X + to.X) / 2f - size.Width / 2f,
                from.Y + 118);
        }

        // =============================================================
        // DFA DRAWING
        // =============================================================
        private void DrawDFA(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (dfaCanvas == null) return;

            HashSet<string> A = new HashSet<string> { "q0" };
            HashSet<string> B = new HashSet<string> { "q0", "q1" };
            HashSet<string> C = new HashSet<string> { "q0", "q1", "q2" };
            HashSet<string> D = new HashSet<string> { "q0", "q2" };

            // Same four-state arrangement as the friend's DFA:
            // q0 -> q0q1 -> q0q1q2 -> q0q2
            // with the return/loop transitions shown separately.
            dfaPositions = new Dictionary<string, PointF>
            {
                [SetKey(A)] = new PointF(80, 260),
                [SetKey(B)] = new PointF(255, 260),
                [SetKey(C)] = new PointF(435, 260),
                [SetKey(D)] = new PointF(585, 260)
            };

            using Pen pen = new Pen(Color.FromArgb(35, 35, 35), 2.5f);
            using Font stateFont = new Font("Segoe UI", 9, FontStyle.Bold);
            using Font labelFont = new Font("Segoe UI", 11, FontStyle.Bold);

            DrawStartArrow(g, pen, dfaPositions[SetKey(A)]);

            // {q0} --0--> {q0,q1}
            DrawStraightArrow(g, pen, labelFont,
                dfaPositions[SetKey(A)], dfaPositions[SetKey(B)], "0");

            // {q0,q1} --0--> {q0,q1,q2}
            DrawStraightArrow(g, pen, labelFont,
                dfaPositions[SetKey(B)], dfaPositions[SetKey(C)], "0");

            // {q0,q1,q2} --1--> {q0,q2}
            DrawStraightArrow(g, pen, labelFont,
                dfaPositions[SetKey(C)], dfaPositions[SetKey(D)], "1");

            // {q0,q1} --1--> {q0} (upper return curve)
            DrawDfaBezierArrow(g, pen, labelFont,
                dfaPositions[SetKey(B)], dfaPositions[SetKey(A)],
                new PointF(215, 195), new PointF(125, 195),
                "1", new PointF(165, 178));

            // {q0,q2} --0--> {q0,q1,q2} (lower return curve)
            DrawDfaBezierArrow(g, pen, labelFont,
                dfaPositions[SetKey(D)], dfaPositions[SetKey(C)],
                new PointF(550, 325), new PointF(470, 325),
                "0", new PointF(510, 342));

            // Self-loops from friend's diagram.
            DrawSelfLoop(g, pen, labelFont, dfaPositions[SetKey(A)], "1");
            DrawSelfLoop(g, pen, labelFont, dfaPositions[SetKey(C)], "0");
            DrawSelfLoop(g, pen, labelFont, dfaPositions[SetKey(D)], "1");

            DrawState(g, stateFont, dfaPositions[SetKey(A)], "q0", false);
            DrawState(g, stateFont, dfaPositions[SetKey(B)], "q0q1", false);
            DrawState(g, stateFont, dfaPositions[SetKey(C)], "q0q1q2", true);
            DrawState(g, stateFont, dfaPositions[SetKey(D)], "q0q2", true);
        }

        private void DrawDfaBezierArrow(
            Graphics g, Pen pen, Font font,
            PointF from, PointF to,
            PointF control1, PointF control2,
            string label, PointF labelPoint)
        {
            double startAngle = Math.Atan2(
                control1.Y - from.Y,
                control1.X - from.X);

            double endAngle = Math.Atan2(
                to.Y - control2.Y,
                to.X - control2.X);

            PointF start = Offset(from, startAngle, Radius);
            PointF end = Offset(to, endAngle, -Radius - 4);

            using GraphicsPath path = new GraphicsPath();
            path.AddBezier(start, control1, control2, end);
            g.DrawPath(pen, path);

            DrawArrowHead(g, control2, end, pen.Color);
            g.DrawString(label, font, Brushes.DarkBlue,
                labelPoint.X, labelPoint.Y);
        }

        // =============================================================
        // GRAPHICS HELPERS
        // =============================================================
        private void DrawState(Graphics g, Font font, PointF center, string name, bool finalState)
        {
            RectangleF outer = new RectangleF(center.X - Radius, center.Y - Radius, Radius * 2, Radius * 2);
            using Pen pen = new Pen(Color.FromArgb(35,35,35), 2.5f);
            g.FillEllipse(Brushes.White, outer);
            g.DrawEllipse(pen, outer);

            if (finalState)
            {
                RectangleF inner = new RectangleF(center.X - Radius + 7, center.Y - Radius + 7,
                    (Radius - 7) * 2, (Radius - 7) * 2);
                g.DrawEllipse(pen, inner);
            }

            SizeF size = g.MeasureString(name, font);
            g.DrawString(name, font, Brushes.Black, center.X - size.Width / 2, center.Y - size.Height / 2);
        }

        private void DrawStartArrow(Graphics g, Pen pen, PointF state)
        {
            PointF start = new PointF(state.X - 95, state.Y);
            PointF end = new PointF(state.X - Radius - 3, state.Y);
            g.DrawLine(pen, start, end);
            DrawArrowHead(g, start, end, pen.Color);
            using Font f = new Font("Segoe UI", 9, FontStyle.Bold);
            g.DrawString("start", f, Brushes.DarkGreen, start.X - 5, start.Y - 24);
        }

        private void DrawArrow(Graphics g, Pen pen, Font font, PointF from, PointF to, string label)
        {
            double angle = Math.Atan2(to.Y - from.Y, to.X - from.X);
            PointF start = Offset(from, angle, Radius);
            PointF end = Offset(to, angle, -Radius - 3);
            g.DrawLine(pen, start, end);
            DrawArrowHead(g, start, end, pen.Color);
            float mx = (start.X + end.X) / 2;
            float my = (start.Y + end.Y) / 2;
            g.DrawString(label, font, Brushes.DarkBlue, mx - 6, my - 25);
        }

        private void DrawStraightArrow(Graphics g, Pen pen, Font font, PointF from, PointF to, string label)
        {
            DrawArrow(g, pen, font, from, to, label);
        }

        private void DrawCurvedArrow(Graphics g, Pen pen, Font font, PointF from, PointF to, string label)
        {
            PointF start = new PointF(from.X - 28, from.Y + 22);
            PointF end = new PointF(to.X + 28, to.Y + 22);
            RectangleF rect = new RectangleF(to.X - 20, from.Y + 15, from.X - to.X + 40, 100);
            g.DrawArc(pen, rect, 0, 180);
            PointF arrowStart = new PointF(to.X + 8, from.Y + 24);
            PointF arrowEnd = new PointF(to.X + 25, from.Y + 34);
            DrawArrowHead(g, arrowStart, arrowEnd, pen.Color);
            g.DrawString(label, font, Brushes.DarkBlue, (from.X + to.X) / 2 - 5, from.Y + 78);
        }

        private void DrawSelfLoop(Graphics g, Pen pen, Font font,
            PointF center, string label)
        {
            const float loopWidth = 52f;
            const float loopHeight = 58f;
            const float startAngle = 200f;
            const float sweepAngle = 220f;

            RectangleF loop = new RectangleF(
                center.X - loopWidth / 2f,
                center.Y - Radius - 62f,
                loopWidth,
                loopHeight);

            g.DrawArc(pen, loop, startAngle, sweepAngle);

            // Calculate the exact endpoint of the same ellipse arc.
            double endAngle = (startAngle + sweepAngle) * Math.PI / 180.0;
            float rx = loop.Width / 2f;
            float ry = loop.Height / 2f;
            float cx = loop.X + rx;
            float cy = loop.Y + ry;

            PointF arrowEnd = new PointF(
                cx + rx * (float)Math.Cos(endAngle),
                cy + ry * (float)Math.Sin(endAngle));

            // Tangent at the endpoint makes the arrowhead follow the loop.
            PointF tangent = new PointF(
                -(float)Math.Sin(endAngle),
                (float)Math.Cos(endAngle));

            PointF arrowStart = new PointF(
                arrowEnd.X - tangent.X * 15f,
                arrowEnd.Y - tangent.Y * 15f);

            DrawArrowHead(g, arrowStart, arrowEnd, pen.Color);

            SizeF size = g.MeasureString(label, font);
            g.DrawString(label, font, Brushes.DarkBlue,
                center.X - size.Width / 2f,
                loop.Y - size.Height - 8f);
        }

        private void DrawArrowHead(Graphics g, PointF from, PointF to, Color color)
        {
            double angle = Math.Atan2(to.Y - from.Y, to.X - from.X);
            const float size = 10;
            PointF p1 = Offset(to, angle + Math.PI - Math.PI / 6, size);
            PointF p2 = Offset(to, angle + Math.PI + Math.PI / 6, size);
            using SolidBrush b = new SolidBrush(color);
            g.FillPolygon(b, new[] { to, p1, p2 });
        }

        private static PointF Offset(PointF p, double angle, double distance)
        {
            return new PointF(
                (float)(p.X + distance * Math.Cos(angle)),
                (float)(p.Y + distance * Math.Sin(angle)));
        }

        // =============================================================
        // DFA SIMULATION
        // =============================================================
        private void RunDFA()
        {
            string input = inputBox.Text.Trim();
            if (input.Length == 0)
            {
                resultLabel.ForeColor = Color.DarkOrange;
                resultLabel.Text = "Enter a string using 0 and 1.";
                return;
            }

            HashSet<string> current = new HashSet<string> { nfaStart };
            List<string> path = new List<string> { FormatSet(current) };

            foreach (char symbol in input)
            {
                if (!alphabet.Contains(symbol))
                {
                    resultLabel.ForeColor = Color.DarkRed;
                    resultLabel.Text = "Invalid symbol: " + symbol + "\nAlphabet = {0,1}";
                    return;
                }

                string key = SetKey(current);
                current = new HashSet<string>(dfaTransitions[key][symbol]);
                path.Add(FormatSet(current));
            }

            bool accepted = current.Any(s => nfaFinal.Contains(s));
            resultLabel.ForeColor = accepted ? Color.DarkGreen : Color.DarkRed;
            resultLabel.Text = (accepted ? "ACCEPTED" : "REJECTED") +
                               "\n\nPath:\n" + string.Join(" → ", path) +
                               "\n\nFinal DFA state:\n" + FormatSet(current);
        }
    }
}
