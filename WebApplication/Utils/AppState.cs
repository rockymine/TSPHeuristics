using Excubo.Blazor.Canvas;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Algorithms;
using TravellingSalesmanProblem.Graph;
using WebApplication.Components;

namespace WebApplication.Utils {
    public class AppState {
        public GraphProblem Instance { get; set; }

        /* How much larger or smaller than the fitted size the drawing is, as a percentage; 100 is
         * the fit. */
        private float _zoom = 100;
        public float Zoom {
            get => _zoom;
            set {
                if (value == _zoom)
                    return;
                /* Zoom 0 collapses the canvas to nothing, which cannot be recovered from without
                 * reloading. The upper bound keeps the buffer inside its memory ceiling. */
                if (value < 25 || value > 400)
                    return;

                _zoom = value;
            }
        }

        private float _nodeRadius = 8;
        public float NodeRadius {
            get => _nodeRadius;
            set {
                if (value == _nodeRadius)
                    return;
                if (value < 1 || value > 10)
                    return;

                _nodeRadius = value;
            }
        }

        private int _animationDelay = 1;
        public int AnimationDelay {
            get => _animationDelay;
            set {
                if (value == _animationDelay)
                    return;
                if (value < 1 || value > 5000)
                    return;

                _animationDelay = value;
            }
        }

        /* Limits follow measured browser response time, from the click on Execute until the page
         * answers again: at 300 nodes Nearest Neighbour takes 2.9 s, Simulated Annealing with
         * phase length 10 7.2 s, Hill Climbing with 2-opt and steepest descent 12.7 s, with swap
         * 36.6 s. The coordinate range follows so a graph that large has room to spread out; the
         * canvas caps its own pixel size independently. */
        private const int MaxNodeCount = 300;
        private const int MaxCoordinate = 300;

        private int _maxX = 30;
        public int MaxX {
            get => _maxX;
            set {
                if (value == _maxX)
                    return;
                if (value < 10 || value > MaxCoordinate)
                    return;

                _maxX = value;
            }
        }

        private int _maxY = 30;
        public int MaxY {
            get => _maxY;
            set {
                if (value == _maxY)
                    return;
                if (value < 10 || value > MaxCoordinate)
                    return;

                _maxY = value;
            }
        }

        private int _nodeCount = 30;
        public int NodeCount {
            get => _nodeCount;
            set {
                if (value == _nodeCount)
                    return;
                if (value < 5 || value > MaxNodeCount)
                    return;

                _nodeCount = value;
            }
        }

        private int NodeCountOfInstance => Instance?.Nodes.Count ?? 0;

        private int _startNode;
        /* Instance is null until a graph has been executed, and both accessors are reachable
         * before that. The getter clamps as well as the setter, since a start node chosen for a
         * large graph would otherwise index a smaller one out of range. */
        public int StartNode {
            get {
                var count = NodeCountOfInstance;
                return count == 0 ? 0 : Math.Clamp(_startNode, 0, count - 1);
            }
            set {
                if (value < 0)
                    return;

                var count = NodeCountOfInstance;
                if (count > 0 && value > count - 1)
                    return;

                _startNode = value;
            }
        }

        private int _phaseLength = 1;
        public int PhaseLength {
            get => _phaseLength;
            set {
                if (value == _phaseLength)
                    return;
                if (value < 1 || value > 80)
                    return;

                _phaseLength = value;
            }
        }

        private double _startTemp = 100;
        public double StartTemp {
            get => _startTemp;
            set {
                if (value == _startTemp)
                    return;
                if (value < 50 || value > 100_000) {
                    return;
                }

                _startTemp = value;
            }
        }

        private double _minTemp = Math.Pow(10, -8);
        public double MinTemp {
            get => _minTemp;
            set {
                if (value == _minTemp)
                    return;
                if (value < 0.0000001 || value >= _startTemp)
                    return;

                _minTemp = value;
            }
        }

        private double _alphaSA = 0.98;
        public double AlphaSA {
            get => _alphaSA;
            set {
                if (value == _alphaSA)
                    return;
                if (value < 0.5 || value >= 1)
                    return;

                _alphaSA = value;
            }
        }

