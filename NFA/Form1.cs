using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace NFAVisualizer
{
    public class Form1 : Form
    {
        private readonly string[] states = { "q0", "q1", "q2" };
        private readonly char[] alphabet = { 'a', 'b' };
        private readonly string startState = "q0";
        private readonly HashSet<string> finalStates = new HashSet<string> { "q2" };

        private readonly Dictionary<string, Dictionary<char, HashSet<string>>> transitions =
            new Dictionary<string, Dictionary<char, HashSet<string>>>
            {
                { "q0", new Dictionary<char, HashSet<string>>
                    {
                        { 'a', new HashSet<string> { "q0" } },
                        { 'b', new HashSet<string> { "q0", "q1" } }
                    }
                },
                { "q1", new Dictionary<char, HashSet<string>>
                    {
                        { 'a', new HashSet<string> { "q1", "q2" } },
                        { 'b', new HashSet<string> { "q1" } }
                    }
                },
                { "q2", new Dictionary<char, HashSet<string>>
                    {
                        { 'a', new HashSet<string> { "q2" } },
                        { 'b', new HashSet<string>() }
                    }
                }
            };

        private Panel drawingPanel;
        private DataGridView transitionTable;
        private TextBox testInput;
        private Label testResult;
        private Dictionary<string, PointF> statePositions = new Dictionary<string, PointF>();
        private const int StateRadius = 42;

        public Form1()
        {
            InitializeForm();
            CreateInterface();
        }

        private void InitializeForm()
        {
            Text = "NFA Visualizer - M = (Q, Σ, δ, q0, F)";
            Width = 1200;
            Height = 760;
            MinimumSize = new Size(1000, 650);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10);
        }

        private void CreateInterface()
        {
            Controls.Add(new Label {
                Text = "NFA VISUALIZER",
                Font = new Font("Segoe UI", 23, FontStyle.Bold),
                ForeColor = Color.FromArgb(40,40,40),
                AutoSize = true,
                Location = new Point(30,15)
            });

            Controls.Add(new Label {
                Text = "M = (Q, Σ, δ, q₀, F)     Q = {q₀, q₁, q₂}     Σ = {a, b}     q₀ = q₀     F = {q₂}",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(50,70,120),
                AutoSize = true,
                Location = new Point(32,58)
            });

            Controls.Add(new Label {
                Text = "Nondeterministic Finite Automaton: a state and input symbol may have multiple possible next states.",
                Font = new Font("Segoe UI", 10, FontStyle.Italic),
                ForeColor = Color.DimGray,
                AutoSize = true,
                Location = new Point(32,88)
            });

            Controls.Add(new Label {
                Text = "NFA State Diagram",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(30,120)
            });

            drawingPanel = new Panel {
                Location = new Point(30,155),
                Size = new Size(700,470),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            drawingPanel.Paint += DrawingPanel_Paint;
            drawingPanel.Resize += (s,e) => drawingPanel.Invalidate();
            Controls.Add(drawingPanel);

            Controls.Add(new Label {
                Text = "NFA Transition Table",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(765,120)
            });

            CreateTransitionTable();
            CreateStringTester();

            Controls.Add(new Label {
                Text = "→ Start state     * Final state     Double circle = accepting state",
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.DimGray,
                AutoSize = true,
                Location = new Point(32,640)
            });
        }

        private void CreateTransitionTable()
        {
            transitionTable = new DataGridView {
                Location = new Point(765,155),
                Size = new Size(390,160),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI",10)
            };

            transitionTable.Columns.Add("state","State");
            transitionTable.Columns.Add("a","a");
            transitionTable.Columns.Add("b","b");

            foreach (string state in states)
            {
                string label = state == startState ? "→ " + state : state;
                if (finalStates.Contains(state)) label = "* " + label;

                int row = transitionTable.Rows.Add();
                transitionTable.Rows[row].Cells[0].Value = label;
                transitionTable.Rows[row].Cells[1].Value = FormatStateSet(transitions[state]['a']);
                transitionTable.Rows[row].Cells[2].Value = FormatStateSet(transitions[state]['b']);
            }

            transitionTable.EnableHeadersVisualStyles = false;
            transitionTable.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle {
                BackColor = Color.FromArgb(55,75,120),
                ForeColor = Color.White,
                Font = new Font("Segoe UI",11,FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };

            transitionTable.DefaultCellStyle = new DataGridViewCellStyle {
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI",10),
                SelectionBackColor = Color.LightBlue,
                SelectionForeColor = Color.Black
            };

            Controls.Add(transitionTable);

            Controls.Add(new Label {
                Text = "δ(q₀,a) = {q₀}\nδ(q₀,b) = {q₀,q₁}\nδ(q₁,a) = {q₁,q₂}\nδ(q₁,b) = {q₁}\nδ(q₂,a) = {q₂}\nδ(q₂,b) = ∅",
                Font = new Font("Consolas",10,FontStyle.Bold),
                ForeColor = Color.FromArgb(50,50,50),
                AutoSize = true,
                Location = new Point(775,340)
            });
        }

        private string FormatStateSet(HashSet<string> set)
        {
            if (set.Count == 0) return "∅";
            return "{" + string.Join(",", set.OrderBy(x => x)) + "}";
        }

        private void CreateStringTester()
        {
            Controls.Add(new Label {
                Text = "Test an input string",
                Font = new Font("Segoe UI",13,FontStyle.Bold),
                AutoSize = true,
                Location = new Point(765,475)
            });

            testInput = new TextBox {
                Location = new Point(765,510),
                Size = new Size(270,30),
                Font = new Font("Segoe UI",11)
            };
            Controls.Add(testInput);

            Button run = new Button {
                Text = "Run",
                Location = new Point(1045,508),
                Size = new Size(105,32)
            };
            run.Click += (s,e) => RunNFA();
            Controls.Add(run);

            testResult = new Label {
                Text = "Enter a string using a and b.",
                Font = new Font("Consolas",10),
                AutoSize = false,
                Size = new Size(390,120),
                Location = new Point(765,550)
            };
            Controls.Add(testResult);
        }

        private void RunNFA()
        {
            string input = testInput.Text.Trim();

            if (input.Length == 0)
            {
                testResult.ForeColor = Color.DarkOrange;
                testResult.Text = "Enter a string using a and b.";
                return;
            }

            HashSet<string> currentStates = new HashSet<string> { startState };
            List<string> steps = new List<string> {
                "Start: " + FormatStateSet(currentStates)
            };

            foreach (char symbol in input)
            {
                if (!alphabet.Contains(symbol))
                {
                    testResult.ForeColor = Color.DarkRed;
                    testResult.Text = "Invalid symbol: " + symbol + "\nAlphabet = {a,b}";
                    return;
                }

                HashSet<string> nextStates = new HashSet<string>();

                foreach (string state in currentStates)
                {
                    if (transitions.ContainsKey(state) && transitions[state].ContainsKey(symbol))
                        nextStates.UnionWith(transitions[state][symbol]);
                }

                currentStates = nextStates;
                steps.Add("Read '" + symbol + "' → " + FormatStateSet(currentStates));
            }

            bool accepted = currentStates.Any(state => finalStates.Contains(state));

            testResult.ForeColor = accepted ? Color.DarkGreen : Color.DarkRed;
            testResult.Text =
                (accepted ? "ACCEPTED" : "REJECTED") +
                "\n\n" + string.Join("\n", steps) +
                "\n\nFinal states reached: " + FormatStateSet(currentStates);
        }

        private void DrawingPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            ComputeStatePositions(drawingPanel.ClientSize);
            DrawNFA(g);
        }

        private void ComputeStatePositions(Size panelSize)
        {
            statePositions.Clear();
            statePositions["q0"] = new PointF(150, panelSize.Height / 2);
            statePositions["q1"] = new PointF(350, panelSize.Height / 2);
            statePositions["q2"] = new PointF(550, panelSize.Height / 2);
        }

        private void DrawNFA(Graphics g)
        {
            using Pen statePen = new Pen(Color.FromArgb(30,30,30),3);
            using Pen arrowPen = new Pen(Color.FromArgb(40,40,40),3);
            using Font labelFont = new Font("Segoe UI",13,FontStyle.Bold);
            using Font stateFont = new Font("Segoe UI",13,FontStyle.Bold);

            PointF q0 = statePositions["q0"];
            PointF q1 = statePositions["q1"];
            PointF q2 = statePositions["q2"];

            DrawSelfLoop(g,arrowPen,labelFont,q0,"a");
            DrawStraightArrow(g,arrowPen,labelFont,q0,q1,"b");
            DrawSelfLoop(g,arrowPen,labelFont,q1,"a,b");
            DrawStraightArrow(g,arrowPen,labelFont,q1,q2,"a");
            DrawSelfLoop(g,arrowPen,labelFont,q2,"a");

            DrawState(g,statePen,stateFont,q0,"q0",false);
            DrawState(g,statePen,stateFont,q1,"q1",false);
            DrawState(g,statePen,stateFont,q2,"q2",true);

            DrawStartArrow(g,arrowPen,q0);

            using Font infoFont = new Font("Segoe UI",11,FontStyle.Bold);
            int y = drawingPanel.ClientSize.Height - 100;
            g.DrawString("Start state: q0",infoFont,Brushes.DarkGreen,20,y);
            g.DrawString("Final state: q2",infoFont,Brushes.DarkRed,20,y+25);
            g.DrawString("Alphabet: { a, b }",infoFont,Brushes.DarkBlue,20,y+50);
        }

        private void DrawState(Graphics g,Pen pen,Font font,PointF center,string name,bool isFinal)
        {
            RectangleF outer = new RectangleF(
                center.X-StateRadius, center.Y-StateRadius,
                StateRadius*2, StateRadius*2);

            g.FillEllipse(Brushes.White,outer);
            g.DrawEllipse(pen,outer);

            if (isFinal)
            {
                int inner = StateRadius-7;
                RectangleF innerRect = new RectangleF(
                    center.X-inner,center.Y-inner,inner*2,inner*2);
                g.DrawEllipse(pen,innerRect);
            }

            SizeF size = g.MeasureString(name,font);
            g.DrawString(name,font,Brushes.Black,
                center.X-size.Width/2,center.Y-size.Height/2);
        }

        private void DrawStartArrow(Graphics g,Pen pen,PointF state)
        {
            PointF start = new PointF(state.X-100,state.Y);
            PointF end = new PointF(state.X-StateRadius-3,state.Y);
            g.DrawLine(pen,start,end);
            DrawArrowHead(g,start,end,pen.Color);

            using Font font = new Font("Segoe UI",10,FontStyle.Bold);
            g.DrawString("start",font,Brushes.DarkGreen,start.X-5,start.Y-26);
        }

        private void DrawStraightArrow(Graphics g,Pen pen,Font font,
            PointF from,PointF to,string label)
        {
            double angle = Math.Atan2(to.Y-from.Y,to.X-from.X);
            PointF start = OffsetPoint(from,angle,StateRadius);
            PointF end = OffsetPoint(to,angle,-StateRadius-3);

            g.DrawLine(pen,start,end);
            DrawArrowHead(g,start,end,pen.Color);

            float midX = (start.X+end.X)/2;
            float midY = (start.Y+end.Y)/2;
            g.DrawString(label,font,Brushes.DarkBlue,midX-10,midY-28);
        }

        private void DrawSelfLoop(Graphics g, Pen pen, Font font,
            PointF center, string label)
        {
            // Self-loop is drawn as a clean loop above the state.
            // IMPORTANT: the arrowhead is placed at the ACTUAL END of
            // the arc, so it follows the arc instead of floating beside it.

            const float loopWidth = 56f;
            const float loopHeight = 60f;

            RectangleF loop = new RectangleF(
                center.X - loopWidth / 2f,
                center.Y - StateRadius - 60f,
                loopWidth,
                loopHeight);

            const float startAngle = 140f;
            const float sweepAngle = 260f;

            g.DrawArc(pen, loop, startAngle, sweepAngle);

            // Exact endpoint of the GDI+ arc.
            double endAngle = (startAngle + sweepAngle) * Math.PI / 180.0;

            float rx = loopWidth / 2f;
            float ry = loopHeight / 2f;
            float cx = loop.X + rx;
            float cy = loop.Y + ry;

            PointF arrowEnd = new PointF(
                cx + rx * (float)Math.Cos(endAngle),
                cy + ry * (float)Math.Sin(endAngle));

            // Tangent to the ellipse at the endpoint.
            // GDI+ angles increase clockwise on screen.
            PointF tangent = new PointF(
                -(float)Math.Sin(endAngle),
                (float)Math.Cos(endAngle));

            // A short tangent segment gives DrawArrowHead the exact
            // direction of the loop at its endpoint.
            PointF arrowStart = new PointF(
                arrowEnd.X - tangent.X * 16f,
                arrowEnd.Y - tangent.Y * 16f);

            DrawArrowHead(g, arrowStart, arrowEnd, pen.Color);

            SizeF textSize = g.MeasureString(label, font);
            g.DrawString(
                label,
                font,
                Brushes.DarkBlue,
                center.X - textSize.Width / 2f,
                loop.Y - textSize.Height - 8f);
        }

        private void DrawArrowHead(Graphics g,PointF from,PointF to,Color color)
        {
            double angle = Math.Atan2(to.Y-from.Y,to.X-from.X);
            const int size = 11;

            PointF p1 = OffsetPoint(
                to,angle-Math.PI+Math.PI/6,size);

            PointF p2 = OffsetPoint(
                to,angle-Math.PI-Math.PI/6,size);

            using SolidBrush brush = new SolidBrush(color);
            g.FillPolygon(brush,new[] { to,p1,p2 });
        }

        private static PointF OffsetPoint(PointF point,double angle,double distance)
        {
            return new PointF(
                (float)(point.X+distance*Math.Cos(angle)),
                (float)(point.Y+distance*Math.Sin(angle)));
        }
    }
}