        /* Every Ant System iteration keeps its own pheromone snapshot in the history, one Edge
         * per edge of the complete graph at roughly 95 bytes. Memory rather than runtime is the
         * binding constraint, so the iteration count is capped against the loaded instance: 500
         * up to about 130 nodes, 58 at 300. */
        private const int MaxIterations = 500;
        private const long IterationMemoryBudget = 250L * 1024 * 1024;
        private const int BytesPerEdge = 95;

        private int IterationLimit {
            get {
                var count = NodeCountOfInstance;
                if (count < 2)
                    return MaxIterations;

                var perIteration = (long)count * (count - 1) / 2 * BytesPerEdge;
                return (int)Math.Clamp(IterationMemoryBudget / perIteration, 1, MaxIterations);
            }
        }

        private int _iterations = 50;
        public int Iterations {
            get => Math.Clamp(_iterations, 1, IterationLimit);
            /* Snaps to the ceiling instead of refusing the input, so the panel shows where the
             * limit is rather than silently keeping the old number. */
            set {
                if (value < 1)
                    return;

                _iterations = Math.Min(value, IterationLimit);
            }
        }

        private double _alphaACS = 0.1;
        public double AlphaACS {
            get => _alphaACS;
            set {
                if (value == _alphaACS)
                    return;
                if (value <= 0 || value >= 1)
                    return;

                _alphaACS = value;
            }
        }

        private double _beta = 2;
        public double Beta {
            get => _beta;
            set {
                if (value == _beta)
                    return;
                if (value <= 0 || value >= 10)
                    return;

                _beta = value;
            }
        }

        /* The colony is not tied to the node count: under the proportional transition rule two
         * ants starting from the same node walk different tours, so more ants than nodes is
         * meaningful. */
        private const int MaxAntCount = 500;

        private int _antCount = 25;
        /* Same two problems as StartNode: the value is read before any graph exists, and a colony
         * sized for one instance stayed behind when another was loaded. */
        public int AntCount {
            get => Math.Clamp(_antCount, 1, MaxAntCount);
            set {
                if (value < 1)
                    return;

                _antCount = Math.Min(value, MaxAntCount);
            }
        }

        public LinkedList<GraphState> GraphHistory;
        public LinkedListNode<GraphState> CurrentHistoryNode;

        public Canvas PreviousCanvas;
        public Canvas CurrentCanvas;

        public TableGenerator TableGenerator;
        /* Only the manual mode shows a second table - the distance matrix the moves are read off. */
        public TableGenerator DistanceTable;
        public ChartJS[] ChartJS = new ChartJS[5];

        //private GraphProblem Instance;
        public string Error;

        public AlgorithmEnum AlgorithmEnum;
        public NeighbourType NeighbourEnum;
        public GraphSource GraphSource;
        public DescentType DescentType;
        public GlobalUpdatingRule GlobalUpdatingRule;
        public SelectionRule SelectionRule = SelectionRule.Proportional;
        public NearestNeighbour NearestNeighbor = new();
        public SimulatedAnnealing SimulatedAnnealing = new();
        public AntSystem AntSystem = new();
        public HillClimbing HillClimbing = new();
        public ManualSearch ManualSearch = new();

        public FileInput FileInput = new();
        public FileOutput FileOutput = new();
        public string DefaultGraph = @"n,1,11,5
n,2,6,4
n,3,4,10
n,4,4,2
n,5,2,4
n,6,7,7
n,7,8,8
n,8,9,2
n,9,5,7
n,10,7,1
n,11,1,6
n,12,11,11";

        public bool AutoAdvance;
        public bool AnnotateEdges;
        public bool ColorizeChanges = true;
        public string BackgroundColor = "#FFFFFF";
        public bool MultiStart;
        public bool CalculateTemperature;
        public bool ShowGrid = true;
        public CanvasSettings CanvasSettings = new();

        public Stopwatch Stopwatch = new();
        public TimeSpan TimeSpan = new();

        public bool BreakWhenPathsAreEqual = true;
    }
}